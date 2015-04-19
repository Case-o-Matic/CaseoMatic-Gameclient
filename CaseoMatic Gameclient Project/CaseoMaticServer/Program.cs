using CaseoMaticCore;
using Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CaseoMaticServer
{
    class Program
    {
        private const float checkBlockedUsersTimeIntervall = 60000;

        public static string version = "1.04";
        private static ServerSocket serverSocket;

        private static Timer checkBlockedUsersForLoginTime;
        private static Dictionary<string, float> blockedUsersForLogin;

        static void Main(string[] args)
        {
            Console.WriteLine("Case-o-Matic Server v" + version);

            port:
            Console.Write("Server port> ");
            string portText = Console.ReadLine();

            int port;
            if (!int.TryParse(portText, out port))
            {
                Console.WriteLine(portText + " is not a valid port number, retry");
                goto port;
            }
            if(port < 4500 || port > 65535)
            {
                Console.WriteLine("The port number must be between 4500 and 65535");
                goto port;
            }

            blockedUsersForLogin = new Dictionary<string, float>();
            checkBlockedUsersForLoginTime = new Timer(checkBlockedUsersTimeIntervall);
            checkBlockedUsersForLoginTime.Elapsed += OnCheckBlockedUsersForLoginTimeRoutine;

            serverSocket = new ServerSocket(port);
            serverSocket.OnReceiveMessage += ServerSocket_OnReceiveMessage;

            serverSocket.Log("Server is initialized: " + serverSocket.localEndPoint.ToString());
            start:

            Console.WriteLine("Press \"Enter\" to start the server");
            if (Console.ReadKey().Key == ConsoleKey.Enter)
            {
                accountdbpath:
                Console.Write("Accounts-DB path> ");
                string accountsDbPath = Console.ReadLine();

                if (!File.Exists(accountsDbPath) || Path.GetExtension(accountsDbPath) != ".accdb")
                {
                    serverSocket.Log("The accounts database filepath \"" + accountsDbPath + "\" is not valid");
                    goto accountdbpath;
                }

                serverSocket.Start(accountsDbPath);
                checkBlockedUsersForLoginTime.Start();

                loop:
                bool breakLoop = false;
                while(!breakLoop)
                {
                    string input = "";
                    try
                    {
                        Console.Write("Command> ");
                        input = Console.ReadLine();

                        string cmd = input;
                        string[] cmdArgs = new string[0] { };
                        if (input.Contains(":"))
                        {
                            cmd = input.Split(':')[0];
                            cmdArgs = input.Split(':')[1].Split(',');
                        }
                        
                        switch (cmd)
                        {
                            case "stop":
                                breakLoop = true;
                                break;

                            case "blockuser":
                                string username = cmdArgs[0];
                                float blockTimeInSeconds = int.Parse(cmdArgs[0]);
                                blockedUsersForLogin.Add(username, blockTimeInSeconds * 1000);
                                break;

                            case "connectedclientscount":
                                serverSocket.Log("Currently connected players count: " + serverSocket.GetConnectedClientsCount());
                                break;

                            default:
                                serverSocket.Log(cmd + " is not a valid command");
                                continue;
                        }
                    }
                    catch(Exception ex)
                    {
                        serverSocket.Log("Exception while executing the command \"" + input + "\": " + ex.ToString());
                        goto loop;
                    }
                }
            }
            else
                goto start;

            if (serverSocket != null)
                serverSocket.Stop();
            if (checkBlockedUsersForLoginTime != null)
                checkBlockedUsersForLoginTime.Dispose();
        }

        private static void OnCheckBlockedUsersForLoginTimeRoutine(object sender, ElapsedEventArgs e)
        {
            for (int i = 0; i < blockedUsersForLogin.Count; i++)
            {
                var blockedUser = blockedUsersForLogin.ElementAt(i);
                blockedUsersForLogin[blockedUser.Key] -= checkBlockedUsersTimeIntervall;
                if (blockedUsersForLogin.ElementAt(i).Value <= 0)
                    blockedUsersForLogin.Remove(blockedUser.Key);
            }
        }

        private static void ServerSocket_OnReceiveMessage(SocketMessage msg)
        {
            switch (msg.type)
            {
                case "login":
                    string loginusername = msg.data[0];
                    string loginpw = msg.data[1];

                    string[] loginRow = serverSocket.DbSelectRowFromTable("AccountsCredentials", loginusername);
                    if (loginRow != null)
                    {
                        if (loginRow[1] == loginpw)
                        {
                            if(blockedUsersForLogin.ContainsKey(loginusername))
                            {
                                serverSocket.Log(loginusername + " tried to log into his account but is blocked for " + (blockedUsersForLogin[loginusername] / 1000) + " ms");
                                break;
                            }
                            serverSocket.Log("\"" + loginusername + "\" logged into his account");
                            serverSocket.Send(new SocketMessage("loginsuccess", loginRow[3], loginRow[4]));
                        }
                        else
                        {
                            serverSocket.Log("\"" + loginusername + "\" couldnt log into his account");
                            serverSocket.Send(new SocketMessage("loginfail", "pw"));
                        }
                    }
                    else
                    {
                        serverSocket.Send(new SocketMessage("loginfail", "name"));
                        serverSocket.Log("The username \"" + loginusername + "\" does not exist");
                    }
                    break;

                case "register":
                    string registerusername = msg.data[0];
                    string registerpw = msg.data[1];
                    string registeremail = msg.data[2];

                    string[] registerRow = serverSocket.DbSelectRowFromTable("AccountsCredentials", registerusername);
                    if (registerRow != null)
                    {
                        serverSocket.Send(new SocketMessage("registerfail", "alreadyexists"));
                        serverSocket.Log("Registering the account \"" + registerusername + "\" failed, username already exists");
                    }
                    else
                    {
                        string dataId = Guid.NewGuid().ToString();
                        serverSocket.DbInsertRowIntoTable("AccountsCredentials", registerusername, registerpw, registeremail, dataId);
                        serverSocket.DbInsertRowIntoTable("AccountsData", dataId, JsonParser.Serialize<AccountSettings>(new AccountSettings()));

                        serverSocket.Send(new SocketMessage("registersuccess", dataId));
                        serverSocket.Log("\"" + registerusername + "\" has been registered");
                    }
                    break;

                case "unregister":
                    string unregisterusername = msg.data[0];
                    string unregisterpw = msg.data[1];

                    string[] unregisterRow = serverSocket.DbSelectRowFromTable("AccountsCredentials", unregisterusername);
                    if(unregisterRow != null)
                    {
                        if(unregisterRow[2] == unregisterpw)
                        {
                            if (serverSocket.DbDeleteRowFromTable("AccountsCredentials", unregisterusername))
                            {
                                serverSocket.DbDeleteRowFromTable("AccountsData", unregisterRow[3]);
                                serverSocket.Send(new SocketMessage("unregistersuccess"));
                                serverSocket.Log("Unregistered \"" + unregisterusername + "\" successfully");
                            }
                            else
                            {
                                serverSocket.Send(new SocketMessage("unregisterfail", "internal"));
                                serverSocket.Log("Unregistering \"" + unregisterusername + "\" was unsuccessful");
                            }
                        }
                        else
                        {
                            serverSocket.Send(new SocketMessage("unregisterfail", "pw"));
                            serverSocket.Log("\"" + unregisterusername + "\" cant unregister his account because he gave wrong credentials");
                        }
                    }
                    else
                    {
                        serverSocket.Send(new SocketMessage("unregisterfail", "notfound"));
                        serverSocket.Log("\"" + unregisterusername + "\" does not exist");
                    }
                    break;

                case "getdata":
                    string dataid = msg.data[0];
                    string[] dataRow = serverSocket.DbSelectRowFromTable("AccountsData", dataid);
                    if(dataRow != null)
                    {
                        serverSocket.Send(new SocketMessage("getdatasuccess", dataRow[1] /* TODO: Add new data items if more data gets stored in the database */));
                    }
                    else
                    {
                        serverSocket.Send(new SocketMessage("getdatafail", "notfound"));
                    }
                    break;

                default:
                    serverSocket.Log("The message type \"" + msg.type + "\" is unknown");
                    break;
            }
        }
    }
}
