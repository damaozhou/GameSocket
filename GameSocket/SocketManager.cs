using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using Comm = GameSocket.Common;
using System.Linq;
using System.Collections.Concurrent;

namespace GameSocket
{
    public class SocketManager
    {
        #region 内部变量
        /// <summary>
        /// 最大连接数
        /// </summary>
        int m_maxConnectNum;

        /// <summary>
        /// 缓冲区大小
        /// </summary>
        int bufferSize;

        /// <summary>
        /// 缓存区
        /// </summary>
        BufferManager m_bufferManager;
        /// <summary>
        /// 申请正常内存的倍数
        /// </summary>
        const int opsToAlloc = 2;
        /// <summary>
        /// 监听的Socket  
        /// </summary>
        Socket listenSocket;
        /// <summary>
        /// 连接池
        /// </summary>
        SocketEventPool m_pool;

        /// <summary>
        /// 计数信号量
        /// </summary>
        Semaphore semaphore;

        /// <summary>
        /// socket连接集合
        /// </summary>
        internal ConcurrentDictionary<Guid, UserSocketEventArgs> dicSocketEventArgs;
        #endregion

        /// <summary>
        /// 心跳超时时间 单位秒   默认30秒
        /// </summary>
        public int HeartBeatTimeOut { get; set; }

        /// <summary>
        /// aes加密key
        /// </summary>
        public static string AESKey { get; set; }

        #region 定义委托

        /// <summary>  
        /// 客户端连接事件  
        /// </summary>  
        /// <param name="connId">连接ID</param>  
        public delegate void OnSocketConnect(Guid connId);

        /// <summary>
        /// 接收到客户端的数据
        /// </summary>
        /// <param name="connId">连接ID</param>
        /// <param name="msg">消息体</param>
        public delegate void OnReceiveData(Guid connId, string msg);

        /// <summary>
        /// 连接关闭
        /// </summary>
        /// <param name="connId">连接ID</param>
        /// <param name="colseType">关闭类型</param>
        public delegate void OnSocketClose(Guid connId, SocketColseType  colseType);

        /// <summary>
        /// 关闭服务
        /// </summary>
        public delegate void OnListenClose();

        #endregion

        #region 定义事件
        /// <summary>  
        /// 客户端连接事件  
        /// </summary>  
        public event OnSocketConnect SocketConnect;

        /// <summary>  
        /// 接收到客户端的数据事件  
        /// </summary>  
        public event OnReceiveData ReceiveData;

        /// <summary>
        /// 连接关闭
        /// </summary>
        public event OnSocketClose SocketClose;

        /// <summary>
        /// 关闭服务
        /// </summary>
        public event OnListenClose ListenClose;

        #endregion

        #region 调用事件
        /// <summary>
        /// 客户端连接事件
        /// </summary>
        void onSocketConnect(Guid connId, SocketAsyncEventArgs e)
        {
            var userSocket = new UserSocketEventArgs(e);
            dicSocketEventArgs.AddOrUpdate(connId, userSocket, (id, t) => { return userSocket; });
            SocketConnect?.Invoke(connId);
        }

        /// <summary>
        /// 接收到客户端的数据事件
        /// </summary>
        void onReceiveData(Guid connId, string msg, SocketAsyncEventArgs e)
        {
            if (dicSocketEventArgs.ContainsKey(connId))
            {
                dicSocketEventArgs[connId].SocketAsyncEventArgs = e;
                dicSocketEventArgs[connId].HeartBeatTime = DateTime.Now;
            }
            else
                dicSocketEventArgs.TryAdd(connId, new UserSocketEventArgs(e));
            ReceiveData?.Invoke(connId, msg);
        }

        /// <summary>
        /// 连接关闭
        /// </summary>
        void onSocketClose(Guid connId, SocketColseType colseType)
        {
            dicSocketEventArgs.TryRemove(connId, out UserSocketEventArgs userSocket);
            SocketClose?.Invoke(connId, colseType);
        }

        /// <summary>
        /// 关闭服务
        /// </summary>
        void onListenClose()
        {
            dicSocketEventArgs.Clear();
            ListenClose?.Invoke();
        }
        #endregion

