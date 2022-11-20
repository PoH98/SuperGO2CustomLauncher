using CefSharp;
using CefSharp.DevTools.CSS;
using CefSharp.Handler;
using CefSharp.WinForms;
using GO2FlashLauncher.Model;
using GO2FlashLauncher.Model.SGO2;
using GO2FlashLauncher.Models;
using GO2FlashLauncher.Script;
using GO2FlashLauncher.Script.GameLogic;
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
using System.Security.Principal;
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
        AbstractScript script;
        string profileName = "Bot1";
        BotSettings settings = new BotSettings();
        readonly RPC rpc = new RPC();
        BaseResources resources;
        GO2HttpService GO2HttpService = new GO2HttpService();
        int userId;
        string base64code;
        FileSystemWatcher fileSystemWatcher;
        int restartTimes;
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
            if(Screen.PrimaryScreen.Bounds.Width <= 1280 || Screen.PrimaryScreen.Bounds.Height <= 970)
            {
                MaximizeBox = false;
            }
            else
            {
                WindowState = FormWindowState.Maximized;
            }
            metroTabControl1.SelectedIndex = 0;
            metroTabControl2.SelectedIndex = 0;
            var settings = new CefSettings();
            settings.CachePath = Path.GetFullPath("cache");
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
            settings.SetOffScreenRenderingBestPerformanceArgs();
            settings.DisableGpuAcceleration();
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
            normalInstance.BackColor = Color.Transparent;
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
            foreach(var constellations in Enum.GetNames(typeof(Constellations)))
            {
                constellationStage.Items.Add(constellations);
            }
            alpha.BackColor = Color.Black;
            beta.BackColor = Color.Black;
            krtools = new ChromiumWebBrowser("https://krtools.deajae.co.uk/");
            alpha.RequestContext = new RequestContext(alphaContext);
            alpha.FocusHandler = null;
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
            instanceSelection.SelectedIndex = this.settings.Instance - 1;
            metroToggle1.Checked = this.settings.RunBot;
            metroToggle2.Checked = this.settings.RunWheel;
            numericUpDown1.Value = this.settings.HaltOn;
            numericUpDown2.Value = this.settings.InstanceHitCount;
            numericUpDown3.Value = this.settings.Delays;
            metroCheckBox1.Checked = this.settings.RestrictFight;
            metroCheckBox2.Checked = this.settings.TrialFight;
            metroComboBox1.SelectedIndex = this.settings.RestrictLevel -1 ;
            spin.Checked = this.settings.SpinWheel;
            metroCheckBox3.Checked = this.settings.SpinWithVouchers;
            numericUpDown4.Value = this.settings.MinVouchers;
            metroComboBox2.SelectedIndex = this.settings.TrialMaxLv - 1;
            constellationStage.SelectedIndex = this.settings.ConstellationStage;
            constellationLevel.SelectedIndex = this.settings.ConstellationLevel;
            constellationFight.Checked = this.settings.ConstellationFight;
            RenderFleets();
            metroTabControl4.SelectedIndex = 0;
            normalInstance.SelectedIndex = 0;
            restrictInstance.SelectedIndex = 0;
            trial.SelectedIndex = 0;
            constellation.SelectedIndex = 0;
            timer2.Start();
            discordRPC.Start();
            rpc.SetPresence();
#if !DEBUG
            metroTabControl1.Controls.Remove(metroTabPage3);
