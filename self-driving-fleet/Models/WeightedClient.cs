using Itinero;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Self_driving_fleet.Models
{
    public class WeightedClient
    {
        public Client Client { get; set; }
        public double FinalWeight { get; set; }

        public WeightedClient(Client client, double finalWeight)
        {
            Client = client;
            FinalWeight = finalWeight;
        }
    }
}
