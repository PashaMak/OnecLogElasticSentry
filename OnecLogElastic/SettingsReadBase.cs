using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace OnecLogElastic
{
    [DataContract]
    public class SettingsReadBase
    {
        [DataMember]
        public string LastRunTime { get; set; }
        [DataMember]
        public Dictionary<string, string> LastTimeReadLogBase { get; set; }

        public void Read()
        {
            // лог пишем в директорию с которой запущен сервис
            string fullFilename = AppDomain.CurrentDomain.BaseDirectory + "\\OnecLogElasticReadBase.json";
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(SettingsReadBase));
            using (FileStream fsread = new FileStream(fullFilename, FileMode.OpenOrCreate))
            {
                SettingsReadBase settingsFile = new SettingsReadBase();
                if (fsread.Length != 0)
                    settingsFile = (SettingsReadBase)jsonFormatter.ReadObject(fsread);

                this.LastRunTime = settingsFile.LastRunTime;
                this.LastTimeReadLogBase = settingsFile.LastTimeReadLogBase;

                if (this.LastRunTime == null)
                    this.LastRunTime = DateTime.Now.ToString("yyyyMMddHHmmss");

                if (this.LastTimeReadLogBase == null)
                    this.LastTimeReadLogBase = new Dictionary<string, string>();
            }
        }

        public void Write()
        {
            // лог пишем в директорию с которой запущен сервис
            string fullFilename = AppDomain.CurrentDomain.BaseDirectory + "\\OnecLogElasticReadBase.json";
            using (FileStream fswrite = new FileStream(fullFilename, FileMode.OpenOrCreate))
            {
                DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(SettingsReadBase));
                jsonFormatter.WriteObject(fswrite, this);
            }
        }
    }
}
