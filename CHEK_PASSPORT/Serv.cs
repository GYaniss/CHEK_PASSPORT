using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Text.Json;

namespace CHEK_PASSPORT
{
    public class MainTask
    {
        const   int cacheBufferTime = 120; //Длительность хранения запроса в минутах в ОЗУ
        public  bool  PoolHTTPSended1 = false;
        public  bool  PoolHTTPSended2 = false;
        public  ConcurrentQueue<string> Pool = new ConcurrentQueue<string>(); //Очередь невыполненных запросов
        private readonly IMemoryCache MCache;
        private readonly VedomstvoHttpClient VedomstvoHttp;
        public MainTask(IMemoryCache mCache, VedomstvoHttpClient VedomstvoClient)
        {
            MCache = mCache;
            VedomstvoHttp = VedomstvoClient;
        }
        public PassportData Registred(int ser, int nom)
        {
           
            string cacheKey = string.Format("{0}{1}", ser, nom);
            PassportData PD;
            if (MCache.TryGetValue(cacheKey, out PD))
            {
                // if { PD.Status= PassStatus.ОК } { MCache.Remove(cacheKey); } //Овобождать кэш после проверки и после чтения результата банком?
                return PD;
            }
            else
            {
                PD = new PassportData() { Ser = ser, Nom = nom, DateTime_Zapros = DateTime.UtcNow, Status = PassStatus.Registred };
                MCache.Set(cacheKey, PD, TimeSpan.FromMinutes(cacheBufferTime));
                Pool.Enqueue(cacheKey); 
                if (!PoolHTTPSended1) {  PoolHTTPSended1 = true; Task.Run(() => SendPoolTask(ref PoolHTTPSended1)); }; //Запуск потока1 к web-api ведомства
                if (!PoolHTTPSended2) {  PoolHTTPSended2 = true; Task.Run(() => SendPoolTask(ref PoolHTTPSended2)); }; //Запуск потока2 к web-api ведомства
                return PD;
            }
        }

        /// <summary>
        /// Последовательный разбор общей очереди
        /// </summary>
        /// <param name="nomPool"></param>
        private void SendPoolTask(ref bool PoolHTTPSended)
        {
            string cacheKey;
            while (Pool.TryDequeue(out cacheKey))
            {
                PassportData PD;
                if (MCache.TryGetValue(cacheKey, out PD))
                {
                    PD.Status = PassStatus.Send_to_vedomsrvo;
                    MCache.Set(cacheKey, PD);
                    try
                    {
                        var reply = VedomstvoHttp.GetProverkaPassportAsync(cacheKey);
                        PD.OtvetHTML = string.Join(";", reply.Result.AsEnumerable().ToArray());
                        PD.Status = PassStatus.ОК;
                        PD.DateTime_Otvet = DateTime.UtcNow;
                    }
                    catch (Exception Err)
                    {
                        PD.Status = PassStatus.Error;
                        PD.OtvetHTML = Err.Message;
                        PD.DateTime_Otvet = DateTime.UtcNow;
                    }
                    MCache.Set(cacheKey, PD);
                }
            }
            PoolHTTPSended= false;
        }
    }

    /// <summary>
    /// Сервис запроса проверки у ведомства по WEB-API
    /// </summary>
    public class VedomstvoHttpClient
    {
        public HttpClient Client { get; }
        public VedomstvoHttpClient(HttpClient client)
        {
            client.BaseAddress = new Uri("http://localhost:5000");
            Client = client;
        }

        public async Task<IEnumerable<string>> GetProverkaPassportAsync(string ser_nom)
        {
            var response = await Client.GetAsync(string.Format("/api/TestV/{0}", ser_nom));
            if (response.IsSuccessStatusCode)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<IEnumerable<string>>(responseStream);

            }
            else
            {
                return new string[] { response.StatusCode.ToString() };
            }
        }
    }



}
 