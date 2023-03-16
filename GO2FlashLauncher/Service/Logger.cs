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
        private static string profile;
        public static void Init(RichTextBox richTextBox, string p)
        {
            profile = p;
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
            try
            {
                if (!File.Exists(logPath))
                {
                    logStream = File.Create(logPath, 4096, FileOptions.WriteThrough);
                }
                else
                {
                    logStream = File.OpenWrite(logPath);
                }
            }
            catch
            {

            }
        }
        /// <summary>
        /// Log info
        /// </summary>
        /// <param name="info"></param>
        public static async void LogInfo(string info)
        {
            if(logStream == null)
            {
                Init(logger, profile);
            }
            var text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][INF]: " + info;
            var data = Encoding.UTF8.GetBytes(text);
            await logStream.WriteAsync(data, 0, data.Length);
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
        public static async void LogError(string info)
        {
            if (logStream == null)
            {
                Init(logger, profile);
            }
            var text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][ERR]: " + info;
            var data = Encoding.UTF8.GetBytes(text);
            await logStream.WriteAsync(data, 0, data.Length);
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
        public static async void ClearLog()
        {
            if (logStream == null)
            {
                Init(logger, profile);
            }
            var text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][CLR]: ===========================================================";
            var data = Encoding.UTF8.GetBytes(text);
            await logStream.WriteAsync(data, 0, data.Length);
            logger.Invoke((MethodInvoker)delegate
            {
                logger.Text = "";
            });
        }

        public static async void LogDebug(string debug)
        {
            if (logger == null || !File.Exists("debug.txt"))
            {
                return;
            }
            if (logStream == null)
            {
                Init(logger, profile);
            }
            var text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][DBG]: " + debug;
            var data = Encoding.UTF8.GetBytes(text);
            await logStream.WriteAsync(data, 0, data.Length);
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


        public static async void LogWarning(string warn)
        {
            if (logStream == null)
            {
                Init(logger, profile);
            }
            var text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][WRN]: " + warn;
            var data = Encoding.UTF8.GetBytes(text);
            await logStream.WriteAsync(data, 0, data.Length);
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

        public static void CloseLog()
        {
            if (logStream == null)
            {
                Init(logger, profile);
            }
            logStream.Close();
        }
    }
}
