using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketChatApp01
{
    public class MyTcpClient : TcpClient
    {
        #region コンストラクタ
        public MyTcpClient() : base() { }
        #endregion

        #region パブリックメソッド
        public bool TryConnection(string addr, int port, int ms)
        {
            IAsyncResult ar = this.BeginConnect(addr, port, null, null);
            WaitHandle handle = ar.AsyncWaitHandle;

            try
            {
                if (!handle.WaitOne(ms, false))
                {
                    this.Close();
                    return false;
                }

                this.EndConnect(ar);
                return true;
            }
            finally
            {
                handle.Close();
            }
        }

        public void Write(byte[] buffer)
        {
            NetworkStream ns = this.GetStream();
            ns.Write(buffer, 0, buffer.Length);
        }

        public byte[] Read(Func<byte[], bool> isComplete, int ms)
        {
            try
            {
                byte[] buffer = new byte[256];
                NetworkStream ns = this.GetStream();
                ns.ReadTimeout = ms;

                while (true)
                {
                    int size = ns.Read(buffer, 0, buffer.Length);

                    if (size == 0)
                    {
                        return Array.Empty<byte>();
                    }

                    if (isComplete(buffer))
                    {
                        break;
                    }
                }
                return buffer;
            }
            catch (Exception)
            {
                return Array.Empty<byte>();
            }
        }
        #endregion
    }
}
