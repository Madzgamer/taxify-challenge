using Itinero;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Self_driving_fleet.Models
{
    public class Depot
    {
        public int UniqueID { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public RouterPoint ResolvedLocation { get; set; }
        public List<Car> ContainedCars { get; set; }

        public Depot(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
            ResolvedLocation = null;
            UniqueID = 0;
            ContainedCars = new List<Car>();
        }

		public Depot(int uniqueID, double latitude, double longitude)
		{
			Latitude = latitude;
			Longitude = longitude;
			UniqueID = uniqueID;

			ContainedCars = new List<Car>();
		}
	}
}
