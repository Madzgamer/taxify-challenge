using Self_driving_fleet.Models;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Itinero;
using self_driving_fleet;
using System.Collections.Concurrent;

namespace Self_driving_fleet
{
    public static class Support
    {

        private static Router router = null;
        public static void Test()
        {


            Stopwatch stopWatch = new Stopwatch();

            //Loading all clients to memory, downside - limited to RAM capacity

            stopWatch.Start();
            List<Client> clients = LoadClients();
            stopWatch.Stop();
            Console.WriteLine("It took {0} ms to read the file", stopWatch.ElapsedMilliseconds);

            stopWatch.Reset();

            //Measuring distances test
            Console.WriteLine("Starting distance measuring");

            stopWatch.Start();

            DistCalc method = DistCalc.FAST;

            List<double> distances = new List<double>();
            for (int i = 0; i < clients.Count; i++)
            {
                double distance = DistanceBetweenCoordinates(clients[i].StartLatitude, clients[i].StartLongitude, clients[i].EndLatitude, clients[i].EndLongitude, method);
                distances.Add(distance);
            }

            stopWatch.Stop();

            Console.WriteLine("It took {0} ms to calculate {1} distances using {2}", stopWatch.ElapsedMilliseconds, distances.Count, method);




            Console.WriteLine("Starting distance measuring");

            stopWatch.Start();

            int earthRadius = 6371000;
            for (int i = 0; i < 100; i++)
            {
                double startLatitudeRad = clients[i].StartLatitude.ToRadians();
                double startLongitude = clients[i].StartLongitude;
                double endLatitudeRad = clients[i].EndLatitude.ToRadians();
                double endLongitude = clients[i].EndLongitude;

                double x = (endLongitude - startLongitude).ToRadians() * Math.Cos((startLatitudeRad + endLatitudeRad) / 2);
                double y = (endLatitudeRad - startLatitudeRad);

                double distance1 = Math.Sqrt(x * x + y * y) * earthRadius;

                GeoCoordinate start = new GeoCoordinate(clients[i].StartLatitude, clients[i].StartLongitude);
                GeoCoordinate end = new GeoCoordinate(clients[i].EndLatitude, clients[i].EndLongitude);

                double distance2 = start.GetDistanceTo(end);

                Console.WriteLine("For client number {0} GeoCoordinate calculated {1} meters and mine calculated {2} meters", i, distance2, distance1);

            }

            stopWatch.Stop();



            Console.ReadLine();


            //Some testing calculations
            //1 - how many clients in each hour

            DateTime firstOrderTime = RoundToHour(clients[0].StartTime);

            //Hardcoded for 2 days (48 hours
            for (int i = 0; i < 48; i++)
            {

                DateTime counterStart = firstOrderTime.AddHours(i);
                DateTime counterEnd = firstOrderTime.AddHours(i + 1);
                Console.WriteLine("Counting clients from {0} to {1}", counterStart.ToString(), counterEnd.ToString());

                int count = clients.FindAll(x => x.IsBetweenDates(counterStart, counterEnd)).Count;
                Console.WriteLine("The count is " + count);


            }





            Console.WriteLine("Hello world!");
            Console.ReadLine();
        }

        static DateTime RoundToHour(DateTime dt)
        {
            long ticks = dt.Ticks + 18000000000;
            return new DateTime(ticks - ticks % 36000000000, dt.Kind);
        }

        public static List<Client> LoadClients(int maxclients = -1)
        {
			//Correct Culture for parsing
			string ciName = Thread.CurrentThread.CurrentCulture.Name;
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");

			List<Client> clients = new List<Client>();
            int line = 1;
            int loggingFreq = 100000;

            using (StreamReader sr = File.OpenText("robotex2.csv"))
            {
                string s = String.Empty;
                bool columns = true;

                Console.WriteLine("Adding clients from lines {0} to {1}", "2", loggingFreq - 1);
                int count = 0;
                while ((s = sr.ReadLine()) != null && count != maxclients)
                {
                    line++;
                    if (columns) { columns = false; continue; }

                    List<string> csvItem = s.Split(',').ToList();

                    if (line % loggingFreq == 0) Console.WriteLine("Adding clients from lines {0} to {1}", line, line + loggingFreq - 1);
                    Client client = new Client(DateTime.Parse(csvItem[0]), Double.Parse(csvItem[1]), Double.Parse(csvItem[2]), Double.Parse(csvItem[3]), Double.Parse(csvItem[4]), Double.Parse(csvItem[5]));

                    clients.Add(client);
                    client.Index = count;

                    count += 1;
                    //TODO - Update console window


                }
            }
			//Reset culture
			Thread.CurrentThread.CurrentCulture = new CultureInfo(ciName);

            return clients;
        }


