using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Nest;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.AccessControl;
using System.Threading;
using System.Timers;
using System.Text.RegularExpressions;

namespace OnecLogElastic
{
    class Elastic
    {
        //https://www.elastic.co/guide/en/elasticsearch/client/net-api/7.x/elasticsearch-net-getting-started.html

        private ConnectionSettings connectionSettings { get; set; }
        private ElasticClient elasticClient { get; set; }
        private Settings settingsFile { get; set; }
        private int NumberRecordsProcessed { get; set; }
        string nameIndex { get; set; }

        public void RunTheard()
        {

            try
            {
                new Elastic().FromFileToElastic();
            }
            catch (Exception e)
            {
                Log.AddRecord("RunTheard", e.Message);
            }
        }

        private IndexOutputLog GetRecord(DictLog dictLog, string nameBase, string lineLog)
        {
            LineLog logData = new LineLog();
            logData.ParseLine(lineLog, dictLog);
            logData.indexOutputLog.Base = nameBase;

            return logData.indexOutputLog;
        }

        private async Task<DateTime> AddRecords(List<IndexOutputLog> listRecords)
        {
            // отправим в эластик
            var asyncIndexResponse = await elasticClient.IndexManyAsync(listRecords, nameIndex).ConfigureAwait(true);

            if (!asyncIndexResponse.IsValid)
                Log.AddRecord("InsertIntoIndex", asyncIndexResponse.OriginalException.Message);

            DateTime LastBoundaryPeriod = listRecords[listRecords.Count - 1].Timestamp;

            return LastBoundaryPeriod;
        }

        private DateTime LogRecordProcessing(DictLog dictLog, StreamReader sreader
            , string nameBase, DateTime BoundaryPeriod)
        {            
            string lineLog = "";

            List<IndexOutputLog> listLine = new List<IndexOutputLog>();

            string patternGUID = "^[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}$";
            Regex regex = new Regex(patternGUID);

            NumberRecordsProcessed = 0;

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 60000;
            timer.Elapsed += new ElapsedEventHandler(onTimer);
            timer.Start();

            while (!sreader.EndOfStream)
            {
                string line = sreader.ReadLine();

                // заголовки пропускаем
                if ((line.Contains("1CV8LOG(ver 2.0)") || (regex.Matches(line).Count != 0) || line.Length == 0))
                    continue;

                // логи всегда будут склеиваться из нескольких строк
                if (lineLog.Length != 0 && ThisNewLineLog(line))
                {
                    // собрали строку лога, разберем ее
                    try
                    {
                        IndexOutputLog record = GetRecord(dictLog, nameBase, lineLog);

                        // пропустим обработанные строки
                        if (record.Timestamp < BoundaryPeriod)
                        {
                            lineLog = line;
                            continue;
                        }

                        //// обрабатываем только метаданные
                        //if (record.Metadata.Length == 0)
                        //{
                        //    lineLog = line;
                        //    continue;
                        //}

                        listLine.Add(record);

                        if (listLine.Count > settingsFile.NumberRecordsProcessedAtTime)
                        {
                            Task<DateTime> period = AddRecords(listLine);
                            BoundaryPeriod = period.Result;
                            NumberRecordsProcessed += listLine.Count;
                            listLine.Clear();
                        }
                    }
                    catch (Exception e)
                    {
                        string error = e.Message + " record log:" + lineLog;
                        Log.AddRecord("LogRecordProcessing", error);
                    }

                    lineLog = line;
                }
                else
                    lineLog += line;
            }

            // обработаем хвост
            if (listLine.Count > 0)
            {
                Task<DateTime> period = AddRecords(listLine);
                BoundaryPeriod = period.Result;
                listLine.Clear();

                NumberRecordsProcessed += listLine.Count;
            }

            timer.Dispose();

            return BoundaryPeriod;
        }

        private void onTimer(object sender, System.Timers.ElapsedEventArgs arg)
        {
            if (settingsFile.DisplayAdditionalInformation)
            {
                Log.AddRecord("Доп. информация", "Обработано записей : " + NumberRecordsProcessed);
                NumberRecordsProcessed = 0;
            }
        }

