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
        private static DateTime logDate;
        public static void Init(RichTextBox richTextBox, string p)
        {
            profile = p;
            logger = richTextBox;
            logPath = Path.Combine(ConfigService.Instance.ConfigFolder, "Profile", "Logs");
            if (!Directory.Exists(logPath))
            {
                _ = Directory.CreateDirectory(logPath);
            }
            foreach (string file in Directory.GetFiles(logPath))
            {
                FileInfo fileInfo = new FileInfo(file);
                if ((DateTime.Now - fileInfo.CreationTime).TotalDays >= 7)
                {
                    fileInfo.Delete();
                }
            }
            logPath = Path.Combine(logPath, DateTime.Now.ToString("yyyy_MM_dd") + ".log");
            logDate = DateTime.Now.Date;
            try
            {
                logStream = !File.Exists(logPath) ? File.Create(logPath, 4096, FileOptions.WriteThrough) : File.OpenWrite(logPath);
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
            if (logStream == null)
            {
                Init(logger, profile);
            }
            if (logDate != DateTime.Now.Date)
            {
                logStream.Close();
                Init(logger, profile);
            }
            string text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][INF]: " + info;
            byte[] data = Encoding.UTF8.GetBytes(text);
            await logStream.WriteAsync(data, 0, data.Length);
            if (logger == null)
            {
                return;
            }
            _ = logger.Invoke((MethodInvoker)delegate
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
            if (logDate != DateTime.Now.Date)
            {
                logStream.Close();
                Init(logger, profile);
            }
            string text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][ERR]: " + info;
            byte[] data = Encoding.UTF8.GetBytes(text);
            await logStream.WriteAsync(data, 0, data.Length);
            if (logger == null)
            {
                return;
            }
            _ = logger.Invoke((MethodInvoker)delegate
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
            if (logDate != DateTime.Now.Date)
            {
                logStream.Close();
                Init(logger, profile);
            }
            string text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][CLR]: ===========================================================";
            byte[] data = Encoding.UTF8.GetBytes(text);
            await logStream.WriteAsync(data, 0, data.Length);
            _ = logger.Invoke((MethodInvoker)delegate
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
            if (logDate != DateTime.Now.Date)
            {
                logStream.Close();
                Init(logger, profile);
            }
            string text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][DBG]: " + debug;
            byte[] data = Encoding.UTF8.GetBytes(text);
            await logStream.WriteAsync(data, 0, data.Length);
            _ = logger.Invoke((MethodInvoker)delegate
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
            if (logDate != DateTime.Now.Date)
            {
                logStream.Close();
                Init(logger, profile);
            }
            string text = "\n" + "[" + DateTime.Now.ToString("HH:mm") + "][WRN]: " + warn;
            byte[] data = Encoding.UTF8.GetBytes(text);
            await logStream.WriteAsync(data, 0, data.Length);
            if (logger == null)
            {
                return;
            }
            _ = logger.Invoke((MethodInvoker)delegate
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
