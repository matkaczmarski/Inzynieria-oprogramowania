using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Configuration;
using SE_lab.Messages;


namespace SE_lab
{
    public class ComponentObject
    {
        public Socket m_socket;
        public byte[] m_buffer;
        public int m_offset;
        //public Semaphore m_receiveBlock;
        public ComponentObject()
        {
            m_buffer = new byte[4096];
            m_offset = 0;
            //m_receiveBlock = new Semaphore(1, 1);
        }
    }

    public class CommunicationServer
    {
        public static int Main(String[] args)
        {
            CommunicationServer communicationServer = new CommunicationServer();
            int port = int.Parse(ConfigurationManager.AppSettings["portNumber"]);
            int timeout = int.Parse(ConfigurationManager.AppSettings["timeout"]);
            communicationServer.StartServer(port, timeout);
            while (true)
            {
                communicationServer.WaitForConnections();
            }
        }

        // Thread signal.
        public ManualResetEvent _allDone = new ManualResetEvent(false);

        private Socket listener;
        //data
        private Semaphore m_memoryLock = new Semaphore(1, 1);
        private List<Problem> m_problems;
        private List<ComputationalNode> m_nodes;
        private List<TaskManager> m_managers;
        private static ulong m_componentId = 1;
        private static ulong m_problemId = 1;
        private int m_timeout;
        private Timer m_checkAvailability;

        public CommunicationServer()
        {
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Start work of server
        /// </summary>
        /// <param name="_portNumber">Port on which server listening</param>
        /// <param name="_timeout">Time after component send theirs status messages</param>
        public void StartServer(int _portNumber, int _timeout)
        {
            m_timeout = _timeout;
            m_nodes = new List<ComputationalNode>();
            m_managers = new List<TaskManager>();
            m_problems = new List<Problem>();
            m_checkAvailability = new Timer(CheckProblemsAndComponentsState, null, 5 * m_timeout, 5 * m_timeout);
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, _portNumber);
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
                Console.WriteLine("Waiting for a connection...");
            }
            catch (Exception)
            {
                Console.WriteLine("Dramatic problem with the server");
            }
        }

        public void WaitForConnections()
        {
            listener.BeginAccept(new AsyncCallback(AcceptConnectionCallback), listener);
            _allDone.WaitOne();
            _allDone.Reset();
        }

        private void AcceptConnectionCallback(IAsyncResult _asyncResult)
        {
            _allDone.Set();
            Socket handler = listener.EndAccept(_asyncResult);
            handler.ReceiveTimeout = 100;
            ComponentObject state = new ComponentObject() { m_socket = handler };
         //   state.m_receiveBlock.WaitOne();
            handler.BeginReceive(state.m_buffer, 0, state.m_buffer.Length, 0, new AsyncCallback(ReceiveDataCallback), state);
        }

