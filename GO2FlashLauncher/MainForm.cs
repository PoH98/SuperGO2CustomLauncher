using CefSharp;
using CefSharp.Handler;
using CefSharp.WinForms;
using GO2FlashLauncher.Model;
using GO2FlashLauncher.Models;
using GO2FlashLauncher.Script;
using GO2FlashLauncher.Service;
using MetroFramework;
using MetroFramework.Controls;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GO2FlashLauncher
{
    public partial class MainForm : Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        ChromiumWebBrowser alpha, beta, krtools;
        HttpClient hc = new HttpClient();
        CancellationTokenSource cancellation;
        MainScript script;
        string profileName = "Bot1";
        BotSettings settings = new BotSettings();
        readonly RPC rpc = new RPC();
        BaseResources resources;
        GO2HttpService GO2HttpService = new GO2HttpService();
        int userId;
        string base64code;
        FileSystemWatcher fileSystemWatcher;
        public MainForm()
        {
            InitializeComponent();
            if (!Directory.Exists("Profile"))
            {
                Directory.CreateDirectory("Profile");
            }
            if (!Directory.Exists("Profile\\" + profileName))
            {
                Directory.CreateDirectory("Profile\\" + profileName);
            }
            if (File.Exists("Profile\\" + profileName + "\\config.json"))
            {
                settings = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText("Profile\\" + profileName + "\\config.json"));
            }
            numericUpDown1.Maximum = decimal.MaxValue;
            numericUpDown2.Maximum = decimal.MaxValue;
            Logger.Init(richTextBox1, profileName);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("debug.log"))
            {
                File.Delete("debug.log");
            }
            
            WindowState = FormWindowState.Maximized;
            metroTabControl1.SelectedIndex = 0;
            var settings = new CefSettings();
            settings.CachePath = Path.GetFullPath("cache");
            settings.CefCommandLineArgs.Add("enable-system-flash", "1");
            settings.CefCommandLineArgs.Add("ppapi-flash-path", Path.Combine(new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ToString(), "libs\\pepflashplayer.dll"));
            settings.CefCommandLineArgs.Add("ppapi-flash-version", "28.0.0.137");
            settings.CefCommandLineArgs.Add("disable-gpu", "1");
            settings.CefCommandLineArgs.Add("disable-gpu-compositing", "1");
            settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");
            settings.CefCommandLineArgs.Add("disable-gpu-vsync", "1");
            settings.CefCommandLineArgs["plugin-policy"] = "allow";
            settings.CefCommandLineArgs.Add("allow-outdated-plugins");
            settings.CefCommandLineArgs.Add("use-angle", "gl");
            settings.CefCommandLineArgs.Add("disable-quic");
            settings.CefCommandLineArgs.Add("off-screen-rendering-enabled");
            settings.CefCommandLineArgs.Add("no-activate");
            settings.BackgroundColor = ColorToUInt(Color.Black);
#if !DEBUG
            metroButton3.Hide();
            if (File.Exists("debug.txt"))
            {
                File.Delete("debug.txt");
            }
            settings.LogSeverity = LogSeverity.Fatal;
