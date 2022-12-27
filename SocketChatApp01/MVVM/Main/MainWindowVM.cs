// ファイル名: MainWindowVM.cs
// 作成日: 2022/12/27
// 作成者: M.Gotou

using SocketChatApp01.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
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

        private string _localPort = "10001";
        /// <summary>
        /// ローカルポート
        /// </summary>
        public string LocalPort
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

        private string _remotePort = "10002";
        /// <summary>
        /// リモートポート
        /// </summary>
        public string RemotePort
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

        private string _messageList = "";
        /// <summary>
        /// メッセージリスト
        /// </summary>
        public string MessageList
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
            SendMessageCommand = new DelegateCommand(SendMessage);
        }
        #endregion

        #region デストラクタ
        /// <summary>
        /// デストラクタ
        /// </summary>
        ~MainWindowVM()
        {
            EndListen();
        }
        #endregion

        #region プライベートメソッド
        /// <summary>
        /// 受信待ち状態を開始します。
        /// 実行時に受信待ち中だった場合は受信待ち状態を終了します。
        /// </summary>
        private void BeginListen()
        {
            try
            {
                if (IsListening)
                {
                    EndListen();
                }
                else
                {
                    if (!ValidateConnect())
                    {
                        return;
                    }

                    IPEndPoint localEP = new(IPAddress.Parse(LocalIP), int.Parse(LocalPort));
                    _client = new UdpClient(localEP);
                    _client.BeginReceive(RecieveCallBack, _client);

                    IsListening = true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                EndListen();
                return;
            }
        }

        /// <summary>
        /// IPアドレスとポートが不正でないか確認します。
        /// </summary>
        /// <returns></returns>
        private bool ValidateConnect()
        {
            if (!IPAddress.TryParse(LocalIP, out IPAddress? _) || !int.TryParse(LocalPort, out int _))
            {
                MessageBox.Show("LocalIPまたはLocalPortが不正です。");
                return false;
            }

            if (!IPAddress.TryParse(RemoteIP, out IPAddress? _) || !int.TryParse(RemotePort, out int _))
            {
                MessageBox.Show("RemoteIPまたはRemotePortが不正です。");
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

                IPEndPoint remoteEP = new(IPAddress.Parse(RemoteIP), int.Parse(RemotePort));
                byte[] rcvBytes = udp.EndReceive(ar, ref remoteEP) ?? Array.Empty<byte>();
                string rcvMessage = Encoding.UTF8.GetString(rcvBytes);

                MessageList += ($"[受信: {DateTime.Now:HH:mm}] {rcvMessage}\n");

                udp.BeginReceive(RecieveCallBack, udp);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                EndListen();
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
                byte[] sendBytes = Encoding.UTF8.GetBytes(InputMessage);

                IPEndPoint remoteEP = new(IPAddress.Parse(RemoteIP), int.Parse(RemotePort));
                _client.BeginSend(sendBytes, sendBytes.Length, remoteEP, SendCallBack, _client);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                EndListen();
                return;
            }
        }

        /// <summary>
        /// 送信完了時に送信したメッセージを画面表示します。
        /// </summary>
        /// <param name="ar"></param>
        private void SendCallBack(IAsyncResult ar)
        {
            try
            {
                UdpClient? udp = ar.AsyncState as UdpClient;
                int? sendBytesCount = udp?.EndSend(ar);

                if (sendBytesCount is not null)
                {
                    MessageList += ($"[送信: {DateTime.Now:HH:mm}] {InputMessage}\n");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                EndListen();
                return;
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
        #endregion
    }
}
