using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OnecLogElasticSentry
{
    [DataContract]
    class Settings
    {
        [DataMember]
        public DateTime date_time { get; set; }
        [DataMember]
        public String timestamp { get; set; }
        [DataMember]
        public string adress_elastic { get; set; }
        [DataMember]
        public int port_elastic { get; set; }
        [DataMember]
        public string path_journal { get; set; }
    }
}
