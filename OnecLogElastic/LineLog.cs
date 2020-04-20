using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace OnecLogElastic
{
    class LineLog
    {
        public IndexOutputLog indexOutputLog { get; set; }

        // Получить объект лога из строки лога оформленного открывающим и закрывающим символом
        public Tuple<string, string> GetObject(string line)
        {
            string symbolLeft = "";
            string symbolRight = "";

            int indexTo = 0;

            symbolLeft = line.Substring(0, 1);

            if (symbolLeft == "{") 
            {
                symbolRight = "}";
            } 
            else if (symbolLeft ==  "\"" || symbolLeft == ",")
            {
                symbolRight = symbolLeft;
            } 
            else
            {
                // по умолчанию разделитель запятая
                symbolRight = ",";
                symbolLeft = symbolRight;
            }
            
            string objectString = "";
            
            // по умолчанию смотрим по закрывающий символ - для ускорения обработки
            indexTo = line.IndexOf(symbolRight);

            // не нашли закрывающий символ, наш объект до конца строки
            if (indexTo == -1)
                objectString = line.Substring(0, line.Length);

            // выделим объект
            while (objectString.Length == 0)
            {
                string value = line.Substring(0, indexTo);

                if (countPairSymbolOdd(value, line, symbolLeft, symbolRight))
                {
                    objectString = value;
                    break;

                }

                if (indexTo > line.Length - 1)
                    throw new Exception("Ошибка разбора части строки: " + line);

                indexTo++;
            }

            Tuple<string, string> res = new Tuple<string, string>(line, objectString);

            return res;
        }

        // currentString - часть строки лога
        // eventString - вся строка лога
        bool countPairSymbolOdd(string currentString, string eventString, string symbolLeft, string symbolRight)
        {

            bool isPair = false;
            bool isPreLastSymbolObject = false;
            //string[] strSymbolLeft = { symbolLeft };
            //string[] strSymbolRight = { symbolRight };
            //int countLeft = currentString.Split(strSymbolLeft, StringSplitOptions.None).Length - 1;
            //int countRight = currentString.Split(strSymbolRight, StringSplitOptions.None).Length - 1;
            int countLeft = currentString.Split(symbolLeft.ToCharArray()).Length - 1;
            int countRight = currentString.Split(symbolRight.ToCharArray()).Length - 1;
            int countQuotationMarks = currentString.Split('\"').Length - 1;

            isPair = countLeft == countRight && countQuotationMarks % 2 == 0;
            // проверка окончания строки
            if (currentString.Length == eventString.Length)
                isPreLastSymbolObject = true;
            else if (currentString.Length == 0)
                isPreLastSymbolObject = false;
            else
                //isPreLastSymbolObject = currentString.EndsWith(",");
                isPreLastSymbolObject = currentString.Substring(currentString.Length - 1, 1) == ",";

            return isPair && isPreLastSymbolObject;
        }

        public string[] ParseStringToArrayObject(string line)
        {
            List<string> listLine = new List<string>();

            // уберем последнюю запятую при ее наличии
            //if (line.EndsWith(","))
            // EndsWith потребляет значительно больше процессора чем Substring
            if (line.Substring(line.Length - 1, 1) == ",")
            {
                line = line.Substring(0, line.Length - 1);
            }

            // первую и последнюю скобки тоже уберем
            // StartsWith потребляет значительно больше процессора чем Substring
            //if (line.StartsWith("{"))
            if (line.Substring(0, 1) == "{")
                line = line.Substring(1, line.Length - 1);
            if (line.Substring(line.Length - 1, 1) == "}")
                line = line.Substring(0, line.Length - 1);

            string lineShort = line;
            int numberIndex = 0;
            
            while (lineShort.Length != 0)
            {              
                Tuple<string, string> splitShortLine = GetObject(lineShort);
                lineShort = splitShortLine.Item1;
                string objectValue = splitShortLine.Item2;

                lineShort = lineShort.Substring(objectValue.Length, lineShort.Length - objectValue.Length);

                //if (objectValue.EndsWith(","))
                if (objectValue.Substring(objectValue.Length - 1, 1) == ",")
                    objectValue = objectValue.Substring(0, objectValue.Length - 1);
                
                listLine.Add(objectValue);
                numberIndex++;
            }

            return listLine.ToArray();
        }

        public async Task ParseLineAsync(string line, DictLog dictLog)
        {
            await Task.Run(() => ParseLine(line, dictLog)).ConfigureAwait(true);
        }

        public void ParseLine(string line, DictLog dictLog)
        {
            //1) Дата и время в формате "yyyyMMddHHmmss", легко превращается в дату функцией Дата();
            //2) Статус транзакции – может принимать четыре значения "N" – "Отсутствует", "U" – "Зафиксирована", "R" – "Не завершена" и "C" – "Отменена";
            //3) Транзакция в формате записи из двух элементов преобразованных в шестнадцатеричное число 
            //  –   первый – число секунд с 01.01.0001 00:00:00 умноженное на 10000, 
            //      второй – номер транзакции;
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

            // {20190729165108,N,{0,0},1,1,2,433,3,I,"",0,{"P",{6,{"S","jenkins"},{"S","ROSSKO\pavel.makarov"}}},"",1,1,0,1,0,{0}},
            // {20190729165201,C,{2435928362a10,74c},1,1,2,433,5,I,\"\",0,{\"U\"},\"\",1,1,0,1,0,{0}},
            // { 20190729165201,U,{ 2435928362a10,74c},1,1,2,433,6,I,"",45,{ "R",21:94e038d547ded5ea11e9af4b2d5a712d},"тест",1,1,0,1,0,{ 0} },
            // {20190731120047,U,{2435984cb07f0,1dde2b},1,1,2,40,6,I,"",41,{"R",107:a49762e541d0f9ee11e2a921473faba2},"WMS_1NSK",1,1,0,1,0,{0}},
            // {20190726083202,U,{243587bd5e620,154a},1,1,2,252,10,I,"",6,{"R",17:b3c300505699197f11e631d8d789bc5c},"Клементьев А.А. (Станционная, 16/1)",1,1,0,2,0,{0}},
            // {20190812000019,N,{ 0,0},4,1,5,336800,10,I,"",247,{ "S","Отправка количества сканированийноменклатуры в сололайн"},"",1,1,0,1,0,{ 0}},
            // {20190812114310,N,{ 0,0},2,1,4,10357,38,E,"{ОбщийМодуль.СобытийнаяМодельФронтПривилигерованный.Модуль(1670)}: При загрузке Маршрутный лист ОТГАГ012099 от 06.08.2019 0:00:00 не найден документ Заявка на возврат товаров от покупателя ОТГАГ046985 от 05.08.2019 17:29:03		ВызватьИсключение СообщениеОбОшибке;",
            //  0,{ "S","{""TypeDocument"": ""МаршрутныйЛист"",""Presentation"": ""Маршрутный лист ОТГАГ012099 от 06.08.2019 0:00:00"",""GUIDWithType"": ""{\""#\"",ba77c4fb-bed9-42ce-be5c-5e5a70a68369,228:ae5800505699b66311e9b7ae029cee1b}"",""GUID"": ""029cee1b-b7ae-11e9-ae58-00505699b663"",""DeletionMark"": false,""Unload"": true,""Comment"": ""tms 06.08.2019 1:22:50"",""Number"": ""ОТГАГ012099"",""DateTime"": ""2019-08-06T00:00:00"",""NumberKIS"": ""ОТГАГ012099"",""DateTimeKIS"": ""2019-08-06T00:00:00"",""Manager"": """",""WarehouseName"": ""Мск_Основной"",""WarehouseCode"": ""ОТ0000014"",""Driver"": ""Луньков Алексей Михайлович"",""TransportVehicle"": ""HYNDAI HD 72"",""Left"": true,""Delivered"": true,""RouteNumber"": ""03"",""RoutePoints"": [{""GUIDDocument"": ""{\""#\"",eddd79d8-809c-497f-9c4a-5943afddda98,195:ae5800505699b66311e9b4fc2cc5830d}"",""Presentation"": ""Реализация товаров и услуг ОТГАГ193609 от 02.08.2019 15:04:39"",""Status"": ""Доставлен"",""TypeDocument"": ""РеализацияТоваровУслуг"",""WarehouseName"": ""Мск_Основной"",""WarehouseCode"": ""ОТ0000014""},{""GUIDDocument"": ""{\""#\"",eddd79d8-809c-497f-9c4a-5943afddda98,195:ae5800505699b66311e9b74765f37017}"",""Presentation"": ""Реализация товаров и услуг ОТГАГ195398 от 05.08.2019 13:08:15"",""Status"": ""Доставлен"",""TypeDocument"": ""РеализацияТоваровУслуг"",""WarehouseName"": ""Мск_Основной"",""WarehouseCode"": ""ОТ0000014""}],""headers"": {""message_id"": ""7c3d54c5-bb3a-465e-8329-ce1f7b0a0c34"",""owner_id"": ""5ade20ac-b9e8-42dc-9003-4122957339e8"",""timestamp"": 1565610184883,""event_owner"": """"}}"},
            //  "",1,3,0,3376,0,{0}},
            // {20191025103710,N,{0,0},369,6,1,499504,2,E,"Имя отчета: ""АнализПродаж""Установленные параметры:РазрешитьВсеФилиалы, значение: НетФилиалПоУмолчанию, значение: МассивПериод, значение: 25.10.2019 - 25.10.2019Установленные отборы:Ответственный, вид сравнения: Равно, значение: Семенов_Константин",0,{"U"},"",1,9,0,3925781,0,{0}},

            string[] arrLine = ParseStringToArrayObject(line);
            string[] data = ParseStringToArrayObject(arrLine[11]);
            string dataObject = "";

            // если символов 32 то это гуид объекта
            // иначае имя фонового задания
            if (data.Length > 1 && data[1] != null)
            {
                string patternUUID = "^[a-fA-F0-9]*:[a-fA-F0-9]{32}$";
                Regex regex = new Regex(patternUUID);
                MatchCollection matches = regex.Matches(data[1]);
                if (matches.Count != 0)
                    dataObject = GetGUIDByUUID(data[1]);
                else
                    dataObject = data[1];
            }

            this.indexOutputLog = new IndexOutputLog();
            this.indexOutputLog.Timestamp = ConvertStringToDateTime(arrLine[0]);
            this.indexOutputLog.Transaction = GetStatusTransaction(arrLine[1]);
            this.indexOutputLog.User = DictLog.GetValueFromDictionaryLog(dictLog, arrLine[3], "user");
            this.indexOutputLog.Computer = DictLog.GetValueFromDictionaryLog(dictLog, arrLine[4], "computer");
            this.indexOutputLog.Application = DictLog.GetValueFromDictionaryLog(dictLog, arrLine[5], "application");
            this.indexOutputLog.NumberConnection = int.Parse(arrLine[6].Length == 0 ? "0" : arrLine[6]);
            this.indexOutputLog.Event = LineLog.GetNameEvent(DictLog.GetValueFromDictionaryLog(dictLog, arrLine[7], "event"));
            this.indexOutputLog.Level = LineLog.GetLevel(arrLine[8]);
            this.indexOutputLog.Comment = arrLine[9].Trim('\"');
            this.indexOutputLog.Metadata = DictLog.GetValueFromDictionaryLog(dictLog, arrLine[10], "metadata");
            this.indexOutputLog.Presentation = arrLine[12].Trim('\"');
            this.indexOutputLog.Server = DictLog.GetValueFromDictionaryLog(dictLog, arrLine[13], "server");
            string strMainPort = DictLog.GetValueFromDictionaryLog(dictLog, arrLine[14], "mainPort");
            this.indexOutputLog.MainPort = int.Parse(strMainPort.Length == 0 ? "0" : strMainPort);
            string strAdditionalPort = DictLog.GetValueFromDictionaryLog(dictLog, arrLine[16], "additionalPort");
            this.indexOutputLog.AdditionalPort = int.Parse(strAdditionalPort.Length == 0 ? "0" : strAdditionalPort);
            this.indexOutputLog.NumberSession = int.Parse(arrLine[17].Length == 0 ? "0" : arrLine[17]);
            this.indexOutputLog.Data = dataObject;

            string[] dataTransaction = ParseStringToArrayObject(arrLine[2]);
            // дату транзакции пока опускаем
            //DateTime timestampTransaction = LineLog.getDateFromTimestamp1C(dataTransaction[0]);
            //if (timestampTransaction != new DateTime(1, 1, 1))
            //    this.indexOutputLog.Timestamp = timestampTransaction;
            this.indexOutputLog.NumberTransaction = Convert.ToInt64(dataTransaction[1], 16);
        }

        public static string GetGUIDByUUID(string uuid)
        {
            string guid = "";

            // uuid состоит из номера таблицы в которой хранятся данные и перевернутого гуида
            // вырежем номер таблицы
            int indexTo = uuid.IndexOf(":");
            string line = uuid.Remove(0, indexTo + 1);

            if (line.Length < 32)
                return guid;

            // перевернем 
            string s1 = line.Substring(24, 8);
            string s2 = line.Substring(20, 4);
            string s3 = line.Substring(16, 4);
            string s4 = line.Substring(0, 4);
            string s5 = line.Substring(4, 12);

            // соберем гуид
            guid = s1 + "-" + s2 + "-" + s3 + "-" + s4 + "-" + s5;

            return guid;
        }

        public static DateTime ConvertStringToDateTime(string timestamp)
        {
            string year = timestamp.Substring(0, 4);
            string month = timestamp.Substring(4, 2);
            string day = timestamp.Substring(6, 2);
            string hour = timestamp.Substring(8, 2);
            string min = timestamp.Substring(10, 2);
            string sec = timestamp.Substring(12, 2);
            DateTime date = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day), int.Parse(hour), int.Parse(min), int.Parse(sec), DateTimeKind.Local);

            return date;
        }

        public static DateTime GetDateFromTimestamp1C(string timestamp1C)
        {
            long datetimestamp = Convert.ToInt64(timestamp1C, 16) / 10000;
            DateTime timestamp = (new DateTime(0001, 1, 1, 0, 0, 0, 0, DateTimeKind.Local)).AddSeconds(datetimestamp);
            return timestamp;
        }

        public static string GetStatusTransaction(string name)
        {
            // "N" – "Отсутствует"
            // "U" – "Зафиксирована"
            // "R" – "Не завершена"
            // "C" – "Отменена"

            if (name == "N")
                return "Отсутствует";
            if (name == "U")
                return "Зафиксирована";
            if (name == "R")
                return "Не завершена";
            if (name == "C")
                return "Отменена";

            return "";
        }

        public static string GetLevel(string name)
        {
            // "I" – "Информация"
            // "E" – "Ошибки"
            // "W" – "Предупреждения"
            // "N" – "Примечания"

            if (name.Contains("I"))
                return "Информация";
            if (name.Contains("E"))
                return "Ошибка";
            if (name.Contains("W"))
                return "Предупреждение";
            if (name.Contains("N"))
                return "Примечание";

            return name;
        }

        public static string GetNameEvent(string name)
        {
            // _$Data$_
            if (name.Contains("_$Data$_.New"))
                return "Данные.Добавление";
            if (name.Contains("_$Data$_.Update"))
                return "Данные.Обновление";
            if (name.Contains("_$Data$_.Delete"))
                return "Данные.Удаление";
            if (name.Contains("_$Data$_.Post"))
                return "Данные.Запись";
            if (name.Contains("_$Data$_.UpdatePredefinedData"))
                return "Данные.ОбновлениеПредопределенныхДанных";
            if (name.Contains("_$Data$_.NewPredefinedData"))
                return "Данные.ДобавлениеПредопределенныхДанных";

            // "_$Job$_"
            if (name.Contains("_$Job$_.Start"))
                return "ФЗ.Старт";
            if (name.Contains("_$Job$_.Succeed"))
                return "ФЗ.УспешноеВыполнение";
            if (name.Contains("_$Job$_.Fail"))
                return "ФЗ.Ошибка";
            if (name.Contains("_$Job$_.Cancel"))
                return "ФЗ.Отменено";

            // _$Session$_
            if (name.Contains("_$Session$_.Start"))
                return "Сессия.Начало";
            if (name.Contains("_$Session$_.Authentication"))
                return "Сессия.Идентификация";
            if (name.Contains("_$Session$_.AuthenticationError"))
                return "Сессия.ОшибкаИдентификации";
            if (name.Contains("_$Session$_.Finish"))
                return "Сессия.Выход";
            if (name.Contains("_$Session$_.Begin"))
                return "Сессия.Вход";
            if (name.Contains("_$Session$_.UpdatePredefinedData"))
                return "Сессия.ОбновлениеПредопределенныхДанных";
            if (name.Contains("_$Session$_.NewPredefinedData"))
                return "Сессия.ДобавлениеПредопределенныхДанных";
            if (name.Contains("_$Session$_.ConfigExtensionApplyError"))
                return "Сессия.ОшибкаПримененияРасширения";

            // _$User$_
            if (name.Contains("_$User$_.New"))
                return "Пользователи.Добавление";
            if (name.Contains("_$User$_.Update"))
                return "Пользователи.Изменение";
            if (name.Contains("_$User$_.Delete"))
                return "Пользователи.Удаление";

            // _$InfoBase$_
            if (name.Contains("_$InfoBase$_.ConfigUpdate"))
                return "БД.ОбновлениеКонфигурации";
            if (name.Contains("_$InfoBase$_.DBConfigUpdate"))
                return "БД.ОбновлениеИБ";
            if (name.Contains("_$InfoBase$_.PredefinedDataUpdate"))
                return "БД.ОбновлениеПредопределенныхДанных";
            if (name.Contains("_$InfoBase$_.DBConfigExtensionUpdate"))
                return "БД.ОбновлениеИБРасширения";
            if (name.Contains("_$InfoBase$_.ConfigExtensionUpdate"))
                return "БД.ОбновлениеРасширения";

            // _$Transaction$_
            if (name.Contains("_$Transaction$_.Begin"))
                return "Транзакция.Начало";
            if (name.Contains("_$Transaction$_.Commit"))
                return "Транзакция.Фиксация";
            if (name.Contains("_$Transaction$_.Rollback"))
                return "Транзакция.Откат";

            return name;
        }
    }
}
