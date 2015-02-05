using SE_lab.Messages;
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
using System.Windows;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Timer = System.Timers.Timer;
using System.Diagnostics;
using UCCTaskSolver;
using System.Reflection;


namespace SE_lab
{
    //brak rozwiazania jezeli chodzi o odliczanie czasu czy to trwania w danym stanie
    //czy jeśli chodzi o timeout związany z przekroczeniem czasu dla rozwiązania zadania


    public class ComputationalNode : Component
    {
        private Timer m_statusTimer, m_solvingTimer;              //zmienić liczby na stałe czy coś tam

        private DateTime m_lastChangeTime;

        private TaskSolver taskSolver;

        private Status m_status;

        private Semaphore m_lockStatus;

        private Stopwatch m_Stopwatch = new Stopwatch();

        public ComputationalNode()
        {
            var addressIP = ConfigurationManager.AppSettings["serverAddressIP"];
            int port = int.Parse(ConfigurationManager.AppSettings["portNumber"]);
            m_lastChangeTime = DateTime.Now;
            m_status = new Status()
            {
                Threads = new StatusThread[1] 
                {  new StatusThread()
                   { TaskIdSpecified = false, 
                     ProblemInstanceIdSpecified = false,
                     State = StatusThreadState.Idle, HowLong=0, ProblemType=""
                   }
                }
            };
            m_lockStatus = new Semaphore(1, 1);
            Connect(IPAddress.Parse(addressIP), port);
        }

        /// <summary>
        /// Send status of node to server
        /// </summary>
        /// <param name="_state">Actual state of component</param>
        /// <param name="_elapsedEventArgs"></param>
        private void SendNodeStatus(object _state, ElapsedEventArgs _elapsedEventArgs)
        {
            m_lockStatus.WaitOne();
            m_status.Threads[0].HowLong = (ulong)((DateTime.Now - m_lastChangeTime).TotalMilliseconds);
            bool result = Send(SerializeMessage<Status>(m_status));
            m_lockStatus.Release();
            if (!result)
            { m_statusTimer.Stop(); }
        }

