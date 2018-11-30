using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using Self_driving_fleet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace self_driving_fleet
{
    static class Database
    {
        public static bool GenerateNewRouteDB(string filename, string output)
        {
            Console.WriteLine("Creating RouterDB file from openstreetmap data");
            var routerDb = new RouterDb();
            using (var stream = new FileInfo(filename).OpenRead())
            {
                // create the network for cars only.
                routerDb.LoadOsmData(stream, Vehicle.Car);
                routerDb.AddContracted(routerDb.GetSupportedProfile("car"));
            }

            // write the routerdb to disk.
            using (var stream = new FileInfo(output).Open(FileMode.Create))
            {
                routerDb.Serialize(stream);
            }

            Console.WriteLine("File created");
            return true;
        }

        public static Coordinates CalculateHotspot(DateTime startTime, DateTime endTime, ConcurrentDictionary<int, Client> clients, float discardEdgePercentage = 0.01f)
        {
            double latitude = 0d, longitude = 0d;
            List<KeyValuePair<int, Client>> betweenTimePeriod = clients.ToList().FindAll(x => DateTime.Compare(startTime, x.Value.StartTime) <= 0 && DateTime.Compare(endTime, x.Value.StartTime) >= 0);
			int discardedFromEnds = (int)(betweenTimePeriod.Count * discardEdgePercentage);
            int totalCount = 0;
            for (int i = discardedFromEnds; i < betweenTimePeriod.Count-discardedFromEnds; i++)
            {
                latitude += betweenTimePeriod[i].Value.StartLatitude;
                longitude += betweenTimePeriod[i].Value.StartLongitude;
                totalCount += 1;
            }

            latitude = latitude / totalCount;
            longitude = longitude / totalCount;

            return new Coordinates((float)latitude, (float)longitude);
        }
    }

    public class Coordinates
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }

        public Coordinates(float latitude, float longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }

    public class Hotspot
    {
        public DateTime TimeFrom { get; set; }
        public DateTime TimeTo { get; set; }
        public Coordinates Coordinates { get; set; }

        public Hotspot(DateTime timeFrom, DateTime timeTo, Coordinates coordinates)
        {
            TimeFrom = timeFrom;
            TimeTo = timeTo;
            Coordinates = coordinates;
        }
    }
}
