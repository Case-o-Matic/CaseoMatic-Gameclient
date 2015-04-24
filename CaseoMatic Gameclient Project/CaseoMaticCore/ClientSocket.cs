using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CaseoMaticCore
{
    public class ClientSocket
    {
        private const string logFilePath = "client.log";

        //public delegate void OnReceiveMessageHandler(SocketMessage msg);
        //public event OnReceiveMessageHandler OnReceiveMessage;

        public bool isConnected { get; private set; }
        public IPEndPoint localEndPoint { get; private set; }

        private Socket socket;
        //private Thread receiveThread;
        private List<string> logFileLines;

        public ClientSocket(int port)
        {
            try
            {
                localEndPoint = new IPEndPoint(IPAddress.Any, port);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // receiveThread = new Thread(ReceiveMessages);
                logFileLines = new List<string>();
            }
            catch (Exception ex)
            {
                Log("Exception while initializing a new client: " + ex.ToString());
            }
        }

        public void Connect(IPEndPoint serverendpoint)
        {
            try
            {
                socket.Bind(localEndPoint);
                socket.Connect(serverendpoint);
                // receiveThread.Start();

                isConnected = true;
                Log("Connected to the server");
            }
            catch (Exception ex)
            {
                Log("Exception while connecting to the server: " + ex.ToString());
            }
        }
        public void Stop()
        {
            try
            {
                socket.Disconnect(true);
                isConnected = false;

                Log("Stopped the client");
                WriteLogToFile();
            }
            catch (Exception ex)
            {
                Log("Exception while disconnecting: " + ex.ToString());
            }
            finally
            {
                WriteLogToFile();
            }
        }

        public string Send(SocketMessage socketmessage)
        {
            try
            {
                string messageSerialized = JsonConvert.SerializeObject(socketmessage);
                string messageSerializedAndEncrypted = messageSerialized;//Crypto.EncryptString(messageSerialized);

                byte[] messageInBytes = ASCIIEncoding.ASCII.GetBytes(messageSerializedAndEncrypted);
                socket.Send(messageInBytes);

                return messageSerialized;
            }
            catch (Exception ex)
            {
                Log("Exception while sending a message: " + ex.ToString());
            }
            return null;
        }
        public SocketMessage Receive()
        {
            try
            {
                var messageInBytes = new byte[256];
                socket.Receive(messageInBytes);

                string message = ASCIIEncoding.ASCII.GetString(messageInBytes);//Crypto.DecryptString(messageInBytes);
                return JsonConvert.DeserializeObject<SocketMessage>(ASCIIEncoding.ASCII.GetString(messageInBytes));
            }
            catch (Exception ex)
            {
                Log("Exception while receiving a message: " + ex.ToString());
                return new SocketMessage("exception", ex.ToString());
            }
        }

        //private void ReceiveMessages()
        //{
        //    try
        //    {
        //        while (true)
        //        {
        //            byte[] messageInBytes = new byte[256];
        //            socket.Receive(messageInBytes);

        //            string message = Crypto.DecryptString(Convert.ToBase64String(messageInBytes), Properties.Resources.CryptoPassword + "-server");
        //            SocketMessage socketMessage = JsonParser.Deserialize<SocketMessage>(Convert.ToBase64String(messageInBytes));
        //            if (OnReceiveMessage != null)
        //                OnReceiveMessage(socketMessage);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log("Exception while receiving messages: " + ex.ToString());
        //    }
        //}

        private void Log(string msg)
        {
            string text = "Client, " + DateTime.Now.ToString() + ": " + msg;

            Console.WriteLine(text);
            logFileLines.Add(text);
        }
        private void WriteLogToFile()
        {
            File.WriteAllLines(logFilePath, logFileLines.ToArray());
        }
    }
}
