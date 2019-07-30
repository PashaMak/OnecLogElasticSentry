using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using FormOnecLogElasticSentry;
using Nest;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.AccessControl;

namespace OnecLogElasticSentry
{
    class Elastic
    {
        //https://www.elastic.co/guide/en/elasticsearch/client/net-api/7.x/elasticsearch-net-getting-started.html

        public async static void Run()
        {
            //await Task.Run(() => new Elastic().FromFileToElastic());
            new Elastic().FromFileToElastic();
        }

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

        private DateTime getDateFromTimestamp1C(string timestamp1C)
        {
            long datetimestamp = Convert.ToInt64(timestamp1C, 16) / 10000;
            DateTime timestamp = (new DateTime(0001, 1, 1, 0, 0, 0, 0, DateTimeKind.Local)).AddSeconds(datetimestamp);
            return timestamp;
        }

        private string getStatusTransaction(string name)
        {
            // "N" – "Отсутствует"
            // "U" – "Зафиксирована"
            // "R" – "Не завершена"
            // "C" – "Отменена"

            if (name == "N")
                return "Отсутствует";
            if (name == "U")
                return  "Зафиксирована";
            if (name == "R")
                return "Не завершена";
            if (name == "C")
                return "Отменена";

            return "";
        }

        private string getLevel(string name)
        {
            // "I" – "Информация"
            // "E" – "Ошибки"
            // "W" – "Предупреждения"
            // "N" – "Примечания"

            if (name == "I")
                return "Информация";
            if (name == "E")
                return "Ошибки";
            if (name == "W")
                return "Предупреждения";
            if (name == "N")
                return "Примечания";

            return "";
        }

        private List<DictLog> parseDictionaryLog(DirectoryInfo subDirLog)
        {
            List<DictLog> dict_log = new List<DictLog>();

            // разберем словарь ЖР
            foreach (FileInfo file in subDirLog.GetFiles("*.lgf"))
            {
                // разрешаем запись другим потокам
                using (var fstream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader sreader_dict = new StreamReader(fstream))
                {
                    // первые 2 строки служебные
                    // 1 тип лога 1CV8LOG(ver 2.0)
                    // 2 идентификатор базы 28319a74-e5ca-487b-bd0d-c9207a1dc5f0
                    while (!sreader_dict.EndOfStream)
                    {
                        string line = sreader_dict.ReadLine();

                        if (!(line.Contains(",\"")))//"1CV8LOG(ver 2.0)"
                            continue;

                        string[] arr = line.Split(new char[] { '{', ',', '}' });
                        DictLog dict = new DictLog();

                        // строки разделяются запятыми, в последней строке ее нет
                        if (line.Contains("},"))
                        {
                            if (arr.Count() == 6)
                            {
                                dict.Type = int.Parse(arr[1]);
                                dict.Name = arr[2];
                                dict.Id = int.Parse(arr[3]);
                            }
                            else if (arr.Count() == 7)
                            {
                                dict.Type = int.Parse(arr[1]);
                                dict.Guid = arr[2];
                                dict.Name = arr[3];
                                dict.Id = int.Parse(arr[4]);
                            }
                            else
                            if (arr.Count() == 5)
                            {
                                dict.Type = int.Parse(arr[1]);
                                dict.Name = arr[2];
                                dict.Id = int.Parse(arr[3]);
                            }
                            else if (arr.Count() == 6)
                            {
                                dict.Type = int.Parse(arr[1]);
                                dict.Guid = arr[2];
                                dict.Name = arr[3];
                                dict.Id = int.Parse(arr[4]);
                            }
                        }

                        if (!(dict.Id == 0))
                            dict_log.Add(dict);
                    }
                }
            }

            return dict_log;
        }

        private string getValueFromDictionaryLog(List<DictLog> logDict, string value, string nameDictValue)
        {
            int? typeValue = null;

            if (nameDictValue == "user")
                typeValue = 1;
            else if (nameDictValue == "computer")
                typeValue = 2;
            else if (nameDictValue == "application")
                typeValue = 3;
            else if (nameDictValue == "event")
                typeValue = 4;
            else if (nameDictValue == "metadata")
                typeValue = 5;
            else if (nameDictValue == "server")
                typeValue = 6;

            if (typeValue == null)
                return "";

            int ivalue = int.Parse(value);

            IEnumerable<DictLog> res = logDict.Where(s => s.Type == typeValue && s.Id == ivalue);

            if (res.Count() != 0)
            {
                    return res.First().Name;
            }

            //Транзакция в формате записи из двух элементов преобразованных в шестнадцатеричное число – 
            // первый – число секунд с 01.01.0001 00:00:00 умноженное на 10000, 
            // второй – номер транзакции;
            //var aaa = int.Parse(elementLog.t1);

            return "";
        }

