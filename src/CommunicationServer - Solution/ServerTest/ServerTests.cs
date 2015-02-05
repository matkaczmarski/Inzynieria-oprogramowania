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

       

        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        [TestMethod]
        public void StartServerWrongPort()
        {
            server.StartServer(-100, 100);
        }

        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        [TestMethod]
        public void StartServerWrongTimeout()
        {
            server.StartServer( 100, -100);
        }

        [TestMethod]
        public void StartServer()
        {
            server.StartServer(100, 1000);
        }
    }
}
