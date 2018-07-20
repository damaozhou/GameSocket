using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameSocket
{
    /// <summary>
    /// Socket连接类型，WebSocket和原生Socket
    /// </summary>
    public enum SocketConnType
    {
        None = 0,
        Socket = 1,
        WebSocket = 2
    }
}
