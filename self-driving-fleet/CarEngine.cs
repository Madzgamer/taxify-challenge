using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using self_driving_fleet;
using Self_driving_fleet.Models;
using System.Collections.Concurrent;
using System.Threading;
using System.Timers;
using self_driving_fleet.Models;
using Itinero;
using System.IO;

namespace Self_driving_fleet
{
	public class CarEngine
	{
		private DateTime startTime = new DateTime(2022,3,1,0,0,0);
		static float runTime = 0.5f;
		private int numOfThreads = 4;

        public int CurrentUnusedCarID { get; set; }

		private ConcurrentDictionary<int, Client> clients;
		private List<Depot> depots;

		public float Money { get; set; }
		public int FinishedThreads { get; set; }

		public List<Log> AllLogs { get; set; }

        private readonly object idLock = new object();
		private readonly object allLogsLock = new object();
		private readonly object moneyLock = new object();
		private readonly object compThreadsLock = new object();

        private Stopwatch runningStopWatch;

        //List<System.Timers.Timer> ThreadTimers { get; set; }
        //public static Dictionary<int, Thread> Threads = new Dictionary<int, Thread>();

        public Router GlobalRouter { get; }

		public CarEngine(int noOfThreads, float runningTime)
		{
            this.numOfThreads = noOfThreads;
            runTime = runningTime;

			Money = 0f;
			FinishedThreads = 0;
            CurrentUnusedCarID = 0;

            var routerDb = new RouterDb();
            using (var stream = new FileInfo(@"harjumaa.routerdb").OpenRead())
            {
                routerDb = RouterDb.Deserialize(stream);
                //routerDb.AddContracted(routerDb.GetSupportedProfile("car"));

            }
            GlobalRouter = new Router(routerDb);
            runningStopWatch = new Stopwatch();
            runningStopWatch.Start();
            //ThreadTimers = new List<System.Timers.Timer>();
        }

		public void Start(int clientsNum = -1)
		{
			clients = Support.LoadClientsTS(clientsNum);
			startTime = clients[0].StartTime;
			//Console.WriteLine("Resolving all client start and end locations");
			//clients = clients.ResolveclientPoints(clients);
			depots = Support.LoadDepots();
			Console.WriteLine("Resolving all depot start and end locations");
			depots = Support.ResolveDepotPoints(depots);
            Logger.LogDepotsToFile(@"depots.csv", depots);
			AllLogs = new List<Log>();

			List<Hotspot> hotspots = Support.CalculateHotspots(clients, 1d, 0);

			for (int i = 0; i < numOfThreads; i++)
			{
                //System.Timers.Timer timer = new System.Timers.Timer(40000);
                //timer.Elapsed += async (sender, e) => await HandleTimer(i);
                //ThreadTimers.Add(timer);
                ManagingThread carThread = new ManagingThread(i, clients, depots, hotspots, startTime, GlobalRouter, this);
				Thread thread = new Thread(() => carThread.Start());
				thread.Name = "Car Thread " + i;
                //Threads.Add(i, thread);
				thread.Start();
			}

			while (FinishedThreads < numOfThreads && runningStopWatch.ElapsedMilliseconds < (runTime*60*1000 + 60 * 1000) )
			{
				Thread.Sleep(10000);
			}
			Console.WriteLine("Total money: " + Money);

		}
        /*
        private static Task HandleTimer(int threadID)
        {
            Console.WriteLine("Thread frozen, restarting the thread");
            Thread frozenThread = Threads[threadID];
            frozenThread.Abort();
        }*/

        class ManagingThread
		{
			private int ID { get; set; }
			private float Money { get; set; }
			ConcurrentDictionary<int, Client> clients;
			List<Depot> depots;
			List<Hotspot> hotspots;
			DateTime startTimeCar;
			DateTime endTimeCompute;
			CarEngine carEngine;
			private Car Car { get; set; }
            Router router;
            //System.Timers.Timer timer;

			public ManagingThread(int id,  ConcurrentDictionary<int, Client> clients, List<Depot> depots, List<Hotspot> hotspots, DateTime startTimeCar, Router router, CarEngine carEngine)
			{
				ID = id;
				Money = 0f;
				this.clients = clients;
				this.depots = depots;
				this.hotspots = hotspots;
				this.startTimeCar = startTimeCar;
				endTimeCompute = DateTime.Now.AddMinutes(runTime);
				this.carEngine = carEngine;
                this.router = router;
                //this.timer = timer;
			}

			public void Start()
			{
				int carNum = 0;
				while (DateTime.Compare(DateTime.Now, endTimeCompute) < 0)
				{
                    //timer.Interval = 40000;
                    Tuple<int, float, List<Log>, long> result = StartCar();
					Money += result.Item2;
					lock (carEngine.allLogsLock)
					{
						carEngine.AllLogs.AddRange(result.Item3);
					}
					Console.WriteLine("CarEngine {0}: Car {1} finished in {2} ms", ID, carNum, result.Item4);
					Console.WriteLine("CarEngine {0}: Car {1} compleated  {2} Clients", ID, carNum, result.Item1);
					Console.WriteLine("CarEngine {0}: Car {1} earned      {2} Euros", ID, carNum, result.Item2);
					Console.WriteLine("CarEngine {0}: Car {1} logged {2} entries", ID, carNum, result.Item3.Count);
					carNum++;
				}

				lock (carEngine.moneyLock)
				{
					carEngine.Money += Money;
				}
				Console.WriteLine("CarEngine {0} earned: {1} Euros", ID, Money);

				lock (carEngine.compThreadsLock)
				{
					carEngine.FinishedThreads++;
				}
			}

			//Tuple<completedClients, money, logs, timeElapsed>
			private Tuple<int, float, List<Log>, long> StartCar()
			{
                int carID;
                lock (carEngine.idLock)
                {
                    carID = carEngine.CurrentUnusedCarID;
                    carEngine.CurrentUnusedCarID += 1;
                }
				Car = new Car(carID, depots[0], startTimeCar, depots, clients, hotspots, router);
				int code = 1;
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();
				while (code == 1)
				{
					code = Car.Cycle();
				}
				stopwatch.Stop();
				return Tuple.Create(Car.CompletedClients, Car.MoneyEarned, Car.Logs, stopwatch.ElapsedMilliseconds);
			}

		}

        private static readonly Random getrandom = new Random();

        public static int GetRandomNumber(int min, int max)
        {
            lock (getrandom) // synchronize
            {
                return getrandom.Next(min, max);
            }
        }

    }
}
