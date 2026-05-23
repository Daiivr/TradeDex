using System;
using System.Windows.Forms;
using PKHeX.Core; // or wherever ILogForwarder is defined

namespace SysBot.Base
{
    public class LogTextBoxForwarder : ILogForwarder
    {
        private readonly RichTextBox _rtb;

        public LogTextBoxForwarder(RichTextBox rtb)
        {
            _rtb = rtb;
        }

        // Implement the interface method exactly
        public void Forward(string logText, string logSource)
        {
            if (_rtb.InvokeRequired)
            {
                _rtb.Invoke(new Action(() => AppendText(logText, logSource)));
            }
            else
            {
                AppendText(logText, logSource);
            }
        }

        private void AppendText(string text, string source)
        {
            if (_rtb.TextLength > _rtb.MaxLength)
                _rtb.Clear();

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var prefix = $"[{timestamp}] [{source}] ";
            var normalizedText = text.Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = normalizedText.Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                if (i == lines.Length - 1 && lines[i].Length == 0)
                    continue;

                _rtb.AppendText($"{prefix}{lines[i]}{Environment.NewLine}");
            }

            _rtb.SelectionStart = _rtb.Text.Length;
            _rtb.ScrollToCaret();
        }
    }
}
