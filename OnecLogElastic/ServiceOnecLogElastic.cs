
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace OnecLogElastic
{
    public partial class ServiceOnecLogElastic: ServiceBase
    {
        private static System.Timers.Timer timer = new System.Timers.Timer();

        public ServiceOnecLogElastic()
        {
            InitializeComponent();
            this.CanStop = true; // службу можно остановить
            this.CanPauseAndContinue = true; // службу можно приостановить и затем продолжить
            this.AutoLog = true; // служба может вести запись в лог
        }

        protected override void OnStart(string[] args)
        {
            // запускаем таймер для периодического выполнения
            timer.Interval = 1000;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            try
            {
                timer.Stop();
                // запускаем в отдельном потоке
                Elastic elastic = new Elastic();
                Thread myThread = new Thread(new ThreadStart(elastic.RunTheard));
                myThread.Start();
                myThread.Join();
                timer.Start();
            }
            catch (Exception e)
            {
                Log.AddRecord("RunService", e.Message);
            }
        }

        protected override void OnStop()
        {
        }

        public void StartAndStop(string[] args)
        {
            this.OnStart(args);
            this.OnStop();
        }

    }
}
