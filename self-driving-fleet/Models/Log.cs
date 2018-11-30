using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace self_driving_fleet.Models
{
    public class Log
    {
        public int CarID { get; set; }
        public int ClientID { get; set; }
        public DateTime RideStartTime { get; set; }
        public DateTime RideEndTime { get; set; }
        public double RideValue { get; set; }
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }
        public double DropoffLatitude { get; set; }
        public double DropoffLongitude { get; set; }
        public double DistanceDrivenSinceCharging { get; set; }
        public int TimesCharged { get; set; }
        public int LastDepotID { get; set; }

        public Log(int carID, int clientID, DateTime rideStartTime, DateTime rideEndTime, double rideValue, double pickupLatitude, double pickupLongitude, double dropoffLatitude, double dropoffLongitude, double distanceDrivenSinceCharging, int timesCharged, int lastDepotID)
        {
            CarID = carID;
            ClientID = clientID;
            RideStartTime = rideStartTime;
            RideEndTime = rideEndTime;
            RideValue = rideValue;
            PickupLatitude = pickupLatitude;
            PickupLongitude = pickupLongitude;
            DropoffLatitude = dropoffLatitude;
            DropoffLongitude = dropoffLongitude;
            DistanceDrivenSinceCharging = distanceDrivenSinceCharging;
            TimesCharged = timesCharged;
            LastDepotID = lastDepotID;
        }
    }
}
