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
        const int cacheBufferTime = 120; //Длительность хранения запроса в минутах в ОЗУ
        public bool PoolHTTPSended = false;
        private readonly ConcurrentQueue<string> Pool = new ConcurrentQueue<string>(); //Очередь невыполненных запросов
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
                if (!PoolHTTPSended)
                {
                    PoolHTTPSended = true;
                    Task.Run(() => SendPoolParralel());
                }
                return PD;
            }
        }

        private void SendPoolParralel()
        {
            Action HTTPSend = () =>
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
                                PD.OtvetHTML = string.Join(";", VedomstvoHttp.GetProverkaPassportAsync(cacheKey).Result.AsEnumerable().ToArray());
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
                };
            //количество одновременных соединений, разрешенных ведомством для обращения к своему API
            //(3 для примера)
            if (ServicePointManager.DefaultConnectionLimit < 3) 
            { 
                ServicePointManager.DefaultConnectionLimit = 3; };
            Parallel.Invoke(HTTPSend, HTTPSend, HTTPSend);
            PoolHTTPSended = false;
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
 