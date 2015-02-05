using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
using SE_lab.Messages;
using Timer = System.Timers.Timer;
using UCCTaskSolver;
using System.Reflection;

namespace SE_lab
{

    class TaskManager : Component
    {
        public ManualResetEvent _allDone = new ManualResetEvent(false);

        private DateTime m_lastChangeTime;

        private TaskSolver taskSolver;

        private Timer m_statusTimer, m_solvingTimer;

        private Status m_status;
        private Semaphore m_lockStatus = new Semaphore(1, 1);

        public Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public TaskManager()
        {
            var addressIP = ConfigurationManager.AppSettings["serverAddressIP"];
            int port = int.Parse(ConfigurationManager.AppSettings["portNumber"]);
            m_lastChangeTime = DateTime.Now;
            m_status = new Status();
            m_status.Threads = new StatusThread[1] 
            { 
                  new StatusThread()
                  { 
                      TaskIdSpecified = false, 
                      ProblemInstanceIdSpecified = false,
                      State = StatusThreadState.Idle, HowLong=0, ProblemType=""
                  }
            };
            Connect(IPAddress.Parse(addressIP), port);
        }

        private void ReceiveRegisterResponse()
        {
            var registerResponse = DeserializeMessage<RegisterResponse>(Receive());
            m_status.Id = registerResponse.Id;
            Console.WriteLine("Received Register Response. Id = {0}", registerResponse.Id);
            m_statusTimer = new Timer(registerResponse.Timeout.Ticks / 10000);
            m_statusTimer.Elapsed += SendManagerStatus;
            m_statusTimer.Start();
            WaitForMessages();
        }

        public void SendManagerStatus(object _state, ElapsedEventArgs _elapsedEventArgs)
        {
            m_lockStatus.WaitOne();
            m_status.Threads[0].HowLong = (ulong)((DateTime.Now - m_lastChangeTime).TotalMilliseconds);
            bool result = Send(SerializeMessage<Status>(m_status));
            if (!result)
                m_statusTimer.Stop();
            m_lockStatus.Release();
        }

        public void Start()
        {
            RegisterComponent();
            while (true)
                WaitForMessages();
        }

        public bool RegisterComponent()
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
            register.Type = RegisterType.TaskManager;
            register.SolvableProblems = new string[myTypes.Count];
            for (int i = 0; i < myTypes.Count; i++)
            {
                TaskSolver solv = (TaskSolver)Activator.CreateInstance(myTypes[i], new byte[0]);
                register.SolvableProblems[i] = solv.Name;
            }
            register.ParallelThreads = (byte)m_status.Threads.Length;
            bool result = Send(SerializeMessage<Register>(register));
            if (result)
                ReceiveRegisterResponse();
            return result;
        }

        private void WaitForMessages()
        {
            Console.WriteLine("Waiting for messages...");
            DivideProblem divide;
            Solutions solution;
            byte[] buffer = Receive();
            try
            {
                divide = DeserializeMessage<DivideProblem>(buffer);
            }
            catch (Exception)
            {
                divide = null;
            }
            if (divide != null)
            {
                Console.WriteLine("Divide Problem received. Id= {0}", divide.Id);
                ReceiveProblem(divide);
            }
            else
            {
                try
                {
                    solution = DeserializeMessage<Solutions>(buffer);
                }
                catch (Exception)
                {
                    solution = null;
                }
                if (solution != null)
                {
                    Console.WriteLine("Partial solution receives. Id = {0}", solution.Id);
                    ReceivePartialSolutions(solution);
                }
            }
        }

