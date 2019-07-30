using Elasticsearch.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FormOnecLogElasticSentry
{
    class ListEnumDictionary
    {
        [StringEnum]
        public enum EnumDictionary : int
        {
            //1 – пользователи;
            //2 – компьютеры;
            //3 – приложения;
            //4 – события;
            //5 – метаданные;
            //6 – серверы;
            //7 – основные порты;
            //8 – вспомогательные порты.
            Users = 1,
            Computer = 2,
            Applicvation = 3,
            Event = 4,
            Metadata = 5,
            Server= 6,
            MainPort= 7,
            AdditionalPort = 8
        }

        public string IntToString(int value)
        {
            switch (value)
            {
                case 1: return "Users";
                case 2: return "Computer";
                case 3: return "Applicvation";
                case 4: return "Event";
                case 5: return "Metadata";
                case 6: return "Server";
                case 7: return "MainPort";
                case 8: return "AdditionalPort";
            }

            return "";
        }

        public string ValueString(EnumDictionary value)
        {
            return value.ToString();
        }

    }
}
