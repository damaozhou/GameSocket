using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace nativeSocket
{
    class Program
    {
        static Socket ClientSocket;
        static void Main(string[] args)
        {
            String IP = "192.168.1.13";
            int port = 10000;
            IPAddress ip = IPAddress.Parse(IP);  //将IP地址字符串转换成IPAddress实例
            ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//使用指定的地址簇协议、套接字类型和通信协议
            IPEndPoint endPoint = new IPEndPoint(ip, port); // 用指定的ip和端口号初始化IPEndPoint实例
            ClientSocket.Connect(endPoint);  //与远程主机建立连接
            Console.WriteLine("开始发送消息");
            byte[] message = Encoding.UTF8.GetBytes("这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socket这是原生socketConnect the Server");  //通信时实际发送的是字节数组，所以要将发送消息转换字节
            var buff = new byte[message.Length + 8];//确定一个整长度的字节数组
            byte[] len = Int32ToBytes(message.Length);//内容长度
            //**************************包头前8个字节为2个整数，第一个无意义默认为1，第二个为包长******************************************
            byte[] logicserverid = Int32ToBytes(1);
            Array.Copy(logicserverid, buff, 4);//把logicserver添加进来
            Array.Copy(len, 0, buff, 4, 4);//插入纯内容总长度
            Array.Copy(message, 0, buff, 8, message.Length);
            ClientSocket.Send(buff);
            Console.WriteLine("发送消息为:" + Encoding.UTF8.GetString(message));
            byte[] receive = new byte[10240];
            int length = ClientSocket.Receive(receive);  // length 接收字节数组长度
            var rev = new byte[length - 8];
            Array.Copy(receive, 8, rev, 0, rev.Length);
            Console.WriteLine("接收消息为：" + Encoding.UTF8.GetString(rev));
            length = ClientSocket.Receive(receive);  // length 接收字节数组长度
            Console.WriteLine("接收消息为：" + length.ToString());
            Console.Read();
            ClientSocket.Close();  //关闭连接
        }

        public static byte[] Int32ToBytes(int n)
        {
            byte[] b = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                b[i] = (byte)(n >> (24 - i * 8));
            }
            return b;
        }
    }
}
