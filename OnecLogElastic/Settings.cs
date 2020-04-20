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
    public class Settings
    {
        [DataMember]
        public string AdressElastic { get; set; }
        [DataMember]
        public int PortElastic { get; set; }
        [DataMember]
        public string PathJournal { get; set; }
        [DataMember]
        public string NameIndexElastic { get; set; }
        [DataMember]
        public string ElasticUserName { get; set; }
        [DataMember]
        public string ElasticUserPassword { get; set; }
        [DataMember]
        public int NumberRecordsProcessedAtTime { get; set; }
        [DataMember]
        public bool DisplayAdditionalInformation { get; set; }

        public void Read()
        {
            // лог пишем в директорию с которой запущен сервис
            string fullFilename = AppDomain.CurrentDomain.BaseDirectory + "\\OnecLogElastic.json";
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(Settings));
            using (FileStream fsread = new FileStream(fullFilename, FileMode.OpenOrCreate))
            {
                Settings settingsFile = new Settings();
                if (fsread.Length != 0)
                    settingsFile = (Settings)jsonFormatter.ReadObject(fsread);

                this.AdressElastic = settingsFile.AdressElastic;
                this.PortElastic = settingsFile.PortElastic;
                this.PathJournal = settingsFile.PathJournal;
                this.NameIndexElastic = settingsFile.NameIndexElastic;
                this.ElasticUserName = settingsFile.ElasticUserName;
                this.ElasticUserPassword = settingsFile.ElasticUserPassword;
                this.DisplayAdditionalInformation = settingsFile.DisplayAdditionalInformation;

                // подключимся к эластику
                if (this.AdressElastic == null)
                    this.AdressElastic = "localhost";

                if (this.PortElastic == 0)
                    this.PortElastic = 9200;

                if (this.PathJournal == null)
                    this.PathJournal = @"C:\Program Files\1cv8\srvinfo\reg_1541";

                if (this.NameIndexElastic == null)
                    this.NameIndexElastic = "onesrj";

                if (this.NumberRecordsProcessedAtTime == 0)
                    this.NumberRecordsProcessedAtTime = 300;
            }
        }

        public void Write()
        {
            // лог пишем в директорию с которой запущен сервис
            string fullFilename = AppDomain.CurrentDomain.BaseDirectory + "\\OnecLogElastic.json";
            using (FileStream fswrite = new FileStream(fullFilename, FileMode.OpenOrCreate))
            {
                DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(Settings));
                jsonFormatter.WriteObject(fswrite, this);
            }
        }
    }
}