        private Dictionary<string, string> GetCatalogBase(Settings settings_file)
        {
            Dictionary<string, string> base_array = new Dictionary<string, string>();

            using (StreamReader sreader = new StreamReader(settings_file.path_journal + "\\1CV8Clst.lst"))
            {
                while (!sreader.EndOfStream)
                {
                    string line = sreader.ReadLine();

                    if (!(line.Contains("Srvr") || line.Contains("File")))
                        continue;

                    string[] arr = line.Split(new char[] { '{', ',' });

                    base_array.Add(arr[1], arr[2]);
                }
            }

            return base_array;
        }

        private void AddRecord(ElasticClient client, List<DictLog> dictLog, string nameBase, string lineLog
            , DateTime BoundaryPeriod, out DateTime LastBoundaryPeriod
            , int countLeft, int countRight, in int idRecordElastic, out int idRecordElasticOut)
        {
            LastBoundaryPeriod = BoundaryPeriod;
            idRecordElasticOut = idRecordElastic;
            string[] arr_line_log = lineLog.Split(new char[] { '{', ',', '}' });

            int idPresentation = 0;
            int idServer = 0;
            int idNumberSession = 0;

            DateTime timestamp = convertStringToDateTime(arr_line_log[1]);
            if (timestamp < BoundaryPeriod)
            {
                return;
            }

            //1) Дата и время в формате "yyyyMMddHHmmss", легко превращается в дату функцией Дата();
            //2) Статус транзакции – может принимать четыре значения "N" – "Отсутствует", "U" – "Зафиксирована", "R" – "Не завершена" и "C" – "Отменена";
            //3) Транзакция в формате записи из двух элементов преобразованных в шестнадцатеричное число – первый – число секунд с 01.01.0001 00:00:00 умноженное на 10000, второй – номер транзакции;
            //4) Пользователь – указывается номер в массиве пользователей;
            //5) Компьютер – указывается номер в массиве компьютеров;
            //6) Приложение – указывается номер в массиве приложений;
            //7) Соединение – номер соединения;
            //8) Событие – указывается номер в массиве событий;
            //9) Важность – может принимать четыре значения – "I" – "Информация", "E" – "Ошибки", "W" – "Предупреждения" и "N" – "Примечания";
            //10) Комментарий – любой текст в кавычках;
            //11) Метаданные – указывается номер в массиве метаданных;
            //12) Данные – самый хитрый элемент, содержащий вложенную запись;
            //13) Представление данных – текст в кавычках;
            //14) Сервер – указывается номер в массиве серверов;
            //15) Основной порт – указывается номер в массиве основных портов;
            //16) Вспомогательный порт – указывается номер в массиве вспомогательных портов;
            //17) Сеанс – номер сеанса;
            //18) Количество дополнительных метаданных, номера которых будут перечислены в следующих элементах записи.
            //    Именно 18 - й элемент определяет длину записи, т.к.дальше будут следовать столько элементов сколько указано здесь +один последний, 
            //    назначение которого пока не определено и обычно там "{0}".Возможно это просто маркер окончания записи.Так же есть идея что {0}
            //    похоже на пустой массив.

            IndexOutputLog output = new IndexOutputLog();
            output.Id = idRecordElasticOut++;
            output.Base = nameBase;
            output.Timestamp = timestamp;
            output.Transaction = getStatusTransaction(arr_line_log[2]);
            output.User = getValueFromDictionaryLog(dictLog, arr_line_log[7], "user");
            output.Computer = getValueFromDictionaryLog(dictLog, arr_line_log[8], "computer");
            output.Application = getValueFromDictionaryLog(dictLog, arr_line_log[9], "application");
            output.NumberConnection = arr_line_log[10];
            output.Event = getValueFromDictionaryLog(dictLog, arr_line_log[11], "event");
            output.Level = getLevel(arr_line_log[12]);
            output.Comment = arr_line_log[13];
            output.Metadata = getValueFromDictionaryLog(dictLog, arr_line_log[14], "metadata");

                if (output.Metadata == "")
                {
                    return;
                }

                long numberTransaction = 0;

                if (arr_line_log[16] == "\"P\"" && arr_line_log.Count() == 36)
                {
                    // {20190729165105,N,{0,0},0,1,2,433,1,I,"",0,{"P",{1,{"S","ROSSKO\pavel.makarov"}}},"",1,1,0,1,0,{0}},
                    if (arr_line_log[4] != "0")
                    {
                        output.Timestamp = getDateFromTimestamp1C(arr_line_log[4]);
                        numberTransaction = Convert.ToInt64(arr_line_log[5], 16);
                    }
                    idPresentation = 25;
                    idServer = 26;
                    idNumberSession = 29;
                }
                else if (arr_line_log[16] == "\"P\"" && arr_line_log.Count() == 40)
                {
                    // {20190729165108,N,{0,0},1,1,2,433,3,I,"",0,{"P",{6,{"S","jenkins"},{"S","ROSSKO\pavel.makarov"}}},"",1,1,0,1,0,{0}},
                    if (arr_line_log[4] != "0")
                    {
                        output.Timestamp = getDateFromTimestamp1C(arr_line_log[4]);
                        numberTransaction = Convert.ToInt64(arr_line_log[5], 16);
                    }
                    idPresentation = 29;
                    idServer = 30;
                    idNumberSession = 33;
                }
                else if (arr_line_log[16] == "\"U\"")
                {
                    // {20190729165201,C,{2435928362a10,74c},1,1,2,433,5,I,\"\",0,{\"U\"},\"\",1,1,0,1,0,{0}},
                    if (arr_line_log[4] != "0")
                    {
                        output.Timestamp = getDateFromTimestamp1C(arr_line_log[4]);
                        numberTransaction = Convert.ToInt64(arr_line_log[5], 16);
                    }
                    idPresentation = 18;
                    idServer = 19;
                    idNumberSession = 22;
                }
                else if (arr_line_log[16] == "\"S\"" || arr_line_log[16] == "\"R\"")
                {
                    //{ 20190729165201,U,{ 2435928362a10,74c},1,1,2,433,6,I,"",45,{ "R",21:94e038d547ded5ea11e9af4b2d5a712d},"тест",1,1,0,1,0,{ 0} },
                    if (arr_line_log[4] != "0")
                    {
                        output.Timestamp = getDateFromTimestamp1C(arr_line_log[4]);
                        numberTransaction = Convert.ToInt64(arr_line_log[5], 16);
                    }
                    idPresentation = 19;
                    idServer = 20;
                    idNumberSession = 23;
                }
                else
                {
                    // {20190729181755,U,{243592b489a30,f9e},1,1,2,433,6,I,"",87,{0}},
                }
                //{20190726083202,U,{243587bd5e620,154a},1,1,2,252,10,I,"",6,{"R",17:b3c300505699197f11e631d8d789bc5c},"Клементьев А.А. (Станционная, 16/1)",1,1,0,2,0,{0}},
                if (idPresentation != 0)
                    output.Presentation = arr_line_log[idPresentation];
                if (idServer != 0)
                    output.Server = getValueFromDictionaryLog(dictLog, arr_line_log[idServer], "server");
                if (idNumberSession != 0)
                    output.NumberSession = arr_line_log[idNumberSession];
                if (numberTransaction != 0)
                    output.NumberTransaction = numberTransaction;
                if (output.Timestamp != new DateTime(1, 1, 1))
                    LastBoundaryPeriod = output.Timestamp;

                // отправим в эластик
                var asyncIndexResponse = client.IndexDocument(output);
        }

