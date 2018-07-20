using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameSocket
{
    internal interface IAsyncUserToken
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        Guid ConnId { get; set; }

        /// <summary>
        /// 连接对象
        /// </summary>
        Socket Socket { get; set; }

        /// <summary>
        /// 连接类型
        /// </summary>
        SocketConnType SocketConnType { get; set; }

        /// <summary>  
        /// 数据缓存区  
        /// </summary>  
        List<byte> Buffer { get; set; }
    }
}