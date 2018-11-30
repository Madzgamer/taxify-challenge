using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using Self_driving_fleet.Models;
using self_driving_fleet;

namespace Self_driving_fleet
{
    class Program
    {
		static void Main(string[] args)
		{
            Console.WriteLine("Please insert the number of car threads");
            int noOfThreads = Int32.Parse(Console.ReadLine());
            Console.WriteLine("Please insert running time in minutes (ex. 15.5)");
            float runningTime = float.Parse(Console.ReadLine());

            Console.WriteLine("Making {0} threads and running the for {1} minutes", noOfThreads, runningTime);


            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
			CarEngine e = new CarEngine(noOfThreads, runningTime);
			e.Start();
            stopwatch.Stop();
			Logger logger = new Logger(e.AllLogs);


			logger.LogToFile(@"output.txt", e.Money);
            Console.WriteLine("Total excecution time was {0} ms", stopwatch.ElapsedMilliseconds);
			Console.WriteLine("Total earnings were {0} Euros", e.Money);
			Console.ReadLine();
		}

    }
}