        private bool ThisNewLineLog(string line)
        {
            // у новой строки фиксированный формат
            //1) Дата и время в формате "yyyyMMddHHmmss", легко превращается в дату функцией Дата();
            //2) Статус транзакции – может принимать четыре значения "N" – "Отсутствует", "U" – "Зафиксирована", "R" – "Не завершена" и "C" – "Отменена";
            // {20190812000001,C,
            // {20190812114310,N,

            //// регулярки любят проц кушать, упрощаем
            //string patternNewLine = "^\\{[0-9]{14},";

            //Regex regex = new Regex(patternNewLine);
            //MatchCollection matches = regex.Matches(line);

            //return matches.Count > 0;

            if (line.Length != 18)
                return false;

            string statusTransaction = LineLog.GetStatusTransaction(line.Substring(16, 1));
            if (statusTransaction.Length != 0)
                return true;
            else
                return false;

        }

        public void FromFileToElastic()
        {
            // прочитаем настройки
            settingsFile = new Settings();
            settingsFile.Read();

            if (!Directory.Exists(settingsFile.PathJournal))
            {
               throw new Exception("Не найден каталог " + settingsFile.PathJournal);
            }

            SettingsReadBase settingsReadBase = new SettingsReadBase();
            settingsReadBase.Read();

            // прочитаем каталог баз
            Dictionary<string, string> arrayBase = DictLog.GetCatalogBase(settingsFile);

            nameIndex = settingsFile.NameIndexElastic + "-" + DateTime.Now.ToString("yyyyMMdd");
            connectionSettings = new ConnectionSettings(new Uri("http://" + settingsFile.AdressElastic + ":" + settingsFile.PortElastic.ToString()));
            connectionSettings.DefaultIndex(nameIndex);
            // при указании идентификации используем ее
            if (settingsFile.ElasticUserName != null)
                connectionSettings.BasicAuthentication(settingsFile.ElasticUserName, settingsFile.ElasticUserPassword);

            elasticClient = new ElasticClient(connectionSettings);
            // добавить настройки кластера для создаваемых по умолчанию индексов
            // 0 реплик
            //elasticClient.Cluster.PutSettings();

            // обойдем подкаталоги и прочитаем логи
            DirectoryInfo di = new DirectoryInfo(settingsFile.PathJournal);
            foreach (DirectoryInfo subDir in di.GetDirectories())
            {
                // получим имя базы
                if (!arrayBase.ContainsKey(subDir.Name))
                    continue;

                string nameBase = arrayBase[subDir.Name];

                // обойдем файлы
                foreach (DirectoryInfo subDirLog in subDir.GetDirectories())
                {
                    // для первого запуска берем общую дату
                    DateTime LastBoundaryPeriod = LineLog.ConvertStringToDateTime(settingsReadBase.LastRunTime);

                    // получим последнее время чтения
                    if (settingsReadBase.LastTimeReadLogBase.ContainsKey(subDir.Name))
                        LastBoundaryPeriod = LineLog.ConvertStringToDateTime(settingsReadBase.LastTimeReadLogBase[subDir.Name]);
                    else
                        settingsReadBase.LastTimeReadLogBase.Add(subDir.Name, LastBoundaryPeriod.ToString("yyyyMMddHHmmss"));

                    // получим словарь для разбора логов ЖР
                    DictLog dictLog = DictLog.ParseDictionaryLog(subDirLog);

                    // разберем лог ЖР
                    foreach (FileInfo file in subDirLog.GetFiles("*.lgp"))
                    {
                        // обработаем файл
                        // разрешаем запись другим потокам
                        using (var fstream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (StreamReader sreader = new StreamReader(fstream))
                        {
                            // ранее обработанные файлы пропускаем
                            // поскольку данные хранятся до секунды округляем время на начало секунды
                            // чтобы не было постоянного повторного считывания
                            DateTime LastWriteTime = file.LastWriteTime.AddMilliseconds(-file.LastWriteTime.Millisecond);
                            if (LastWriteTime < LastBoundaryPeriod)
                                continue;
                            try
                            {
                                LastBoundaryPeriod = LogRecordProcessing(dictLog, sreader, nameBase, LastBoundaryPeriod);
                            } 
                            catch (Exception e)
                            {
                                string error = e.Message;
                                Log.AddRecord("LogRecordProcessing", error);
                            }
                        }
                    }

                    // некорректное сохранение часов, доделать
                    settingsReadBase.LastTimeReadLogBase[subDir.Name] = LastBoundaryPeriod.ToString("yyyyMMddHHmmss");
                }
            }

            // запишем настройки
            settingsFile.Write();
            settingsReadBase.Write();
        }
    }
}
