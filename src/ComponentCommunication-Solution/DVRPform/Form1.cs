using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DVRPform
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Text files (*.txt)|*.txt";
            ofd.ShowDialog();
            DVRPDescription dvrpDescription = new DVRPDescription();
            try
            {
                using (StreamReader sr = new StreamReader(ofd.FileName))
                {
                    sr.ReadLine();
                    string line = sr.ReadLine().Trim();
                    while (line != "EOF")
                    {
                        string[] lineParts = line.Split(':');
                        if (lineParts.Length == 1)
                        {
                            int index = 0;
                            switch (line.Trim())
                            {
                                case "DEPOTS":
                                    dvrpDescription.depotCoordinateIndex = Int32.Parse(sr.ReadLine().Trim());
                                    break;
                                case "DEMAND_SECTION":
                                    index = 0;
                                    while (true)
                                    {
                                        dvrpDescription.clients[index].coordinateIndex = Int32.Parse(lineParts[0]);
                                        line = sr.ReadLine().Trim();
                                        lineParts = line.Split(' ');
                                        if (lineParts.Length == 1)
                                            break;
                                        if (Int32.Parse(lineParts[0]) > dvrpDescription.depotCoordinateIndex)
                                            dvrpDescription.clients[Int32.Parse(lineParts[0]) - 1].demand = Math.Abs(Int32.Parse(lineParts[lineParts.Length - 1]));
                                        else
                                            dvrpDescription.clients[Int32.Parse(lineParts[0])].demand = Math.Abs(Int32.Parse(lineParts[lineParts.Length - 1]));
                                    }
                                    continue;
                                case "LOCATION_COORD_SECTION":
                                    while (true)
                                    {
                                        line = sr.ReadLine().Trim();
                                        lineParts = line.Split(' ');
                                        if (lineParts.Length == 1)
                                            break;
                                        dvrpDescription.locations[Int32.Parse(lineParts[0])] = new Point(Math.Abs(Int32.Parse(lineParts[lineParts.Length - 2])), Math.Abs(Int32.Parse(lineParts[lineParts.Length - 1])));
                                    }
                                    continue;
                                case "DEPOT_LOCATION_SECTION":
                                    break;
                                case "VISIT_LOCATION_SECTION":
                                    break;
                                case "DURATION_SECTION":
                                    index = 0;
                                    while (true)
                                    {
                                        line = sr.ReadLine().Trim();
                                        lineParts = line.Split(' ');
                                        if (lineParts.Length == 1)
                                            break;
                                        dvrpDescription.clients[Int32.Parse(lineParts[0])].durationTime = Int32.Parse(lineParts[lineParts.Length - 1]);
                                    }
                                    continue;
                                /*case "DEPOT_TIME_WINDOW_SECTION":
                                    while (true)
                                    {
                                        line = sr.ReadLine().Trim();
                                        lineParts = line.Split(' ');
                                        if (lineParts.Length == 1)
                                            break;
                                        dvrpDescription.clients[Int32.Parse(lineParts[0])].availableTime = Int32.Parse(lineParts[lineParts.Length - 1]);
                                    } 
                                    continue;*/
                                case "TIME_AVAIL_SECTION":
                                    while (true)
                                    {
                                        line = sr.ReadLine().Trim();
                                        lineParts = line.Split(' ');
                                        if (lineParts.Length == 1)
                                            break;
                                        dvrpDescription.clients[Int32.Parse(lineParts[0])].availableTime = Int32.Parse(lineParts[lineParts.Length - 1]);
                                    }
                                    continue;

                            }
                        }
                        else
                        {
                            switch (lineParts[0])
                            {
                                case "NAME":
                                    break;
                                case "NUM_VISITS":
                                    dvrpDescription.clients = new List<Client>();
                                    for (int i = 0; i < Int32.Parse(lineParts[1]); i++)
                                        dvrpDescription.clients.Add(new Client());
                                    break;
                                case "NUM_VEHICLES":
                                    dvrpDescription.vehicles = new List<Vehicle>();
                                    for (int i = 0; i < Int32.Parse(lineParts[1]); i++)
                                        dvrpDescription.vehicles.Add(new Vehicle());
                                    break;
                                case "CAPACITIES":
                                    foreach (Vehicle v in dvrpDescription.vehicles)
                                        v.capacity = Int32.Parse(lineParts[1]);
                                    break;
                                case "SPEED":
                                    foreach (Vehicle v in dvrpDescription.vehicles)
                                        v.speed = Int32.Parse(lineParts[1]);
                                    break;
                                case "EDGE_WEIGHT_TYPE":
                                    dvrpDescription.edgeWeightType = (EdgeWeightType)Enum.Parse(typeof(EdgeWeightType), lineParts[1].Trim());
                                    break;
                                default:
                                    break;
                            }
                        }
                        line = sr.ReadLine().Trim();
                    }

                }
            }
            catch (Exception ee)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(ee.Message);
            }


        }
    }
}
