using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SocketChatApp01
{
    public class MyTcpListener : TcpListener
    {
        private TcpClient? _client = null;

        public bool Listened { get; private set; }

        public MyTcpListener(IPEndPoint localEP) : base(localEP) { }

        public static MyTcpListener? Create(string addr, int port)
        {
            if (!IPAddress.TryParse(addr, out IPAddress ip))
            {
                return null;
            }

            return new MyTcpListener(new IPEndPoint(ip, port));
        }

        public new void Start()
        {
            if (this.Active)
            {
                return;
            }

            base.Start();
            Listened = true;
        }

        public new void Stop()
        {
            if (!this.Active)
            {
                return;
            }

            this.Disconnect();
            base.Stop();
            this.Listened = false;
        }

        public void Restart()
        {
            if (!this.Active)
            {
                return;
            }

            this.Disconnect();
            base.Stop();
            base.Start();
        }

        public bool WaitConnect()
        {
            if (!this.Active || _client is not null)
            {
                return false;
            }

            try
            {
                _client = this.AcceptTcpClient();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        public void Disconnect()
        {
            if (!this.Active || _client == null)
            {
                return;
            }

            if (_client.Connected)
            {
                NetworkStream? ns = _client.GetStream();
                ns?.Close();
                _client.Close();
            }
            _client = null;
        }

        public bool Write(byte[] data)
        {
            if (!this.Active || _client == null)
            {
                return false;
            }

            NetworkStream ns = _client.GetStream();
            ns.Write(data, 0, data.Length);
            return true;
        }

        public byte[] Read(Func<byte[], bool> isComplete, int ms)
        {
            if (!this.Active || _client == null)
            {
                return Array.Empty<byte>();
            }

            try
            {
                byte[] buffer = new byte[256];
                NetworkStream ns = _client.GetStream();
                ns.ReadTimeout = ms;

                while (true)
                {
                    int size = ns.Read(buffer, 0, buffer.Length);

                    if (size == 0 || _client == null)
                    {
                        this.Disconnect();
                        return Array.Empty<byte>();
                    }

                    if (isComplete(buffer))
                    {
                        break;
                    }
                }
                return buffer;
            }
            catch (Exception e)
            {
                if (!this.Listened)
                {
                    return Array.Empty<byte>();
                }
                throw e;
            }
        }
    }
}
