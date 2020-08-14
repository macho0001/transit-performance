using System.ServiceProcess;

namespace gtfsrt_events_tu_latest_prediction
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "noservice")
            {

                gtfsrt_events_tu_latest_prediction_service.Start();
            }
            else
            {


                var ServicesToRun = new ServiceBase[] { new gtfsrt_events_tu_latest_prediction_service() };
                ServiceBase.Run(ServicesToRun);
            }

        }
    }
}
