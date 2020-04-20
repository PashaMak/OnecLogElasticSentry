using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OnecLogElastic
{
    class DictLog
    {
        // порядковый номер в типе метаданных
        public int Id { get; set; }
        // тип метаданных
        public int Type { get; set; }
        // гуид метаданных
        public string Guid { get; set; }
        // имя метаданных
        public string Name { get; set; }

        public Dictionary<string, string> Users { get; set; }
        public Dictionary<string, string> Computers { get; set; }
        public Dictionary<string, string> Applications { get; set; }
        public Dictionary<string, string> Events { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public Dictionary<string, string> Servers { get; set; }
        public Dictionary<string, string> MainPorts { get; set; }
        public Dictionary<string, string> AdditionalPorts { get; set; }

        public Dictionary<string, string> Cache { get; set; } 

        public DictLog()
        {
            if (Users == null)
                Users = new Dictionary<string, string>();
            
            if (Computers == null)
                Computers = new Dictionary<string, string>();
            
            if (Applications == null)
                Applications = new Dictionary<string, string>();
            
            if (Events == null)
                Events = new Dictionary<string, string>();
            
            if (Metadata == null)
                Metadata = new Dictionary<string, string>();
            
            if (Servers == null)
                Servers = new Dictionary<string, string>();
            
            if (MainPorts == null)
                MainPorts = new Dictionary<string, string>();
            
            if (AdditionalPorts == null)
                AdditionalPorts = new Dictionary<string, string>();

            if (Cache == null)
                Cache = new Dictionary<string, string>();
        }

        public static string GetValueFromDictionaryLog(DictLog logDict, string value, string nameDictValue)
        {
            int? typeValue = null;
            string findValue = null;

            // поищем в кэше
            string hash = nameDictValue + value;
            logDict.Cache.TryGetValue(hash, out findValue);

            if (findValue != null)
                return findValue;

            if (nameDictValue == "user")
            {
                typeValue = 1;
                logDict.Users.TryGetValue(value, out findValue);
            }
            else if (nameDictValue == "computer")
            {
                typeValue = 2;
                logDict.Computers.TryGetValue(value, out findValue);
            }
            else if (nameDictValue == "application")
            {
                typeValue = 3;
                logDict.Applications.TryGetValue(value, out findValue);
            }
            else if (nameDictValue == "event")
            {
                typeValue = 4;
                logDict.Events.TryGetValue(value, out findValue);
            }
            else if (nameDictValue == "metadata")
            {
                typeValue = 5;
                logDict.Metadata.TryGetValue(value, out findValue);
            }
            else if (nameDictValue == "server")
            {
                typeValue = 6;
                logDict.Servers.TryGetValue(value, out findValue);
            }
            else if (nameDictValue == "mainPort")
            {
                typeValue = 7;
                logDict.MainPorts.TryGetValue(value, out findValue);
            }
            else if (nameDictValue == "additionalPort")
            {
                typeValue = 8;
                logDict.AdditionalPorts.TryGetValue(value, out findValue);
            }

            // добавим в кэш
            if (!logDict.Cache.ContainsKey(hash))
                logDict.Cache.Add(hash, findValue);

            if (typeValue == null)
                return "";

            if (typeValue == 8)
                return "0";

            //// linq потребляет много проца, заменил на поиск по словарям
            //int ivalue = int.Parse(value);
            //IEnumerable<DictLog> res = logDict.Where(s => s.Type == typeValue && s.Id == ivalue);

            //if (res.Count() != 0)
            //{
            //    findValue = res.First().Name;
            //    Cache.Add(hash, findValue);
            //    return findValue;
            //}

            return "";
        }

        public static DictLog ParseDictionaryLog(DirectoryInfo subDirLog)
        {
            DictLog dictLog = new DictLog();

            // 6d184c14-f352-49bd-9baa-740acf9eca79
            string patternGUID = "^[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}$";
            Regex regex = new Regex(patternGUID);

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

                    string lineDictLog = "";

                    while (!sreader_dict.EndOfStream)
                    {
                        string line = sreader_dict.ReadLine();

                        // заголовки пропускаем
                        if (line.Contains("1CV8LOG(ver 2.0)") || (regex.Matches(line).Count != 0))
                            continue;
                        
                        //TODO: есть похожий метод в LogRecordProcessing, подумать над их объединением
                        if (ThisNewLineDictLog(lineDictLog) && ThisEndLineDictLog(lineDictLog) && lineDictLog.Length != 0)
                        {
                            // собрали строку лога, разберем ее
                            try
                            {
                                LineLog lineLog = new LineLog();
                                string[] arrLine = lineLog.ParseStringToArrayObject(lineDictLog);
                                addRecordDictLog(arrLine, dictLog);
                                // обработали прошлую строку, обновим чтоб текущая обработалась при следующей итерации
                                lineDictLog = line;
                            }
                            catch (Exception e)
                            {
                                string error = e.Message + " record log:" + line;
                                Log.AddRecord("DicLogProcessing", error);
                            }
                        }
                        else
                        {
                            lineDictLog += line;
                        }
                    }

                    // обработаем хвост
                    if (lineDictLog.Length != 0)
                        try
                        {
                            LineLog lineLog = new LineLog();
                            string[] arrLine = lineLog.ParseStringToArrayObject(lineDictLog);
                            addRecordDictLog(arrLine, dictLog);
                        }
                        catch (Exception e)
                        {
                            string error = e.Message + " record log:" + lineDictLog + "; Файл логов " + file.FullName;
                            Log.AddRecord("DicLogProcessing", error);
                        }
                }
            }

            return dictLog;
        }

        public static void addRecordDictLog(string[] arr, DictLog dictLog)
        {
            DictLog dict = new DictLog();
            int arrCountRecord = arr.Count();

            if (arrCountRecord == 3)
            {
                dict.Type = int.Parse(arr[0]);
                dict.Name = arr[1].Trim('"');
                dict.Id = int.Parse(arr[2]);
            }
            else if (arrCountRecord == 4)
            {
                dict.Type = int.Parse(arr[0]);
                dict.Guid = arr[1];
                dict.Name = arr[2].Trim('"');
                dict.Id = int.Parse(arr[3]);
            }

            string id = dict.Id.ToString();

            switch (dict.Type)
            {
                case 1:
                    if (!dictLog.Users.ContainsKey(id))
                        dictLog.Users.Add(id, dict.Name);
                    else
                        dictLog.Users[id] = dict.Name;
                    break;
                case 2:
                    if (!dictLog.Computers.ContainsKey(id))
                        dictLog.Computers.Add(id, dict.Name);
                    else
                        dictLog.Computers[id] = dict.Name;
                    break;
                case 3:
                    if (!dictLog.Applications.ContainsKey(id))
                        dictLog.Applications.Add(id, dict.Name);
                    else
                        dictLog.Applications[id] = dict.Name;
                    break;
                case 4:
                    if (!dictLog.Events.ContainsKey(id))
                        dictLog.Events.Add(id, dict.Name);
                    else
                        dictLog.Events[id] = dict.Name;
                    break;
                case 5:
                    if (!dictLog.Metadata.ContainsKey(id))
                        dictLog.Metadata.Add(id, dict.Name);
                    else
                        dictLog.Metadata[id] = dict.Name;
                    break;
                case 6:
                    if (!dictLog.Servers.ContainsKey(id))
                        dictLog.Servers.Add(id, dict.Name);
                    else
                        dictLog.Servers[id] = dict.Name;
                    break;
                case 7:
                    if (!dictLog.MainPorts.ContainsKey(id))
                        dictLog.MainPorts.Add(id, dict.Name);
                    else
                        dictLog.MainPorts[id] = dict.Name;
                    break;
                case 8:
                    if (!dictLog.AdditionalPorts.ContainsKey(id))
                        dictLog.AdditionalPorts.Add(id, dict.Name);
                    else
                        dictLog.AdditionalPorts[id] = dict.Name;
                    break;
            }
        }

        private static bool ThisNewLineDictLog(string line)
        {
            // у новой строки фиксированный формат
            // {11,

            string patternNewLine = "^\\{[0-9]*,";

            Regex regex = new Regex(patternNewLine);
            MatchCollection matches = regex.Matches(line);

            return matches.Count > 0;
        }

        private static bool ThisEndLineDictLog(string line)
        {
            // у окончания строки фиксированный формат
            // },
            // }

            string patternEndLine = "},";
            string patternEndLine2 = "}";

            Regex regex = new Regex(patternEndLine);
            MatchCollection matches = regex.Matches(line);

            Regex regex2 = new Regex(patternEndLine2);
            MatchCollection matches2 = regex.Matches(line);

            return matches.Count > 0 || matches2.Count > 0;
        }

        public static Dictionary<string, string> GetCatalogBase(Settings settingsFile)
        {
            Dictionary<string, string> arrayBase = new Dictionary<string, string>();

            using (StreamReader sreader = new StreamReader(settingsFile.PathJournal + "\\1CV8Clst.lst"))
            {
                while (!sreader.EndOfStream)
                {
                    string line = sreader.ReadLine();

                    if (!(line.Contains("Srvr") || line.Contains("File")))
                        continue;

                    string[] arr = line.Split(new char[] { '{', ',' });

                    arrayBase.Add(arr[1], arr[2].Trim('\"'));
                }
            }

            return arrayBase;
        }
    }
}