		public static ConcurrentDictionary<int, Client> LoadClientsTS(int maxclients = -1)
		{
			//Correct Culture for parsing
			string ciName = Thread.CurrentThread.CurrentCulture.Name;
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");

			ConcurrentDictionary<int, Client> clients = new ConcurrentDictionary<int, Client>();
			int line = 1;
			int loggingFreq = 100000;

			using (StreamReader sr = File.OpenText("robotex2.csv"))
			{
				string s = String.Empty;
				bool columns = true;

				Console.WriteLine("Adding clients from lines {0} to {1}", "2", loggingFreq - 1);
				int count = 0;
                //double totalValue = 0d;
                while ((s = sr.ReadLine()) != null && count != maxclients)
				{
					line++;
					if (columns) { columns = false; continue; }

					List<string> csvItem = s.Split(',').ToList();

					if (line % loggingFreq == 0) Console.WriteLine("Adding clients from lines {0} to {1}", line, line + loggingFreq - 1);
					Client client = new Client(DateTime.Parse(csvItem[0]), Double.Parse(csvItem[1]), Double.Parse(csvItem[2]), Double.Parse(csvItem[3]), Double.Parse(csvItem[4]), Double.Parse(csvItem[5]));

					clients.TryAdd(count, client);
					client.Index = count;
                    //totalValue += client.RideValue;

					count += 1;
					//TODO - Update console window


				}
                //Console.WriteLine("Average ride value was {0}", totalValue / (double)count);
			}
			//Reset culture
			Thread.CurrentThread.CurrentCulture = new CultureInfo(ciName);

			return clients;
		}


		public static List<Client> ResolveClientPoints(List<Client> clients)
        {
            List<RouterPoint> ResolvedPoints = new List<RouterPoint>();

            int i = 0;
            while(i < clients.Count)
            {
                ResolveResult resultResolveStart = ResolvePoint((float)clients[i].StartLatitude, (float)clients[i].StartLongitude, ResolvedPoints);
                if (resultResolveStart.ResolvedPoint == null)
                {
                    clients.Remove(clients[i]);
                    continue;
                }
                if(!resultResolveStart.AlreadyResolved) ResolvedPoints.Add(resultResolveStart.ResolvedPoint);
                ResolveResult resultResolveEnd = ResolvePoint((float)clients[i].EndLatitude, (float)clients[i].EndLongitude,ResolvedPoints);
                if (resultResolveEnd.ResolvedPoint == null)
                {
                    clients.Remove(clients[i]);
                    continue;
                }
                if(!resultResolveEnd.AlreadyResolved) ResolvedPoints.Add(resultResolveEnd.ResolvedPoint);

                clients[i].ResolvedStart = resultResolveStart.ResolvedPoint;
                clients[i].ResolvedEnd = resultResolveEnd.ResolvedPoint;
                i++;
            }

            Console.WriteLine("Got a total of {0}", ResolvedPoints.Count);
            return clients;
        }

        public static List<Depot> ResolveDepotPoints(List<Depot> depots)
        {
            int i = 0;
            while(i < depots.Count)
            {
                RouterPoint resolveLocation = ResolveDepotPoint((float)depots[i].Latitude, (float)depots[i].Longitude).ResolvedPoint;
                if (resolveLocation == null)
                {
                    Console.WriteLine("ERROR: Depot NO. {2} on coordinates: {0}, {1} is UNREACHABLE ", depots[i].Latitude, depots[i].Longitude, depots.FindIndex(x =>  x == depots[i]));
                    depots.Remove(depots[i]);
                    continue;
                }
                depots[i].ResolvedLocation = resolveLocation;
                i++;
            }

            return depots;
        }

        public static ResolveResult ResolvePoint(float latitude, float longitude, List<RouterPoint> alreadyResolvedPoints = null)
        {
            RouterPoint routerPoint = null;

            //Check if the point has already been calculated
            bool alreadyCalculated = false;
            if (alreadyResolvedPoints != null && alreadyResolvedPoints.Count != 0) 
            {
                foreach (RouterPoint point in alreadyResolvedPoints)
                {
                    if (Support.DistanceBetweenCoordinates(point.Latitude, point.Longitude, latitude, longitude, DistCalc.FAST) < 50d)
                    {
                        alreadyCalculated = true;
                        routerPoint = point;
                        break;
                    }
                }
            }
            if (alreadyCalculated) return new ResolveResult(true,routerPoint);

            //Otherwise, calculate the point yourself
            if (router == null)
            {
                var routerDb = new RouterDb();
                using (var stream = new FileInfo(@"harjumaa.routerdb").OpenRead())
                {
                    routerDb = RouterDb.Deserialize(stream);
                    //routerDb.AddContracted(routerDb.GetSupportedProfile("car"));
                }
                router = new Router(routerDb);
            }

            Result<RouterPoint> result = router.TryResolveConnected(Itinero.Osm.Vehicles.Vehicle.Car.Shortest(), latitude,longitude);
            if (!result.IsError)
            {
                routerPoint = result.Value;
            }
            return new ResolveResult(false, routerPoint);
        }

