using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnecLogElastic
{
    public static class Log
    {
        private static object sync = new object();

        public static void AddRecord(string events, string message)
        {
            lock (sync)
            {
                // лог пишем в директорию с которой запущен сервис
                string fullFilename = AppDomain.CurrentDomain.BaseDirectory + "\\OnecLogElastic.log";
                // в случае отсутствия файла он будет создан
                using (TextWriter twriter = File.AppendText(fullFilename))
                {
                    twriter.WriteLine(DateTime.Now + " " + events + ": " + message);
                }
            }
        }
    }
}
