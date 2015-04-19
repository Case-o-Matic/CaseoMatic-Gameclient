using Json;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CaseoMaticCore
{
    public class ServerSocket
    {
        private const string logFilePath = "server.log";

        public delegate void OnReceiveMessageHandler(SocketMessage msg);
        public event OnReceiveMessageHandler OnReceiveMessage;

        public bool isOnline { get; private set; }
        public IPEndPoint localEndPoint { get; private set; }

        private Socket socket;
        private Dictionary<Socket, Thread> socketConnections;
        private System.Timers.Timer timer;

        private OleDbConnection dbConnection;
        private List<string> logFileLines;

        public ServerSocket(int port)
        {
            try
            {
                logFileLines = new List<string>();
                localEndPoint = new IPEndPoint(IPAddress.Any, port);

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socketConnections = new Dictionary<Socket, Thread>();

                timer = new System.Timers.Timer();
                timer.Interval = 10000;
                timer.Elapsed += OnCheckClientConnectionsRoutine;
            }
            catch (Exception ex)
            {
                Log("Exception while initializing a new server: " + ex.ToString());
            }
        }

        public void Start(string dbname)
        {
            try
            {
                DbInitialize(dbname);
                socket.Bind(localEndPoint);

                timer.Start();
                Log("Started the server");
                isOnline = true;

                while (isOnline)
                {
                    Console.WriteLine("Listening to clients");
                    socket.Listen(1);
                    Console.WriteLine("Client connected");

                    Socket newSocket = socket.Accept();
                    Thread receiveThread = new Thread(ReceiveMessages);
                    Console.WriteLine("Socket accepted and receive-thread created");

                    socketConnections.Add(newSocket, receiveThread);
                    receiveThread.Start(newSocket);
                    Console.WriteLine("Started the receive-thread");
                }
            }
            catch (Exception ex)
            {
                Log("Exception while listening to clients: " + ex.ToString());
            }
        }
        public void Stop()
        {
            try
            {
                isOnline = false;

                DbClose();
                timer.Stop();

                socket.Close();
                foreach (var conn in socketConnections)
                {
                    conn.Key.Close();
                }

                Log("Stopped the server");
            }
            catch(Exception ex)
            {
                Log("Exception while closing the server: " + ex.ToString());
            }
            finally
            {
                WriteLogToFile();
            }
        }

        public void Send(SocketMessage socketmessage)
        {
            try
            {
                string messageSerialized = JsonParser.Serialize<SocketMessage>(socketmessage);
                string messageSerializedAndEncrypted = Crypto.EncryptString(messageSerialized, Properties.Resources.CryptoPassword + "-server");

                byte[] messageInBytes = Convert.FromBase64String(messageSerializedAndEncrypted);
                socket.Send(messageInBytes);
            }
            catch (Exception ex)
            {
                Log("Exception while sending a message: " + ex.ToString());
            }
        }

        public int GetConnectedClientsCount()
        {
            return socketConnections.Count;
        }
        public void Log(string msg)
        {
            string text = "Server, " + DateTime.Now.ToString() + ": " + msg;

            Console.WriteLine(text);
            logFileLines.Add(text);
        }

        public string[] DbSelectRowFromTable(string table, string username)
        {
            return new string[0] { };
        }
        public bool DbInsertRowIntoTable(string table, params object[] data)
        {
            return true;
        }
        public bool DbDeleteRowFromTable(string table, string key)
        {
            return true;
        }

        private void OnCheckClientConnectionsRoutine(object sender, EventArgs e)
        {
            CheckClientConnections();
        }

        private void ReceiveMessages(object socket)
        {
            try
            {
                Socket sock = socket as Socket;
                while (isOnline)
                {
                    Console.WriteLine("Receiving message");
                    byte[] msgInBytes = new byte[256];
                    sock.Receive(msgInBytes);
                    Console.WriteLine("Received a message");

                    string message = Crypto.DecryptString(Convert.ToBase64String(msgInBytes), Properties.Resources.CryptoPassword + "-client");
                    Console.WriteLine("The message: " + message);

                    SocketMessage socketMessage = JsonParser.Deserialize<SocketMessage>(message);
                    if (OnReceiveMessage != null)
                        OnReceiveMessage(socketMessage);
                }
            }
            catch (Exception ex)
            {
                Log("Exception while receiving messages: " + ex.ToString());
            }
        }
        private void CheckClientConnections()
        {
            for (int i = 0; i < socketConnections.Count; i++)
            {
                Socket sock = socketConnections.ElementAt(i).Key;
                bool part1 = sock.Poll(350, SelectMode.SelectRead); // Find a right time for the response
                bool part2 = (sock.Available == 0);
                if (part1 && part2)
                {
                    sock.Close();
                    socketConnections.Remove(sock);
                }
            }
        }

        private void DbInitialize(string dbname)
        {
            if (dbConnection == null)
                dbConnection = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + dbname + ";Persist Security=False");
            else
                Log("The database is already initialized");
        }
        private void DbClose()
        {
            if (dbConnection != null)
            {
                dbConnection.Close();
                dbConnection = null;
            }
            else
                Log("The database is already closed");
        }

        private void WriteLogToFile()
        {
            File.WriteAllLines(logFilePath, logFileLines.ToArray());
        }
    }
}
