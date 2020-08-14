using System.Collections.Generic;
using System.Configuration;

using TransitRealtime;

namespace gtfsrt_events_vp_current_status
{
    internal class EntityFactory
    {
        internal Dictionary<VehicleEntity, Entity> ProduceEntites(FeedMessage feedMessages)
        {
            var feedEntityList = feedMessages.Entities;
            var vehicleEntitySet = new Dictionary<VehicleEntity, Entity>();

            var includeEntitiesWithoutTrip = ConfigurationManager.AppSettings["IncludeEntitiesWithoutTrip"].ToUpper();

            foreach (var feedEntity in feedEntityList)
            {
                //if a trip id exists for this entity or if config parameter says to include entities without a trip id 
                //then do the following...else skip (discard) this entity
                if (feedEntity.Vehicle.Trip == null && !"TRUE".Equals(includeEntitiesWithoutTrip))
                    continue;

                var currentStopStatus = feedEntity.Vehicle.CurrentStatus.ToString();
                var tripId = feedEntity.Vehicle?.Trip?.TripId;
                var routeId = feedEntity.Vehicle?.Trip?.RouteId;
                var stopId = feedEntity.Vehicle.StopId;
                var stopSequence = feedEntity.Vehicle.CurrentStopSequence;
                var vehicletimeStamp = feedEntity.Vehicle.Timestamp;
                var vehicleId = feedEntity.Vehicle.Vehicle.Id;
                var vehicleLabel = feedEntity.Vehicle.Vehicle.Label;
                var fileStamp = feedMessages.Header.Timestamp;
                var startDate = feedEntity.Vehicle?.Trip?.StartDate;
                var directionId = feedEntity.Vehicle?.Trip?.DirectionId;
                var entity = new Entity(tripId, routeId, stopId, stopSequence, currentStopStatus, vehicletimeStamp, fileStamp, startDate, directionId);
                var vehicleEntity = new VehicleEntity
                                    {
                                        VehicleId = vehicleId,
                                        VehicleLabel = vehicleLabel,
                                        tripId = feedEntity.Vehicle?.Trip?.TripId
                                    };
                if (vehicleEntitySet.ContainsKey(vehicleEntity))
                {
                    vehicleEntitySet.Remove(vehicleEntity);
                }
                vehicleEntitySet.Add(vehicleEntity, entity);
            }
            return vehicleEntitySet;
        }
    }
}