#endif
            metroTabControl1.BackColor = Color.Transparent;
            metroTabControl2.BackColor = Color.Transparent;
            metroTabControl3.BackColor = Color.Transparent;
            var alphaContext = new RequestContextSettings
            {
                IgnoreCertificateErrors = true,
                PersistUserPreferences = true,
                PersistSessionCookies = true,
                CachePath = Path.GetFullPath("cache"),
                
            }
            ;
            var betaContext = new RequestContextSettings
            {
                IgnoreCertificateErrors = true,
                PersistUserPreferences = true,
                PersistSessionCookies = true,
                CachePath = Path.GetFullPath("cache")
            };
            if (!Cef.Initialize(settings, true))
            {
                throw new Exception("Unable to Initialize Cef");
            }

            beta = new ChromiumWebBrowser("blank");
            alpha = new ChromiumWebBrowser("blank");
            alpha.BackColor = Color.Black;
            beta.BackColor = Color.Black;
            krtools = new ChromiumWebBrowser("https://krtools.deajae.co.uk/");
            alpha.RequestContext = new RequestContext(alphaContext);
            beta.RequestContext = new RequestContext(betaContext);
            alpha.BrowserSettings.Plugins = CefState.Enabled;
            beta.BrowserSettings.Plugins = CefState.Enabled;
            Cef.UIThreadTaskFactory.StartNew(delegate
            {
                alpha.RequestContext.SetPreference("profile.default_content_setting_values.plugins", 1, out string error);
                beta.RequestContext.SetPreference("profile.default_content_setting_values.plugins", 1, out error);
            });
            alpha.MenuHandler = new CustomMenuHandler();
            beta.MenuHandler = new CustomMenuHandler();
            krtools.MenuHandler = new CustomMenuHandler();
            panel1.Controls.Add(alpha);
            panel2.Controls.Add(beta);
            panel3.Controls.Add(krtools);
            alpha.Dock = DockStyle.Fill;
            beta.Dock = DockStyle.Fill;
            krtools.Dock = DockStyle.Fill;
            beta.IsBrowserInitializedChanged += Beta_IsBrowserInitializedChanged;
            alpha.IsBrowserInitializedChanged += BrowserInitializedChanged;
            alpha.LoadingStateChanged += ChromiumWebBrowser_LoadingStateChanged;
            beta.LoadingStateChanged += ChromiumWebBrowser_LoadingStateChanged;
            timer1_Tick(null, null);
            timer1.Start();
            discordRPC_Tick(null, null);
            instanceSelection.SelectedIndex = this.settings.Instance - 1;
            metroToggle1.Checked = this.settings.RunBot;
            numericUpDown1.Value = this.settings.HaltOn;
            numericUpDown2.Value = this.settings.InstanceHitCount;
            numericUpDown3.Value = this.settings.Delays;
            RenderFleets();
            timer2.Start();
#if !DEBUG
            metroTabControl1.Controls.Remove(metroTabPage3);
