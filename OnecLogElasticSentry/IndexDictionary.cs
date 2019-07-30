using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FormOnecLogElasticSentry
{
    class IndexDictionary
    {
        public int Id { get; set; }
        public string guid_base { get; set; }
        public string value { get; set; }
        public string event_string {
            get {
                return new ListEnumDictionary().IntToString(this.type);
            }
        }
        public int type { get; set; }
    }
}
