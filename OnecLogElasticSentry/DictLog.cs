using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnecLogElasticSentry
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
    }
}
