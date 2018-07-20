using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GameSocket;

namespace websocket
{
    class UserInfo
    {
        public string UserId { get; set; }
        public Socket Socket { get; set; }
        public string Message { get; set; }
        public DateTime HeartBeatTime { get; set; }
    }
}