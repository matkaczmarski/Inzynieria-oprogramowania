using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace SE_lab
{
    public abstract class Component
    {
        protected static ManualResetEvent connectDone = new ManualResetEvent(false);
       // protected static ManualResetEvent sendDone = new ManualResetEvent(false);

        protected Socket host;
        private ReceivedData receivedData;
        //private byte[] localBuffer;
        private Semaphore m_receiveBlock;
        //private Semaphore m_receiveBlock2;
        private Semaphore m_sendBlock;

        public Component()
        {
            host = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            host.ReceiveTimeout = 50;
            //localBuffer = new byte[ReceivedData.BUFFERSIZE];
            m_receiveBlock = new Semaphore(0, 1);
            //m_receiveBlock2 = new Semaphore(1, 1);
            m_sendBlock = new Semaphore(1, 1);
        }

        public bool Connect(IPAddress _serverIP, int _portNumber)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(_serverIP, _portNumber);
                host.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), host);
                connectDone.WaitOne();
            }
            catch (Exception)
            { }
            return host.Connected;
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                host.EndConnect(ar);
            }
            catch (Exception) { }
            connectDone.Set();
        }

        /// <summary>
        /// Method used to send data to server
        /// </summary>
        /// <param name="data">Message to send to server</param>
        /// <returns>Method return true, when component succesfully send message; Method return false when send was failed</returns>
        public bool Send(byte[] _data)
        {
            m_sendBlock.WaitOne();
            int offset = 0;
            if (host == null || !host.Connected) return false;
            try
            {
                while (offset < _data.Length)
                    offset += host.Send(_data, offset, _data.Length - offset, 0);
            }
            catch (Exception)
            {
                m_sendBlock.Release();
                return false;
            }
            m_sendBlock.Release();
            return true;
            //if (host == null || !host.Connected) return false;
            //try
            //{
            //    host.BeginSend(data, 0, data.Length, 0,
            //        new AsyncCallback(SendCallback), null);
            //    sendDone.WaitOne();
            //}
            //catch (Exception)
            //{
            //    return false;
            //}
            //return true;
        }

        //private void SendCallback(IAsyncResult ar)
        //{
        //    try
        //    {
        //        int bytesSent =host.EndSend(ar);
        //    }
        //    catch (Exception) { }
        //    sendDone.Set();
        //}

        /// <summary>
        /// Method used to receive data from server
        /// </summary>
        /// <returns>Method return received message</returns>
        protected byte[] Receive()
        {
            receivedData = new ReceivedData();
            try
            {
                host.BeginReceive(receivedData.m_buffer, receivedData.m_offset, ReceivedData.BUFFERSIZE, 0, new AsyncCallback(ReceiveCallback), receivedData);
            }
            catch (Exception)
            {
            }
            m_receiveBlock.WaitOne();
            //m_receiveBlock2.Release();
            return receivedData.m_buffer.Take(receivedData.m_offset).ToArray();
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                //m_receiveBlock2.WaitOne();
                ReceivedData receivedData = (ReceivedData)ar.AsyncState;
                int bytesRead = host.EndReceive(ar);
                if (bytesRead > 0)
                {
                    do
                    {
                        receivedData.m_offset += bytesRead;
                        if (receivedData.m_offset == receivedData.m_buffer.Length)
                        {
                            byte[] request = new byte[receivedData.m_buffer.Length * 2];
                            receivedData.m_buffer.CopyTo(request, 0);
                            receivedData.m_buffer = request;
                        }
                        try
                        {
                            bytesRead = host.Receive(receivedData.m_buffer, receivedData.m_offset, receivedData.m_buffer.Length - receivedData.m_offset, 0);
                        }
                        catch (SocketException) { bytesRead = 0;}
                    } while (bytesRead > 0);
                    /*if (receivedData.m_offset + bytesRead == receivedData.m_buffer.Length)
                    {
                        byte[] response = new byte[receivedData.m_buffer.Length * 2];
                        receivedData.m_buffer.Take(receivedData.m_offset).ToArray().CopyTo(response, 0);
                        receivedData.m_buffer = response;
                    }
                    localBuffer.Take(bytesRead).ToArray().CopyTo(receivedData.m_buffer, receivedData.m_offset);
                    receivedData.m_offset += bytesRead;
                    if (bytesRead == ReceivedData.BUFFERSIZE)
                    {
                        host.BeginReceive(localBuffer, 0, ReceivedData.BUFFERSIZE, 0, new AsyncCallback(ReceiveCallback), receivedData);
                        return;
                    }*/
                }
                m_receiveBlock.Release();
            }
            catch (Exception)
            {
                Console.WriteLine("Connection with server was interrupted");
                m_receiveBlock.Release();
            }
        }

        /// <summary>
        /// Deserialize message from xml format to c# class
        /// </summary>
        /// <typeparam name="T">Message class</typeparam>
        /// <param name="_xml">Xml message which we receive from socket</param>
        /// <returns>Message which we receive in c#</returns>
        protected T DeserializeMessage<T>(byte[] _xml)
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
        protected byte[] SerializeMessage<T>(T _message)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(_message.GetType());
            MemoryStream memoryStream = new MemoryStream();
            XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
            xmlSerializer.Serialize(xmlTextWriter, _message);
            memoryStream = (MemoryStream)xmlTextWriter.BaseStream;
            return memoryStream.ToArray();
        }

        protected T SerializeToClass<T>(byte[] classInBytes)
        {
            MemoryStream ms = new MemoryStream(classInBytes);
            BinaryFormatter bf = new BinaryFormatter();
            ms.Write(classInBytes, 0, classInBytes.Length);
            ms.Seek(0, SeekOrigin.Begin);
            Object o = bf.Deserialize(ms);
            return (T)o;
        }

        protected byte[] SerializeFromClass<T>(T obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }
}