        private void ReceiveDataCallback(IAsyncResult _asyncResult)
        {
            int bytesRead = 0;
            ComponentObject state = (ComponentObject)_asyncResult.AsyncState;
            Socket handler = state.m_socket;
            try
            {
                bytesRead = handler.EndReceive(_asyncResult);
            }
            catch (SocketException)
            {
              //  state.m_receiveBlock.Release();
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                return;
            }
            if (bytesRead > 0)
            {
                do
                {
                    state.m_offset += bytesRead;
                    if (state.m_offset == state.m_buffer.Length)
                    {
                        byte[] request = new byte[state.m_buffer.Length * 2];
                        state.m_buffer.CopyTo(request, 0);
                        state.m_buffer = request;
                    }
                    try
                    {
                        bytesRead = handler.Receive(state.m_buffer, state.m_offset, state.m_buffer.Length - state.m_offset, 0);
                    }
                    catch (SocketException) { bytesRead = 0; }

                } while (bytesRead > 0);
            }
            if (state.m_offset > 0)
            {
                List<byte[]> messages = DivideMessages(state.m_buffer, state.m_offset);
                //state.m_receiveBlock.Release();
                byte[] msg = new byte[0];
                for (int i = 0; i < messages.Count; i++)
                {
                    msg = msg.Concat(messages[i]).ToArray();
                    switch (GetMessageType(msg))
                    {
                        case "Status":
                            try
                            {
                                var statusMessage = DeserializeMessage<Status>(msg);
                                RefreshLastStatus(statusMessage);
                                Console.WriteLine("Status message received from component ID = {0}", statusMessage.Id);
                                msg = new byte[0];
                            }
                            catch (Exception) { continue; }
                            break;
                        case "Register":
                            try
                            {
                                var registerMessage = DeserializeMessage<Register>(msg);
                                Console.WriteLine("Register Message received from {0}, address: {1}",
                                    registerMessage.Type == RegisterType.ComputationalNode ? "ComputationalNode" : "TaskManager",
                                    handler.RemoteEndPoint.ToString());
                                ulong componentId = RegisterComponent(registerMessage, state);
                                ResponseRegisterMessage(handler, componentId);
                                msg = new byte[0];
                            }
                            catch (Exception) { continue; }
                            break;
                        case "SolveRequest":
                            try
                            {
                                var solveRequestMessage = DeserializeMessage<SolveRequest>(msg);
                                ulong problemId = AddProblem(solveRequestMessage);
                                Console.WriteLine("Problem nr {0} was registered", problemId);
                                ResponseSolveRequest(state.m_socket, problemId);
                                m_memoryLock.WaitOne();
                                SendProblemToTaskManager(problemId);
                                m_memoryLock.Release();
                                msg = new byte[0];
                            }
                            catch (Exception) { continue; }
                            break;
                        case "SolutionRequest":
                            try
                            {
                                var solutionRequestMessage = DeserializeMessage<SolutionRequest>(msg);
                                SendSolutionInfo(handler, solutionRequestMessage);
                                msg = new byte[0];
                            }
                            catch (Exception) { continue; }
                            break;
                        case "SolvePartialProblems":
                            try
                            {
                                var solvePartialProblemMessage = DeserializeMessage<SolvePartialProblems>(msg);
                                SaveTaskManagerDataAndSendPartialProblemsToNodes(solvePartialProblemMessage);
                                Console.WriteLine("Sent partial problem to nodes. Problem ID={0}", solvePartialProblemMessage.Id);
                                msg = new byte[0];
                            }
                            catch (Exception) { continue; }
                            break;
                        case "Solutions":
                            try
                            {
                                var solutionsMessage = DeserializeMessage<Solutions>(msg);
                                ReceiveSolution(solutionsMessage);
                                Console.WriteLine("Solution Id = {0} received from component", solutionsMessage.Id);
                            }
                            catch (Exception) { continue; }
                            break;
                        default:
                            Console.WriteLine("Unknown message type ");
                            break;
                    }
                }
                state.m_offset = 0;
                try
                {
                   // state.m_receiveBlock.WaitOne();
                    handler.BeginReceive(state.m_buffer, 0, state.m_buffer.Length, 0, new AsyncCallback(ReceiveDataCallback), state);
                }
                catch (Exception) { }
            }
        }

        private void RefreshLastStatus(Status _status)
        {
            m_memoryLock.WaitOne();
            var computationalNode = m_nodes.Find(e => e.Id == _status.Id);
            if (computationalNode != null)
            {
                computationalNode.lastStatus = DateTime.Now;
                computationalNode.statusThreads = _status.Threads;
            }
            else
            {
                var taskManager = m_managers.Find(e => e.Id == _status.Id);
                if (taskManager != null)
                {
                    taskManager.lastStatus = DateTime.Now;
                    taskManager.statusThreads = _status.Threads;
                }
            }
            m_memoryLock.Release();
        }

        private ulong RegisterComponent(Register _registerMessage, ComponentObject _state)
        {
            ulong newComponentId = 0;
            switch (_registerMessage.Type)
            {  
                case RegisterType.ComputationalNode:
                    ComputationalNode computationalNode = new ComputationalNode()
                    {
                        lastStatus = DateTime.Now,
                        statusThreads = new StatusThread[_registerMessage.ParallelThreads],
                        solvableProblems = _registerMessage.SolvableProblems,
                        state = _state
                    };
                    // for (int i = 0; i < computationalNode.statusThreads.Length; i++)
                    // {
                    computationalNode.statusThreads[0] = new StatusThread()
                    {
                        TaskIdSpecified = false,
                        ProblemInstanceIdSpecified = false, ProblemType="",
                        State = StatusThreadState.Idle, HowLong=0
                    };
                    // }
                    m_memoryLock.WaitOne();
                    newComponentId = computationalNode.Id = m_componentId++;
                    m_nodes.Add(computationalNode);
                    m_memoryLock.Release();
                    break;
                case RegisterType.TaskManager:
                    TaskManager taskManager = new TaskManager()
                    {
                        lastStatus = DateTime.Now,
                        statusThreads = new StatusThread[_registerMessage.ParallelThreads],
                        solvableProblems = _registerMessage.SolvableProblems,
                        state = _state
                    };
                    // for (int i = 0; i < taskManager.statusThreads.Length; i++)
                    //  {
                    taskManager.statusThreads[0] = new StatusThread()
                    {
                        TaskIdSpecified = false,
                        ProblemInstanceIdSpecified = false, ProblemType="",
                        State = StatusThreadState.Idle,HowLong=0
                    };
                    //}
                    m_memoryLock.WaitOne();
                    newComponentId = taskManager.Id = m_componentId++;
                    m_managers.Add(taskManager);
                    m_memoryLock.Release();
                    break;
            }
            return newComponentId;
        }

