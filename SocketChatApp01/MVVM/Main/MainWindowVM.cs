﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SocketChatApp01
{
    class MainWindowVM : BindableBase
    {
        #region 定数
        private readonly int MAX_MESSAGE_COUNT = 50;
        #endregion

        #region プライベートフィールド
        private UdpClient? client = null;
        #endregion

        #region プロパティ
        private string _localIP = "127.0.0.1";
        public string LocalIP
        {
            get { return _localIP; }
            set
            {
                if (_localIP != value)
                {
                    _localIP = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? _localPort = 10001;
        public int? LocalPort
        {
            get { return _localPort; }
            set
            {
                if (_localPort != value)
                {
                    _localPort = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _remoteIP = "127.0.0.1";
        public string RemoteIP
        {
            get { return _remoteIP; }
            set
            {
                if (_remoteIP != value)
                {
                    _remoteIP = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? _remotePort = 10002;
        public int? RemotePort
        {
            get { return _remotePort; }
            set
            {
                if (_remotePort != value)
                {
                    _remotePort = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _inputMessage = "";
        public string InputMessage
        {
            get { return _inputMessage; }
            set
            {
                if (_inputMessage != value)
                {
                    _inputMessage = value;
                    OnPropertyChanged();
                    SendMessageCommand.OnCanExecuteChanged();
                }
            }
        }

        private ObservableCollection<MessageControl> _messageList = new();
        public ObservableCollection<MessageControl> MessageList
        {
            get { return _messageList; }
            set
            {
                if (_messageList != value)
                {
                    _messageList = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isRunning;
        public bool IsListening
        {
            get { return _isRunning; }
            private set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged();
                    ConnectCommand.OnCanExecuteChanged();
                }
            }
        }
        #endregion

        #region コマンド
        public DelegateCommand ConnectCommand { get; private set; }

        public DelegateCommand SendMessageCommand { get; private set; }

        #endregion

        #region コンストラクタ
        public MainWindowVM()
        {
            ConnectCommand = new DelegateCommand(Connect);
            SendMessageCommand = new DelegateCommand(SendMessage, () => InputMessage != "");
        }
        #endregion

        #region デストラクタ
        ~MainWindowVM()
        {
            this.BeginInvoke(this.DisConnect);
        }
        #endregion

        #region プライベートメソッド
        private void Connect()
        {
            try
            {
                if (IsListening)
                {
                    this.BeginInvoke(this.DisConnect);
                }
                else
                {
                    if (!ValidateConnect())
                    {
                        return;
                    }

                    IPEndPoint localEP = new(IPAddress.Parse(this.LocalIP), this.LocalPort!.Value);

                    client = new UdpClient(localEP);
                    client.BeginReceive(this.RecieveCallBack, client);

                    IsListening = true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        private bool ValidateConnect()
        {
            if (!IPAddress.TryParse(this.LocalIP, out IPAddress? localIP) || localIP is null || !this.LocalPort.HasValue)
            {
                MessageBox.Show("LocalIPまたはLocalPortが不正です。");
                return false;
            }

            if (new Ping().Send(localIP).Status != IPStatus.Success)
            {
                MessageBox.Show("Localが見つかりません。");
                return false;
            }

            if (!IPAddress.TryParse(this.RemoteIP, out IPAddress? remoteIP) || remoteIP is null || !this.RemotePort.HasValue)
            {
                MessageBox.Show("RemoteIPまたはRemotePortが不正です。");
                return false;
            }

            if (new Ping().Send(remoteIP).Status != IPStatus.Success)
            {
                MessageBox.Show("Remoteが見つかりません。");
                return false;
            }

            return true;
        }

        private void RecieveCallBack(IAsyncResult ar)
        {
            try
            {
                UdpClient udp = ar.AsyncState as UdpClient;
                if (udp.Client is null)
                {
                    return;
                }

                IPEndPoint remoteEP = new(IPAddress.Parse(this.RemoteIP), this.RemotePort!.Value);

                byte[] rcvBytes = udp.EndReceive(ar, ref remoteEP) ?? Array.Empty<byte>();
                string rcvMessage = Encoding.GetEncoding("Shift_JIS").GetString(rcvBytes);

                this.BeginInvoke(() =>
                {
                    if (this.MessageList.Count == MAX_MESSAGE_COUNT)
                    {
                        this.MessageList.RemoveAt(0);
                    }

                    this.MessageList.Add(new MessageControl
                    {
                        MessageText = rcvMessage,
                        MessageTime = DateTime.Now,
                        IsSendMessage = false,
                    });
                });

                udp.BeginReceive(this.RecieveCallBack, udp);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                this.BeginInvoke(this.DisConnect);
                return;
            }
        }

        private void SendMessage()
        {
            try
            {
                byte[] sendBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(this.InputMessage);

                client ??= new UdpClient();

                IPEndPoint remoteEP = new(IPAddress.Parse(this.RemoteIP), this.RemotePort!.Value);
                client.BeginSend(sendBytes, sendBytes.Length, remoteEP, this.SendCallBack, client);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        private void SendCallBack(IAsyncResult ar)
        {
            try
            {
                UdpClient udp = ar.AsyncState as UdpClient;
                int? sendBytesCount = udp?.EndSend(ar);

                if (sendBytesCount is not null && 0 < sendBytesCount)
                {
                    this.BeginInvoke(() =>
                    {
                        if (this.MessageList.Count == MAX_MESSAGE_COUNT)
                        {
                            this.MessageList.RemoveAt(0);
                        }

                        this.MessageList.Add(new MessageControl
                        {
                            MessageText = this.InputMessage,
                            MessageTime = DateTime.Now,
                            IsSendMessage = true,
                        });
                    });
                }
                else
                {
                    MessageBox.Show("メッセージ送信に失敗しました。");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return;
            }
        }

        private void DisConnect()
        {
            client?.Close();
            client = null;
            IsListening = false;
        }

        private void BeginInvoke(Delegate method, params object[] args)
        {
            Application.Current.Dispatcher.BeginInvoke(method, args);
        }
        #endregion
    }
}