        /// <summary>
        /// Method used to register ComputationalNode by sending register Message
        /// </summary>
        /// <returns>Method return true, if ComputationalNode successfully send RegisterMessage; otherwise false</returns>
        private bool RegisterComponent()
        {

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var loadedPaths = loadedAssemblies.Select(a => a.Location).ToArray();
            var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
            var toLoad = referencedPaths.Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase)).ToList();
            toLoad.ForEach(path => loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path))));
            List<Assembly> list = AppDomain.CurrentDomain.GetAssemblies().ToList();
            List<Type> myTypes = new List<Type>();
            foreach (var asem in list)
            {
                foreach (var type in asem.GetTypes())
                {
                    if (type.BaseType == typeof(TaskSolver))
                    {
                        myTypes.Add(type);
                    }
                }
            }
            Register register = new Register();
            register.Type = RegisterType.ComputationalNode;
            register.SolvableProblems = new string[myTypes.Count];
            for (int i = 0; i < myTypes.Count; i++)
            {
                TaskSolver solv = (TaskSolver)Activator.CreateInstance(myTypes[i], new byte[0]);
                register.SolvableProblems[i] = solv.Name;
            }
            register.ParallelThreads = (byte)m_status.Threads.Length;
            return Send(SerializeMessage<Register>(register));
        }

        private void ReceiveRegisterResponse()
        {
            try
            {
                var registerResponse = DeserializeMessage<RegisterResponse>(Receive());
                if (registerResponse != null)
                {
                    Console.WriteLine("Received Register Response. Id = {0}", registerResponse.Id);
                    m_status.Id = registerResponse.Id;
                    m_statusTimer = new System.Timers.Timer(registerResponse.Timeout.Ticks /10000);
                    m_statusTimer.Elapsed += SendNodeStatus;
                    m_statusTimer.Start();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Wrong Message Type");
                if (host.Connected)
                    ReceiveRegisterResponse();
                else
                    throw new Exception();
            }
        }

        private void ReceivePartialProblem()
        {
            try
            {
                var partialProblem = DeserializeMessage<SolvePartialProblems>(Receive());
                if (partialProblem != null)
                {
                    Console.WriteLine("Partial problem received from server");
                    Console.WriteLine("I will try to solve PartialProblem ID = {0} Task ID = {1}", partialProblem.Id, partialProblem.PartialProblems[0].TaskId);

                    // for (int i = 0; i < partialProblem.PartialProblems.Length; i++)
                    //  {  
                    m_lockStatus.WaitOne();
                    m_lastChangeTime = DateTime.Now;
                    if (m_status.Threads[0].State == StatusThreadState.Busy)
                        throw new Exception("Hard exception, I have work yet");
                    m_status.Threads[0] = new StatusThread()
                    {
                        HowLong = 0,
                        ProblemInstanceId = partialProblem.Id,
                        ProblemInstanceIdSpecified = true,
                        ProblemType = partialProblem.ProblemType,
                        State = StatusThreadState.Busy,
                        TaskId = partialProblem.PartialProblems[0].TaskId,
                        TaskIdSpecified = true
                    };
                    m_lockStatus.Release();
                    //int[] boundaries = SerializeToClass<int[]>(partialProblem.PartialProblems[0].Data);
                    bool isEnd = false;

/*                    var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
                    var loadedPaths = loadedAssemblies.Select(a => a.Location).ToArray();
                    var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
                    var toLoad = referencedPaths.Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase)).ToList();
                    toLoad.ForEach(path => loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path))));*/
                    List<Assembly> list = AppDomain.CurrentDomain.GetAssemblies().ToList();
                    Type myType = null;
                    foreach (var asem in list)
                    {
                        foreach (var type in asem.GetTypes())
                        {
                            if (type.BaseType == typeof(TaskSolver))
                            {
                                TaskSolver solv = (TaskSolver)Activator.CreateInstance(type, new byte[0]);
                                if (solv.Name == partialProblem.ProblemType)
                                {
                                    myType = type; isEnd = true; break;
                                }
                            }
                        }
                        if (isEnd) break;
                    }

                    taskSolver = (UCCTaskSolver.TaskSolver)Activator.CreateInstance(myType, partialProblem.CommonData);
                    byte[] sol = taskSolver.Solve(partialProblem.PartialProblems[0].Data, partialProblem.SolvingTimeoutSpecified ? TimeSpan.FromMilliseconds(partialProblem.SolvingTimeout) : TimeSpan.Zero);

                    m_lockStatus.WaitOne();

                    Solutions solution = new Solutions()
                    {
                        Id = partialProblem.Id,
                        CommonData = partialProblem.CommonData,
                        ProblemType = partialProblem.ProblemType,
                        Solutions1 = new SolutionsSolution[1] 
                        { new SolutionsSolution()
                            { ComputationsTime=(ulong)((DateTime.Now-m_lastChangeTime).TotalMilliseconds),
                             TimeoutOccured=false,
                             TaskId=partialProblem.PartialProblems[0].TaskId,
                             TaskIdSpecified=true, 
                             Type=SolutionsSolutionType.Partial,
                             Data=sol
                            }
                        }
                    };
                    m_lastChangeTime = DateTime.Now;
                    SendPartialSolution(solution);
                    m_lockStatus.Release();
                }
            }
            catch (Exception)
            {
                if (host.Connected)
                    Console.WriteLine("Wrong type of message");
                else throw new Exception();
            }
        }

        private void SendPartialSolution(Solutions solution)
        {
            Console.WriteLine("I will send one partial solution to server. Problem Id={0}, Task Id={1}", solution.Id, solution.Solutions1[0].TaskId);
            Console.WriteLine("Work done");
            //  m_solvingTimer.Enabled = false;
            bool result = Send(SerializeMessage<Solutions>(solution));
            m_status.Threads[0] = new StatusThread()
            {
                HowLong = 0,
                ProblemInstanceIdSpecified = false,
                ProblemType = "",
                TaskIdSpecified = false,
                State = StatusThreadState.Idle
            };
        }

        public void Start()
        {
            bool result = RegisterComponent();
            if (result)
            {
                try
                {
                    ReceiveRegisterResponse();
                    while (true)
                    {
                        ReceivePartialProblem();
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Connection with server was interrupted");
                }
            }
            else
                Console.WriteLine("Registration was failed");
        }
    }
}