        private void ResponseRegisterMessage(Socket _socket, ulong _componentId)
        {
            RegisterResponse registerResponse = new RegisterResponse()
            {
                Id = _componentId,
                Timeout = new DateTime(1, 1, 1, 0, 0, m_timeout / 1000)
            };
            Send(SerializeMessage(registerResponse), _socket);
        }

        private ulong AddProblem(SolveRequest _solveRequestMessage)
        {   
            Problem problem = new Problem()
            {
                Data = _solveRequestMessage.Data,
                ProblemType = _solveRequestMessage.ProblemType,
                manager = null,
                timeoutOccured = false,
                timeoutSpecified=_solveRequestMessage.SolvingTimeoutSpecified
            };
            if(_solveRequestMessage.SolvingTimeoutSpecified)
              problem.solvingTimeout=_solveRequestMessage.SolvingTimeout;
            m_memoryLock.WaitOne();
            problem.Id = m_problemId++;
            m_problems.Add(problem);
            m_memoryLock.Release();
            return problem.Id;
        }

        private void ResponseSolveRequest(Socket _socket, ulong _problemId)
        {
            SolveRequestResponse solveRequestResponse = new SolveRequestResponse() { Id = _problemId };
            Send(SerializeMessage(solveRequestResponse), _socket);
        }

        private bool SendProblemToTaskManager(ulong _problemId)
        {
            ComponentObject state = new ComponentObject();
            ulong freeThreads = 0;
            //  bool flag = false;
            Problem problem = m_problems.Find(e => e.Id == _problemId);
            /* foreach (var computationalNode in m_nodes)
                 if (computationalNode.solvableProblems.Contains(problem.ProblemType))
                     foreach (var thread in computationalNode.statusThreads)
                         if (thread.State == StatusThreadState.Idle)
                             freeThreads++;*/
            freeThreads = (ulong)m_nodes.Count(node => node.solvableProblems.Contains(problem.ProblemType) && node.statusThreads[0].State == StatusThreadState.Idle);

            foreach (var taskManager in m_managers)
                if (taskManager.solvableProblems.Contains(problem.ProblemType) && taskManager.statusThreads[0].State == StatusThreadState.Idle)
                {
                    problem.manager = taskManager;
                    state = taskManager.state;
                    break;
                }
            if (state.m_socket == null || freeThreads == 0)
            {
                problem.manager = null;
                return false;
            }
            DivideProblem divideProblem = new DivideProblem()
            {
                ComputationalNodes = freeThreads,
                Data = problem.Data,
                Id = _problemId,
                ProblemType = problem.ProblemType
            };
            return Send(SerializeMessage(divideProblem), state.m_socket);
        }

        private void SendSolutionInfo(Socket _handler, SolutionRequest _solutionRequestMessage)
        {
            m_memoryLock.WaitOne();
            Problem problem = m_problems.Find(e => e.Id == _solutionRequestMessage.Id);
            Solutions solutionInfo = new Solutions() { Id = _solutionRequestMessage.Id, Solutions1 = null };
            if (problem != null)
            {
                solutionInfo.CommonData = problem.Data;
                solutionInfo.ProblemType = problem.ProblemType;
                solutionInfo.Solutions1 = problem.solutions;
                //solutionInfo.Solutions1[0].TimeoutOccured = problem.timeoutOccured;
            }
            m_memoryLock.Release();
            Send(SerializeMessage(solutionInfo), _handler);
        }

