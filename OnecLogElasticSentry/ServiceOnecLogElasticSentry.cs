using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace OnecLogElasticSentry
{
    public partial class ServiceOnecLogElasticSentry : ServiceBase
    {
        public ServiceOnecLogElasticSentry()
        {
            InitializeComponent();
            this.CanStop = true; // службу можно остановить
            this.CanPauseAndContinue = true; // службу можно приостановить и затем продолжить
            this.AutoLog = true; // служба может вести запись в лог
        }

        protected override void OnStart(string[] args)
        {
            Elastic.Run();
        }

        protected override void OnStop()
        {
        }
    }
}
