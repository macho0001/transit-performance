using System.Diagnostics.Eventing.Reader;
using System.ServiceProcess;

namespace gtfsrt_events_vp_current_status
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {


            if (args.Length > 0 && args[0] == "noservice")
            {

                var service = new gtfsrt_events_vp_current_status_service();
                gtfsrt_events_vp_current_status_service.StartGTFSRealtimeService();
            }
            else
            {
                var ServicesToRun = new ServiceBase[]
                                        {
                                            new gtfsrt_events_vp_current_status_service()
                                        };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
