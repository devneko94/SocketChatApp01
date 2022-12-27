using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SocketChatApp01
{
    public class MyTextBox : TextBox
    {
        public string Format
        {
            get { return (string)GetValue(FormatProperty); }
            set { SetValue(FormatProperty, value); }
        }
        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register("Format", typeof(string), typeof(MyTextBox), new PropertyMetadata(""));

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);

            if (string.IsNullOrEmpty(this.Format))
            {
                return;
            }

            var format = $"[^{Format}]+";
            var regex = new Regex(format);
            var text = e.Text;
            var result = regex.IsMatch(text);
            e.Handled = result;
        }
    }
}
