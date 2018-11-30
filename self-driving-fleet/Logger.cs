using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using self_driving_fleet.Models;
using System.IO;
using Self_driving_fleet.Models;

namespace self_driving_fleet
{
	class Logger
	{

		public List<Log> LogsList { get; set; }
		public const int loggingFrequency = 1000;
		public int startingPoint;
		private readonly int logCount;

		public Logger(List<Log> logsList)
		{
			LogsList = logsList;
			startingPoint = 0;
			logCount = LogsList.Count;
		}

        public void LogToFile(string filePath, float totalMoney)
        {
			using (StreamWriter sw = File.CreateText(filePath))
			{
				StringBuilder sb = new StringBuilder();
				//Console.WriteLine("LogCount: " + logCount);
				while (startingPoint <= logCount)
				{
					//Console.WriteLine("SP: " + startingPoint + "  lc: " + logCount);
					sb.Clear();
					int endPoint = Math.Min(loggingFrequency + startingPoint, logCount);
					//Console.WriteLine("EndPoint: " + endPoint);
				    for (int i = startingPoint; i < endPoint; i++)
				    {
				        Log log = LogsList[i];
				        sb.Append($"{log.CarID}, {log.ClientID}, {log.RideStartTime}, {log.RideEndTime}, {log.RideValue}, {log.PickupLatitude}, {log.PickupLongitude}, { log.DropoffLatitude }, { log.DropoffLongitude}, { log.DistanceDrivenSinceCharging}, { log.TimesCharged}, { log.LastDepotID} " + Environment.NewLine);
				    }
					sw.Write(sb);
					if (startingPoint == endPoint) break;
					startingPoint = endPoint;
				}
				sw.Write($"{totalMoney}");
			}

			//List<int> ride_ids = new List<int>();
			//foreach (var item in LogsList)
			//{
			//	if(ride_ids.Contains(item.ClientID))
			//	{
			//		Console.WriteLine("CLIENT ALREADY CONTAINED " + item.ClientID);
			//	}
			//	ride_ids.Add(item.ClientID);
			//}

        }

        public static void LogDepotsToFile(string filePath, List<Depot> depots)
        {
            using (StreamWriter sw = File.CreateText(filePath))
            {
                sw.Write("depo_lat,depo_lng" + Environment.NewLine);
                StringBuilder sb = new StringBuilder();
                
                foreach(Depot depot in depots)
                {
                    sw.Write($"{depot.ResolvedLocation.Latitude},{depot.ResolvedLocation.Longitude}" + Environment.NewLine);
                }
            }
        }


    }
}
