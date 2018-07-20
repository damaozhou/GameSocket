using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameSocket
{
    class HeartBeatDaemon
    {
        /// <summary>
        /// SocketManager
        /// </summary>
        SocketManager socketManager;

        /// <summary>
        /// 心跳过期时间
        /// </summary>
        int HeartBeatTimeOut;
        /// <summary>
        /// 心跳守护
        /// </summary>
        /// <param name="socketManager"></param>
        public HeartBeatDaemon(SocketManager socketManager)
        {
            this.socketManager = socketManager;
            HeartBeatTimeOut = socketManager.HeartBeatTimeOut;
        }

        public void Run()
        {
            Task.Run(() => {
                while (true)
                {
                    if (socketManager.dicSocketEventArgs != null)
                    {
                        var lst_connId = socketManager.dicSocketEventArgs.Where(s => s.Value.HeartBeatTime.AddSeconds(HeartBeatTimeOut) < DateTime.Now).Select(s => s.Key);
                        if (lst_connId != null)
                            lst_connId.ToList().ForEach(l => {
                                socketManager.CloseSocket(l);
                            });
                    }
                    Thread.Sleep(5000);
                }
            });
        }

    }
}