        public static ResolveResult ResolveDepotPoint(float latitude, float longitude)
        {
            RouterPoint routerPoint = null;

            //Otherwise, calculate the point yourself
            if (router == null)
            {
                var routerDb = new RouterDb();
                using (var stream = new FileInfo(@"harjumaa.routerdb").OpenRead())
                {
                    routerDb = RouterDb.Deserialize(stream);
                    //routerDb.AddContracted(routerDb.GetSupportedProfile("car"));
                }
                router = new Router(routerDb);
            }

            Result<RouterPoint> result = router.TryResolveConnected(Itinero.Osm.Vehicles.Vehicle.Car.Shortest(), new Itinero.LocalGeo.Coordinate(latitude, longitude),2000,250);
            if (!result.IsError)
            {
                routerPoint = result.Value;
            }
            return new ResolveResult(false, routerPoint);
        }

        public static List<Depot> LoadDepots()
		{
			//Correct Culture for parsing
			string ciName = Thread.CurrentThread.CurrentCulture.Name;
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");

			List<Depot> depots = new List<Depot>();
			int line = 1;
			int loggingFreq = 10;

			using (StreamReader sr = File.OpenText("robotex-depos.csv"))
			{
				string s = String.Empty;
				bool columns = true;

				Console.WriteLine("Adding depots from lines {0} to {1}", "2", loggingFreq - 1);
				int count = 0;
				while ((s = sr.ReadLine()) != null)
				{
					line++;
					if (columns) { columns = false; continue; }

					List<string> csvItem = s.Split(',').ToList();

					if (line % loggingFreq == 0) Console.WriteLine("Adding depots from lines {0} to {1}", line, line + loggingFreq - 1);
					Depot depot = new Depot(line-2, Double.Parse(csvItem[0]), Double.Parse(csvItem[1]));

					depots.Add(depot);

					count += 1;
					//TODO - Update console window


				}
			}
			//Reset culture
			Thread.CurrentThread.CurrentCulture = new CultureInfo(ciName);

			return depots;
		}

        public static List<Hotspot> CalculateHotspots(ConcurrentDictionary<int, Client> clients, double stepIncrementInHours, float discardEdgePercentage = 0.01f)
        {
            List<Hotspot> hotspots = new List<Hotspot>();
            DateTime startTime = clients[0].StartTime;
            DateTime endTime = clients[clients.Count - 1].StartTime;

            DateTime calculationStart = startTime;
            while (true)
            {
                DateTime calculationEnd = calculationStart.AddHours(stepIncrementInHours);
                if(DateTime.Compare(endTime, calculationEnd) < 0)
                {
                    calculationEnd = endTime;
                    hotspots.Add(new Hotspot(calculationStart, calculationEnd, Database.CalculateHotspot(calculationStart, calculationEnd, clients, discardEdgePercentage)));
                    break;
                }
                hotspots.Add(new Hotspot(calculationStart, calculationEnd, Database.CalculateHotspot(calculationStart, calculationEnd, clients, discardEdgePercentage)));
                calculationStart = calculationStart.AddHours(stepIncrementInHours);

            }

            return hotspots;
        }

        public static double DistanceBetweenCoordinates(double lat1, double long1, double lat2, double long2, DistCalc distCalc)
        {
            if (distCalc == DistCalc.FAST)
            {
                int earthRadius = 6371000;
                double lat1rad = lat1.ToRadians();
                double lat2rad = lat2.ToRadians();

                double x = (long2 - long1).ToRadians() * Math.Cos((lat1rad + lat2rad) / 2);
                double y = (lat2rad - lat1rad);

                return Math.Sqrt(x * x + y * y) * earthRadius;
            }

            if (distCalc == DistCalc.ACCURATE)
            {
                GeoCoordinate start = new GeoCoordinate(lat1, long1);
                GeoCoordinate end = new GeoCoordinate(lat2, long2);

                return start.GetDistanceTo(end);
            }

            return 0;
        }

		public static double clientDistance(Client client, DistCalc distCalc)
		{
			return DistanceBetweenCoordinates(
						client.StartLatitude,
						client.StartLongitude,
						client.EndLatitude,
						client.EndLongitude,
						distCalc);
		}
    }

    /// <summary>
    /// Convert to Radians.
    /// </summary>
    /// <param name="val">The value to convert to radians</param>
    /// <returns>The value in radians</returns>
    public static class NumericExtensions
    {
        public static double ToRadians(this double val)
        {
            return (Math.PI / 180) * val;
        }
    }

    public enum DistCalc
    {
        FAST,
        ACCURATE
    }

    public class ResolveResult
    {
        public bool AlreadyResolved;
        public RouterPoint ResolvedPoint;

        public ResolveResult(bool alreadyResolved, RouterPoint resolvedPoint)
        {
            AlreadyResolved = alreadyResolved;
            ResolvedPoint = resolvedPoint;
        }
    }

    public static class AIWeights
    {
        public static int NumberOfPossibleRides = 15;
        public static double DistFromCenterWeight = -0.2d;
        public static double RideValueWeight = 500d;
        public static double WaitTimeWeight = 1d;
    }

}
