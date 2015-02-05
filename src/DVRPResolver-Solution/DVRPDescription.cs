using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DVRPResolver
{
    [Serializable]
    public class DVRPDescription
    {
        public List<Client> clients;
        public int vehiclesCount;
        public double vehicleCapacity;
        public Point coordinateDepot;
        public double startTimeDepot;
        public double endTimeDepot;
        public double cutOffTime;
        public DVRPDescription()
        { cutOffTime = 0.5; }

        public double CountDistance(Point p1, Point p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }
    }

    [Serializable]
    public class Client
    {
        public int demand { get; set; }
        public Point coordinate { get; set; }
        public int durationTime { get; set; }
        public int availableTime { get; set; }
    }
}