        /// <summary>  
        /// 构造函数  
        /// </summary>  
        /// <param name="numConnections">最大连接数</param>  
        /// <param name="receiveBufferSize">缓存区大小</param>  
        public SocketManager(int numConnections, int receiveBufferSize, string aes_key = "")
        {
            dicSocketEventArgs = new ConcurrentDictionary<Guid, UserSocketEventArgs>();
            HeartBeatTimeOut = 30;
            AESKey = aes_key;
            m_maxConnectNum = numConnections;
            bufferSize = receiveBufferSize;
            m_bufferManager = new BufferManager(receiveBufferSize * numConnections * opsToAlloc, receiveBufferSize);
            m_pool = new SocketEventPool(numConnections);
            semaphore = new Semaphore(numConnections, numConnections);
        }

        /// <summary>  
        /// 初始化  
        /// </summary>  
        public void Init()
        {
            m_bufferManager.InitBuffer();
            for (int i = 0; i < m_maxConnectNum; i++)
            {
                var readWriteEventArg = new SocketAsyncEventArgs() { UserToken = new AsyncUserToken() };
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                m_bufferManager.SetBuffer(readWriteEventArg);
                m_pool.Push(readWriteEventArg);
            }
        }

        /// <summary>  
        /// 启动服务  
        /// </summary>  
        /// <param name="lendPoint"></param>  
        public void Start(IPEndPoint lendPoint)
        {
            new HeartBeatDaemon(this).Run();
            listenSocket = new Socket(lendPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            listenSocket.Bind(lendPoint);
            listenSocket.Listen(m_maxConnectNum);
            StartAccept(null);
        }

        /// <summary>  
        /// 停止服务  
        /// </summary>
        public void Stop()
        {
            listenSocket.Shutdown(SocketShutdown.Both);
            listenSocket.Close();
            onListenClose();
        }

        /// <summary>
        /// 从客户端开始接受一个连接操作 
        /// </summary>
        /// <param name="acceptEventArg"></param>
        void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
                acceptEventArg.AcceptSocket = null;
            semaphore.WaitOne();
            if (!listenSocket.AcceptAsync(acceptEventArg))
                ProcessAccept(acceptEventArg);
            //如果I/O挂起等待异步则触发AcceptAsyn_Asyn_Completed事件  
            //此时I/O操作同步完成，不会触发Asyn_Completed事件，所以指定BeginAccept()方法  
        }

