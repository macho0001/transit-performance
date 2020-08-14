using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using GtfsRealtimeLib;
using gtfsrt_vehicleposition_denormalized.DataAccess;


using log4net;
using log4net.Config;

using TransitRealtime;

namespace gtfsrt_vehicleposition_denormalized
{
    internal class VehiclePositionService
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly List<string> AcceptedRoutes;

        public VehiclePositionService()
        {
            var acceptedRoutes = ConfigurationManager.AppSettings["ACCEPTROUTE"].Trim();
            AcceptedRoutes = acceptedRoutes.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        public void Start()
        {
            try
            {
                XmlConfigurator.Configure();
                Log.Info("Program started");

                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                Log.Info($"Enabled Protocols: {ServicePointManager.SecurityProtocol}");

                Thread.Sleep(1000);

                var data = new GtfsData(Log);
                data.NewFeedMessage += Data_NewFeedMessage;

                var feedMessageThread = new Thread(data.GetData);
                feedMessageThread.Start();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.InnerException);
                Log.Error(e.StackTrace);

                Thread.Sleep(1000);
                Environment.Exit(1);
            }
        }

        private void Data_NewFeedMessage(FeedMessage feedMessage)
        {
            var vehiclePositions = new List<VehiclePositionData>();

            foreach (var entity in feedMessage.Entities.Where(x => !AcceptedRoutes.Any() || (!string.IsNullOrEmpty(x.Vehicle?.Trip?.RouteId) &&
                                                                                           AcceptedRoutes.Contains(x.Vehicle?.Trip?.RouteId))))
            {
                vehiclePositions.Add(new VehiclePositionData
                                     {
                                         gtfs_realtime_version = feedMessage.Header.GtfsRealtimeVersion,
                                         incrementality = feedMessage.Header.incrementality.ToString(),
                                         header_timestamp = feedMessage.Header.Timestamp,
                                         feed_entity_id = entity.Id,
                                         trip_id = entity.Vehicle?.Trip?.TripId,
                                         route_id = entity.Vehicle?.Trip?.RouteId,
                                         direction_id = entity.Vehicle?.Trip?.DirectionId,
                                         trip_start_date = entity.Vehicle?.Trip?.StartDate,
                                         trip_start_time = entity.Vehicle?.Trip?.StartTime,
                                         trip_schedule_relationship = entity.Vehicle?.Trip?.schedule_relationship.ToString(),
                                         vehicle_id = entity.Vehicle?.Vehicle?.Id,
                                         vehicle_label = entity.Vehicle?.Vehicle?.Label,
                                         vehicle_license_plate = entity.Vehicle?.Vehicle?.LicensePlate,
                                         vehicle_timestamp = entity.Vehicle?.Timestamp,
                                         current_stop_sequence = entity.Vehicle?.CurrentStopSequence,
                                         current_status = entity.Vehicle?.CurrentStatus.ToString(),
                                         stop_id = entity.Vehicle?.StopId,
                                         congestion_level = entity.Vehicle?.congestion_level.ToString(),
                                         occupancy_status = entity.Vehicle?.occupancy_status.ToString(), 
                                         latitude = entity.Vehicle?.Position?.Latitude,
                                         longitude = entity.Vehicle?.Position?.Longitude,
                                         bearing = entity.Vehicle?.Position?.Bearing,
                                         odometer = entity.Vehicle?.Position?.Odometer,
                                         speed = entity.Vehicle?.Position?.Speed
                                     });
            }

            InsertVehiclePositionsRows(vehiclePositions);
        }

        readonly VehiclePositionsDataSet _dataSet = new VehiclePositionsDataSet();

        private void InsertVehiclePositionsRows(List<VehiclePositionData> vehiclePositions)
        {
            if (!vehiclePositions.Any())
            {
                Log.Debug("No vehicle positions to save...");
                return;
            }

            try
            {
                Log.Debug($"Trying to insert {vehiclePositions.Count} vehicle position rows in database.");
                _dataSet.SaveData(vehiclePositions);
                Log.Debug($"Inserted {vehiclePositions.Count} vehicle position rows in database.");
            }
            catch (Exception exception)
            {
                Log.Debug($"Failed to save data: {exception.Message}");
            }
        }
        public void Stop()
        {
            Thread.Sleep(1000);
            Environment.Exit(0);
        }
    }
}
