using Newtonsoft.Json;
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

        public delegate void OnReceiveMessageHandler(Socket socket, SocketMessage msg);
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

                timer.Start();
                socket.Bind(localEndPoint);

                Log("Started the server");
                isOnline = true;

                while (isOnline)
                {
                    socket.Listen(1);

                    Socket newSocket = socket.Accept();

                    Thread receiveThread = new Thread(ReceiveMessages);
                    socketConnections.Add(newSocket, receiveThread);
                    receiveThread.Start(newSocket);
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

        public void Send(Socket socket, SocketMessage socketmessage)
        {
            try
            {
                if(!socketConnections.ContainsKey(socket))
                {
                    Log("The socket " + socket.RemoteEndPoint + " is not a part of the socket connections list");
                    return;
                }
                if(!socket.Connected)
                {
                    Log("The socket is not connected anymore");
                    socket.Close();

                    return;
                }

                string messageSerialized = JsonConvert.SerializeObject(socketmessage);
                string messageSerializedAndEncrypted = messageSerialized;//Crypto.EncryptString(messageSerialized);

                byte[] messageInBytes = ASCIIEncoding.ASCII.GetBytes(messageSerializedAndEncrypted);
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

        public string[] DbSelectRowFromTableString(string table, string username)
        {
            using (var cmd = new OleDbCommand("SELECT * FROM " + table + " WHERE Username = " + username, dbConnection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    var data = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        data.Add(reader.GetString(i));
                    }

                    return data.ToArray();
                }
            }
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
            Socket sock = socket as Socket;
            try
            {
                while (isOnline)
                {
                    byte[] msgInBytes = new byte[512];
                    sock.Receive(msgInBytes);
                    
                    string message = ASCIIEncoding.ASCII.GetString(msgInBytes);//Crypto.DecryptString(msgInBytes);
                    SocketMessage socketMessage = JsonConvert.DeserializeObject<SocketMessage>(message);
                    if (OnReceiveMessage != null)
                        OnReceiveMessage(sock, socketMessage);
                }
            }
            catch (Exception ex)
            {
                Log("Exception while receiving messages: " + ex.ToString());
            }
            finally
            {
                if (sock != null)
                {
                    // Send a "forceclose" message to the client?

                    if (sock.Connected)
                        sock.Close();
                    socketConnections.Remove(sock);
                }
            }
        }
        private void CheckClientConnections()
        {
            for (int i = 0; i < socketConnections.Count; i++)
            {
                Socket sock = socketConnections.ElementAt(i).Key;
                if (sock.Connected)
                {
                    bool part1 = sock.Poll(350, SelectMode.SelectRead); // Find a right time for the response
                    bool part2 = (sock.Available == 0);
                    if (part1 && part2)
                    {
                        sock.Close();
                        socketConnections.Remove(sock);
                    }
                }
            }
        }

        private void DbInitialize(string dbname)
        {
            if (dbConnection == null)
            {
                dbConnection = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + dbname + ";Persist Security=False;");
                dbConnection.Open();
            }
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