#endif
        }

        private async void BrowserInitializedChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(settings.AuthKey))
            {
                if (string.IsNullOrEmpty(settings.CredentialHash))
                {
                    //need login
                    Login login = new Login(profileName);
                    if (login.ShowDialog() == DialogResult.OK)
                    {
                        if (File.Exists("Profile\\" + profileName + "\\config.json"))
                        {
                            settings = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText("Profile\\" + profileName + "\\config.json"));
                        }
                    }
                    else
                    {
                        MessageBox.Show("We can't log you in without your credentials! Exiting...");
                        Close();
                    }
                }
                try
                {
                    var profile = Encryption.Decrypt(settings.CredentialHash);
                    Logger.LogInfo("Logging " + profile.Email + " in....Please be patient!");
                    var credential = await GO2HttpService.Login(profile.Email, profile.Password);
                    var planet = await GO2HttpService.GetPlanets();
                    PlanetSelection planetSelection;
                    do
                    {
                        planetSelection = new PlanetSelection(planet);
                    }
                    while (planetSelection.ShowDialog() != DialogResult.OK);
                    //get selected planet
                    var selectedPlanet = planet.Data[planetSelection.SelectedProfile];
                    var url = await GO2HttpService.GetIFrameUrl(selectedPlanet.UserId);
                    alpha.Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                    Logger.LogInfo("Logging " + profile.Email + " success!");
                    settings.AuthKey = credential.Data.Token;
                    if (planetSelection.RememberMe)
                    {
                        settings.PlanetId = url.Data.UserId;
                    }
                    userId = url.Data.UserId;
                }
                catch (Exception ex)
                {
                    settings.AuthKey = null;
                    Logger.LogError("Login failed! Retrying...\nError Info: \n" + ex.ToString());
                    BrowserInitializedChanged(sender, e);
                }
            }
            else
            {
                try
                {
                    GO2HttpService.SetToken(settings.AuthKey);
                    if (settings.PlanetId == -1)
                    {
                        var planet = await GO2HttpService.GetPlanets();
                        PlanetSelection planetSelection;
                        do
                        {
                            planetSelection = new PlanetSelection(planet);
                        }
                        while (planetSelection.ShowDialog() != DialogResult.OK);
                        var selectedPlanet = planet.Data[planetSelection.SelectedProfile];
                        var url = await GO2HttpService.GetIFrameUrl(selectedPlanet.UserId);
                        if (planetSelection.RememberMe)
                        {
                            settings.PlanetId = url.Data.UserId;
                        }
                        userId = url.Data.UserId;
                        (sender as ChromiumWebBrowser).Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                    }
                    else
                    {
                        userId = settings.PlanetId;
                        var url = await GO2HttpService.GetIFrameUrl(settings.PlanetId);
                        (sender as ChromiumWebBrowser).Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                    }
                }
                catch
                {
                    settings.AuthKey = null;
                    BrowserInitializedChanged(sender, e);
                }
            }
        }

        private void Beta_IsBrowserInitializedChanged(object sender, EventArgs e)
        {

        }
        private void ChromiumWebBrowser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            var chrome = (ChromiumWebBrowser)sender;
            if (chrome.Address == "blank")
            {
                return;
            }
            if (!e.IsLoading)
            {

                if (chrome.Address.Contains("igg"))
                {
                    chrome.Back();
                    return;
                }              
                else if (chrome.Address.StartsWith("https://beta-client.supergo2.com/?userId="))
                {
                    if (!e.Browser.HasDocument)
                    {
                        //error
                        settings.AuthKey = null;
                        BrowserInitializedChanged(sender, e);
                    }
                    RunScript(chrome);
                }
            }
        }

        private void RunScript(ChromiumWebBrowser chrome)
        {
            if (script != null)
            {
                if (script.Running)
                {
                    return;
                }
            }
            if (!metroToggle1.Checked)
            {
                return;
            }
            cancellation = new CancellationTokenSource();
            script = new MainScript(settings);
            _ = script.Run(cancellation.Token, chrome, userId, GO2HttpService).ConfigureAwait(false);
        }

        private string ConvertImage(string path)
        {
            using (Image image = Image.FromFile(path))
            {
                using (MemoryStream m = new MemoryStream())
                {
                    image.Save(m, image.RawFormat);
                    byte[] imageBytes = m.ToArray();

                    // Convert byte[] to Base64 String
                    string base64String = Convert.ToBase64String(imageBytes);
                    return base64String;
                }
            }
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(Path.GetFullPath("cache\\background.settings"), openFileDialog.FileName);
            }
        }

        private async void metroButton2_Click(object sender, EventArgs e)
        {
            if(alpha == null)
            {
                return;
            }
            var url = await GO2HttpService.GetIFrameUrl(userId);
            alpha.Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
            if (cancellation != null)
                cancellation.Cancel();
        }

        private void metroButton3_Click(object sender, EventArgs e)
        {
            alpha.ShowDevTools();
        }

        private void closeBtn_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void minimizeBtn_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void maximizeBtn_Click(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
            {
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                WindowState = FormWindowState.Normal;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!Directory.Exists("Profile"))
            {
                Directory.CreateDirectory("Profile");
            }
            if (!Directory.Exists("Profile\\" + profileName))
            {
                Directory.CreateDirectory("Profile\\" + profileName);
            }
            File.WriteAllText("Profile\\" + profileName + "\\config.json", JsonConvert.SerializeObject(settings));
            if(cancellation != null)
            {
                cancellation.Cancel();
            }
            Cef.Shutdown();
            Application.Exit();
        }

        private void metroButton5_Click(object sender, EventArgs e)
        {
            if (File.Exists(Path.GetFullPath("cache\\background.settings")))
            {
                File.Delete(Path.GetFullPath("cache\\background.settings"));
            }
            alpha.ExecuteScriptAsync("document.body.style.backgroundColor = 'black'; document.body.style.backgroundImage = 'none'");
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (Width < 1260)
            {
                Width = 1260;
            }
            if (Height < 1060)
            {
                Height = 1060;
            }
            if (alpha == null)
            {
                return;
            }
            FormBorderStyle = FormBorderStyle.Sizable;
            metroTabPage1.Dock = DockStyle.Fill;
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                var response = await hc.GetAsync("https://beta-api.supergo2.com/metrics/online");
                var online = JsonConvert.DeserializeObject<OnlinePlayers>(await response.Content.ReadAsStringAsync());
                label1.Text = "Online Players: " + online.Data.Online;
            }
            catch
            {
                label1.Text = "Online Players: 0";
            }
            if(script != null)
            {
                if (!script.Running)
                {
                    return;
                }
                if(resources == null && (script.Resources.Metal != 0 || script.Resources.HE3 != 0 || script.Resources.Gold != 0))
                {
                    resources = script.Resources;
                }
                if(resources != null)
                {
                    //set resources gain
                    metalTotal.Text = (script.Resources.Metal - resources.Metal).ToString("N0");
                    heTotal.Text = (script.Resources.HE3 - resources.HE3).ToString("N0");
                    goldTotal.Text = (script.Resources.Gold - resources.Gold).ToString("N0");
                    if(script.BotRuntime.TotalHours < 0)
                    {
                        return;
                    }
                    metalPerHour.Text = ((script.Resources.Metal - resources.Metal) / script.BotRuntime.TotalHours).ToString("N0") + "/h";
                    hePerHour.Text = ((script.Resources.HE3 - resources.HE3) / script.BotRuntime.TotalHours).ToString("N0") + "/h";
                    goldPerHour.Text = ((script.Resources.Gold - resources.Gold) / script.BotRuntime.TotalHours).ToString("N0") + "/h";
                }
            }
            _ = Task.Run(async () =>
            {
                do
                {
                    if (!File.Exists(Path.GetFullPath("cache\\background.settings")) && alpha.CanExecuteJavascriptInMainFrame)
                    {
                        alpha.ExecuteScriptAsync("document.body.style.backgroundColor = 'black'; document.body.style.backgroundImage = 'none';");
                    }
                    else if (alpha.CanExecuteJavascriptInMainFrame)
                    {
                        if (string.IsNullOrEmpty(base64code))
                        {
                            base64code = ConvertImage(File.ReadAllText(Path.GetFullPath("cache\\background.settings")));
                        }
                        alpha.ExecuteScriptAsync("document.body.style.backgroundImage = 'url(data:image/png;base64," + base64code + ")';");
                    }
                    await Task.Delay(2000);
                }
                while (!alpha.CanExecuteJavascriptInMainFrame);
            });
        }

        private void metroButton6_Click(object sender, EventArgs e)
        {
            beta.ShowDevTools();
        }

        private async void metroToggle1_CheckedChanged(object sender, EventArgs e)
        {
            if (cancellation != null)
            {
                cancellation.Cancel();
            }
            settings.RunBot = metroToggle1.Checked;
            if (metroToggle1.Checked)
            {
                Logger.ClearLog();
                await Task.Delay(1000);
                Logger.LogWarning("Bot Started");
                RunScript(alpha);
            }
            else
            {
                Logger.LogWarning("Bot Stopped");
                resources = null;
            }
        }

        private void instanceSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.Instance = instanceSelection.SelectedIndex + 1;
        }

        private void metroButton8_Click(object sender, EventArgs e)
        {
            settings.Fleets.Add(new Fleet
            {
                Name = "Fleet " + (settings.Fleets.Count + 1),
                Order = settings.Fleets.Count
            });
            RenderFleets();
        }

        private void metroButton7_Click(object sender, EventArgs e)
        {
            beta.Reload(true);
        }

        private void RenderFleets()
        {
            metroTabControl3.Width = (170 * 3) + 30;
            metroTabControl3.Controls.Clear();
            int tabs = settings.Fleets.Count / 9;
            int x = 0;
            for (x = 0; x < tabs; x++)
            {
                var tab = new MetroTabPage
                {
                    Name = "FleetTab" + x,
                    Text = "Fleet Page " + x,
                    Theme = MetroThemeStyle.Dark
                };
                var panel = new FlowLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent
                };
                for (int y = 1 * x; y < ((x + 1) * 9); y++)
                {
                    var group = new GroupBox
                    {
                        Text = settings.Fleets[y].Name,
                        Height = 45,
                        Width = 170,
                        ForeColor = Color.White
                    };
                    var input = new NumericUpDown
                    {
                        Name = settings.Fleets[y].Name,
                        Value = settings.Fleets[y].Order,
                        Top = 15,
                        Left = 15,
                        Width = 120
                    };
                    var removeFleet = new MetroButton
                    {
                        Name = "btnRem_" + settings.Fleets[y].Name,
                        Text = "🗙",
                        Top = 15,
                        Left = 140,
                        Height = 20,
                        Width = 20,
                        Theme = MetroThemeStyle.Dark
                    };
                    removeFleet.Click += RemoveFleet_Click;
                    input.ValueChanged += Input_ValueChanged;
                    group.Controls.Add(input);
                    group.Controls.Add(removeFleet);
                    panel.Controls.Add(group);
                }
                tab.Controls.Add(panel);
                metroTabControl3.Controls.Add(tab);
            }
            var t = new MetroTabPage
            {
                Name = "FleetTab" + x,
                Text = "Fleet Page " + x,
                Theme = MetroThemeStyle.Dark
            };
            var p = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            for (int y = x * 9; y < settings.Fleets.Count; y++)
            {
                var group = new GroupBox
                {
                    Text = settings.Fleets[y].Name,
                    Height = 45,
                    Width = 170,
                    ForeColor = Color.White
                };
                var input = new NumericUpDown
                {
                    Name = settings.Fleets[y].Name,
                    Value = settings.Fleets[y].Order,
                    Top = 15,
                    Left = 15,
                    Width = 120
                };
                var removeFleet = new MetroButton
                {
                    Name = "btnRem_" + settings.Fleets[y].Name,
                    Text = "🗙",
                    Top = 15,
                    Left = 140,
                    Height = 20,
                    Width = 20,
                    Theme = MetroThemeStyle.Dark
                };
                removeFleet.Click += RemoveFleet_Click;
                input.ValueChanged += Input_ValueChanged;
                group.Controls.Add(input);
                group.Controls.Add(removeFleet);
                p.Controls.Add(group);
            }
            t.Controls.Add(p);
            metroTabControl3.Controls.Add(t);
        }

        private void metroButton10_Click(object sender, EventArgs e)
        {
            krtools.Reload();
        }

        private void metroButton9_Click(object sender, EventArgs e)
        {
            if (krtools.CanGoBack)
            {
                krtools.Back();
            }
        }

        private void metroButton11_Click(object sender, EventArgs e)
        {
            if (krtools.CanGoForward)
            {
                krtools.Forward();
            }
        }

        private void discordRPC_Tick(object sender, EventArgs e)
        {
            rpc.SetPresence();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            settings.HaltOn = numericUpDown1.Value;
        }

        private void numericUpDown1_KeyPress(object sender, KeyPressEventArgs e)
        {
            settings.HaltOn = numericUpDown1.Value;
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            settings.InstanceHitCount = numericUpDown2.Value;
        }

        private void numericUpDown2_KeyPress(object sender, KeyPressEventArgs e)
        {
            settings.InstanceHitCount = numericUpDown2.Value;
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            settings.Delays = (int)Math.Round(numericUpDown3.Value);
        }

        private void numericUpDown3_KeyPress(object sender, KeyPressEventArgs e)
        {
            settings.Delays = (int)Math.Round(numericUpDown3.Value);
        }

        private void RemoveFleet_Click(object sender, EventArgs e)
        {
            var name = (sender as MetroButton).Name.Replace("btnRem_", "");
            var data = settings.Fleets.First(x => x.Name == name);
            settings.Fleets.Remove(data);
            RenderFleets();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if(fileSystemWatcher == null)
            {
                fileSystemWatcher = new FileSystemWatcher(Path.Combine(new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ToString(), "cache\\"));
                fileSystemWatcher.Filter = "background.settings";
                fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                fileSystemWatcher.EnableRaisingEvents = true;
                fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            }
            _ = Task.Run(async () =>
            {
                do
                {
                    if (!File.Exists(Path.GetFullPath("cache\\background.settings")) && alpha.CanExecuteJavascriptInMainFrame)
                    {
                        alpha.ExecuteScriptAsync("document.body.style.backgroundColor = 'black'; document.body.style.backgroundImage = 'none';");
                    }
                    else if (alpha.CanExecuteJavascriptInMainFrame)
                    {
                        if (string.IsNullOrEmpty(base64code))
                        {
                            var path = File.ReadAllText(Path.GetFullPath("cache\\background.settings"));
                            base64code = ConvertImage(path);
                        }
                        alpha.ExecuteScriptAsync("document.body.style.backgroundImage = 'url(data:image/png;base64," + base64code + ")'; document.body.style.backgroundPosition = '0 -122px 0 -180px'; document.body.style.backgroundRepeat = 'no-repeat'; )");

                    }
                    await Task.Delay(2000);
                }
                while (!alpha.CanExecuteJavascriptInMainFrame);
            });
        }

        private async void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            await Task.Delay(500);
            var path = File.ReadAllText(Path.GetFullPath("cache\\background.settings"));
            base64code = ConvertImage(path);
        }

        private void Input_ValueChanged(object sender, EventArgs e)
        {
            settings.Fleets.First(x => x.Name == (sender as NumericUpDown).Name).Order = (int)Math.Round((sender as NumericUpDown).Value);

        }

        private uint ColorToUInt(Color color)
        {
            return (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | (color.B << 0));
        }
    }

    public class CustomMenuHandler : IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            model.Clear();
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
