using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UCCTaskSolver;

namespace FileManager
{
    public partial class ClientForm : Form
    {
        public DVRPDescription dvrpDescription;

        public bool timeoutSpecified = false;

        public long timeout = 0;

        public int solutionId = -1;

        public ClientForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (solvingTimeoutText.Text != "")
            {
                timeoutSpecified = true;
                try
                {
                    timeout = long.Parse(solvingTimeoutText.Text);
                }
                catch
                {
                    timeoutSpecified = false;
                }
            }
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Vehicle Routing Problem File (*.vrp)|*.vrp";
            ofd.ShowDialog();
            if (ofd.FileName == null || ofd.FileName == "") return;
            dvrpDescription = new DVRPDescription();
            try
            {
                using (StreamReader sr = new StreamReader(ofd.FileName))
                {
                    sr.ReadLine();
                    string line = sr.ReadLine().Trim();
                    int depotCoordinateIndex = 0;
                    while (line != "EOF")
                    {
                        string[] lineParts = line.Split(':');
                        if (lineParts.Length == 1)
                        {
                            int index = 0;
                            switch (line.Trim())
                            {
                                case "DEPOTS":
                                   depotCoordinateIndex = Int32.Parse(sr.ReadLine().Trim());
                                    break;
                                case "DEMAND_SECTION":
                                    index = 0;
                                    while(true)
                                    {
                                        
                                        line = sr.ReadLine().Trim();
                                        lineParts = line.Split(' ');
                                        if (lineParts.Length == 1)
                                            break;
                                       if (Int32.Parse(lineParts[0]) > depotCoordinateIndex)
                                           dvrpDescription.clients[Int32.Parse(lineParts[0]) - 1].demand = Math.Abs(Int32.Parse(lineParts[lineParts.Length - 1]));
                                       else
                                           dvrpDescription.clients[Int32.Parse(lineParts[0])].demand = Math.Abs(Int32.Parse(lineParts[lineParts.Length - 1]));
                                    } 
                                    continue;
                                case "LOCATION_COORD_SECTION":
                                    while(true)
                                    {
                                        line = sr.ReadLine().Trim();
                                        lineParts = line.Split(' ');
                                        if (lineParts.Length == 1)
                                            break;
                                        if(Int32.Parse(lineParts[0])==depotCoordinateIndex)
                                            dvrpDescription.coordinateDepot=new Point((Int32.Parse(lineParts[lineParts.Length - 2])), (Int32.Parse(lineParts[lineParts.Length - 1])));
                                        else if (Int32.Parse(lineParts[0]) < depotCoordinateIndex)
                                            dvrpDescription.clients[Int32.Parse(lineParts[0])].coordinate = new Point((Int32.Parse(lineParts[lineParts.Length - 2])), (Int32.Parse(lineParts[lineParts.Length - 1])));
                                        else
                                          dvrpDescription.clients[Int32.Parse(lineParts[0])-1].coordinate = new Point((Int32.Parse(lineParts[lineParts.Length - 2])), (Int32.Parse(lineParts[lineParts.Length - 1])));
                                    } 
                                    continue;
                                case "DEPOT_LOCATION_SECTION":
                                    break;
                                case "VISIT_LOCATION_SECTION":
                                    break;
                                case "DURATION_SECTION":
                                   index = 0;
                                    while(true)
                                    {
                                        line = sr.ReadLine().Trim();
                                        lineParts = line.Split(' ');
                                        if (lineParts.Length == 1)
                                            break;
                                        if (Int32.Parse(lineParts[0]) > depotCoordinateIndex)
                                            dvrpDescription.clients[Int32.Parse(lineParts[0]) - 1].durationTime = Math.Abs(Int32.Parse(lineParts[lineParts.Length - 1]));
                                        else
                                            dvrpDescription.clients[Int32.Parse(lineParts[0])].durationTime = Math.Abs(Int32.Parse(lineParts[lineParts.Length - 1]));
                                    } 
                                    continue;
                                case "DEPOT_TIME_WINDOW_SECTION":
                                   // while (true)
                                   // {
                                        line = sr.ReadLine().Trim();
                                        lineParts = line.Split(' ');
                                        if (lineParts.Length == 1)
                                            break;
                                        dvrpDescription.startTimeDepot = Int32.Parse(lineParts[1]);
                                        dvrpDescription.endTimeDepot = Int32.Parse(lineParts[2]);
                                   // } 
                                    break;
                                case "TIME_AVAIL_SECTION":
                                    while (true)
                                    {
                                        line = sr.ReadLine().Trim();
                                        lineParts = line.Split(' ');
                                        if (lineParts.Length == 1)
                                            break;
                                       if (Int32.Parse(lineParts[0]) > depotCoordinateIndex)
                                            dvrpDescription.clients[Int32.Parse(lineParts[0]) - 1].availableTime = Math.Abs(Int32.Parse(lineParts[lineParts.Length - 1]));
                                       else
                                            dvrpDescription.clients[Int32.Parse(lineParts[0])].availableTime = Math.Abs(Int32.Parse(lineParts[lineParts.Length - 1]));
                                    } 
                                    continue;
                                default:
                                    break;
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
                                   // dvrpDescription.locations = new List<Point>();
                                   // dvrpDescription.coordinateDepot
                                   // for (int i = 0; i < Int32.Parse(lineParts[1]) + 1; i++)
                                   //     dvrpDescription.clients[i].locations.Add(new Point());
                                    break;
                                case "NUM_VEHICLES":
                                    dvrpDescription.vehiclesCount=Int32.Parse(lineParts[1]);
                                    //.vehicles = new List<Vehicle>();
                                    //for (int i = 0; i < ; i++)
                                   //     dvrpDescription.vehicles.Add(new Vehicle());
                                    break;
                                case "CAPACITIES":
                                   // foreach (Vehicle v in dvrpDescription.vehicles)
                                        dvrpDescription.vehicleCapacity = Int32.Parse(lineParts[1]);
                                    break;
                                case "SPEED":
                                    //foreach (Vehicle v in dvrpDescription.vehicles)
                                    //    v.speed = Int32.Parse(lineParts[1]);
                                    break;
                                case "EDGE_WEIGHT_TYPE":
                                   // dvrpDescription.edgeWeightType = (EdgeWeightType)Enum.Parse(typeof(EdgeWeightType), lineParts[1].Trim());
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

            this.Close();
        }

        private void getSolutionButton_Click(object sender, EventArgs e)
        {
            try
            {
                solutionId = Int32.Parse(textBox1.Text.Trim());
            }
            catch
            {
                solutionId = -2;
            }
            this.Close();
        }
    }
}
