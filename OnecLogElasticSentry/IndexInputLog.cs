using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnecLogElasticSentry
{
    class IndexInputLog
    {
        public int Id { get; set; }
        public string guid_base { get; set; }
        public string timestamp { get; set; }
        public DateTime timestampDateTime {
            get {
                return convertStringToDateTime(this.timestamp);
            }
        }
        public int user_id { get; set; }
        public string user_string { get; set; }
        public int application_id { get; set; }
        public string application_string { get; set; }
        public int computer_id { get; set; }
        public string computer_string { get; set; }
        public int connection_id { get; set; }
        public int metadata_id { get; set; }
        public string session { get; set; }
        public string transaction_status { get; set; }
        public string transaction_status_string {
            get {              
                // "N" – "Отсутствует"
                // "U" – "Зафиксирована"
                // "R" – "Не завершена"
                // "C" – "Отменена"

                if (this.transaction_status == "N")
                    return "Отсутствует";
                if (this.transaction_status == "U")
                    return "Зафиксирована";
                if (this.transaction_status == "R")
                    return "Не завершена";
                if (this.transaction_status == "C")
                    return "Отменена";

                return "";
            }
        }
        public string level { get; set; }
        public string level_string {
            get {
                // "I" – "Информация"
                // "E" – "Ошибки"
                // "W" – "Предупреждения"
                // "N" – "Примечания"

                if (this.level == "I")
                    return "Информация";
                if (this.level == "E")
                    return "Ошибки";
                if (this.level == "W")
                    return "Предупреждения";
                if (this.level == "N")
                    return "Примечания";

                return "";
            }
            }
        public int event_id { get; set; }
        public string event_string { get; set; }
        public string event_data { get; set; }
        public string event_data_repr { get; set; }
        public string comment { get; set; }
        public string t1 { get; set; }
        public string t2 { get; set; }
        public string metadata_string { get; set; }

        private DateTime convertStringToDateTime(string timestamp)
        {
            long unixDate = long.Parse(timestamp);
            string year = timestamp.Substring(0, 4);
            string month = timestamp.Substring(4, 2);
            string day = timestamp.Substring(6, 2);
            string hour = timestamp.Substring(8, 2);
            string min = timestamp.Substring(10, 2);
            string sec = timestamp.Substring(12, 2);
            DateTime date = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day), int.Parse(hour), int.Parse(min), int.Parse(sec), DateTimeKind.Local);

            return date;
        }
    }
}
