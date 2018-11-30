using Itinero;
using Itinero.Osm.Vehicles;
using self_driving_fleet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using self_driving_fleet.Models;

namespace Self_driving_fleet.Models
{
    public class Car
    {
        public int ID { get; set; }
        public DrivingState State { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public RouterPoint CurrentResolvedLocation { get; set; }

        //public double DestLatitude { get; set; }
        //public double DestLongitude { get; set; }

        public float ChargeLeft { get; set; } //In km
        public int TimesCharged { get; set; }
        public int LastDepotID { get; set; }

        const float fullCharge = 200f; // in km
        const float critcalCharge = 25f; // in km
        const float carSpeed = 50f; // in km/h
        const float carSpeedMeter = carSpeed / 3.6f;
        const float chargingSpeed = 2f; //in hours to 100% charge
        const int maxPossibleDestinations = 50;

        public List<Client> ClientsOnBoard { get; set; }

        public DateTime LocalTime { get; set; }
        public float MoneyEarned { get; set; }

        private List<Depot> Depots;
        private ConcurrentDictionary<int, Client> Clients;
        private List<Hotspot> Hotspots;

        private List<WeightedClient> CyclePotentialClients;
		private int FirstIndex;
		public int CompletedClients { get; set; }

        private int ClientCount { get; set; }

        public List<Log> Logs { get; set; }

        private static Router Router;

        public Car(int id, Depot depot, DateTime startTime, List<Depot> depots, ConcurrentDictionary<int, Client> clients, List<Hotspot> hotspots, Router router){
            ID = id;
            State = DrivingState.WAITING;
            Latitude = depot.Latitude;
            Longitude = depot.Longitude;
            CurrentResolvedLocation = Support.ResolvePoint((float)Latitude, (float)Longitude).ResolvedPoint;
            //DestLatitude = depot.Latitude;
            //DestLongitude = depot.Longitude;

            ChargeLeft = fullCharge;
            TimesCharged = 0;
            LastDepotID = depot.UniqueID;

            ClientsOnBoard = new List<Client>();

            LocalTime = startTime;
            MoneyEarned = 0f;

            Depots = depots;
			Clients = clients;
            Hotspots = hotspots;

            CyclePotentialClients = new List<WeightedClient>();
			FirstIndex = 0;
			CompletedClients = 0;

            ClientCount = clients.Count;

            Logs = new List<Log>();
            Router = router;
            /*
            for(int i = 0; i < Hotspots.Count;  i++)
            {
                Console.WriteLine("Hotspot #{0}: From {1} to {2}, coordinates are ({3},{4})", i, Hotspots[i].TimeFrom, Hotspots[i].TimeTo, Hotspots[i].Coordinates.Latitude, Hotspots[i].Coordinates.Longitude);
            }*/
            //Console.ReadLine();
        }


		// 0 - STOP
		// 1 - CONTINUE
        public int Cycle(){
			//Check if need recharging
            if(DateTime.Compare(LocalTime, new DateTime(2022,3,1,0,0,0).AddHours(48)) > 0){
                Console.WriteLine("Time is up!");
                Console.WriteLine("I earned a total of {0} EUR", MoneyEarned);
                return 0;
            }


			//Console.WriteLine("Checking charge");
            if(State == DrivingState.CHARGING){
                int secondsTillFull = (int)((fullCharge - ChargeLeft) / (chargingSpeed * 3600f));

                LocalTime = LocalTime.AddSeconds(secondsTillFull);
                State = DrivingState.WAITING;
                ChargeLeft = fullCharge;

                TimesCharged += 1;
                //Console.WriteLine("Charging car to full, waiting for {0} seconds", secondsTillFull);
                return 1;
            }


			if (ChargeLeft < critcalCharge){
                Depot nearestDepot = FindNearestDepot(Depots);
                Route routeToDepot = null;
                if(nearestDepot == null){
                    Console.WriteLine("FATAL ERROR: Car didn't find ANY depots");
                    return 0;
                }
                else{
                    routeToDepot = CalculateRouteTo(nearestDepot.ResolvedLocation);

                    if(routeToDepot == null){
                        List<Depot> depotCopy;
                        Depot[] temporaryDepots = new Depot[Depots.Count];
                        Depots.CopyTo(temporaryDepots);
                        depotCopy = temporaryDepots.ToList();
                        depotCopy.Remove(nearestDepot);
                        while (routeToDepot == null && depotCopy.Count != 0){
                            nearestDepot = FindNearestDepot(depotCopy);
                            routeToDepot = CalculateRouteTo(nearestDepot.ResolvedLocation);
                            depotCopy.Remove(nearestDepot);

                        }
                        if(routeToDepot == null){
                            Console.WriteLine("FINAL ERROR: No routes to any available depots");
                            return 1;
                        }
                    }
                }
				Console.WriteLine("Car went charging, depot was {0} m away", routeToDepot.TotalDistance);
				//Console.WriteLine(nearestDepot.Latitude + "," + nearestDepot.Longitude);
				RideToDestination(nearestDepot.ResolvedLocation, routeToDepot.TotalDistance, DrivingState.CHARGING);
                LastDepotID = nearestDepot.UniqueID;
                return 1;
            }

            //Looking for clients
            if (CyclePotentialClients.Count == 0)
            {
                if (FirstIndex == Clients.Count - 1)
                {
                    Console.WriteLine("All rides taken!");
                    Console.WriteLine("I earned a total of {0} EUR", MoneyEarned);

                    return 0;
                }
            }
            CyclePotentialClients = WeightPotentialRides(FindSuitableClients(AIWeights.NumberOfPossibleRides));
            if (CyclePotentialClients.Count != 0)
            {
                Client selectedClient = CyclePotentialClients[0].Client;
                //Console.WriteLine("Picking up passenger");

                Route clientRoute = CalculateRouteTo(selectedClient.ResolvedStart);
                if (clientRoute == null)
                {
					selectedClient.Accessible = false;
                    CyclePotentialClients.RemoveAt(0);
					//throw new SystemException();
                    return 1;
                }
                double distToClient = clientRoute.TotalDistance;
                double clientWaitFromNow = selectedClient.StartTime.AddMinutes(3d).Subtract(LocalTime).TotalSeconds;
                //Let's check if we can make it in time
                if (clientWaitFromNow * carSpeedMeter < distToClient)
                {
                    //double distOnFly = DistanceBetweenCoordinates(Latitude, Longitude, selectedClient.StartLatitude, selectedClient.StartLongitude);
                    Console.WriteLine("Client(ID : {0} found, but could not make it in time, missed it by {1} s", selectedClient.Index, (distToClient - (clientWaitFromNow * carSpeedMeter))/ carSpeedMeter);
                    //Console.WriteLine("On fly was {0} m, estimated distance was {1} m, real distance was {2}", distOnFly, distOnFly * EstimationMultiplier(distOnFly), distToClient);
                    //Console.WriteLine("LocalTime: {0}, Final arrival time: {1}, My arrival time: {2}", LocalTime, selectedClient.StartTime.AddMinutes(3d), LocalTime.AddSeconds(distToClient/carSpeedMeter));
                    //Console.WriteLine("Values used in calculation: {0} , {1}", distOnFly * EstimationMultiplier(distOnFly), clientWaitFromNow * carSpeedMeter);
                    //if (selectedClient.Index == 7609) Console.ReadLine();
                    CyclePotentialClients.RemoveAt(0);
                    return 1;
                }

                Route destRoute = CalculateRouteTo(selectedClient.ResolvedStart,selectedClient.ResolvedEnd);
                if (destRoute == null)
                {
					selectedClient.Accessible = false;
                    CyclePotentialClients.RemoveAt(0);
                    return 1;
                } else if (selectedClient.Used || !selectedClient.Accessible)
				{
					CyclePotentialClients.RemoveAt(0);
					return 1;
				} else
				{
					selectedClient.Used = true;
				}
                double distToDest = destRoute.TotalDistance;


                //Console.WriteLine("Delivering passenger to destination");
                DateTime rideStartTime = LocalTime;
                double startLatitude = Latitude;
                double startLongitude = Longitude;

                //OPTIMIZATION - later to be replaced by prebaked distance calculations
                RideToDestination(selectedClient.ResolvedStart, distToClient, DrivingState.TOCLIENT);
                RideToDestination(selectedClient.ResolvedEnd, distToDest, DrivingState.WITHCLIENT);

                selectedClient.Used = true;
				CompletedClients++;
                CyclePotentialClients.Clear();

				//Console.WriteLine("Time spent on road: " + (distToDest + distToClient) / carSpeedMeter);
				//Console.WriteLine("Passenger delivered! Made {0} EUR", selectedClient.RideValue);
				MoneyEarned += (float)selectedClient.RideValue;
                Log log = new Log(ID, selectedClient.Index, rideStartTime, LocalTime, selectedClient.RideValue, startLatitude, startLongitude, Latitude, Longitude, fullCharge - ChargeLeft, TimesCharged, LastDepotID);
                Logs.Add(log);

                ChargeLeft -= ((float)distToClient + (float)distToDest) / 1000f;
                State = DrivingState.WAITING;

                return 1;

            }
			Console.WriteLine("No client, waiting for {0} minutes");

			LocalTime = LocalTime.AddMinutes(15);
            return 0;

        }


		public List<Client> FindSuitableClients(int numberOfCandidates = 20)
        {
			//Console.WriteLine("Finding suitable client");
			//DateTime searchStart = LocalTime;

			//Find location in clients array, where to start searching
			//int firstIndex = Clients.ToList().FindIndex(x => DateTime.Compare(LocalTime, x.Value.StartTime) < 0);
			//Siin Ikka jooksis 420000-ni aga vist fixisin ära
			if (FirstIndex == ClientCount - 1 || FirstIndex == ClientCount) return new List<Client>();
			while (DateTime.Compare(LocalTime, Clients[FirstIndex].StartTime) > 0)
			{
				FirstIndex++;
				if (FirstIndex == ClientCount - 1 || FirstIndex == ClientCount) return new List<Client>();
			}
			Client client = Clients[FirstIndex];
            List<Client> selectedClients = new List<Client>();
            int maxTries = 100000;
            int counter = 0;
            while(counter != maxTries && selectedClients.Count != numberOfCandidates){

                if(client.Used == false & client.Accessible)
                {
                    double distOnFly = DistanceBetweenCoordinates(Latitude, Longitude, client.StartLatitude, client.StartLongitude);
                    double timeTillDecay = client.StartTime.AddMinutes(3d).Subtract(LocalTime).TotalSeconds;
                    //Console.WriteLine(client.StartTime.ToString());
                    //Console.WriteLine(searchStart.ToString());

                    //Console.WriteLine("LocalTime: {0}", LocalTime);
                    //Console.WriteLine("ID: {0} - Estimated distance: {1}, Maximum time to get there: {2}", client.Index, distOnFly * EstimationMultiplier(distOnFly), timeTillDecay * carSpeedMeter);
                    //CHANGE THIS - temporary values
                    if (distOnFly * EstimationMultiplier(distOnFly) < timeTillDecay * carSpeedMeter)
                    {
                        selectedClients.Add(client);
                    }
                    //if (client.Index == 7609) Console.ReadLine();
                }

                counter += 1;
                if (FirstIndex + counter == ClientCount) break;
                client = Clients[FirstIndex + counter];
            }
            FirstIndex += counter;

            return selectedClients;
        }

        public List<WeightedClient> WeightPotentialRides(List<Client> potentialClients)
        {


            //Something something clever about AI
            List<WeightedClient> weightedClients = new List<WeightedClient>();
            Hotspot hotspot = Hotspots.Find(x => DateTime.Compare(x.TimeFrom, LocalTime) <= 0 && DateTime.Compare(x.TimeTo, LocalTime) >= 0);
            if (hotspot == null) hotspot = Hotspots[0];
            Coordinates currentHotspot = hotspot.Coordinates;


            foreach(Client client in potentialClients)
            {
                double distanceFromCenterWeight = DistanceBetweenCoordinates(client.EndLatitude, client.EndLongitude, currentHotspot.Latitude, currentHotspot.Longitude) * AIWeights.DistFromCenterWeight;
                double rideValueWeight = client.RideValue * AIWeights.RideValueWeight;
                double WaitTimeWeight = 0;
                double totalWeight = distanceFromCenterWeight + rideValueWeight + WaitTimeWeight;
                weightedClients.Add(new WeightedClient(client, totalWeight));
            }

            return weightedClients.OrderByDescending(x => x.FinalWeight).ToList();
        }

		private static double DistanceBetweenCoordinates(double lat1, double long1, double lat2, double long2)
		{
			int earthRadius = 6371000;
			double lat1rad = lat1.ToRadians();
			double lat2rad = lat2.ToRadians();

			double x = (long2 - long1).ToRadians() * Math.Cos((lat1rad + lat2rad) / 2);
			double y = (lat2rad - lat1rad);

			return Math.Sqrt(x * x + y * y) * earthRadius;
		}

		private static double ClientDistance(Client client)
		{
			return DistanceBetweenCoordinates(
						client.StartLatitude,
						client.StartLongitude,
						client.EndLatitude,
						client.EndLongitude);
		}


        public static double EstimationMultiplier(double distOnFly)
        {
            double multiplier = 1.144 * distOnFly + 504.41d;
            if (multiplier > 3) multiplier = 3d;
            if (multiplier < 1.1d) multiplier = 1.1d;
            return multiplier;
        }

		public Route CalculateRouteTo(RouterPoint endPoint){
            return CalculateRouteTo(CurrentResolvedLocation, endPoint);
        }

        public Route CalculateRouteTo(RouterPoint startPoint, RouterPoint endPoint)
        {
            if (Router == null)
            {
                var routerDb = new RouterDb();
                using (var stream = new FileInfo(@"harjumaa.routerdb").OpenRead())
                {
                    routerDb = RouterDb.Deserialize(stream);
                    //routerDb.AddContracted(routerDb.GetSupportedProfile("car"));
                }
                Router = new Router(routerDb);
            }
			//EXCEPTION
            Result<Route> routingResult = Router.TryCalculate(Vehicle.Car.Shortest(), startPoint, endPoint);
            if (!routingResult.IsError)
            {
                return routingResult.Value;
            }
            return null;
        }


        public void RideToDestination(RouterPoint endPoint, double distanceCovered, DrivingState endState){
            Latitude = endPoint.Latitude;
            Longitude = endPoint.Longitude;
            CurrentResolvedLocation = endPoint;
            int seconds = (int)(distanceCovered / carSpeedMeter + 1d);
            LocalTime = LocalTime.AddSeconds(seconds);
            State = endState;
        }

        public Depot FindNearestDepot(List<Depot> depots, double latitude = double.NaN, double longitude = double.NaN){
            if(latitude == double.NaN || longitude == double.NaN){
                latitude = Latitude;
                longitude = Longitude;
            }
            double minDistOnFly = double.PositiveInfinity;
            Depot nearestDepot = null;

            foreach(Depot depot in depots){
                double distOnFly = DistanceBetweenCoordinates(Latitude, Longitude, depot.Latitude, depot.Longitude);
                //Console.WriteLine(distOnFly);
                if (distOnFly < minDistOnFly){
                    minDistOnFly = distOnFly;
                    nearestDepot = depot;
                }

            }
            //throw new SystemException();
            return nearestDepot;
        }
         
        public enum DrivingState
        {
            WAITING,
            TOCLIENT,
            WITHCLIENT,
            CHARGING,
            TOCHARGING
        }
    }
}
