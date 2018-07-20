using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace GameSocket
{
    internal class AsyncUserToken : IAsyncUserToken
    {
        public AsyncUserToken()
        {
            ConnId = Guid.NewGuid();
            Buffer = new List<byte>();
            SocketConnType = SocketConnType.None;
        }

        /// <summary>
        /// 连接ID
        /// </summary>
        public Guid ConnId { get; set; }

        /// <summary>
        /// 连接对象
        /// </summary>
        public Socket Socket { get; set; }

        /// <summary>
        /// 连接类型
        /// </summary>
        public SocketConnType SocketConnType { get; set; }

        /// <summary>  
        /// 数据缓存区  
        /// </summary>  
        public List<byte> Buffer { get; set; }
    }
}