        private void LogRecordProcessing(List<DictLog> dictLog, StreamReader sreader, string nameBase, ElasticClient client
            , in int idRecordElastic, out int idRecordElasticOut, in DateTime BoundaryPeriod, out DateTime LastBoundaryPeriod)
        {
            idRecordElasticOut = idRecordElastic;
            LastBoundaryPeriod = BoundaryPeriod;
            
            // смотрим количетсво открывающих и закрывающих скобок,
            // при их равентве определяем окончание записи лога

            int countLeft = 0;
            int countRight = 0;
            string lineLog = "";

            while (!sreader.EndOfStream)
            {
                string line = sreader.ReadLine();

                // заголовки пропускаем
                if ((line.Contains("1CV8LOG(ver 2.0)") || line.Contains("-") || line == ""))
                    continue;

                string[] arrLine = line.Split(new char[] { ',' });
                countLeft += arrLine.Where(s => s.Contains("{")).Count();
                countRight += arrLine.Where(s => s.Contains("}")).Count();

                lineLog = lineLog + line;

                // собрали строку лога, разберем ее
                if (countLeft == countRight)
                {
                    try
                    {
                        AddRecord(client, dictLog, nameBase, lineLog
                            , BoundaryPeriod, out LastBoundaryPeriod
                            , countLeft, countRight, idRecordElastic, out idRecordElasticOut);

                    }
                    catch (Exception e)
                    {
                        // Доработать парсер чтобы разбила строки
                        // {20190730122419,U,{243595222e430,13eced},1,1,2,2,8,I,"",75,{"R",84:baa7005056997d3311e79f43761850ad},"10009020/210917/0017116/19, БЕЛЬГИЯ ",1,1,0,1,0,{0}},
                        string error = e.Message + " record log:" + lineLog;
                    }

                    countLeft = 0;
                    countRight = 0;
                    lineLog = "";
                }
            }
        }