        private void SaveTaskManagerDataAndSendPartialProblemsToNodes(SolvePartialProblems _solvePartialProblems)
        {   //_solvePartialProblems.SolvingTimeout??
            m_memoryLock.WaitOne();
            Problem problem = m_problems.Find(e => e.Id == _solvePartialProblems.Id);
            // if (_solvePartialProblems.CommonData == null)
            //    problem.isAllSubproblems = true;
           
            problem.manager = null;
            problem.nodes = new ComputationalNode[_solvePartialProblems.PartialProblems.Length];
            problem.problems = new SolvePartialProblemsPartialProblem[_solvePartialProblems.PartialProblems.Length];
            problem.solutions = new SolutionsSolution[_solvePartialProblems.PartialProblems.Length];
            //problem.Data = _solvePartialProblems.CommonData;
            //problem.solutions.AddRange(_solvePartialProblems = new SolutionsSolution[_solvePartialProblems.PartialProblems.Length];
            
            for (int ii = 0; ii < problem.solutions.Length; ii++)
            {
                problem.problems[ii] = _solvePartialProblems.PartialProblems[ii];
                problem.solutions[ii] = new SolutionsSolution() { Type = SolutionsSolutionType.Ongoing };
            }
            SolvePartialProblems partialProblem = new SolvePartialProblems()
            {
                CommonData = problem.Data,
                Id = _solvePartialProblems.Id,
                ProblemType = _solvePartialProblems.ProblemType,
                SolvingTimeoutSpecified = problem.timeoutSpecified,
                
            };
            if (partialProblem.SolvingTimeoutSpecified)
                partialProblem.SolvingTimeout = problem.solvingTimeout;
            int i = 0;
            foreach (var computationalNode in m_nodes)
            {
                if (computationalNode.solvableProblems.Contains(_solvePartialProblems.ProblemType) && computationalNode.statusThreads[0].State == StatusThreadState.Idle)
                {
                    //for (int j = 0; j < computationalNode.statusThreads.Length; j++)
                    //    if (computationalNode.statusThreads[j].State == StatusThreadState.Idle)
                    //        freeThreads++;
                    //freeThreads = Math.Min(freeThreads, _solvePartialProblems.PartialProblems.Length - i);
                    partialProblem.PartialProblems = new SolvePartialProblemsPartialProblem[1];
                    // for (int j = 0; j < freeThreads; j++, i++)
                    // {
                    partialProblem.PartialProblems[0] = problem.problems[i];
                    problem.nodes[i++] = computationalNode;
                    //}
                    Send(SerializeMessage(partialProblem), computationalNode.state.m_socket);
                }
                if (i == problem.problems.Length) break;
            }
            m_memoryLock.Release();
        }

        private void ReceiveSolution(Solutions _solutionsMessage)
        {
            m_memoryLock.WaitOne();
            var problem = m_problems.Find(e => e.Id == _solutionsMessage.Id);
            if (_solutionsMessage.Solutions1[0].TimeoutOccured)
                problem.timeoutOccured = true;
            if (_solutionsMessage.Solutions1[0].TaskIdSpecified == false)
            {
                problem.solutions = _solutionsMessage.Solutions1;
            }
            else
            {   
                // if (_solutionsMessage.Solutions1[0].TimeoutOccured != false)
                problem.nodes[(int)_solutionsMessage.Solutions1[0].TaskId] = null;
                problem.solutions[(int)_solutionsMessage.Solutions1[0].TaskId] = _solutionsMessage.Solutions1[0];
                // foreach (var solution in _solutionsMessage.Solutions1)
                //     problem.solutions[solution.TaskId] = solution;
            }
            m_memoryLock.Release();
        }

        //private void FreeComponent(ulong _id, ulong _problemId)
        //{
        //    ComputationalNode computationalNode = m_nodes.Find(e => e.Id == _id);
        //    if (computationalNode != null)
        //    {
        //        foreach (StatusThread statusThread in computationalNode.statusThreads)
        //        {
        //            if (statusThread.TaskId == _problemId)
        //            {
        //                statusThread.State = StatusThreadState.Idle;
        //                statusThread.TaskIdSpecified = false;
        //                statusThread.ProblemInstanceIdSpecified = false;
        //            }
        //        }

        //        TaskManager taskManager = m_managers.First();
        //        SendSolutionsToTaskManager(taskManager, _problemId);
        //    }
        //    else
        //    {
        //        TaskManager taskManager = m_managers.Find(e => e.Id == _id);
        //        if (taskManager != null)
        //        {
        //            foreach (StatusThread statusThread in taskManager.statusThreads)
        //            {
        //                if (statusThread.TaskId == _problemId)
        //                {
        //                    statusThread.State = StatusThreadState.Idle;
        //                    statusThread.TaskIdSpecified = false;
        //                    statusThread.ProblemInstanceIdSpecified = false;
        //                }
        //            }

