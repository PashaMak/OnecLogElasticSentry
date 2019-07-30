using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace OnecLogElasticSentry
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main()
        {
        #if DEBUG
            new Test();
        #else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ServiceOnecLogElasticSentry()
            };
            ServiceBase.Run(ServicesToRun);
        #endif
        }
    }
}
