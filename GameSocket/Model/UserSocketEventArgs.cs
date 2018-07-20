using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace GameSocket
{
    internal class UserSocketEventArgs
    {
        /// <summary>
        /// 表示异步套接字操作
        /// </summary>
        public SocketAsyncEventArgs SocketAsyncEventArgs { get; set; }

        public UserSocketEventArgs(SocketAsyncEventArgs e)
        {
            SocketAsyncEventArgs = e;
            HeartBeatTime = DateTime.Now;
        }

        /// <summary>
        /// 心跳时间
        /// </summary>
        public DateTime HeartBeatTime { get; set; }
    }
}