        private void AddRecordLog(string message)
        {
            using (FileStream fs = new FileStream("OnecLogElasticSentry.log", FileMode.OpenOrCreate))
            using (StreamWriter swriter = new StreamWriter(fs))
            {
                swriter.WriteLine(message);
            }
        }

        public void FromFileToElastic()
        {
            Settings settings_file;
            // считаем настройки
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(Settings));
            using (FileStream fsread = new FileStream("OnecLogElasticSentry.json", FileMode.OpenOrCreate))
            {
                if (fsread.Length != 0)
                    settings_file = (Settings)jsonFormatter.ReadObject(fsread);
                else
                    settings_file = new Settings();
            }

            if (settings_file.path_journal == null)
                settings_file.path_journal = @"C:\Program Files\1cv8\srvinfo\reg_1541";

            if (!Directory.Exists(settings_file.path_journal))
            {
               string message = "Не найден каталог " + settings_file.path_journal;
               return;
            }

            // прочитаем каталог баз
            Dictionary<string, string> base_array = GetCatalogBase(settings_file);

            // фиксируем время начала
            if (settings_file.date_time == null)
                settings_file.date_time = DateTime.Now;

            // подключимся к эластику
            if (settings_file.adress_elastic == null)
                settings_file.adress_elastic = "localhost";

            if (settings_file.port_elastic == 0)
                settings_file.port_elastic = 9200;

            var settings = new ConnectionSettings(new Uri("http://" + settings_file.adress_elastic + ":" + settings_file.port_elastic.ToString()));
            settings.DefaultIndex("indexlogjr");

            ElasticClient client = new ElasticClient(settings);
            var countDocument = client.Search<IndexDictionary>(s => s.TotalHitsAsInteger());
            int countRecord = (int)countDocument.Total;

            DateTime LastBoundaryPeriod = settings_file.date_time;

            // обойдем подкаталоги и прочитаем логи
            DirectoryInfo di = new DirectoryInfo(settings_file.path_journal);
            foreach (DirectoryInfo subDir in di.GetDirectories())
            {
                // получим имя базы
                if (!base_array.ContainsKey(subDir.Name))
                    continue;

                string nameBase = base_array[subDir.Name];

                // обойдем файлы
                foreach (DirectoryInfo subDirLog in subDir.GetDirectories())
                {
                    List<DictLog> dict_log = parseDictionaryLog(subDirLog);

                    // разберем лог ЖР
                    foreach (FileInfo file in subDirLog.GetFiles("*.lgp"))
                    {
                        // обработаем файл
                        // разрешаем запись другим потокам
                        using (var fstream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (StreamReader sreader = new StreamReader(fstream))
                        {
                            // ранее обработанные файлы пропускаем
                            // поскольку данные хранятеся до секунды округляем время на начало секунды
                            // чтобы не было постоянного повторного считывания
                            DateTime LastWriteTime = file.LastWriteTime.AddMilliseconds(-file.LastWriteTime.Millisecond);
                            if (LastWriteTime < LastBoundaryPeriod)
                                continue;
                            try
                            {
                                LogRecordProcessing(dict_log, sreader, nameBase, client
                                    , in countRecord, out countRecord
                                    , in LastBoundaryPeriod, out LastBoundaryPeriod);
                            } catch (Exception e)
                            {
                                string error = e.Message;
                                AddRecordLog(error);
                            }
                        }
                    }
                }
            }

            // запишем настройки
            using (FileStream fswrite = new FileStream("OnecLogElasticSentry.json", FileMode.Open))
            {
                settings_file.date_time = LastBoundaryPeriod;
                jsonFormatter.WriteObject(fswrite, settings_file);
            }
        }
    }
}
