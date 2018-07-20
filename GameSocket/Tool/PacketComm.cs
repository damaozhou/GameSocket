using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Comm = GameSocket.Common;

namespace GameSocket
{
    internal class PacketComm
    {
        public static SocketConnType GetPacketType(byte[] data,out string packetStr)
        {
            packetStr = string.Empty;
            var Pac_Type = data[0] & 0xF;
            switch (Pac_Type)
            {
                case 7:
                    packetStr = Comm.BytesToString(data);
                    if (Regex.Match(packetStr.ToLower(), "upgrade: websocket").Value != "")//websocket握手协议
                        return SocketConnType.WebSocket;
                    return SocketConnType.None;
                default:
                    return SocketConnType.Socket;
            }
        }

        /// <summary>
        /// 应答WebSocket连接包
        /// </summary>
        /// <param name="packetStr">数据包字符串</param>
        /// <returns></returns>
        public static byte[] AnswerWebSocketHandShake(string packetStr)
        {
            string handShakeText = packetStr;
            string key = string.Empty;
            Regex reg = new Regex(@"Sec\-WebSocket\-Key:(.*?)\r\n");
            Match m = reg.Match(handShakeText);
            if (m.Value != "")
            {
                key = Regex.Replace(m.Value, @"Sec\-WebSocket\-Key:(.*?)\r\n", "$1").Trim();
            }

            byte[] secKeyBytes = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
            string secKey = Convert.ToBase64String(secKeyBytes);

            var responseBuilder = new StringBuilder();
            responseBuilder.Append("HTTP/1.1 101 Switching Protocols" + "\r\n");
            responseBuilder.Append("Upgrade: websocket" + "\r\n");
            responseBuilder.Append("Connection: Upgrade" + "\r\n");
            responseBuilder.Append("Sec-WebSocket-Accept: " + secKey + "\r\n\r\n");

            return Encoding.UTF8.GetBytes(responseBuilder.ToString());
        }
    }
}
