using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace CHEK_PASSPORT
{
    [Route("api/[controller]")]
    [ApiController]
    public class passport : ControllerBase
    {
        private readonly MainTask MTask;
        private readonly VedomstvoHttpClient vedomstvoHttp;
        public passport(MainTask M, VedomstvoHttpClient client)
        {
            MTask = M;
            vedomstvoHttp = client;
        }

        [HttpGet("{Serial:length(4):int}/{Number:length(6):int}")]
        [Produces("application/json")]
        public IEnumerable<string> Get(int Serial, int Number)
        {
            PassportData PD = MTask.Registred(Serial, Number);
            return new string[] { PD.Ser.ToString(), PD.Nom.ToString(), PD.Status.ToString(), PD.DateTime_Zapros.ToString(), PD.DateTime_Otvet.ToString(), PD.OtvetHTML };
        }
    }

    /// <summary>
    /// Предположим что Ведомство имеет тоже WEB-api отвечающее с задержкой 10 секунд
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TestV : ControllerBase
    {
        [HttpGet("{ser_nom:length(10):int}")]
        [Produces("application/json")]
        public IEnumerable<string> Get(string ser_nom)
        {
            System.Threading.Thread.Sleep(10000);
            return new string[] {
                "Ответ ведомства в течение 10 секунд",
                string.Format("Паспорт# {0}",ser_nom),
                string.Format("CurrentThread.ManagedThreadId={0}", System.Threading.Thread.CurrentThread.ManagedThreadId)
            };
        }
    }
}

