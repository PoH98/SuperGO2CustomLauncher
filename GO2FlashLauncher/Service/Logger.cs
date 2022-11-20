using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace GO2FlashLauncher.Service
{
    internal class Logger
    {
        private static RichTextBox logger;
        private static string logPath;
        private static FileStream logStream;
        public static void Init(RichTextBox richTextBox, string profile)
        {
            logger = richTextBox;
            logPath = Path.GetFullPath("Profile\\" + profile + "\\Logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            foreach (var file in Directory.GetFiles(logPath))
            {
                FileInfo fileInfo = new FileInfo(file);
                if ((DateTime.Now - fileInfo.CreationTime).TotalDays >= 7)
                {
                    fileInfo.Delete();
                }
            }
            logPath += "\\" + DateTime.Now.ToString("yyyy_MM_dd") + ".log";
            if (!File.Exists(logPath))
            {
                logStream = File.Create(logPath);
            }
            else
            {
                logStream = File.OpenWrite(logPath);
            }
        }
        /// <summary>
        /// Log info
        /// </summary>
        /// <param name="info"></param>
        public static void LogInfo(string info)
        {
            var text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][INF]: " + info;
            var data = Encoding.UTF8.GetBytes(text);
            logStream.WriteAsync(data, 0, data.Length);
            if (logger == null)
            {
                return;
            }
            logger.Invoke((MethodInvoker)delegate
            {
                logger.SelectionStart = logger.TextLength;
                logger.SelectionLength = 0;
                logger.SelectionColor = Color.Lime;
                logger.AppendText(text);
                logger.Select(logger.TextLength, 0);
                logger.ScrollToCaret();
            });
        }
        /// <summary>
        /// Log error
        /// </summary>
        /// <param name="info"></param>
        public static void LogError(string info)
        {
            var text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][ERR]: " + info;
            var data = Encoding.UTF8.GetBytes(text);
            logStream.WriteAsync(data, 0, data.Length);
            if (logger == null)
            {
                return;
            }
            logger.Invoke((MethodInvoker)delegate
            {
                logger.SelectionStart = logger.TextLength;
                logger.SelectionLength = 0;
                logger.SelectionColor = Color.Red;
                logger.AppendText(text);
                logger.Select(logger.TextLength, 0);
                logger.ScrollToCaret();
            });
        }
        /// <summary>
        /// Clear logs
        /// </summary>
        public static void ClearLog()
        {
            var text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][CLR]: ===========================================================";
            var data = Encoding.UTF8.GetBytes(text);
            logStream.WriteAsync(data, 0, data.Length);
            logger.Invoke((MethodInvoker)delegate
            {
                logger.Text = "";
            });
        }

        public static void LogDebug(string debug)
        {
            if (logger == null || !File.Exists("debug.txt"))
            {
                return;
            }
            var text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][DBG]: " + debug;
            var data = Encoding.UTF8.GetBytes(text);
            logStream.WriteAsync(data, 0, data.Length);
            logger.Invoke((MethodInvoker)delegate
            {
                logger.SelectionStart = logger.TextLength;
                logger.SelectionLength = 0;
                logger.SelectionColor = Color.LightGray;
                logger.AppendText(text);
                logger.Select(logger.TextLength, 0);
                logger.ScrollToCaret();
            });
        }


        public static void LogWarning(string warn)
        {
            var text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][WRN]: " + warn;
            var data = Encoding.UTF8.GetBytes(text);
            logStream.WriteAsync(data, 0, data.Length);
            if (logger == null)
            {
                return;
            }
            logger.Invoke((MethodInvoker)delegate
            {
                logger.SelectionStart = logger.TextLength;
                logger.SelectionLength = 0;
                logger.SelectionColor = Color.Yellow;
                logger.AppendText(text);
                logger.Select(logger.TextLength, 0);
                logger.ScrollToCaret();
            });
        }
    }
}
