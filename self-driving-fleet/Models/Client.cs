using Itinero;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Self_driving_fleet.Models
{
    public class Client
    {
        public DateTime StartTime { get; set; }
        public double StartLatitude { get; set; }
        public double StartLongitude { get; set; }
        private RouterPoint resolvedStart;
        public RouterPoint ResolvedStart
        {
            get
            {
                if (resolvedStart == null)
                {
                    resolvedStart = Support.ResolvePoint((float)StartLatitude, (float)StartLongitude).ResolvedPoint;
                }
                return resolvedStart;
            }
            set
            {
                resolvedStart = value;
            }
        }
        public double EndLatitude { get; set; }
        public double EndLongitude { get; set; }
        private RouterPoint resolvedEnd;
        public RouterPoint ResolvedEnd
        {
            get
            {
                if (resolvedEnd == null)
                {
                    resolvedEnd = Support.ResolvePoint((float)EndLatitude, (float)EndLongitude).ResolvedPoint;
                }
                return resolvedEnd;
            }
            set
            {
                resolvedEnd = value;
            }
        }
        public double DistanceOnRoad { get; set; }
        public double RideValue { get; set; }
		public int Index { get; set; }
        public bool Used { get; set; }
		public bool Accessible { get; set; }

		private object p;
		private double v1;
		private double v2;
		private double v3;
		private double v4;
		private double v5;

		public Client(DateTime startTime, double startLatitude, double startLongitude, double endLatitude, double endLongitude, double rideValue)
        {
            StartTime = startTime;
            StartLatitude = startLatitude;
            StartLongitude = startLongitude;
            resolvedStart = null;

            EndLatitude = endLatitude;
            EndLongitude = endLongitude;
            resolvedEnd = null;
            RideValue = rideValue;
            DistanceOnRoad = 0;
            Used = false;
            Index = -1;
			Accessible = true;
        }

		public Client(object p, double v1, double v2, double v3, double v4, double v5)
		{
			this.p = p;
			this.v1 = v1;
			this.v2 = v2;
			this.v3 = v3;
			this.v4 = v4;
			this.v5 = v5;
		}

		public bool IsBetweenDates(DateTime start, DateTime end)
        {
            //DateTime dTime;
            //if(!DateTime.TryParse(date, out dTime))
            //{
            //    return false;
            //}
            if (DateTime.Compare(StartTime, start) >= 0 && DateTime.Compare(StartTime, end) <= 0)
            {
                return true;
            }


            return false;
        }

    }
}
