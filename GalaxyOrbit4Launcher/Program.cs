using CefSharp;
using CefSharp.SchemeHandler;
using CefSharp.WinForms;
using EasyTabs;
using GalaxyOrbit4Launcher.Service;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace GalaxyOrbit4Launcher
{
    internal static class Program
    {
        private static readonly string Host = "client.guerradenaves.lat";
        private static uint ColorToUInt(Color color)
        {
            return (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | (color.B << 0));
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            CefSettings settings = new CefSettings
            {
                CachePath = Path.GetFullPath("cache")
            };
            settings.CefCommandLineArgs.Add("enable-system-flash", "1");
            settings.CefCommandLineArgs.Add("ppapi-flash-path", Path.Combine(new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ToString(), "libs\\pepflashplayer.dll"));
            settings.CefCommandLineArgs.Add("ppapi-flash-version", "28.0.0.137");
            settings.CefCommandLineArgs["plugin-policy"] = "allow";
            settings.CefCommandLineArgs.Add("allow-outdated-plugins");
            settings.CefCommandLineArgs.Add("use-angle", "gl");
            settings.CefCommandLineArgs.Add("disable-quic");
            settings.CefCommandLineArgs.Add("off-screen-rendering-enabled");
            settings.CefCommandLineArgs.Add("no-activate");
            settings.BackgroundColor = ColorToUInt(Color.Black);
            settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) supergo2-beta/1.0.0-beta Chrome/85.0.4183.121 Electron/10.1.3 Safari/537.36";
            settings.SetOffScreenRenderingBestPerformanceArgs();
            settings.LogSeverity = LogSeverity.Fatal;
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "https",
                DomainName = Host,
                SchemeHandlerFactory = new FolderSchemeHandlerFactory(
                    rootFolder: ClientUpdatorService.Instance.RootPath,
                    hostName: Host,
                    defaultPage: "index.html"
                )
            });
            if (!Cef.Initialize(settings, true))
            {
                throw new Exception("Unable to Initialize Cef");
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Loader loader = new Loader();
            Thread load = new Thread(() =>
            {
                _ = loader.ShowDialog();
            });
            load.Start();
            ClientUpdatorService.Instance.UpdateFiles();
            string wmiQueryString = "SELECT ProcessID, ExecutablePath FROM Win32_Process";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQueryString))
            using (ManagementObjectCollection results = searcher.Get())
            {
                var query = from mo in results.Cast<ManagementObject>()
                            select new
                            {
                                ProcessID = (int)(uint)mo["ProcessID"],
                                Path = (string)mo["ExecutablePath"],
                            };
                foreach (var item in query.Where(x => !string.IsNullOrEmpty(x.Path)))
                {
                    try
                    {
                        if (item.Path.EndsWith("Fiddle.exe"))
                        {
                            _ = MessageBox.Show("Detected cheating applications! The game will exit immediately!");
                            Environment.Exit(-1);
                        }
                        if (item.Path.EndsWith("Wireshark.exe"))
                        {
                            _ = MessageBox.Show("Detected cheating applications! The game will exit immediately!");
                            Environment.Exit(-1);
                        }
                    }
                    catch
                    {

                    }
                }
            }
            AppContainer container = new AppContainer();
            TitleBarTabsApplicationContext applicationContext = new TitleBarTabsApplicationContext();
            load = new Thread(() =>
            {
                _ = loader.Invoke((MethodInvoker)delegate
                {
                    loader.Close();
                });
            });
            load.Start();
            applicationContext.Start(container);
            Application.Run(applicationContext);
        }
    }
}
