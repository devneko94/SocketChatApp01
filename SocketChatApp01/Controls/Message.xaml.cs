// ファイル名: Message.xaml.cs
// 作成日: 2022/12/27
// 作成者: M.Gotou

using System;
using System.Windows;
using System.Windows.Controls;

namespace SocketChatApp01.Controls
{
    /// <summary>
    /// MessageControl.xaml の相互作用ロジック
    /// </summary>
    public partial class Message : UserControl
    {
        #region 依存関係プロパティ
        /// <summary>
        /// メッセージテキスト依存関係プロパティ
        /// </summary>
        public string MessageText
        {
            get { return (string)GetValue(MessageTextProperty); }
            set { SetValue(MessageTextProperty, value); }
        }
        public static readonly DependencyProperty MessageTextProperty =
            DependencyProperty.Register("MessageText", typeof(string), typeof(Message), new PropertyMetadata(""));

        /// <summary>
        /// メッセージ時間依存関係プロパティ
        /// </summary>
        public DateTime MessageTime
        {
            get { return (DateTime)GetValue(MessageTimeProperty); }
            set { SetValue(MessageTimeProperty, value); }
        }
        public static readonly DependencyProperty MessageTimeProperty =
            DependencyProperty.Register("MessageTime", typeof(DateTime), typeof(Message), new PropertyMetadata(default(DateTime)));

        /// <summary>
        /// 送信メッセージフラグ依存関係プロパティ
        /// </summary>
        public bool IsSendMessage
        {
            get { return (bool)GetValue(IsSendMessageProperty); }
            set { SetValue(IsSendMessageProperty, value); }
        }
        public static readonly DependencyProperty IsSendMessageProperty =
            DependencyProperty.Register("IsSendMessage", typeof(bool), typeof(Message), new PropertyMetadata(false));
        #endregion

        #region コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Message()
        {
            InitializeComponent();
        }
        #endregion
    }
}