        private void ReceivePartialSolutions(Solutions _solution)
        {
            bool isEnd = false;

           /* var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            List<string> loadedP = new List<string>();
            foreach (var asembly in loadedAssemblies)
            {
                try
                {
                    string s = asembly.Location;
                    loadedP.Add(s);
                }
                catch (Exception) { }
            }
            var loadedPaths = loadedP.ToArray();
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
                        if (solv.Name == _solution.ProblemType)
                        {
                            myType = type; isEnd = true; break;
                        }
                    }
                }
                if (isEnd) break;
            }

            taskSolver = (UCCTaskSolver.TaskSolver)Activator.CreateInstance(myType, _solution.CommonData);
            Console.WriteLine("Trying to choose final solution of problem ID = {0}", _solution.Id);
            m_lockStatus.WaitOne();
            m_lastChangeTime = DateTime.Now;
            m_status.Threads[0] = new StatusThread()
            {
                State = StatusThreadState.Busy,
                HowLong = 0,
                ProblemInstanceIdSpecified = true,
                ProblemInstanceId = _solution.Id,
                ProblemType = _solution.ProblemType,
                TaskIdSpecified = false
            };
            m_lockStatus.Release();
            Solutions finalSolution;

            ChooseFinalSolution(_solution);

        }

        private void ChooseFinalSolution(Solutions _solution)
        {
            allSolutions = _solution;
            // SolutionDescription solutions = SerializeToClass<SolutionDescription>(_solution.CommonData);
            ulong _allTime = 0;

            byte[][] solutions = new byte[_solution.Solutions1.Length][];
            for (int i = 0; i < _solution.Solutions1.Length; i++)
            {
                solutions[i] = _solution.Solutions1[i].Data;
                _allTime += _solution.Solutions1[i].ComputationsTime;
            }
            allTime = _allTime;
            taskSolver.SolutionsMergingFinished += taskSolver_SolutionsMergingFinished;
            taskSolver.MergeSolution(solutions);
        }

        private Solutions allSolutions;
        private ulong allTime;



        void taskSolver_SolutionsMergingFinished(EventArgs eventArgs, TaskSolver sender)
        {
            var _bestSolution = new Solutions()
            {
                Id = allSolutions.Id,
                CommonData = allSolutions.CommonData,
                ProblemType = allSolutions.ProblemType,
                Solutions1 = new SolutionsSolution[1]{new SolutionsSolution()
                {
                    Data = taskSolver.Solution,
                    TaskIdSpecified = false, 
                    TimeoutOccured=allSolutions.Solutions1.Any(sol=>sol.TimeoutOccured),
                    ComputationsTime=allTime,
                    Type = SolutionsSolutionType.Final
                }}
            };
            bool result = Send(SerializeMessage<Solutions>(_bestSolution));
            Console.WriteLine("Sending final solution to server.");
            Console.WriteLine("Work done");
            m_lockStatus.WaitOne();
            m_lastChangeTime = DateTime.Now;
            m_status.Threads[0] = new StatusThread()
            {
                State = StatusThreadState.Idle,
                HowLong = 0,
                ProblemInstanceIdSpecified = false,
                ProblemType = "",
                TaskIdSpecified = false
            };
            m_lockStatus.Release();
        }

        private void SendSolution(Solutions _finalSolution)  ////TUTAJ ZMIENILAM 
        {
            // m_solvingTimer.Enabled = false;
            //Solutions solutions = new Solutions();
            //solutions.Id = _Id;
            //solutions.Solutions1 = new SolutionsSolution[1];
            //    _statusThread.State = StatusThreadState.Idle;
            //jakas obsluga watku by sie przydala


        }


        private void ReceiveProblem(DivideProblem _problem)
        {
            Console.WriteLine("Trying to divide Problem ID = {0}", _problem.Id);
            m_lockStatus.WaitOne();
            m_lastChangeTime = DateTime.Now;
            m_status.Threads[0] = new StatusThread()
            {
                State = StatusThreadState.Busy,
                HowLong = 0,
                ProblemInstanceIdSpecified = true,
                ProblemInstanceId = _problem.Id,
                ProblemType = _problem.ProblemType,
                TaskIdSpecified = false
            };
            m_lockStatus.Release();
            DivideProblem(_problem);
        }

        private void DivideProblem(DivideProblem _problem)
        {
            bool isEnd = false;

         /*   var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            List<string> loadedP=new List<string>();
            foreach (var asembly in loadedAssemblies)
            {
                try
                {
                    string s = asembly.Location;
                    loadedP.Add(s);
                }
                catch (Exception) { }
            }


            var loadedPaths = loadedP.ToArray();
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
                        if (solv.Name == _problem.ProblemType)
                        {
                            myType = type; isEnd = true; break;
                        }
                    }
                }
                if (isEnd) break;
            }

            taskSolver = (UCCTaskSolver.TaskSolver)Activator.CreateInstance(myType, _problem.Data);
            byte[][] partials = taskSolver.DivideProblem((int)_problem.ComputationalNodes);

            SendPartialProblem(_problem, partials);
            m_lockStatus.WaitOne();
            m_lastChangeTime = DateTime.Now;
            m_status.Threads[0] = new StatusThread()
            {
                State = StatusThreadState.Idle,
                HowLong = 0,
                ProblemInstanceIdSpecified = false,
                ProblemType = "",
                TaskIdSpecified = false
            };
            m_lockStatus.Release();

            //List<List<List<List<int>>>> permutationForNodes = new List<List<List<List<int>>>>();
            //for (int i = 0; i < (int)_problem.ComputationalNodes; i++)
            //    permutationForNodes.Add(new List<List<List<int>>>());
            //int ind = 0;
            //foreach (var set in allDevisions)
            // {
            //     permutationForNodes[ind].Add(set);
            //     ind = (ind + 1) % (int)_problem.ComputationalNodes;
            // }
            // SendPartialProblem(_problem, permutationForNodes);
        }

        private void SendPartialProblem(DivideProblem _problem, byte[][] partitions)
        {
            SolvePartialProblems solvePartialProblems = new SolvePartialProblems()
            {
                Id = _problem.Id,
                CommonData = _problem.Data,
                ProblemType = taskSolver.Name,
                PartialProblems = new SolvePartialProblemsPartialProblem[partitions.Length]
            };
            for (int i = 0; i < solvePartialProblems.PartialProblems.Length; i++)
            {
                solvePartialProblems.PartialProblems[i] = new SolvePartialProblemsPartialProblem()
                    {
                        TaskId = (ulong)i,
                        Data = partitions[i]
                    };
            }
            Send(SerializeMessage<SolvePartialProblems>(solvePartialProblems));
            Console.WriteLine("Sending Partial Problem from Problem ID={0} to server.", _problem.Id);
        }
    }
}
