using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
using SE_lab.Messages;
using FileManager;
using System.Windows;
using System.Windows.Forms;
using System.Threading;
using UCCTaskSolver;

namespace SE_lab
{
    public class ComputationalClient : Component
    {
        // bool m_isConnected = false;
        private Semaphore waiting = new Semaphore(0, 1);
        private int[][] m_sol;
        private UCCTaskSolver.DVRPDescription m_des;
        private System.Timers.Timer m_requestTimer, m_solvingTimer;              //zmienić liczby na stałe czy coś tam
        private SolutionsSolution[] solutions;
        public UCCTaskSolver.DVRPDescription problemData = new UCCTaskSolver.DVRPDescription();
        private ClientForm cf = new ClientForm();

        public ComputationalClient()
        {
            var addressIP = ConfigurationManager.AppSettings["serverAddressIP"];
            int port = int.Parse(ConfigurationManager.AppSettings["portNumber"]);
            Connect(IPAddress.Parse(addressIP), port);
            m_requestTimer = new System.Timers.Timer();
            m_solvingTimer = new System.Timers.Timer();
            //    m_isConnected = true;
        }

        /// <summary>
        /// Method used to send request to solve a problem with data
        /// </summary>
        /// <returns>Method returns true, when Client succesfully send SolveRequest; Method return flase when send was failed</returns>
        public bool SolveRequest()
        {
            //GenerateData();
            byte[] bytesToSend;
            if (problemData == null)
                bytesToSend = null;
            bytesToSend = SerializeFromClass(problemData);

            SolveRequest solveRequest = new SolveRequest()
                {
                    Data = bytesToSend,
                    ProblemType = "DVRP-02",
                    
                    SolvingTimeoutSpecified = cf.timeoutSpecified
                };
            if (cf.timeoutSpecified) solveRequest.SolvingTimeout = (ulong)cf.timeout;

            // bool result = 
            Send(SerializeMessage<SolveRequest>(solveRequest));
            //if (result)
            ReceiveSolveRequest();
            //return result;
            return true;
        }

        private void ReceiveSolveRequest()
        {
            var solveRequestResponse = DeserializeMessage<SolveRequestResponse>(Receive());
            Console.WriteLine("Server register problem ID = {0}", solveRequestResponse.Id);
            m_requestTimer = new System.Timers.Timer(10000);
            m_requestTimer.Elapsed += (sender, e) => { SolutionRequest(solveRequestResponse.Id); };
            m_requestTimer.Start();
        }

        /// <summary>
        /// Method used to send request to check whether cluster has succesfully computed the solution
        /// </summary>
        /// <param name="_id"></param>
        private void SolutionRequest(ulong _id)
        {
            SolutionRequest solutionRequest = new SolutionRequest() { Id = _id };
            Send(SerializeMessage<SolutionRequest>(solutionRequest));
            ReceiveSolutionInfo(_id);
        }

        private void ReceiveSolutionInfo(ulong _id)
        {
            var solutionInfo = DeserializeMessage<Solutions>(Receive());
            solutions = solutionInfo.Solutions1;
            if (solutions != null && solutions[0].Type == SolutionsSolutionType.Final && solutions[0].TaskIdSpecified == false)
            {
                m_requestTimer.Stop();                                                      //zapis danych na dysku
                SolutionDescription solutionDescription = SerializeToClass<SolutionDescription>(solutions[0].Data);
                SolutionDescriptionToFileParser solutionDescriptionToFileParser = new SolutionDescriptionToFileParser();
                m_des = SerializeToClass<DVRPDescription>(solutionInfo.CommonData);
                Console.WriteLine("Received Problem Solution Id = {0}.", solutionInfo.Id);
                Console.WriteLine("Write name of the file for solution (MAX 15 characters)");
                string solutionFileName = Console.ReadLine();
                if (solutionFileName.Length == 0)
                    solutionFileName = "Result File";
                solutionFileName = solutionFileName.Substring(0, solutionFileName.Length < 15 ? solutionFileName.Length : 15);
                int[][] x = m_sol = solutionDescription.m_permutation;
                //solutionDescriptionToFileParser.WriteSolutionToFile(solutionDescription, solutionFileName + ".txt");
                solutionDescriptionToFileParser.WriteSolutionToFile(solutionDescription.m_permutation, solutionDescription.m_result, solutionFileName + ".txt");
                Console.WriteLine("Solution saved correctly in file " + solutionFileName);
                Console.WriteLine("Would you like to see the visualisation? [y/n]");
                string visualisationChoice = Console.ReadLine();
                if ( visualisationChoice.Length > 0 && (visualisationChoice[0] == 'y' || visualisationChoice[0] == 'Y'))
                {
                    Thread t = new Thread(new ThreadStart(ShowVisualisation));
                    t.Start();
                }
                Console.WriteLine("Work done. Good bye!");
                Console.ReadLine();
                waiting.Release();
            }
            else
                Console.WriteLine("We haven't result yet");
        }
        private void ShowVisualisation()
        {
            Visualisation visualisation = new Visualisation(m_des, m_sol);
            visualisation.ShowDialog();
        }

