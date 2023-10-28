//WanChen
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using GameServerForLinux;
using System.Timers;

namespace GameServerForLinux
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Server server = new Server(6666);

            Console.WriteLine("***********************************************");
            Console.Write("**          ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[Two Rooms] Game Server");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("          **");
            Console.WriteLine("***********************************************");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("StarryTree Studio");
            Console.WriteLine("@Copyright 2023/3/2 by Wudi");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("***********************************************");
            server.Initialize();

            while (true)
            {
                string commandLine = Console.ReadLine();
                string command = commandLine.Split(' ')[0];
                switch (command)
                {
                    case "/Help":
                    case "/help":

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("**********************************************************************************************");
                        Console.WriteLine("/Help /help :                                Get help");
                        Console.WriteLine("/Quit /quit :                                Close server");
                        Console.WriteLine("/Send /send + {message} :                    Send message to clients connected");
                        Console.WriteLine("/DebugSend /debugSend + {message} :          Send debug message to clients connected");
                        Console.WriteLine("/Debug /debug + on/off :                     Control debug mode");
                        Console.WriteLine("/Client /client :                            Get the number of clients connected currently");
                        Console.WriteLine("/ClientInfo /clientInfo :                    Get the details of clients connected currently");
                        Console.WriteLine("/Room /room :                                Get the number of rooms currently");
                        Console.WriteLine("/RoomInfo /roomInfo :                        Get the details of rooms currently");
                        Console.WriteLine("/RoomsClear /roomsClear :                    Clear all the rooms in game");
                        Console.WriteLine("/RemoveClient /removeClient + {IP:port}:     Remove the specified client forcibly");
                        Console.WriteLine("/Record /record :                            Get the latest record of game");
                        Console.WriteLine("/Clean /clean :                              Clean all log messages");
                        Console.WriteLine("**********************************************************************************************");
                        Console.ForegroundColor = ConsoleColor.White;

                        break;

                    case "/Quit":
                    case "/quit":

                        server.BeforeExit();
                        break;

                    case "/Send":
                    case "/send":

                        string info = commandLine.Substring(5).Trim();
                        server.SendMsgFromServer(info);
                        break;

                    case "/DebugSend":
                    case "/debugSend":

                        string debugInfo = commandLine.Substring(10).Trim();
                        server.SendDebugMsgFromServer(debugInfo);
                        break;


                    case "/Debug":
                    case "/debug":

                        string mode = commandLine.Substring(6).Trim();
                        switch (mode)
                        {
                            case "on":
                            case "On":
                                server.Debug(true);
                                break;
                            case "off":
                            case "Off":
                                server.Debug(false);
                                break;
                            default:
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"Tip: [{command} {mode}] is not a command.");
                                Console.WriteLine("Input /Help or /help to get help.");
                                Console.ForegroundColor = ConsoleColor.White;
                                break;
                        }
                        break;

                    case "/Client":
                    case "/client":

                        server.GetClientNum();
                        break;

                    case "/ClientInfo":
                    case "/clientInfo":

                        server.GetClientInfo();
                        break;

                    case "/Room":
                    case "/room":

                        server.GetRoomNum();
                        break;
                    
                    case "/Record":
                    case "/record":

                        server.GetRecord();
                        break;

                    case "/RoomInfo":
                    case "/roomInfo":

                        server.GetRoomInfo();
                        break;
                    
                    case "/RoomsClear":
                    case "/roomsClear":

                        server.BeforeCloseRoom();
                        break;

                    case "/RemoveClient":
                    case "/removeClient":

                        server.BeforeRemoveClientForce(commandLine.Substring(13).Trim());
                        break;

                    case "/Clean":
                    case "/clean":

                        Console.Clear();
                        Console.WriteLine("***********************************************");
                        Console.Write("**          ");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("[Two Rooms] Game Server");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("          **");
                        Console.WriteLine("***********************************************");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("StarryTree Studio");
                        Console.WriteLine("@Copyright 2023/3/2 by Wudi");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("***********************************************");
                        Console.Write($"Current Time : ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{System.DateTime.Now.ToString("g")}");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("***********************************************");
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Tip: [{command}] is not a command.");
                        Console.WriteLine("Input /Help or /help to get help.");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }
            }
        }
    }


    /// <summary>
    /// 代理客户端
    /// </summary>
    class ProxySocket
    {
        public Socket socket;
        public EndPoint ID;
        public string room="";
        public string name="";

        public ProxySocket(Socket socket)
        {
            this.socket = socket;
            this.ID = socket.RemoteEndPoint;
        }
    }


    /// <summary>
    /// 服务器本体
    /// </summary>
    internal class Server
    {
        private Socket socket;
        private byte[] Receivedata = new byte[1024];
        private RoomMgr roomMgr;
        private RecordMgr recordMgr;

        private Dictionary<EndPoint, ProxySocket> ProxysocketDic = new Dictionary<EndPoint, ProxySocket>();

        private int port;

        private bool debugMode = false;
        private bool output = true;

        public bool DebugMode { get => debugMode; set => debugMode = value; }

        public Server(int port)
        {
            this.port = port;
        }

        /// <summary>
        /// 服务器初始化
        /// </summary>
        public void Initialize()
        {
            roomMgr = new RoomMgr();
            recordMgr = new RecordMgr();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen(2);
            StartAccept();
            Console.WriteLine("The server is initialized successfully");
            recordMgr.ReadRecord();
            Console.WriteLine("***********************************************");
            Console.Write($"Current Time : ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{System.DateTime.Now.ToString("g")}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("***********************************************");

        }
        /// <summary>
        /// 服务端开始应答
        /// </summary>
        private void StartAccept()
        {
            socket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }
        /// <summary>
        /// 应答回调函数
        /// </summary>
        /// <param name="asyncResult"></param>
        private void AcceptCallback(IAsyncResult asyncResult)
        {
            Socket client = socket.EndAccept(asyncResult);
            GetProxySocket(client);//及时纳入代理字典中
            client.Send(Encoding.UTF8.GetBytes("Server connected"));
            if(output)
                Console.WriteLine($"client[{client.RemoteEndPoint}] gets connected at {System.DateTime.Now:M.d HH:mm:ss}");

            StartReceive(client);
            StartAccept();
        }
        /// <summary>
        /// 异步接收客户端消息
        /// </summary>
        /// <param name="client"></param>
        private void StartReceive(Socket client)
        {

            client.BeginReceive(Receivedata, 0, Receivedata.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), client);

        }
        /// <summary>
        /// 接收消息的回调函数
        /// </summary>
        /// <param name="asyncResult"></param>
        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            if (asyncResult.AsyncState as Socket == null)
                return;

            ProxySocket client = GetProxySocket(asyncResult.AsyncState as Socket);

            int len;
            try
            {
                len = client.socket.EndReceive(asyncResult);
            }
            catch
            {
                ClientDisconnect(client);
                return;
            }

            if (len == 0)
            {
                ClientDisconnect(client);
                return;
            }

            string info = Encoding.UTF8.GetString(Receivedata, 0, len);

            if (DebugMode && output)
                Console.WriteLine($"client[{client.ID}] message: {info}");

            Task task = Task.Run(() => CheckInfo(info, client));

            StartReceive(client.socket);
        }
        /// <summary>
        /// 命令检索
        /// </summary>
        /// <param name="info"></param>
        /// <param name="client"></param>
        private void CheckInfo(string info, ProxySocket client)
        {

            if (info.StartsWith("Initialize:"))
            {
                ClientInitialize(client, info);
            }
            else if (info.StartsWith("GetRank:"))
            {
                SendRank(client);
            }
            else if (info.StartsWith("NewRank:"))
            {
                PutNewRank(info, client);
                UpdateRank();
            }
            else if (info.StartsWith("Exit:"))
            {
                RemoveClient(client);
            }
            else if (info.StartsWith("ExitRoom:"))
            {
                RemoveClientFromRoom(client);
            }
            else if (info.StartsWith("GetName:"))
            {
                SendPlayerName(client);
            }

            else
                Send(info, client);
        }
        /// <summary>
        /// 将客户端移出房间
        /// </summary>
        /// <param name="client"></param>
        private void RemoveClientFromRoom(ProxySocket client)
        {
            if (client.room == "")
                return;

            Send("Other exit", client);

            if (output)
                Console.WriteLine($"client[{client.ID}] exits room[{client.room}]");

            roomMgr.RoomExit(client.room, client);

            client.room = "";
            client.name = "";
        }
        /// <summary>
        /// 客户端断线
        /// </summary>
        /// <param name="client"></param>
        private void ClientDisconnect(ProxySocket client)
        {

            if (client.room != "")
            {
                roomMgr.RoomExit(client.room, client);
                if (output)
                    Console.WriteLine($"client[{client.ID}] exits room[{client.room}]");
                client.room = "";
                client.name = "";
            }

            ProxysocketDic.Remove(client.ID);

            if (output)
                Console.WriteLine($"client[{client.ID}] gets offline at {System.DateTime.Now:M.d HH:mm:ss}");

            client.socket.Close();

        }
        /// <summary>
        /// 移除代理客户端
        /// </summary>
        /// <param name="client"></param>
        private void RemoveClient(ProxySocket client)
        {
            Send("Other exit", client);
        }

        /// <summary>
        /// 强制关闭客户端前检查确认
        /// </summary>
        /// <param name="key"></param>
        public void BeforeRemoveClientForce(string key)
        {
            try
            {
                System.Net.IPAddress IPadr = System.Net.IPAddress.Parse(key.Split(':')[0].Trim());//先把string类型转换成IPAddress类型
                System.Net.IPEndPoint EndPoint = new System.Net.IPEndPoint(IPadr, int.Parse(key.Split(':')[1].Trim()));//传递
                if (!ProxysocketDic.ContainsKey(EndPoint))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Wrong IP address or wrong port.");
                    Console.ForegroundColor = ConsoleColor.White;
                }

                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Are you sure to remove client[{EndPoint}]? \n[Press Y/y to confirm, any other key to cancel.]");
                    Console.ForegroundColor = ConsoleColor.White;
                    output = false;
                    string? info = Console.ReadLine();
                    output = true;
                    if (info == "y" || info == "Y")
                        RemoveClientForce(EndPoint);
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Command [Remove Client] Cancel");
                        Console.ForegroundColor = ConsoleColor.White;
                        return;
                    }

                }

            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Wrong Format: You should type in like \"{{IP Address : Port}}\".");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        /// <summary>
        /// 强制关闭客户端
        /// </summary>
        /// <param name="key"></param>
        private void RemoveClientForce(System.Net.IPEndPoint key)
        {
            ProxySocket client = ProxysocketDic[key];
            try
            {
                client.socket.Send(Encoding.UTF8.GetBytes("Server disconnected"));
            }catch 
            {
                ClientDisconnect(client);
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Waiting... ");
            Console.ForegroundColor = ConsoleColor.White;
            Task task = new Task(()=> CheckClientRemove(key, client));
            task.Start();
        }
        /// <summary>
        /// 移除客户端检验
        /// </summary>
        /// <param name="key"></param>
        private void CheckClientRemove(System.Net.IPEndPoint key, ProxySocket client)
        {
            Task.Delay(1000);
            Console.ForegroundColor = ConsoleColor.Yellow;
            if(ProxysocketDic.ContainsKey(key))
                ClientDisconnect(client);

            Console.WriteLine($"Remove client[{key}] successfully.");
            Console.ForegroundColor = ConsoleColor.White;
        }
        /// <summary>
        /// 向客户端所在房间的其他客户端发送消息
        /// </summary>
        /// <param name="room"></param>
        /// <param name="player"></param>
        /// <param name="info"></param>
        private void Send(string info, ProxySocket socket)
        {
            if (socket.room != "" && roomMgr.GetRoomPlayerNum(socket.room) > 1)
                roomMgr.RoomSendMsg(info, socket);
        }
        /// <summary>
        /// 代理客户端初始化
        /// </summary>
        /// <param name="client"></param>
        /// <param name="info"></param>
        /// <param name="info"></param>
        private void ClientInitialize(ProxySocket client, string info)
        {
            string[] infos = info.Split(':');
            client.name = infos[2];

            if (output)
                Console.WriteLine($"client[{client.ID}] Initialize {{name[{infos[2]}], room[{infos[1]}]}}");

            if (!roomMgr.CreateOrJoinRoom(infos[1], client))
            {
                client.socket.Send(Encoding.UTF8.GetBytes("Room error"));
                if (DebugMode && output)
                    Console.WriteLine($"ERROR for client [{client.ID}]: room [{infos[1]}] has been occupied .");
                //房间号已被占用
            }
            else
            {
                client.room = infos[1];
                if (DebugMode && output)
                    Console.WriteLine($"client [{client.ID}] has been connected to room [{infos[1]}] .");
                if (roomMgr.GetRoomPlayerNum(infos[1]) == 2)
                {
                    roomMgr.RoomBroadcast("Room connected & ready", infos[1]);
                }
                else
                {
                    client.socket.Send(Encoding.UTF8.GetBytes("Room connected"));
                }

            }
        }
        /// <summary>
        /// 写入新纪录
        /// </summary>
        /// <param name="info"></param>
        /// <param name="client"></param>
        private void PutNewRank(string info, ProxySocket client)
        {
            string[] strings = info.Split(':');
            string nameA = client.name;
            string nameB = "";
            foreach (var c in roomMgr.GetRoom(client.room))
            {
                if (!c.Equals(client))
                    nameB = c.name;
            }
            recordMgr.PutInRecord(nameA + "/" + nameB + "/" + strings[1]);
            if (output)
                Console.WriteLine($"client[{client.ID}] creates new record {{{nameA + "/" + nameB + "/" + strings[1]}}}");
        }
        /// <summary>
        /// 更新客户端纪录数据
        /// </summary>
        private void UpdateRank()
        {
            foreach (var item in ProxysocketDic)
            {
                item.Value.socket.Send(Encoding.UTF8.GetBytes("Rank:" + recordMgr.GetLatestRecord()));
            }
        }
        /// <summary>
        /// 发送玩家名称
        /// </summary>
        /// <param name="client"></param>
        private void SendPlayerName(ProxySocket client)
        {
            foreach (var c in roomMgr.GetRoom(client.room))
            {
                if (!c.Equals(client))
                    client.socket.Send(Encoding.UTF8.GetBytes("PlayerName:" + c.name));
            }
        }
        /// <summary>
        /// 发送纪录
        /// </summary>
        /// <param name="client"></param>
        private void SendRank(ProxySocket client)
        {
            client.socket.Send(Encoding.UTF8.GetBytes("Rank:" + recordMgr.GetLatestRecord()));
        }
        /// <summary>
        /// 得到代理客户端
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        private ProxySocket GetProxySocket(Socket socket)
        {
            if (ProxysocketDic.ContainsKey(socket.RemoteEndPoint))
                return ProxysocketDic[socket.RemoteEndPoint];
            else
            {
                ProxySocket client = new ProxySocket(socket);
                ProxysocketDic.Add(client.ID, client);
                return client;
            }
        }
        /// <summary>
        /// 关闭服务器前确认
        /// </summary>
        public void BeforeExit()
        {
            GetClientNum();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Are you sure to close the server? \n[Press Y/y to confirm, any other key to cancel.]");
            Console.ForegroundColor = ConsoleColor.White;
            output = false;
            string? info = Console.ReadLine();
            output = true;
            if (info == "y" || info == "Y")
                Exit();
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Command [Server Quit] Cancel");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }
        }
        /// <summary>
        /// 清空房间前确认
        /// </summary>
        public void BeforeCloseRoom()
        {
            GetRoomNum();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Are you sure to close all rooms? \n[Press Y/y to confirm, any other key to cancel.]");
            Console.ForegroundColor = ConsoleColor.White;
            output = false;
            string? info = Console.ReadLine();
            output = true;
            if (info == "y" || info == "Y")
                roomMgr.RoomClose();
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Command [Server Quit] Cancel");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }
        }
        /// <summary>
        /// 关闭服务器
        /// </summary>
        public void Exit()
        {

            foreach (var item in ProxysocketDic)
            {
                item.Value.socket.Send(Encoding.UTF8.GetBytes("Server exit"));
            }
            Environment.Exit(0);
        }
        /// <summary>
        /// 发送调试信息
        /// </summary>
        /// <param name="command"></param>
        public void SendDebugMsgFromServer(string command)
        {
            if(command == "")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: You can't send nothing.");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }
            foreach (var item in ProxysocketDic)
                item.Value.socket.Send(Encoding.UTF8.GetBytes("command"));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Successfully send debug message{{{command}}}");
            Console.ForegroundColor = ConsoleColor.White;

        }
        /// <summary>
        /// 服务器发送信息
        /// </summary>
        public void SendMsgFromServer(string command)
        {
            if (command == "")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: You can't send nothing.");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }
            else if (command.Contains(':')|| command.Contains('#'))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Do not contain character '#' or '/'.");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                foreach (var item in ProxysocketDic)
                    item.Value.socket.Send(Encoding.UTF8.GetBytes("Event_1string:ServerMsg:" + command + "#"));
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Successfully Sent{{{command}}}");
                Console.ForegroundColor = ConsoleColor.White;
            }
            
        }

        public void Debug(bool mode)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            if (mode)
                Console.WriteLine($"\n***  Debug Mode On  ***");
            if (!mode)
                Console.WriteLine($"\n***  Debug Mode Off ***");
            Console.ForegroundColor = ConsoleColor.White;
            DebugMode = mode;

        }

        public void GetClientNum()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{ProxysocketDic.Count}] Client(s) Online");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void GetClientInfo()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{ProxysocketDic.Count}] Client(s) Online");
            if(ProxysocketDic.Count!=0)
                Console.WriteLine("Clients Details:");
            foreach(var a in ProxysocketDic)
            {
                Console.WriteLine($"client[{a.Key}]: Name[{a.Value.name}], Room[{a.Value.room}]");
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void GetRoomNum()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{roomMgr.GetRoomNum()}] Room(s) In Game");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void GetRecord()
        {
            int maxNum = recordMgr.maxNum;
            string record = recordMgr.GetLatestRecord();
            string[] recordLine = record.Split(':');
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Latest Record:");
            Console.WriteLine("***********************************************");
            for (int i = 0; i < maxNum; i++)
            {
                string[] parts = recordLine[i].Split('/');
                Console.WriteLine($"[{i+1}] Time: {Convert.ToInt32(parts[2])}  PlayerA: {parts[0]}  PlayerB: {parts[1]}");
            }
            Console.WriteLine("***********************************************");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void GetRoomInfo()
        {
            roomMgr.RoomInfo();
        }
    }
}
