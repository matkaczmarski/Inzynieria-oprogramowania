using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SE_lab;
using SE_lab.Messages;

namespace ServerTest
{
    [TestClass]
    public class ServerTests
    {
        public CommunicationServer server;

        [TestInitialize]
        public void Initialize()
        {
            server = new CommunicationServer();
        }

        [TestCleanup]
        public void Dispose()
        {
           // server.listener.Close();
        }

        [TestMethod]
        public void Constructor()
        {
            Assert.AreNotEqual(null, server);
        }

        [ExpectedException(typeof(FormatException))]
        [TestMethod]
        public void StartServerWrongIP()
        {
            server.StartServer(System.Net.IPAddress.Parse("1000.2.3.4"), 100, 100); 
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void StartServerWrongPort()
        {
            server.StartServer(System.Net.IPAddress.Parse("100.2.3.4"), -100, 100);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void StartServerWrongTimeout()
        {
            server.StartServer(System.Net.IPAddress.Parse("100.2.3.4"), 100, -100);
        }

        [TestMethod]
        public void StartServer()
        {
            server.StartServer(System.Net.IPAddress.Parse("127.0.0.1"), 100, 1000);
        }
    }
}
