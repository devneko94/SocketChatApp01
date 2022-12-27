using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SocketChatApp01.Controls
{
    /// <summary>
    /// MessageControl.xaml の相互作用ロジック
    /// </summary>
    public partial class MessageControl : UserControl
    {
        #region 依存関係プロパティ
        public string MessageText
        {
            get { return (string)GetValue(MessageTextProperty); }
            set { SetValue(MessageTextProperty, value); }
        }
        public static readonly DependencyProperty MessageTextProperty =
            DependencyProperty.Register("MessageText", typeof(string), typeof(MessageControl), new PropertyMetadata(""));

        public DateTime MessageTime
        {
            get { return (DateTime)GetValue(MessageTimeProperty); }
            set { SetValue(MessageTimeProperty, value); }
        }
        public static readonly DependencyProperty MessageTimeProperty =
            DependencyProperty.Register("MessageTime", typeof(DateTime), typeof(MessageControl), new PropertyMetadata(default(DateTime)));

        public bool IsSendMessage
        {
            get { return (bool)GetValue(IsSendMessageProperty); }
            set { SetValue(IsSendMessageProperty, value); }
        }
        public static readonly DependencyProperty IsSendMessageProperty =
            DependencyProperty.Register("IsSendMessage", typeof(bool), typeof(MessageControl), new PropertyMetadata(false));
        #endregion

        #region コンストラクタ
        public MessageControl()
        {
            InitializeComponent();
        }
        #endregion
    }
}
