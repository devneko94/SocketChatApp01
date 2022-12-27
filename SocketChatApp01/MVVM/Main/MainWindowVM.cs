using System;
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
        Socket? socket = null;
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
        public bool IsRunning
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
            socket?.Shutdown(SocketShutdown.Both);
            socket?.Close();
            socket = null;

            IsRunning = false;
        }
        #endregion

        #region イベント処理
        private void OnReceiveCompleted(object? sender, SocketAsyncEventArgs e)
        {
            if (e?.Buffer is null)
            {
                return;
            }

            string message = Encoding.GetEncoding("Shift_JIS").GetString(e.Buffer);

            if (this.MessageList.Count == MAX_MESSAGE_COUNT)
            {
                this.MessageList.RemoveAt(0);
            }

            this.MessageList.Add(new MessageControl
            {
                MessageText = message,
                MessageTime = DateTime.Now,
                IsSendMessage = false,
            });

            this.ListenMessage();
        }
        #endregion

        #region プライベートメソッド
        private void Connect()
        {
            if (!ValidateConnect())
            {
                return;
            }

            try
            {
                if (IsRunning)
                {
                    socket?.Shutdown(SocketShutdown.Both);
                    socket?.Close();
                    socket = null;

                    IsRunning = false;
                }
                else
                {
                    IPEndPoint local = new(IPAddress.Parse(this.LocalIP), this.LocalPort!.Value);

                    socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
                    socket.Bind(local);

                    IsRunning = true;

                    this.ListenMessage();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
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

            if (!IPAddress.TryParse(this.RemoteIP, out IPAddress? remoteIP) || remoteIP is null || !this.RemotePort.HasValue)
            {
                MessageBox.Show("RemoteIPまたはRemotePortが不正です。");
                return false;
            }

            return true;
        }

        private void ListenMessage()
        {
            IPEndPoint remote = new(IPAddress.Parse(this.RemoteIP), this.RemotePort!.Value);
            SocketAsyncEventArgs e = new()
            {
                RemoteEndPoint = remote
            };
            e.SetBuffer(0, 1024);
            e.Completed += this.OnReceiveCompleted;

            socket?.ReceiveFromAsync(e);
        }

        private void SendMessage()
        {
            try
            {
                byte[] message = Encoding.GetEncoding("Shift_JIS").GetBytes(this.InputMessage);

                IPEndPoint remote = new(IPAddress.Parse(this.RemoteIP), this.RemotePort!.Value);
                SocketAsyncEventArgs e = new()
                {
                    RemoteEndPoint = remote
                };
                e.SetBuffer(message, 0, message.Length);

                bool? isSended = socket?.SendToAsync(e);

                if (isSended is bool sendedFlg && sendedFlg)
                {
                    this.MessageList.Add(new MessageControl
                    {
                        MessageText = this.InputMessage,
                        MessageTime = DateTime.Now,
                        IsSendMessage = true,
                    });

                    this.InputMessage = "";
                }
                else
                {
                    MessageBox.Show("メッセージ送信に失敗しました。");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                throw;
            }
        }
        #endregion
    }
}