#endif
        }

        private async void BrowserInitializedChanged(object sender, EventArgs e)
        {
            if(script != null)
            {
                script.IsReloading = true;
            }
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
                    Datum selectedPlanet;
                    if (settings.PlanetId > -1)
                    {
                        selectedPlanet = planet.Data.FirstOrDefault(x => x.UserId == settings.PlanetId);
                    }
                    else
                    {
                        PlanetSelection planetSelection;
                        do
                        {
                            planetSelection = new PlanetSelection(planet);
                        }
                        while (planetSelection.ShowDialog() != DialogResult.OK);
                        selectedPlanet = planet.Data[planetSelection.SelectedProfile];
                        if (planetSelection.RememberMe)
                        {
                            settings.PlanetId = selectedPlanet.UserId;
                        }
                    }
                    //get selected planet
                    var url = await GO2HttpService.GetIFrameUrl(selectedPlanet.UserId);
                    alpha.Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                    Logger.LogInfo("Logging " + profile.Email + " success!");
                    settings.AuthKey = credential.Data.Token;
                    userId = url.Data.UserId;
                    if (script != null)
                    {
                        script.IsReloading = false;
                    }
                }
                catch
                {
                    restartTimes++;
                    Logger.LogError("Login failed! Retrying after " + (restartTimes * restartTimes) + " sec...");
                    await Task.Delay(restartTimes * restartTimes * 1000);
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
                        if (script != null)
                        {
                            script.IsReloading = false;
                        }
                    }
                    else
                    {
                        userId = settings.PlanetId;
                        var url = await GO2HttpService.GetIFrameUrl(settings.PlanetId);
                        (sender as ChromiumWebBrowser).Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                        if (script != null)
                        {
                            script.IsReloading = false;
                        }
                    }
                }
                catch
                {
                    restartTimes++;
                    settings.AuthKey = null;
                    Logger.LogError("Login failed! Retrying after " + (restartTimes * restartTimes) + " sec...");
                    await Task.Delay(restartTimes * restartTimes * 1000);
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
                    if (settings.RunBot)
                    {
                        RunScript(chrome, new InstanceScript(settings));
                    }
                    else if (settings.RunWheel)
                    {
                        RunScript(chrome, new WheelScript(settings));
                    }
                }
            }
        }

        private async void RunScript(ChromiumWebBrowser chrome, AbstractScript s)
        {
            if (script != null)
            {
                if (script.Running)
                {
                    return;
                }
            }
            script = s;
            await script.Run(chrome, userId, GO2HttpService).ConfigureAwait(false);
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
            try
            {
                var url = await GO2HttpService.GetIFrameUrl(userId);
                alpha.Load("https://beta-client.supergo2.com/?userId=" + url.Data.UserId + "&sessionKey=" + url.Data.SessionKey);
                if(script != null)
                {
                    if (script.Running)
                    {
                        script.Stop();
                    }
                }
            }
            catch
            {
                settings.AuthKey = null;
                BrowserInitializedChanged(alpha, e);
            }
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
            if (script != null)
            {
                if (script.Running)
                {
                    script.Stop();
                }
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
            if (Height < 970)
            {
                Height = 970;
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
            if (settings.RunWheel && metroToggle1.Checked)
            {
                MessageBox.Show("There is already running script here!");
                return;
            }
            if (script != null)
            {
                if (script.Running)
                {
                    script.Stop();
                }
            }
            settings.RunBot = metroToggle1.Checked;
            if (metroToggle1.Checked)
            {
                Logger.ClearLog();
                await Task.Delay(1000);
                Logger.LogWarning("Bot Started");
                RunScript(alpha, new InstanceScript(settings));
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
            normalInstance.Width = restrictInstance.Width = trial.Width = constellation.Width = (170 * 3) + 30;
            normalInstance.Controls.Clear();
            restrictInstance.Controls.Clear();
            trial.Controls.Clear();
            constellation.Controls.Clear();
            GenerateFleetTabs(0, normalInstance);
            GenerateFleetTabs(1, restrictInstance);
            GenerateFleetTabs(2, trial);
            GenerateFleetTabs(3, constellation);
            normalInstance.Controls.Add(GenerateLeftFleetTabs(0));
            restrictInstance.Controls.Add(GenerateLeftFleetTabs(1));
            trial.Controls.Add(GenerateLeftFleetTabs(2));
            constellation.Controls.Add(GenerateLeftFleetTabs(3));
        }

        public Control GenerateLeftFleetTabs(int type)
        {
            var tabCount = settings.Fleets.Count / 9;
            var t = new MetroTabPage    
            {
                Name = "FleetTab" + tabCount,
                Text = "Fleet Page " + tabCount,
                Theme = MetroThemeStyle.Dark
            };
            var p = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            for (int y = (settings.Fleets.Count - settings.Fleets.Count % 9); y < settings.Fleets.Count; y++)
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
                    Name = settings.Fleets[y].Name + "_" + type,
                    Top = 15,
                    Left = 15,
                    Width = 120,
                    Minimum = -1
                };
                switch(type)
                {
                    case 0:
                        input.Value = settings.Fleets[y].Order;
                        break;
                    case 1:
                        input.Value = settings.Fleets[y].RestrictOrder;
                        break;
                    case 2:
                        input.Value = settings.Fleets[y].TrialOrder;
                        break;
                    case 3:
                        input.Value = settings.Fleets[y].ConstellationOrder;
                        break;
                }
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
            return t;
        }

        public void GenerateFleetTabs(int type, Control tabControl)
        {
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
                for (int y = 9 * x; y < ((x + 1) * 9); y++)
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
                        Name = settings.Fleets[y].Name + "_" + type,
                        Top = 15,
                        Left = 15,
                        Width = 120,
                        Minimum = -1
                    };
                    switch (type)
                    {
                        case 0:
                            input.Value = settings.Fleets[y].Order;
                            break;
                        case 1:
                            input.Value = settings.Fleets[y].RestrictOrder;
                            break;
                        case 2:
                            input.Value = settings.Fleets[y].TrialOrder;
                            break;
                        case 3:
                            input.Value = settings.Fleets[y].ConstellationOrder;
                            break;
                    }
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
                tabControl.Controls.Add(tab);
            }
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

        private void discordRPC_Tick(object sender, EventArgs e)
        {
            rpc.SetPresence();
        }

        private async void metroToggle2_CheckedChanged(object sender, EventArgs e)
        {
            if (settings.RunBot && metroToggle2.Checked)
            {
                MessageBox.Show("There is already running script here!");
                return;
            }
            if (script != null)
            {
                if (script.Running)
                {
                    script.Stop();
                }
            }
            settings.RunWheel = metroToggle2.Checked;
            if (metroToggle2.Checked)
            {
                Logger.ClearLog();
                await Task.Delay(1000);
                Logger.LogWarning("Bot Started");
                RunScript(alpha, new WheelScript(settings));
            }
            else
            {
                Logger.LogWarning("Bot Stopped");
                resources = null;
            }
        }

        private void metroCheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            settings.RestrictFight = metroCheckBox1.Checked;
        }

        private void metroCheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            settings.TrialFight = metroCheckBox2.Checked;
        }

        private void metroComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.RestrictLevel = metroComboBox1.SelectedIndex + 1;
        }

        private void metroComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.TrialMaxLv = metroComboBox2.SelectedIndex + 1;
        }

        private void spin_CheckedChanged(object sender, EventArgs e)
        {
            settings.SpinWheel = spin.Checked;
        }

        private void metroCheckBox3_CheckedChanged(object sender, EventArgs e)
        {
            settings.SpinWithVouchers = metroCheckBox3.Checked;
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            settings.MinVouchers = (int)numericUpDown4.Value;
        }

        private void numericUpDown4_KeyDown(object sender, KeyEventArgs e)
        {
            settings.MinVouchers = (int)numericUpDown4.Value;
        }

        private void constellationStage_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.ConstellationStage = constellationStage.SelectedIndex;
        }

        private void constellationLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.ConstellationLevel = constellationLevel.SelectedIndex;
        }

        private void constellationFight_CheckedChanged(object sender, EventArgs e)
        {
            settings.ConstellationFight = constellationFight.Checked;
        }

        private void Input_ValueChanged(object sender, EventArgs e)
        {
            var name = string.Join("_", (sender as NumericUpDown).Name.Split('_').Take((sender as NumericUpDown).Name.Split('_').Length - 1));
            var type = Convert.ToInt32((sender as NumericUpDown).Name.Split('_').Last());
            switch (type)
            {
                case 0:
                    settings.Fleets.First(x => x.Name == name).Order = (int)Math.Round((sender as NumericUpDown).Value);
                    break;
                case 1:
                    settings.Fleets.First(x => x.Name == name).RestrictOrder = (int)Math.Round((sender as NumericUpDown).Value);
                    break;
                case 2:
                    settings.Fleets.First(x => x.Name == name).TrialOrder = (int)Math.Round((sender as NumericUpDown).Value);
                    break;
                case 3:
                    settings.Fleets.First(x => x.Name == name).ConstellationOrder = (int)Math.Round((sender as NumericUpDown).Value);
                    break;
            }

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
