using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using GtfsRealtimeLib;
using gtfsrt_tripupdate_denormalized.DataAccess;


using log4net;
using log4net.Config;

using TransitRealtime;

namespace gtfsrt_tripupdate_denormalized
{
    public class TripUpdateService
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly List<string> AcceptedRoutes;

        public TripUpdateService()
        {
            var acceptedRoutes = ConfigurationManager.AppSettings["ACCEPTROUTE"].Trim();
            AcceptedRoutes = string.IsNullOrEmpty(acceptedRoutes) ? new List<string>()
                : acceptedRoutes.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList();
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
            var tripUpdates = new List<TripUpdateData>();

            foreach (var entity in feedMessage.Entities.Where(x => !AcceptedRoutes.Any() || (!string.IsNullOrEmpty(x.TripUpdate?.Trip?.RouteId) &&
                                                                 AcceptedRoutes.Contains(x.TripUpdate?.Trip?.RouteId))))
            {
                tripUpdates.AddRange(entity.TripUpdate.StopTimeUpdates
                                           .Select(stopTimeUpdate => new TripUpdateData
                                                                     {
                                                                         GtfsRealtimeVersion = feedMessage.Header.GtfsRealtimeVersion,
                                                                         Incrementality = feedMessage.Header.incrementality.ToString(),
                                                                         HeaderTimestamp = feedMessage.Header.Timestamp,
                                                                         FeedEntityId = entity.Id,
                                                                         VehicleTimestamp = entity.TripUpdate?.Timestamp,
                                                                         //TripDelay = ..., TripDelay not applicable
                                                                         TripId = entity.TripUpdate?.Trip?.TripId,
                                                                         TripStartDate = entity.TripUpdate?.Trip?.StartDate,
                                                                         TripStartTime = entity.TripUpdate?.Trip?.StartTime,
                                                                         RouteId = entity.TripUpdate?.Trip?.RouteId,
                                                                         DirectionId = entity.TripUpdate?.Trip?.DirectionId,
                                                                         TripScheduleRelationship =
                                                                             entity.TripUpdate?.Trip?.schedule_relationship.ToString(),
                                                                         StopSequence = stopTimeUpdate.StopSequence,
                                                                         StopId = stopTimeUpdate.StopId,
                                                                         StopScheduleRelationship = stopTimeUpdate.schedule_relationship.ToString(),
                                                                         PredictedArrivalTime = stopTimeUpdate.Arrival?.Time,
                                                                         PredictedArrivalDelay = stopTimeUpdate.Arrival?.Delay,
                                                                         PredictedArrivalUncertainty = stopTimeUpdate.Arrival?.Uncertainty,
                                                                         PredictedDepartureTime = stopTimeUpdate.Departure?.Time,
                                                                         PredictedDepartureDelay = stopTimeUpdate.Departure?.Delay,
                                                                         PredictedDepartureUncertainty = stopTimeUpdate.Departure?.Uncertainty,
                                                                         VehicleId = entity.TripUpdate?.Vehicle?.Id,
                                                                         VehicleLabel = entity.TripUpdate?.Vehicle?.Label,
                                                                         VehicleLicensePlate = entity.TripUpdate?.Vehicle?.LicensePlate
                                                                     }));
            }

            InsertTripUpdatesRows(tripUpdates);
        }

        public void Stop()
        {
            Thread.Sleep(1000);
            Environment.Exit(0);}

        readonly TripUpdateDataSet _tripUpdateDataSet = new TripUpdateDataSet();

        private void InsertTripUpdatesRows(List<TripUpdateData> tripUpdates)
        {
            if (!tripUpdates.Any())
            {
                Log.Debug("No trip updates to save...");
                return;
            }

            try
            {
                Log.Debug($"Trying to insert {tripUpdates.Count} trip update rows in database.");
                _tripUpdateDataSet.SaveTripUpdates(tripUpdates);
                Log.Debug($"Inserted {tripUpdates.Count} trip update rows in database.");
            }
            catch (Exception exception)
            {
                Log.Debug($"Failed to save data: {exception.Message}");
            }
        }
    }
}
