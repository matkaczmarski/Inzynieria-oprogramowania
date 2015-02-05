using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SE_lab;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace ClientTests
{
    public class ComponentA : Component { }

    //there are passed only when only one is run in one time

    [TestClass]
    public class ComponentTest
    {

        // start server mockup
        public Semaphore synchronize = new Semaphore(0, 1);
        public Socket listener;

        public void CreateServer(object _port)
        {
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), (int)_port);
            listener.Bind(localEndPoint);
            listener.Listen(100);
            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            synchronize.Release();
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            listener.EndAccept(ar);
        }
        //end server mockup


        [TestMethod]
        public void Constructor()
        {
            ComponentA a = new ComponentA();
            Assert.AreNotEqual(null, a);
        }

        [TestMethod]
        public void WrongConnect()
        {
            int port = 11000;
            ComponentA a = new ComponentA();
            bool b = a.Connect(IPAddress.Parse("127.0.0.1"), port);
            Thread t = new Thread(new ParameterizedThreadStart(CreateServer));
            t.Start(port);
            synchronize.WaitOne();
            b = a.Connect(IPAddress.Parse("127.0.0.1"), port + 1);
            Assert.AreEqual(false, b);
            t.Abort();
        }

        [TestMethod]
        public void RightConnect()
        {
            int port = 11005;
            ComponentA a = new ComponentA();
            Thread t = new Thread(new ParameterizedThreadStart(CreateServer));
            t.Start(port);
            synchronize.WaitOne();
            bool b=a.Connect(IPAddress.Parse("127.0.0.1"), port);
            Assert.AreEqual(true, b);
            b = a.Connect(IPAddress.Parse("127.0.0.1"), port);
            Assert.AreEqual(false, b);
        }

        [TestMethod]
        public void Send()
        {
            int port = 11010;
            ComponentA a = new ComponentA();
            string data = "data";
            bool b = a.Send(Encoding.UTF8.GetBytes(data));
            Assert.AreEqual(false, b);
            Thread t = new Thread(new ParameterizedThreadStart(CreateServer));
            t.Start(port);
            synchronize.WaitOne();
            b = a.Connect(IPAddress.Parse("127.0.0.1"), port);
            Assert.AreEqual(true, b);
            b = a.Send(Encoding.UTF8.GetBytes(data));
            Assert.AreEqual(true, b);
            t.Abort();
        }

        //[TestCleanup]
        //public void Clean()
        //{
        //    if (listener != null && listener.Connected)
        //    {
        //        listener.Shutdown(SocketShutdown.Both);
        //        listener.Close();
        //    }
        //}
    }
}
