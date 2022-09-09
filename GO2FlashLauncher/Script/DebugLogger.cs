using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GO2FlashLauncher.Script
{
    public delegate void StringArgReturningVoidDelegate(string text);
    public class DebugLogger : TextWriter
    {
        private readonly RichTextBox _richTextBox;
        public DebugLogger(RichTextBox richTexttbox)
        {
            _richTextBox = richTexttbox;
        }

        public override void Write(char value)
        {
            SetText(value.ToString());
        }

        public override void Write(string value)
        {
            SetText(value);
        }

        public override void WriteLine(char value)
        {
            SetText(value + Environment.NewLine);
        }

        public override void WriteLine(string value)
        {
            SetText(value + Environment.NewLine);
        }

        public override Encoding Encoding => Encoding.ASCII;

        //Write to your UI object in thread safe way:
        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the  
            // calling thread to the thread ID of the creating thread.  
            // If these threads are different, it returns true.  
            if (_richTextBox.InvokeRequired)
            {
                _richTextBox.Invoke((MethodInvoker)delegate
                {
                    _richTextBox.SelectionStart = _richTextBox.TextLength;
                    _richTextBox.SelectionLength = 0;
                    _richTextBox.SelectionColor = Color.Gray;
                    _richTextBox.AppendText("\n" + "[" + DateTime.Now.ToString("HH:mm") + "]: " + text);
                    _richTextBox.Focus();
                    _richTextBox.Select(_richTextBox.TextLength, 0);
                    _richTextBox.ScrollToCaret();
                });
            }
            else
            {
                _richTextBox.Text += text;
            }
        }
    }
}
