using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestneOnecLogElastic
{
    [TestClass]
    public class UnitTest1
    {
        // {20190729165108,N,{0,0},1,1,2,433,3,I,"",0,{"P",{6,{"S","jenkins"},{"S","ROSSKO\pavel.makarov"}}},"",1,1,0,1,0,{0}},
        // {20190729165201,C,{2435928362a10,74c},1,1,2,433,5,I,\"\",0,{\"U\"},\"\",1,1,0,1,0,{0}},
        // { 20190729165201,U,{ 2435928362a10,74c},1,1,2,433,6,I,"",45,{ "R",21:94e038d547ded5ea11e9af4b2d5a712d},"тест",1,1,0,1,0,{ 0} },
        // {20190731120047,U,{2435984cb07f0,1dde2b},1,1,2,40,6,I,"",41,{"R",107:a49762e541d0f9ee11e2a921473faba2},"WMS_1NSK",1,1,0,1,0,{0}},
        // {20190726083202,U,{243587bd5e620,154a},1,1,2,252,10,I,"",6,{"R",17:b3c300505699197f11e631d8d789bc5c},"Клементьев А.А. (Станционная, 16/1)",1,1,0,2,0,{0}},
        // {20190812000019,N,{ 0,0},4,1,5,336800,10,I,"",247,{ "S","Отправка количества сканированийноменклатуры в сололайн"},"",1,1,0,1,0,{ 0}},
        // {20191025103710,N,{0,0},369,6,1,499504,2,E,"Имя отчета: ""АнализПродаж""Установленные параметры:РазрешитьВсеФилиалы, значение: НетФилиалПоУмолчанию, значение: МассивПериод, значение: 25.10.2019 - 25.10.2019Установленные отборы:Ответственный, вид сравнения: Равно, значение: Семенов_Константин",0,{"U"},"""",1,9,0,3925781,0,{0}},

        [TestMethod]
        public void GetObjectDoubleQuotationMarks()
        {
            string line = @"{20191025103710,N,{0,0},369,6,1,499504,2,E,""Имя отчета: """"АнализПродаж""""Установленные: 25.10.2019 - 25.10.2019 Установленные отборы:Ответственный, вид сравнения: Равно, значение: Семенов_Константин"",0,{""U""},"""",1,9,0,3925781,0,{0}},";
            string message = @"""Имя отчета: """"АнализПродаж""""Установленные: 25.10.2019 - 25.10.2019 Установленные отборы:Ответственный, вид сравнения: Равно, значение: Семенов_Константин""";
            
            OnecLogElastic.ForTest forTest = new OnecLogElastic.ForTest();
            string[] arr = forTest.ParseStringToArrayObject(line);

            Assert.AreEqual("20191025103710", arr[0]);
            Assert.AreEqual("N", arr[1]);
            Assert.AreEqual("{0,0}", arr[2]);
            Assert.AreEqual("369", arr[3]);
            Assert.AreEqual("6", arr[4]);
            Assert.AreEqual("1", arr[5]);
            Assert.AreEqual("499504", arr[6]);
            Assert.AreEqual("2", arr[7]);
            Assert.AreEqual("E", arr[8]);
            Assert.AreEqual(message, arr[9]);
            Assert.AreEqual("0", arr[10]);
            Assert.AreEqual(@"{""U""}", arr[11]);
            Assert.AreEqual("\"\"", arr[12]);
            Assert.AreEqual("1", arr[13]);
            Assert.AreEqual("9", arr[14]);
            Assert.AreEqual("0", arr[15]);
            Assert.AreEqual("3925781", arr[16]);
            Assert.AreEqual("0", arr[17]);
            Assert.AreEqual("{0}", arr[18]);
        }

        [TestMethod]
        public void GetObjectNormal()
        {
            string line = "{20190729165108,N,{0,0},1,1,2,433,3,I,\"\",0,{\"P\",{6,{\"S\",\"jenkins\"},{\"S\",\"ROSSKO\\pavel.makarov\"}}},\"\",1,1,0,1,0,{0}},";
            string message = "{\"P\",{6,{\"S\",\"jenkins\"},{\"S\",\"ROSSKO\\pavel.makarov\"}}}";

            OnecLogElastic.ForTest forTest = new OnecLogElastic.ForTest();
            string[] arr = forTest.ParseStringToArrayObject(line);

            Assert.AreEqual("20190729165108", arr[0]);
            Assert.AreEqual("N", arr[1]);
            Assert.AreEqual("{0,0}", arr[2]);
            Assert.AreEqual("1", arr[3]);
            Assert.AreEqual("1", arr[4]);
            Assert.AreEqual("2", arr[5]);
            Assert.AreEqual("433", arr[6]);
            Assert.AreEqual("3", arr[7]);
            Assert.AreEqual("I", arr[8]);
            Assert.AreEqual("\"\"", arr[9]);
            Assert.AreEqual("0", arr[10]);
            Assert.AreEqual(message, arr[11]);
            Assert.AreEqual("\"\"", arr[12]);
            Assert.AreEqual("1", arr[13]);
            Assert.AreEqual("1", arr[14]);
            Assert.AreEqual("0", arr[15]);
            Assert.AreEqual("1", arr[16]);
            Assert.AreEqual("0", arr[17]);
            Assert.AreEqual("{0}", arr[18]);
        }
        
        [TestMethod]
        public void GetObjectLeftCurlyBrace()
        {
            string line = "{20191027090353,N,{0,0},1,1,2,328,22,E,\"{ВнешняяОбработка.ВыгрузитьВВМС.Форма.Форма.Форма(11)}: Ошибка, лишний символ {\",0,{ \"U\"},\"\",1,1,0,2,0,{0}}";
            string message = "\"{ВнешняяОбработка.ВыгрузитьВВМС.Форма.Форма.Форма(11)}: Ошибка, лишний символ {\"";

            OnecLogElastic.ForTest forTest = new OnecLogElastic.ForTest();
            string[] arr = forTest.ParseStringToArrayObject(line);

            Assert.AreEqual("20191027090353", arr[0]);
            Assert.AreEqual("N", arr[1]);
            Assert.AreEqual("{0,0}", arr[2]);
            Assert.AreEqual("1", arr[3]);
            Assert.AreEqual("1", arr[4]);
            Assert.AreEqual("2", arr[5]);
            Assert.AreEqual("328", arr[6]);
            Assert.AreEqual("22", arr[7]);
            Assert.AreEqual("E", arr[8]);
            Assert.AreEqual(message, arr[9]);
            Assert.AreEqual("0", arr[10]);
            Assert.AreEqual("{ \"U\"}", arr[11]);
            Assert.AreEqual("\"\"", arr[12]);
            Assert.AreEqual("1", arr[13]);
            Assert.AreEqual("1", arr[14]);
            Assert.AreEqual("0", arr[15]);
            Assert.AreEqual("2", arr[16]);
            Assert.AreEqual("0", arr[17]);
            Assert.AreEqual("{0}", arr[18]);
        }

        [TestMethod]
        public void GetObjectInnerObject()
        {
            string line = "{6,{\"S\",\"jenkins\"},{\"S\",\"ROSSKO\\pavel.makarov\"}}";

            OnecLogElastic.ForTest forTest = new OnecLogElastic.ForTest();
            string[] arr = forTest.ParseStringToArrayObject(line);

            Assert.AreEqual("6", arr[0]);
            Assert.AreEqual("{\"S\",\"jenkins\"}", arr[1]);
            Assert.AreEqual("{\"S\",\"ROSSKO\\pavel.makarov\"}", arr[2]);
 
        }

        [TestMethod]
        public void GetObjectDictionaryLog()
        {
            string line = "{5,d6c7fbe0-27c7-491f-85cd-d852bb8312bf,\"Справочник.Номенклатура\",1},";

            OnecLogElastic.ForTest forTest = new OnecLogElastic.ForTest();
            string[] arr = forTest.ParseStringToArrayObject(line);

            Assert.AreEqual("5", arr[0]);
            Assert.AreEqual("d6c7fbe0-27c7-491f-85cd-d852bb8312bf", arr[1]);
            Assert.AreEqual("\"Справочник.Номенклатура\"", arr[2]);
            Assert.AreEqual("1", arr[3]);

        }


        [TestMethod]
        public void GetObjectEmptyData()
        {
            string line = "{20191030000000,C,{2436bb95f1800,3d},0,1,4,41523,6,I,\"\",0,{\"U\"},\"\",1,1,1,27514,0,{0}},";

            OnecLogElastic.ForTest forTest = new OnecLogElastic.ForTest();
            string[] arr = forTest.ParseStringToArrayObject(line);

            Assert.AreEqual("20191030000000", arr[0]);
            Assert.AreEqual("C", arr[1]);
            Assert.AreEqual("{2436bb95f1800,3d}", arr[2]);
            Assert.AreEqual("0", arr[3]);
            Assert.AreEqual("1", arr[4]);
            Assert.AreEqual("4", arr[5]);
            Assert.AreEqual("41523", arr[6]);
            Assert.AreEqual("6", arr[7]);
            Assert.AreEqual("I", arr[8]);
            Assert.AreEqual("\"\"", arr[9]);
            Assert.AreEqual("0", arr[10]);
            Assert.AreEqual("{\"U\"}", arr[11]);
            Assert.AreEqual("\"\"", arr[12]);
            Assert.AreEqual("1", arr[13]);
            Assert.AreEqual("1", arr[14]);
            Assert.AreEqual("1", arr[15]);
            Assert.AreEqual("27514", arr[16]);
            Assert.AreEqual("0", arr[17]);
            Assert.AreEqual("{0}", arr[18]);
        }
    }
}
