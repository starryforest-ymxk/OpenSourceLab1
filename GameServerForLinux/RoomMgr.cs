//by WanChen B2
using GameServerForLinux;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServerForLinux
{
    internal class RoomMgr
    {
        private Dictionary<string, List<ProxySocket>> roomDic = new Dictionary<string, List<ProxySocket>>();

        private void RoomRigister(string roomName)
        {
            roomDic.Add(roomName, new List<ProxySocket>());
        }

        public void RoomDestroy(string roomName)
        {
            try
            {
                roomDic.Remove(roomName);
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"###[Server Error]### Try to destroy a room which doesn't exist. (Room name[{roomName}])");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private bool RoomJoin(string roomName, ProxySocket socket)
        {
            if (roomDic.ContainsKey(roomName) && roomDic[roomName].Count < 2)
            {
                roomDic[roomName].Add(socket);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool CreateOrJoinRoom(string roomName, ProxySocket socket)
        {
            if (!roomDic.ContainsKey(roomName))
            {
                RoomRigister(roomName);
            }
            return RoomJoin(roomName, socket);
        }

        public void RoomExit(string roomName, ProxySocket socket)
        {

            roomDic[roomName].Remove(socket);
            if (roomDic[roomName].Count == 0)
                RoomDestroy(roomName);
        }

        public int GetRoomPlayerNum(string roomName)
        {
            if (roomDic.ContainsKey(roomName))
                return roomDic[roomName].Count;
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"###[Server Error]### Room [{roomName}]doesn't exist.");
                Console.ForegroundColor = ConsoleColor.White;
                return 0;
            }

        }
        public int GetRoomNum()
        {
            return roomDic.Count;
        }

        public List<ProxySocket> GetRoom(string roomName)
        {
            if (roomDic.ContainsKey(roomName))
                return roomDic[roomName];
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"###[Server Error]### Room [{roomName}]doesn't exist.");
                Console.ForegroundColor = ConsoleColor.White;
                return null;
            }
        }

        public void RoomBroadcast(string info, ProxySocket socket)
        {
            try
            {
                foreach (var client in roomDic[socket.room])
                {
                    client.socket.Send(Encoding.UTF8.GetBytes(info));
                }

            }
            catch { }
        }

        public void RoomBroadcast(string info, string RoomName)
        {
            try
            {
                foreach (var client in roomDic[RoomName])
                {
                    client.socket.Send(Encoding.UTF8.GetBytes(info));
                }

            }
            catch { }
        }

        public void RoomSendMsg(string info, ProxySocket socket)
        {
            try
            {
                foreach (var client in roomDic[socket.room])
                {
                    if (!client.Equals(socket))
                    {
                        client.socket.Send(Encoding.UTF8.GetBytes(info));
                    }
                }

            }
            catch { }
        }

        public void RoomInfo()
        {
            int num = GetRoomNum();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{num}] Room(s) Occupied");
            if(num > 0 ) 
            foreach (var a in roomDic)
            {
                Console.Write($"Room [{a.Key}]: {{");
                foreach(var b in a.Value)
                {
                    Console.Write($" client[{b.ID}]");
                }
                Console.WriteLine(" }");
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void RoomClose()
        {
            foreach(var a in roomDic)
            {
                RoomBroadcast("Event_1string:ServerMsg:房间已关闭#", a.Key);
            }
            roomDic.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Clear all rooms successfully");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
