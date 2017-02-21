using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace TCPService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
#if DEBUG
            try
            {
                TCPService _tcpService = new TCPService();
                _tcpService.OnDebug();
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
#else
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new TCPService()
                };
                ServiceBase.Run(ServicesToRun);
            
#endif
        }
    }
}