        /// <summary>
        /// 监听回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        /// <summary>
        /// 监听Socket接受处理
        /// </summary>
        /// <param name="e"></param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            lock (m_pool)
            {
                SocketAsyncEventArgs readEventArgs = m_pool.Pop();
                AsyncUserToken userToken = (AsyncUserToken)readEventArgs.UserToken;
                userToken.Socket = e.AcceptSocket;//和客户端关联的socket  
                userToken.Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);//20170118
                e.UserToken = userToken;
                if (e.AcceptSocket != null)
                {
                    if (e.AcceptSocket.RemoteEndPoint != null)
                    {
                        onSocketConnect(userToken.ConnId,e);
                        if (!e.AcceptSocket.ReceiveAsync(readEventArgs))//投递接收请求  
                            ProcessReceive(readEventArgs);
                    }
                }
            }
            if (e.SocketError == SocketError.OperationAborted)
                return;
            StartAccept(e);//投递下一个接受请求
        }

        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        /// <summary>
        /// 接受数据
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                if (e.BytesTransferred > 0)
                {
                    byte[] data = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);//把当前所接收到数据放到data数组里面
                    if (token.SocketConnType == SocketConnType.None)
                    {
                        token.SocketConnType = PacketComm.GetPacketType(data, out string packetStr);
                        if (token.SocketConnType == SocketConnType.WebSocket)//websocket连接
                        {
                            var HandPacket = Comm.BytesToString(PacketComm.AnswerWebSocketHandShake(packetStr));//应答握手包
                            Send(token, HandPacket);
                        }
                    }
                    else if (token.SocketConnType == SocketConnType.WebSocket)//WebSocket数据包
                    {
                        var Pac_Type = data[0] & 0xF;
                        if (Pac_Type == 8)//WebSocket关闭
                        {
                            CloseClientSocketEx(e, SocketColseType.ClientClose);
                        }
                        else
                        {
                            #region WebSocket数据包 https://www.cnblogs.com/smark/archive/2012/11/26/2789812.html
                            lock (token.Buffer)
                                token.Buffer.AddRange(data);
                            if (token.Socket.Available == 0)//接受完毕
                            {
                                var buffer = token.Buffer.ToArray();
                                var len = buffer.Length;
                                bool mask = false;
                                int lodlen = 0;
                                if (len > 2)
                                {
                                    var Pac_Fin = (buffer[0] >> 7) > 0;//是否最后一个数据包
                                    if (Pac_Fin)
                                    {
                                        mask = (buffer[1] >> 7) > 0;
                                        lodlen = buffer[1] & 0x7F;
                                        byte[] loddata;
                                        byte[] masks = new byte[4];
                                        if (lodlen == 126)
                                        {
                                            Array.Copy(buffer, 4, masks, 0, 4);
                                            lodlen = (UInt16)(buffer[2] << 8 | buffer[3]);
                                            loddata = new byte[lodlen];
                                            Array.Copy(buffer, 8, loddata, 0, lodlen);
                                        }
                                        else if (lodlen == 127)
                                        {
                                            Array.Copy(buffer, 10, masks, 0, 4);
                                            byte[] uInt64Bytes = new byte[8];
                                            for (int i = 0; i < 8; i++)
                                            {
                                                uInt64Bytes[i] = buffer[9 - i];
                                            }
                                            lodlen = (int)BitConverter.ToUInt64(uInt64Bytes, 0);

                                            loddata = new byte[lodlen];
                                            for (int i = 0; i < lodlen; i++)
                                            {
                                                loddata[i] = buffer[i + 14];
                                            }
                                        }
                                        else
                                        {
                                            Array.Copy(buffer, 2, masks, 0, 4);
                                            loddata = new byte[lodlen];
                                            Array.Copy(buffer, 6, loddata, 0, lodlen);
                                        }
                                        for (var i = 0; i < lodlen; i++)
                                        {
                                            loddata[i] = (byte)(loddata[i] ^ masks[i % 4]);
                                        }
                                        var Message = Comm.BytesToString(loddata);
                                        onReceiveData(token.ConnId, Message,e);
                                    }
                                }
                                lock (token.Buffer)
                                    token.Buffer.Clear();
                            }
                            #endregion
                        }
                    }
                    if (token.SocketConnType == SocketConnType.Socket)//原生Socket数据包
                    {
                        #region 原生Socket数据包
                        lock (token.Buffer)
                            token.Buffer.AddRange(data);
                        do
                        {
                            //**************************包头前8个字节为2个整数，第一个无意义默认为1，第二个为包长******************************************
                            byte[] lenBytes = token.Buffer.GetRange(4, 4).ToArray();
                            int packageLen = Comm.BytesToInt32(lenBytes, 0);
                            if (packageLen > token.Buffer.Count - 8)
                                break;
                            byte[] rev = token.Buffer.GetRange(8, packageLen).ToArray();//获取从第8个字节起的净内容(加密过的内容)
                            var Message = Comm.AESDecrypt(Comm.BytesToString(rev), AESKey);
                            lock (token.Buffer)
                                token.Buffer.RemoveRange(0, packageLen + 8);//从数据池中移除这组数据  
                            onReceiveData(token.ConnId, Message,e);
                        } while (token.Buffer.Count > 8);//(前4个字节是gameserverid(int),随后四个字节是内容总长度(int))
                        #endregion
                    }
                    if (token.Socket != null && token.Socket.Connected == true && !token.Socket.ReceiveAsync(e))
                        ProcessReceive(e);//继续接收. 为什么要这么写,请看Socket.ReceiveAsync方法的说明  
                }
                else
                    CloseClientSocketEx(e, SocketColseType.ClientClose);
            }
            else
                CloseClientSocketEx(e, SocketColseType.ClientClose);
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="token"></param>
        /// <param name="message"></param>
        bool Send(AsyncUserToken socket,string Message)
        {
            if (socket == null || !socket.Socket.Connected || String.IsNullOrEmpty(Message))
                return false;
            if (socket.SocketConnType == SocketConnType.WebSocket)
            {
                #region WebSocket发包
                return WebSocketSend(socket, Comm.AESEncrypt(Message, AESKey));
                #endregion
            }
            else
            {
                #region 原生Socket发包
                var message = Comm.StringToBytes(Comm.AESEncrypt(Message, AESKey));
                var buff = new byte[message.Length + 8];//确定一个整长度的字节数组
                byte[] len = Comm.Int32ToBytes(message.Length);//内容长度
                //**************************包头前8个字节为2个整数，第一个无意义默认为1，第二个为包长******************************************
                byte[] logicserverid = Comm.Int32ToBytes(1);
                Array.Copy(logicserverid, buff, 4);//把logicserver添加进来
                Array.Copy(len, 0, buff, 4, 4);//插入纯内容总长度
                Array.Copy(message, 0, buff, 8, message.Length);
                SocketAsyncEventArgs sendArg = new SocketAsyncEventArgs();
                sendArg.SetBuffer(buff, 0, buff.Length);
                return socket.Socket.SendAsync(sendArg);
                #endregion
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="connId"></param>
        /// <param name="Message"></param>
        public bool Send(Guid connId, string Message)
        {
            if (dicSocketEventArgs.ContainsKey(connId))
            {
                AsyncUserToken token = dicSocketEventArgs[connId].SocketAsyncEventArgs.UserToken as AsyncUserToken;
                return Send(token, Message);
            }
            return false;
        }

        /// <summary>
        /// 发送数据方法
        /// </summary>
        /// <param name="socket">客户端socket</param>
        /// <param name="message">要发送的数据</param>
        bool WebSocketSend(AsyncUserToken token, string message)
        {
            var socket = token.Socket;
            byte[] bytes = Comm.StringToBytes(message);
            bool send = true;
            int SendMax = bufferSize;//每次分片最大1kb数据
            int count = 0;//发送的次数
            int sendedlen = 0;//已经发送的字节长度
            while (send)
            {
                byte[] contentBytes = null;//待发送的消息内容
                var sendArr = bytes.Skip(count * SendMax).Take(SendMax).ToArray();
                sendedlen += sendArr.Length;
                if (sendArr.Length > 0)
                {
                    send = bytes.Length > sendedlen;//是否继续发送
                    if (sendArr.Length < 126)
                    {
                        contentBytes = new byte[sendArr.Length + 2];
                        contentBytes[0] = (byte)(count == 0 ? 0x81 : (!send ? 0x80 : 0));
                        contentBytes[1] = (byte)sendArr.Length;//1个字节存储真实长度
                        Array.Copy(sendArr, 0, contentBytes, 2, sendArr.Length);
                        send = false;
                    }
                    else if (sendArr.Length <= 65535)
                    {
                        contentBytes = new byte[sendArr.Length + 4];
                        if (!send && count == 0)
                            contentBytes[0] = 0x81;//非分片发送
                        else
                            contentBytes[0] = (byte)(count == 0 ? 0x01 : (!send ? 0x80 : 0));//处于连续的分片发送
                        contentBytes[1] = 126;
                        byte[] slen = BitConverter.GetBytes((short)sendArr.Length);//2个字节存储真实长度
                        contentBytes[2] = slen[1];
                        contentBytes[3] = slen[0];
                        Array.Copy(sendArr, 0, contentBytes, 4, sendArr.Length);
                    }
                    else if (sendArr.LongLength < long.MaxValue)
                    {
                        contentBytes = new byte[sendArr.Length + 10];
                        contentBytes[0] = (byte)(count == 0 ? 0x01 : (!send ? 0x80 : 0));//处于连续的分片发送
                        contentBytes[1] = 127;
                        byte[] llen = BitConverter.GetBytes((long)sendArr.Length);//8个字节存储真实长度
                        for (int i = 7; i >= 0; i--)
                        {
                            contentBytes[9 - i] = llen[i];
                        }
                        Array.Copy(sendArr, 0, contentBytes, 10, sendArr.Length);
                    }
                }
                SocketAsyncEventArgs sendArg = new SocketAsyncEventArgs();
                sendArg.SetBuffer(contentBytes, 0, contentBytes.Length);
                socket.SendAsync(sendArg);
                count++;
            }
            return true;
        }

        void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
                }
            }
        }

        //关闭客户端  
        void CloseClientSocketEx(SocketAsyncEventArgs e, SocketColseType colseType)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            if (token != null)
            {
                if (token.Socket != null && token.Socket.Connected)
                {
                    token.Socket.Shutdown(SocketShutdown.Both);
                    token.Socket.Close();
                }
            }
            onSocketClose(token.ConnId, colseType);
            semaphore.Release();
            lock (m_pool)
            {
                if (m_pool.Count < m_maxConnectNum)
                {
                    e.UserToken = new AsyncUserToken();
                    m_pool.Push(e);
                }
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <param name="socketUser"></param>
        public void CloseSocket(Guid connId)
        {
            if (dicSocketEventArgs.ContainsKey(connId))
            {
                CloseClientSocketEx(dicSocketEventArgs[connId].SocketAsyncEventArgs, SocketColseType.ServerClose);
            }
        }
    }
}
