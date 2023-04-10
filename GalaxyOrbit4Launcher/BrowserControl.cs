using CefSharp;
using CefSharp.WinForms;
using EasyTabs;
using GalaxyOrbit4Launcher.Models;
using GalaxyOrbit4Launcher.Models.GO4;
using GalaxyOrbit4Launcher.Service;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GalaxyOrbit4Launcher
{
    internal partial class BrowserControl : Form
    {
        private ChromiumWebBrowser chrome;
        private readonly string url = "https://client.guerradenaves.lat/?userId={0}&sessionKey={1}";
        private readonly GO2HttpService httpService;
        private readonly PlanetSettings planet;
        public int ExistingPlanets;
        protected TitleBarTabs ParentTabs => ParentForm as TitleBarTabs;
        private readonly Timer timer = new Timer();
        public int PlanetIndex = -1;
        private Process BotProcess = null;
        internal BrowserControl(PlanetSettings planet, GO2HttpService httpService)
        {
            InitializeComponent();
            this.planet = planet;
            this.httpService = httpService;
            timer.Tick += Timer_Tick;
            timer.Interval = 20000;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
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
                        if (item.Path.EndsWith("p" + PlanetIndex + ".exe"))
                        {
                            //bot running
                            BotProcess = Process.GetProcessById(item.ProcessID);
                            BotProcess.Exited += BotProcess_Exited;
                            ChromeContainer.Hide();
                            metroLabel1.Show();
                        }
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void BotProcess_Exited(object sender, EventArgs e)
        {
            ChromeContainer.Show();
            metroLabel1.Hide();
            button1_Click(sender, e);
        }

        private async void BrowserControl_Load(object sender, EventArgs e)
        {
            if (ExistingPlanets >= 3)
            {
                button2.Hide();
            }
            RequestContextSettings alphaContext = new RequestContextSettings
            {
                PersistUserPreferences = true,
                PersistSessionCookies = true,
                CachePath = Path.GetFullPath("cache"),
            };
            if (File.Exists("skiplogin.txt"))
            {
                chrome = new ChromiumWebBrowser(string.Format(url, null, null))
                {
                    RequestContext = new RequestContext(alphaContext),
                    FocusHandler = null
                };
                await Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    _ = chrome.RequestContext.SetPreference("profile.default_content_setting_values.plugins", 1, out string error);
                });
            }
            else
            {
                GetFrameResponse iframeUrl = await httpService.GetIFrameUrl(planet.PlanetId);
                if (iframeUrl.Data == null)
                {
                    try
                    {
                        Config settings = ConfigService.Instance.Config;
                        Profile profile = Encryption.Decrypt(settings.CredentialHash);
                        LoginResponse credential = await httpService.Login(profile.Email, profile.Password);
                        settings.AuthKey = credential.Data.Token;
                        httpService.SetToken(settings.AuthKey);
                        iframeUrl = await httpService.GetIFrameUrl(planet.PlanetId);
                    }
                    catch
                    {
                        _ = MessageBox.Show("Login failed! Auto restarting launcher now!");
                        _ = Process.Start(Process.GetCurrentProcess().MainModule.FileName);
                        Environment.Exit(0);
                    }
                }
                chrome = new ChromiumWebBrowser(string.Format(url, iframeUrl.Data.UserId, iframeUrl.Data.SessionKey))
                {
                    RequestContext = new RequestContext(alphaContext),
                    FocusHandler = null
                };
                await Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    _ = chrome.RequestContext.SetPreference("profile.default_content_setting_values.plugins", 1, out string error);
                });
            }
            chrome.MenuHandler = new CustomMenuHandler();
            chrome.Dock = DockStyle.Fill;
            chrome.IsBrowserInitializedChanged += Chrome_IsBrowserInitializedChanged;
            chrome.FrameLoadEnd += Chrome_FrameLoadEnd;
            _ = ChromeContainer.Invoke((MethodInvoker)delegate
            {
                ChromeContainer.Controls.Add(chrome);
            });
        }

        private void Chrome_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            BrowserControl_Resize(sender, e);
        }

        private void Chrome_IsBrowserInitializedChanged(object sender, EventArgs e)
        {
            if (!chrome.IsBrowserInitialized)
            {
                return;
            }
#if DEBUG
            if (File.Exists("debug.txt"))
            {
                _ = Task.Run(() =>
                {
                    chrome.Invoke((MethodInvoker)delegate
                    {
                        chrome.ShowDevTools();
                    });
                });
            }
#endif
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (File.Exists("skiplogin.txt"))
            {
                chrome.Load(string.Format(url, null, null));
                return;
            }
            if (BotProcess != null && BotProcess.HasExited)
            {
                ChromeContainer.Show();
                metroLabel1.Hide();
                button1_Click(sender, e);
            }
            else
            {
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
                            if (item.Path.EndsWith("p" + PlanetIndex + ".exe"))
                            {
                                //bot running
                                BotProcess = Process.GetProcessById(item.ProcessID);
                                BotProcess.Exited += BotProcess_Exited;
                                ChromeContainer.Hide();
                                metroLabel1.Show();
                                return;
                            }
                        }
                        catch
                        {

                        }
                    }
                }
            }
            GetFrameResponse iframeUrl = await httpService.GetIFrameUrl(planet.PlanetId);
            if (iframeUrl.Data == null)
            {
                try
                {
                    Config settings = ConfigService.Instance.Config;
                    Profile profile = Encryption.Decrypt(settings.CredentialHash);
                    LoginResponse credential = await httpService.Login(profile.Email, profile.Password);
                    settings.AuthKey = credential.Data.Token;
                    httpService.SetToken(settings.AuthKey);
                    iframeUrl = await httpService.GetIFrameUrl(planet.PlanetId);
                }
                catch
                {
                    _ = MessageBox.Show("Login failed! Auto restarting launcher now!");
                    _ = Process.Start(Process.GetCurrentProcess().MainModule.FileName);
                    Environment.Exit(0);
                }
            }
            _ = chrome.Invoke((MethodInvoker)delegate
            {
                chrome.Load(string.Format(url, iframeUrl.Data.UserId, iframeUrl.Data.SessionKey));
            });
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (ExistingPlanets >= 3)
            {
                button2.Hide();
                return;
            }
            CreatePlanet create = new CreatePlanet(httpService);
            if (create.ShowDialog() == DialogResult.OK)
            {
                _ = Process.Start(Process.GetCurrentProcess().MainModule.FileName);
                Environment.Exit(0);
            }
        }

        private void BrowserControl_Resize(object sender, EventArgs e)
        {
            try
            {
                chrome?.Invoke((MethodInvoker)delegate
                {
                    if (Width < 800 || Height < 800)
                    {
                        var wScale = ((Width / 800f) > 1 ? 1 : (Width / 800f));
                        var hScale = ((Height / 800f) > 1 ? 1 : (Height / 800f));
                        var rScale = Math.Min(wScale, hScale);
                        chrome.ExecuteScriptAsync("document.body.style.transform = \"scale(" +rScale+ ")\"");
                    }
                    else
                    {
                        chrome.ExecuteScriptAsync("document.body.style.transform = \"scale(1)\";");
                    }
                });
            }
            catch
            {

            }
        }
    }

    public class CustomMenuHandler : IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            _ = model.Clear();
        }

        public bool OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {

            return false;
        }

        public void OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {

        }

        public bool RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            return false;
        }
    }
}
