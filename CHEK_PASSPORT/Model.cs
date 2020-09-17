using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CHEK_PASSPORT
{
    public enum PassStatus : byte
    {
        /// <summary>
        /// Заявка зарегистрирована
        /// </summary>
        Registred = 1,

        /// <summary>
        /// Заявка отправлена в ведомство
        /// </summary>
        Send_to_vedomsrvo = 2,

        /// <summary>
        /// Получен ответ от ведомства
        /// </summary>
        ОК = 3,

        /// <summary>
        /// Отработка заявки завешилась не успешно
        /// </summary>
        Error = 4
    }

    public class PassportData
    {
        public int Ser { get; set; }
        public int Nom { get; set; }
        public PassStatus Status { get; set; }
        public DateTime DateTime_Zapros { get; set; }
        public DateTime DateTime_Otvet { get; set; }
        public string OtvetHTML { get; set; }
    }
}

