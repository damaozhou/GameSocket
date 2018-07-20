using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameSocket
{
    public enum SocketColseType
    {
        /// <summary>
        /// 未收到心跳关闭
        /// </summary>
        HeartBeatClose = 0,
        /// <summary>
        /// 客户端主动关闭
        /// </summary>
        ClientClose = 1,
        /// <summary>
        /// 服务端主动关闭
        /// </summary>
        ServerClose = 2,
        /// <summary>
        /// 异常关闭
        /// </summary>
        ExClose = 3
    }
}
