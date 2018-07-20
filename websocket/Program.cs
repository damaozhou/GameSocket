using GameSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace websocket
{
    class Program
    {
        public static ConcurrentDictionary<Guid, UserInfo> dic_UserInfo;
        static SocketManager socket;
        static void Main(string[] args)
        {
            dic_UserInfo = new ConcurrentDictionary<Guid, UserInfo>();
            socket = new SocketManager(1000, 1024);
            socket.HeartBeatTimeOut = 30;
            socket.Init();
            socket.ListenClose += Socket_ListenClose;
            socket.ReceiveData += Socket_ReceiveData;
            socket.SocketClose += Socket_SocketClose; ;
            socket.SocketConnect += Socket_SocketConnect;
            socket.Start(new IPEndPoint(IPAddress.Parse("192.168.1.13"), 10000));
            Console.WriteLine("开始监听");
            Console.Read();
        }

        /// <summary>
        /// socket连接
        /// </summary>
        /// <param name="token"></param>
        private static void Socket_SocketConnect(Guid connId)
        {
            dic_UserInfo.AddOrUpdate(connId, new UserInfo(), (id, t) => { return new UserInfo(); });
        }

        /// <summary>
        /// socket关闭
        /// </summary>
        /// <param name="token"></param>
        private static void Socket_SocketClose(Guid connId, SocketColseType colseType)
        {
            dic_UserInfo.TryRemove(connId, out UserInfo tk);
        }

        /// <summary>
        /// 接受消息
        /// </summary>
        /// <param name="token"></param>
        /// <param name="buff"></param>
        private static void Socket_ReceiveData(Guid connId, string Message)
        {
            if (dic_UserInfo.ContainsKey(connId))
            {
                var userInfo = dic_UserInfo[connId];
                socket.Send(connId, "这是socket" + Message);
            }
        }

        /// <summary>
        /// 关闭服务
        /// </summary>
        private static void Socket_ListenClose()
        {
            dic_UserInfo.Clear();
        }
    }
}