        public void Start()
        {
            cf.ShowDialog();
            if (cf.solutionId == -2)
            {
                return;
            }
            else if (cf.solutionId != -1)
            {
                m_requestTimer = new System.Timers.Timer(10000);
                m_requestTimer.Elapsed += (sender, e) => { SolutionRequest((ulong)cf.solutionId); };
                m_requestTimer.Start();
                waiting.WaitOne();
                return;
            }
            problemData = cf.dvrpDescription;
            //problemData = GenerateData2();

            SolveRequest();
            waiting.WaitOne();
        }

/*
        private DVRPDescription GenerateData2()
        {
            DVRPDescription description = new DVRPDescription()
            {
                clients = new List<Client>()
                {   new Client(){ availableTime=616, coordinate=new Point(-55,-26), durationTime=20, demand=48},
                    new Client(){ availableTime=91, coordinate=new Point(-24,38), durationTime=20, demand=20},
                    new Client(){ availableTime=240, coordinate=new Point(-99,-29), durationTime=20, demand=45},
                    new Client(){ availableTime=356, coordinate=new Point(-42,30), durationTime=20, demand=19},
                    new Client(){ availableTime=528, coordinate=new Point(59,66), durationTime=20, demand=32},
                    new Client(){ availableTime=459, coordinate=new Point(55,-35), durationTime=20, demand=42},
                    new Client(){ availableTime=433, coordinate=new Point(-42,3), durationTime=20, demand=19},
                    new Client(){ availableTime=513, coordinate=new Point(95,13), durationTime=20, demand=35},
                    new Client(){ availableTime=444, coordinate=new Point(71,-90), durationTime=20, demand=30},
                    new Client(){ availableTime=44, coordinate=new Point(38,32), durationTime=20, demand=26},
                    new Client(){ availableTime=318, coordinate=new Point(67,-22), durationTime=20, demand=41},
                    new Client(){ availableTime=20, coordinate=new Point(58,-97), durationTime=20, demand=27},
                 //   new Client(){ availableTime=549, coordinate=new Point(-41,34), durationTime=20, demand=5},
                 //   new Client(){ availableTime=635, coordinate=new Point(58,-42), durationTime=20, demand=35}
                },
                coordinateDepot = new Point(0, 0),
                cutOffTime = 0.5,
                vehicleCapacity = 100,
                endTimeDepot = 640,
                vehiclesCount = 12,
                startTimeDepot = 0
            };
            return description;
        }

        private DVRPDescription GenerateData()
        {
            DVRPDescription description = new DVRPDescription()
            {
                clients = new List<Client>()
                {   new Client(){ availableTime=199, coordinate=new Point(-95,44), durationTime=20, demand=15},
                    new Client(){ availableTime=410, coordinate=new Point(-40,28), durationTime=20, demand=32},
                    new Client(){ availableTime=432, coordinate=new Point(9,94), durationTime=20, demand=36},
                    new Client(){ availableTime=593, coordinate=new Point(9,28), durationTime=20, demand=26},
                    new Client(){ availableTime=82, coordinate=new Point(-12,47), durationTime=20, demand=31},
                    new Client(){ availableTime=660, coordinate=new Point(16,-100), durationTime=20, demand=16},
                    new Client(){ availableTime=464, coordinate=new Point(-97,51), durationTime=20, demand=21},
                    new Client(){ availableTime=678, coordinate=new Point(92,28), durationTime=20, demand=42},
                    new Client(){ availableTime=68, coordinate=new Point(73,-11), durationTime=20, demand=13},
                    new Client(){ availableTime=209, coordinate=new Point(-30,-46), durationTime=20, demand=20},
                    new Client(){ availableTime=635, coordinate=new Point(-60,47), durationTime=20, demand=21},
                    new Client(){ availableTime=113, coordinate=new Point(-42,20), durationTime=20, demand=47},
                    new Client(){ availableTime=549, coordinate=new Point(-41,34), durationTime=20, demand=5},
                    new Client(){ availableTime=635, coordinate=new Point(58,-42), durationTime=20, demand=35}
                },
                coordinateDepot = new Point(0, 0),
                cutOffTime = 0.5,
                vehicleCapacity = 100,
                endTimeDepot = 680,
                vehiclesCount = 14,
                startTimeDepot = 0
            };
            return description;
        }
       */ 
    }
}
