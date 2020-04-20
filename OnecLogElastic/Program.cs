using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OnecLogElastic
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main()
        {
#if DEBUG
            Elastic elastic = new Elastic();
            Thread myThread = new Thread(new ThreadStart(elastic.RunTheard));
            myThread.Start();
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ServiceOnecLogElastic()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }
    }
}