        //            Problem problem = m_problems.Find(e => e.Id == _problemId);
        //            if (problem != null)
        //            {
        //                problem.solutions[0].Type = SolutionsSolutionType.Final;
        //            }
        //        }
        //    }
        //}

        private void SendSolutionsToTaskManager(TaskManager _manager, Problem _problem)
        {
            if (_manager.state.m_socket == null)
            {
                _problem.manager = null;
                return;
            }
            Solutions solutions = new Solutions()
            {
                CommonData = _problem.Data,
                Id = _problem.Id,
                ProblemType = _problem.ProblemType,
                Solutions1 = _problem.solutions
            };
            Send(SerializeMessage<Solutions>(solutions), _manager.state.m_socket);
        }

        private void CheckProblemsAndComponentsState(object _state)
        {
            m_memoryLock.WaitOne();

            List<ComputationalNode> removedNodes = m_nodes.FindAll(e => e.lastStatus.AddMilliseconds(5 * m_timeout) < DateTime.Now);
            List<TaskManager> removedManagers = m_managers.FindAll(e => e.lastStatus.AddMilliseconds(5 * m_timeout) < DateTime.Now);
            int nodeCounter = 0;
            int managerCounter = 0;
            foreach (var problem in m_problems)                                                     //sprawdzamy tutaj czy kazdy problem ma juz swojego taskManagera i czy podzielil on juz swoj problem
            {
                if (problem.problems == null)
                {
                    if (problem.manager == null || removedManagers.Contains(problem.manager))
                    {
                        problem.manager = null;
                        while (managerCounter < m_managers.Count)
                        {
                            if (m_managers[managerCounter].statusThreads[0].State == StatusThreadState.Idle && m_managers[managerCounter].solvableProblems.Contains(problem.ProblemType))
                            {
                                SendProblemToTaskManager(problem.Id);
                                break;
                            }
                            managerCounter++;
                        }
                    }
                }
                else
                {
                    if (problem.solutions[0].Type == SolutionsSolutionType.Final) continue;
                    if (problem.solutions.All(solution => solution.Type == SolutionsSolutionType.Partial))
                    {
                        if (problem.manager == null || removedManagers.Contains(problem.manager))
                        {
                            problem.manager = null;
                            while (managerCounter < m_managers.Count)
                            {
                                if (m_managers[managerCounter].statusThreads[0].State == StatusThreadState.Idle && m_managers[managerCounter].solvableProblems.Contains(problem.ProblemType))
                                {
                                    problem.manager = m_managers[managerCounter];
                                    SendSolutionsToTaskManager(m_managers[managerCounter], problem);
                                    break;
                                }
                                managerCounter++;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < problem.problems.Length; i++)
                        {
                            if ((problem.nodes[i] == null || removedNodes.Contains(problem.nodes[i])) && problem.solutions[i].Type == SolutionsSolutionType.Ongoing)
                            {
                                problem.nodes[i] = null;
                                while (nodeCounter < m_nodes.Count)
                                {
                                    if (m_nodes[nodeCounter].statusThreads[0].State == StatusThreadState.Idle && m_nodes[nodeCounter].solvableProblems.Contains(problem.ProblemType))
                                    {
                                        problem.nodes[i] = m_nodes[nodeCounter];
                                        SendPartialProblemToNode(problem, m_nodes[nodeCounter], i);
                                        nodeCounter++;
                                        break;
                                    }
                                    nodeCounter++;
                                }
                            }
                        }
                    }
                }
            }
            m_nodes.RemoveAll(computationalNode => removedNodes.Contains(computationalNode));
            m_managers.RemoveAll(manager => removedManagers.Contains(manager));
            Console.WriteLine("Components count: {0} ", m_nodes.Count + m_managers.Count);
            m_memoryLock.Release();
        }

        private void SendPartialProblemToNode(Problem problem, ComputationalNode computationalNode, int problemIndex)
        {
            SolvePartialProblems partialProblem = new SolvePartialProblems()
            {
                CommonData = problem.Data,
                Id = problem.Id,
                ProblemType = problem.ProblemType,
                SolvingTimeoutSpecified = problem.timeoutSpecified,

                PartialProblems = new SolvePartialProblemsPartialProblem[1]
                { new SolvePartialProblemsPartialProblem()
                 { TaskId=problem.problems[problemIndex].TaskId, 
                     Data=problem.problems[problemIndex].Data
                }
                }
            };
            if (problem.timeoutSpecified)
                partialProblem.SolvingTimeout = problem.solvingTimeout;
            Send(SerializeMessage(partialProblem), computationalNode.state.m_socket);
        }

        public bool Send(byte[] _data, Socket _host)
        {   
            int offset = 0;
            if (_host == null || !_host.Connected) return false;
            try
            {
                while (offset < _data.Length)
                    offset += _host.Send(_data, offset, _data.Length - offset, 0);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Deserialize message from xml format to c# class
        /// </summary>
        /// <typeparam name="T">Message class</typeparam>
        /// <param name="_xml">Xml message which we receive from socket</param>
        /// <returns>Message which we receive in c#</returns>
        private T DeserializeMessage<T>(byte[] _xml)
        {
            MemoryStream ms = new MemoryStream(_xml);
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            return (T)xmlSerializer.Deserialize(ms);
        }

        /// <summary>
        /// Serialize message in c# to xml format in bytes
        /// </summary>
        /// <typeparam name="T">Message class</typeparam>
        /// <param name="_message">Message in c# which we want to send</param>
        /// <returns>Xml message which we can send via socket</returns>
        private byte[] SerializeMessage<T>(T _message)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(_message.GetType());
            MemoryStream memoryStream = new MemoryStream();
            XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
            xmlSerializer.Serialize(xmlTextWriter, _message);
            memoryStream = (MemoryStream)xmlTextWriter.BaseStream;
            return memoryStream.ToArray();
        }

        /// <summary>
        /// helping method to retrieve information about message type
        /// </summary>
        /// <param name="_xmlMessage">Bytes which contain the xml message</param>
        /// <returns>name of type of message</returns>
        private string GetMessageType(byte[] _xml)
        {
            string xmlMessage = Encoding.UTF8.GetString(_xml);
            int index = 0;
            for (int i = xmlMessage.Length - 1; i >= 0; i--)
                if (xmlMessage[i] == '/')
                {
                    index = i;
                    break;
                }
            return xmlMessage.Substring(index + 1, xmlMessage.Length - index - 2);
            /*  MemoryStream ms = new MemoryStream(_xml);
              XmlDocument doc = new XmlDocument();
              doc.Load(ms);
              return doc.LastChild.Name;*/
        }

        /// <summary>
        /// divide concatenated messages 
        /// </summary>
        /// <param name="data">data which contains messages</param>
        /// <param name="length">length of the 'true' message (without the rubbish) </param>
        /// <returns>a list of corrected xml messages</returns>
        private List<byte[]> DivideMessages(byte[] data, int length)
        {
            //List<byte[]> messages = new List<byte[]>();
            //List<XmlDocument> documents = new List<XmlDocument>();
            /*XmlReader xmlReader=XmlReader.Create(new MemoryStream(data.Take(length).ToArray()));
            //xmlReader.MoveToContent();
            try
            {
                while (xmlReader.Read())
                {
                    // Check for XML declaration.
                    if (xmlReader.NodeType != XmlNodeType.XmlDeclaration)
                    {
                        throw new Exception("Expected XML declaration.");
                    }

                    // Move to the first element.
                    xmlReader.Read();
                    xmlReader.MoveToContent();
                    XmlDocument document = new XmlDocument();
                    document.Load(xmlReader.ReadSubtree());
                    byte[] bytes = Encoding.Default.GetBytes(document.OuterXml);
                    messages.Add(bytes);
                    //documents.Add(document);
                }
            }
            catch (XmlException ex)
            {
                // Record exception reading stream.
                // Move reader to start of next document or rethrow exception to exit.
            }*/
            //239-sign on the front of every message
            List<int> indexes = new List<int>();
            List<byte[]> messages = new List<byte[]>();
            for (int i = 0; i < length; i++)
                if (data[i] == 239)
                    indexes.Add(i);
            indexes.Add(length);
            for (int i = 0; i < indexes.Count - 1; i++)
            {
                byte[] msg = new byte[indexes[i + 1] - indexes[i]];
                Array.Copy(data, indexes[i], msg, 0, msg.Length);
                messages.Add(msg);
            }
            return messages;
          //  return messages;
        }
    }

}

