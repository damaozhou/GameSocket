using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace GameSocket
{
    /// <summary>
    /// 缓冲区
    /// </summary>
    internal class BufferManager
    {
        /// <summary>
        /// 缓存区总字节数
        /// </summary>
        int m_numBytes;
        /// <summary>
        /// 开辟的内存总大小
        /// </summary>
        byte[] m_buffer;
        /// <summary>
        /// 空闲的缓冲区
        /// </summary>
        Stack<int> m_freeIndexPool;
        /// <summary>
        /// 指针偏移量
        /// </summary>
        int m_currentIndex;
        /// <summary>
        /// 单个缓冲区大小
        /// </summary>
        int m_bufferSize;

        /// <summary>
        /// socket缓冲区
        /// </summary>
        /// <param name="totalBytes">缓存区数量</param>
        /// <param name="bufferSize">单个缓冲区大小</param>
        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void InitBuffer()
        {
            m_buffer = new byte[m_numBytes];
        }

        /// <summary>
        /// 设置缓冲区
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (m_freeIndexPool.Count > 0)
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
            else
            {
                if ((m_numBytes - m_bufferSize) < m_currentIndex)
                    return false;
                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                m_currentIndex += m_bufferSize;
            }
            return true;
        }

        /// <summary>
        /// 释放缓冲区
        /// </summary>
        /// <param name="args"></param>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}