// ファイル名: MainWindowVM.cs
// 作成日: 2022/12/27
// 作成者: M.Gotou

using SocketChatApp01.Common;
using SocketChatApp01.Controls;
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

namespace SocketChatApp01.MVVM.Main
{
    /// <summary>
    /// MainWindowのViewModelクラス
    /// </summary>
    class MainWindowVM : BindableBase
    {
        #region 定数
        /// <summary>
        /// メッセージリストの最大保持数
        /// </summary>
        private readonly int MAX_MESSAGE_COUNT = 50;
        #endregion

        #region プライベートフィールド
        /// <summary>
        /// UDPクライアント
        /// </summary>
        private UdpClient? _client = null;
        #endregion

        #region プロパティ
        private string _localIP = "127.0.0.1";
        /// <summary>
        /// ローカルIP
        /// </summary>
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
        /// <summary>
        /// ローカルポート
        /// </summary>
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
        /// <summary>
        /// リモートIP
        /// </summary>
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
        /// <summary>
        /// リモートポート
        /// </summary>
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
        /// <summary>
        /// 入力メッセージ
        /// </summary>
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
        /// <summary>
        /// メッセージリスト
        /// </summary>
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

        private bool _isListening;
        /// <summary>
        /// 受信待ち中フラグ
        /// </summary>
        public bool IsListening
        {
            get { return _isListening; }
            private set
            {
                if (_isListening != value)
                {
                    _isListening = value;
                    OnPropertyChanged();
                    ListenCommand.OnCanExecuteChanged();
                    SendMessageCommand.OnCanExecuteChanged();
                }
            }
        }
        #endregion

        #region コマンド
        /// <summary>
        /// 受信待ち状態開始/終了コマンド
        /// </summary>
        public DelegateCommand ListenCommand { get; private set; }

        /// <summary>
        /// メッセージ送信コマンド
        /// </summary>
        public DelegateCommand SendMessageCommand { get; private set; }

        #endregion

        #region コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindowVM()
        {
            ListenCommand = new DelegateCommand(BeginListen);
            SendMessageCommand = new DelegateCommand(SendMessage, () => IsListening && InputMessage != "");
        }
        #endregion

        #region デストラクタ
        /// <summary>
        /// デストラクタ
        /// </summary>
        ~MainWindowVM()
        {
            BeginInvoke(this.EndListen);
        }
        #endregion

        #region プライベートメソッド
        /// <summary>
        /// 受信待ち状態を開始します。
        /// </summary>
        private void BeginListen()
        {
            try
            {
                if (IsListening)
                {
                    BeginInvoke(this.EndListen);
                }
                else
                {
                    if (!ValidateConnect())
                    {
                        return;
                    }

                    IPEndPoint localEP = new(IPAddress.Parse(LocalIP), LocalPort!.Value);
                    _client = new UdpClient(localEP);
                    _client.BeginReceive(RecieveCallBack, _client);

                    IsListening = true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                BeginInvoke(this.EndListen);
                return;
            }
        }

        /// <summary>
        /// 接続ソケットの疎通確認を行います。
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 受信完了時に受信したメッセージを画面表示します。
        /// </summary>
        /// <param name="ar"></param>
        private void RecieveCallBack(IAsyncResult ar)
        {
            try
            {
                UdpClient? udp = ar.AsyncState as UdpClient;
                if (udp?.Client is null)
                {
                    return;
                }

                IPEndPoint remoteEP = new(IPAddress.Parse(RemoteIP), RemotePort!.Value);
                byte[] rcvBytes = udp.EndReceive(ar, ref remoteEP!) ?? Array.Empty<byte>();
                string rcvMessage = Encoding.GetEncoding("Shift_JIS").GetString(rcvBytes);

                BeginInvoke(() =>
                {
                    if (MessageList.Count == MAX_MESSAGE_COUNT)
                    {
                        MessageList.RemoveAt(0);
                    }

                    MessageList.Add(new MessageControl
                    {
                        MessageText = rcvMessage,
                        MessageTime = DateTime.Now,
                        IsSendMessage = false,
                    });
                });

                udp.BeginReceive(RecieveCallBack, udp);
            }
            catch (SocketException)
            {
                MessageBox.Show("受信時にRemoteとの接続が切断されました。");
                BeginInvoke(this.EndListen);
                return;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                BeginInvoke(this.EndListen);
                return;
            }
        }

        /// <summary>
        /// メッセージ送信します。
        /// </summary>
        private void SendMessage()
        {
            try
            {
                _client ??= new UdpClient();
                byte[] sendBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(InputMessage);

                IPEndPoint remoteEP = new(IPAddress.Parse(RemoteIP), RemotePort!.Value);
                _client.BeginSend(sendBytes, sendBytes.Length, remoteEP, SendCallBack, _client);
            }
            catch (SocketException)
            {
                MessageBox.Show("送信時にRemoteとの接続が切断されました。");
                BeginInvoke(this.EndListen);
                return;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                BeginInvoke(this.EndListen);
                return;
            }
        }

        /// <summary>
        /// 送信完了時に送信したメッセージを画面表示します。
        /// </summary>
        /// <param name="ar"></param>
        private void SendCallBack(IAsyncResult ar)
        {
            UdpClient? udp = ar.AsyncState as UdpClient;
            int? sendBytesCount = udp?.EndSend(ar);

            if (sendBytesCount is not null && 0 < sendBytesCount)
            {
                BeginInvoke(() =>
                {
                    if (MessageList.Count == MAX_MESSAGE_COUNT)
                    {
                        MessageList.RemoveAt(0);
                    }

                    MessageList.Add(new MessageControl
                    {
                        MessageText = InputMessage,
                        MessageTime = DateTime.Now,
                        IsSendMessage = true,
                    });
                });
            }
        }

        /// <summary>
        /// 受信待ち状態を終了します。
        /// </summary>
        private void EndListen()
        {
            _client?.Close();
            _client = null;
            IsListening = false;
        }

        /// <summary>
        /// Application.Current.Dispatcher.BeginInvokeに処理を渡します。
        /// </summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        private void BeginInvoke(Action action, params object[] args)
        {
            Application.Current.Dispatcher.BeginInvoke(action, args);
        }
        #endregion
    }
}
