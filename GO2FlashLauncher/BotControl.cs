using CefSharp;
using CefSharp.SchemeHandler;
using CefSharp.WinForms;
using Discord.WebSocket;
using GO2FlashLauncher.Model;
using GO2FlashLauncher.Script;
using GO2FlashLauncher.Script.GameLogic;
using GO2FlashLauncher.Service;
using MetroFramework;
using MetroFramework.Controls;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GO2FlashLauncher
{
    internal partial class BotControl : UserControl
    {
        private ChromiumWebBrowser chrome;
        private readonly string Host = "client.guerradenaves.lat";
        private readonly string url = "https://client.guerradenaves.lat/?userId={0}&sessionKey={1}";
        private readonly GO2HttpService httpService;
        private readonly PlanetSettings planet;
        private readonly BotSettings botSettings;
        private AbstractScript script;
        private DiscordSocketClient client;
        private BaseResources resources;

        public BotControl(BotSettings botSettings, PlanetSettings planet, GO2HttpService httpService, DiscordSocketClient client)
        {
            this.planet = planet;
            this.botSettings = botSettings;
            this.httpService = httpService;
            this.client = client;
            if (!Cef.IsInitialized)
            {
                CefSettings settings = new CefSettings
                {
                    CachePath = Path.GetFullPath("cache")
                };
                settings.CefCommandLineArgs.Add("enable-system-flash", "1");
                settings.CefCommandLineArgs.Add("ppapi-flash-path", Path.Combine(Application.ExecutablePath.Remove(Application.ExecutablePath.LastIndexOf("\\")), "libs\\pepflashplayer.dll"));
                settings.CefCommandLineArgs.Add("ppapi-flash-version", "28.0.0.137");
                settings.CefCommandLineArgs["plugin-policy"] = "allow";
                settings.CefCommandLineArgs.Add("allow-outdated-plugins");
                settings.CefCommandLineArgs.Add("use-angle", "gl");
                settings.CefCommandLineArgs.Add("disable-quic");
                settings.CefCommandLineArgs.Add("off-screen-rendering-enabled");
                settings.CefCommandLineArgs.Add("no-activate");
                settings.BackgroundColor = ColorToUInt(Color.Black);
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
            }
            ClientUpdatorService.Instance.UpdateFiles();
            InitializeComponent();
            timer2.Start();
            if (client != null)
            {
                client.MessageReceived += Client_MessageReceived;
            }
            if (File.Exists("debug.txt"))
            {
                metroButton3.Visible = true;
            }
            if (File.Exists("bot.txt"))
            {
                ConfigTabs.Visible = true;
                metroToggle1.Visible = true;
                metroLabel14.Visible = true;
                RenderFleets();
            }
            else
            {
                BotInstance.Controls.Remove(BotSettings);
            }
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            if (arg.Author.IsBot)
            {
                return;
            }
            try
            {
                bool replyOnce = botSettings.PlanetSettings.IndexOf(planet) == 0;
                if (botSettings.DiscordUserID == ulong.Parse("0"))
                {
                    if (replyOnce)
                    {
                        if (arg.Content.Trim() == "bind " + botSettings.DiscordSecret)
                        {
                            botSettings.DiscordUserID = arg.Author.Id;
                            _ = await arg.Channel.SendMessageAsync("User bind successfully");
                        }
                        else
                        {
                            _ = arg.Content.Trim() == "help"
                                ? await arg.Channel.SendMessageAsync("bind - bind you to this bot\nhelp - this message\n")
                                : await arg.Channel.SendMessageAsync("You are not authorized to use this bot. Plase use \"bind <secret>\" to bind your user with this bot");
                        }
                    }
                }
                else
                {
                    if (arg.Author.Id != botSettings.DiscordUserID)
                    {
                        if (replyOnce)
                        {
                            _ = await arg.Channel.SendMessageAsync("You are not authorized to use this bot.");
                        }

                    }
                    else
                    {
                        switch (arg.Content.Trim())
                        {
                            case "help":
                                if (replyOnce)
                                {
                                    if (File.Exists("bot.txt"))
                                    {
                                        _ = await arg.Channel.SendMessageAsync(
                                            "bind <secret>- bind you to this bot\n" +
                                            "help - this message\n" +
                                            "list - list all planet name\n" +
                                            "resource <planetName> - check current planet resource\n" +
                                            "gain <planetName> - get current planet gain\n" +
                                            "start <planetName> - start bot selected planet\n" +
                                            "stop <planetName> - stop bot selected planet\n" +
                                            "refresh <planetName> - refresh selected planet\n" +
                                            "img <planetName> - send screenshot of current planet doing (Might have delays)");
                                    }
                                    else
                                    {
                                        _ = await arg.Channel.SendMessageAsync(
                                            "bind <secret>- bind you to this bot\n" +
                                            "help - this message\n" +
                                            "list - list all planet name\n" +
                                            "refresh <planetName> - refresh selected planet\n" +
                                            "img <planetName> - send screenshot of current planet doing (Might have delays)");
                                    }
                                }
                                break;
                            case "list":
                                if (replyOnce)
                                {
                                    StringBuilder sb = new StringBuilder();
                                    foreach (PlanetSettings planet in botSettings.PlanetSettings)
                                    {
                                        _ = sb.AppendLine(planet.PlanetName);
                                    }
                                    _ = await arg.Channel.SendMessageAsync("Listed planet list: \n" + sb.ToString());
                                }
                                break;
                            default:
                                if (File.Exists("bot.txt"))
                                {
                                    if (arg.Content.Trim() == "resource " + planet.PlanetName)
                                    {
                                        _ = await arg.Channel.SendMessageAsync("Current planet resources: \n" + script.Resources.ToString());
                                    }
                                    else if (arg.Content.Trim() == "gain " + planet.PlanetName)
                                    {
                                        if (script == null || !script.Running)
                                        {
                                            _ = await arg.Channel.SendMessageAsync("Bot is not started.");
                                        }
                                        if (resources == null)
                                        {
                                            _ = await arg.Channel.SendMessageAsync("Bot not loaded resources details yet.");
                                        }
                                        _ = await arg.Channel.SendMessageAsync("Current planet gained resources: \nMetal:" + (script.Resources.Metal - resources.Metal) +
                                            "\nHE3: " + (script.Resources.HE3 - resources.HE3) + "\nGold: " + (script.Resources.Gold - resources.Gold));
                                    }
                                    else if (arg.Content.Trim().Contains("start " + planet.PlanetName))
                                    {
                                        _ = metroToggle1.Invoke((MethodInvoker)delegate ()
                                        {
                                            metroToggle1.Checked = true;
                                        });
                                        _ = await arg.Channel.SendMessageAsync("Bot Started");
                                    }
                                    else if (arg.Content.Trim().Contains("stop " + planet.PlanetName))
                                    {
                                        _ = metroToggle1.Invoke((MethodInvoker)delegate ()
                                        {
                                            metroToggle1.Checked = false;
                                        });
                                        _ = await arg.Channel.SendMessageAsync("Bot Stopped");
                                    }
                                }
                                if (arg.Content.Trim().Contains("refresh " + planet.PlanetName))
                                {
                                    _ = metroButton2.Invoke((MethodInvoker)delegate
                                    {
                                        metroButton2.PerformClick();
                                    });
                                    _ = await arg.Channel.SendMessageAsync("Refreshing browser...");
                                }
                                else if (arg.Content.Trim().Contains("img " + planet.PlanetName))
                                {
                                    var bmp = await chrome.GetBrowser().GetDevToolsClient().Screenshot();
                                    using (MemoryStream stream = new MemoryStream())
                                    {
                                        bmp.Save(stream, ImageFormat.Png);
                                        _ = await arg.Channel.SendFileAsync(stream, "screenshot.png");
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = await arg.Channel.SendMessageAsync(ex.Message);
            }

        }

        public void SetDiscordBot(DiscordSocketClient client)
        {
            this.client = client;
            if (this.client != null)
            {
                this.client.MessageReceived += Client_MessageReceived;
            }
        }

        private async void BotControl_Load(object sender, EventArgs e)
        {
            if (planet.LoadPlanet)
            {
                RequestContextSettings alphaContext = new RequestContextSettings
                {
                    IgnoreCertificateErrors = true,
                    PersistUserPreferences = true,
                    PersistSessionCookies = true,
                    CachePath = Path.GetFullPath("cache"),
                };
                Model.SGO2.GetFrameResponse iframeUrl = await httpService.GetIFrameUrl(planet.PlanetId);
                chrome = new ChromiumWebBrowser(string.Format(url, iframeUrl.Data.UserId, iframeUrl.Data.SessionKey))
                {
                    RequestContext = new RequestContext(alphaContext),
                    FocusHandler = null
                };
                await Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    _ = chrome.RequestContext.SetPreference("profile.default_content_setting_values.plugins", 1, out string error);
                });
                chrome.MenuHandler = new CustomMenuHandler();
                chrome.Dock = DockStyle.Fill;
                ChromeContainer.Controls.Add(chrome);
                haltLabel.Visible = false;
            }
            foreach (string constellations in Enum.GetNames(typeof(Constellations)))
            {
                _ = metroComboBox5.Items.Add(constellations);
            }
            if (!File.Exists("bot.txt"))
            {
                return;
            }
            metroComboBox1.SelectedIndex = planet.Instance - 1;
            numericUpDown1.Value = planet.InstanceHitCount;
            textBox2.Text = planet.HaltOn.ToString();
            metroCheckBox1.Checked = planet.RestrictFight;
            metroComboBox2.SelectedIndex = planet.RestrictLevel - 1;
            metroComboBox3.SelectedIndex = planet.TrialMaxLv - 1;
            metroComboBox5.SelectedIndex = planet.ConstellationStage - 1;
            metroComboBox4.SelectedIndex = planet.ConstellationLevel - 1;
            metroCheckBox3.Checked = planet.ConstellationFight;
            numericUpDown2.Value = planet.ConstellationCount;
            BotInstance.SelectedIndex = 0;
            ConfigTabs.SelectedIndex = 0;
            metroTabControl1.SelectedIndex = 0;
            metroToggle1.Checked = planet.RunBot;
            textBox1.Text = botSettings.Delays.ToString();
            metroCheckBox4.Checked = planet.SpinWheel;
            textBox3.Text = planet.MinVouchers.ToString();
            metroCheckBox5.Checked = planet.SpinWithVouchers;
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
            int tabCount = planet.Fleets.Count / 9;
            MetroTabPage t = new MetroTabPage
            {
                Name = "FleetTab" + tabCount,
                Text = "Fleet Page " + tabCount,
                Theme = MetroThemeStyle.Dark
            };
            FlowLayoutPanel p = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            for (int y = planet.Fleets.Count - (planet.Fleets.Count % 9); y < planet.Fleets.Count; y++)
            {
                GroupBox group = new GroupBox
                {
                    Text = planet.Fleets[y].Name,
                    Height = 45,
                    Width = 170,
                    ForeColor = Color.White
                };
                NumericUpDown input = new NumericUpDown
                {
                    Name = planet.Fleets[y].Name + "_" + type,
                    Top = 15,
                    Left = 15,
                    Width = 120,
                    Minimum = -1
                };
                switch (type)
                {
                    case 0:
                        input.Value = planet.Fleets[y].Order;
                        break;
                    case 1:
                        input.Value = planet.Fleets[y].RestrictOrder;
                        break;
                    case 2:
                        input.Value = planet.Fleets[y].TrialOrder;
                        break;
                    case 3:
                        input.Value = planet.Fleets[y].ConstellationOrder;
                        break;
                }
                MetroButton removeFleet = new MetroButton
                {
                    Name = "btnRem_" + planet.Fleets[y].Name,
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

        private void RemoveFleet_Click(object sender, EventArgs e)
        {
            string name = (sender as MetroButton).Name.Replace("btnRem_", "");
            Fleet data = planet.Fleets.First(x => x.Name == name);
            _ = planet.Fleets.Remove(data);
            RenderFleets();
        }

        private void Input_ValueChanged(object sender, EventArgs e)
        {
            string name = string.Join("_", (sender as NumericUpDown).Name.Split('_').Take((sender as NumericUpDown).Name.Split('_').Length - 1));
            int type = Convert.ToInt32((sender as NumericUpDown).Name.Split('_').Last());
            switch (type)
            {
                case 0:
                    planet.Fleets.First(x => x.Name == name).Order = (int)Math.Round((sender as NumericUpDown).Value);
                    break;
                case 1:
                    planet.Fleets.First(x => x.Name == name).RestrictOrder = (int)Math.Round((sender as NumericUpDown).Value);
                    break;
                case 2:
                    planet.Fleets.First(x => x.Name == name).TrialOrder = (int)Math.Round((sender as NumericUpDown).Value);
                    break;
                case 3:
                    planet.Fleets.First(x => x.Name == name).ConstellationOrder = (int)Math.Round((sender as NumericUpDown).Value);
                    break;
            }

        }

        public void GenerateFleetTabs(int type, Control tabControl)
        {
            int tabs = planet.Fleets.Count / 9;
            int x = 0;
            for (x = 0; x < tabs; x++)
            {
                MetroTabPage tab = new MetroTabPage
                {
                    Name = "FleetTab" + x,
                    Text = "Fleet Page " + x,
                    Theme = MetroThemeStyle.Dark
                };
                FlowLayoutPanel panel = new FlowLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent
                };
                for (int y = 9 * x; y < ((x + 1) * 9); y++)
                {
                    GroupBox group = new GroupBox
                    {
                        Text = planet.Fleets[y].Name,
                        Height = 45,
                        Width = 170,
                        ForeColor = Color.White
                    };
                    NumericUpDown input = new NumericUpDown
                    {
                        Name = planet.Fleets[y].Name + "_" + type,
                        Top = 15,
                        Left = 15,
                        Width = 120,
                        Minimum = -1
                    };
                    switch (type)
                    {
                        case 0:
                            input.Value = planet.Fleets[y].Order;
                            break;
                        case 1:
                            input.Value = planet.Fleets[y].RestrictOrder;
                            break;
                        case 2:
                            input.Value = planet.Fleets[y].TrialOrder;
                            break;
                        case 3:
                            input.Value = planet.Fleets[y].ConstellationOrder;
                            break;
                    }
                    MetroButton removeFleet = new MetroButton
                    {
                        Name = "btnRem_" + planet.Fleets[y].Name,
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

        private uint ColorToUInt(Color color)
        {
            return (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | (color.B << 0));
        }

        private async void metroButton2_Click(object sender, EventArgs e)
        {
            Model.SGO2.GetFrameResponse iframeUrl = await httpService.GetIFrameUrl(planet.PlanetId);
            chrome.Load(string.Format(url, iframeUrl.Data.UserId, iframeUrl.Data.SessionKey));
        }

        private void metroToggle1_CheckedChanged(object sender, EventArgs e)
        {
            if(chrome == null)
            {
                metroToggle1.Checked = false;
                return;
            }
            if (metroToggle1.Checked)
            {
                if (script == null || !script.Running)
                {
                    Logger.ClearLog();
                    Logger.LogInfo("Bot Started");
                    Logger.LogInfo("Locked browser for botting...");
                    script = new InstanceScript(botSettings, planet, client);
                    _ = script.Run(chrome, planet.PlanetId, httpService);
                    chrome.Enabled = false;
                    planet.RunBot = true;
                }
            }
            else
            {
                Logger.LogInfo("Bot Stopped");
                Logger.LogInfo("Unlocked browser for botting...");
                script.Stop();
                script = null;
                resources = null;
                chrome.Enabled = true;
                planet.RunBot = false;
            }
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            planet.Fleets.Add(new Fleet
            {
                Name = "Fleet " + (planet.Fleets.Count + 1),
                Order = planet.Fleets.Count,
                ConstellationOrder = planet.Fleets.Count,
                RestrictOrder = planet.Fleets.Count,
                TrialOrder = planet.Fleets.Count,
            });
            RenderFleets();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            planet.HaltOn = int.Parse(textBox2.Text.Replace(".0", "").Replace(".", ""));
        }

        private void numericOnly(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void metroCheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            planet.RestrictFight = metroCheckBox1.Checked;
        }

        private void metroComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            planet.RestrictLevel = metroComboBox2.SelectedIndex + 1;
        }

        private void metroComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            planet.Instance = metroComboBox1.SelectedIndex + 1;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            planet.InstanceHitCount = numericUpDown1.Value;
        }

        private void metroCheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            planet.TrialFight = metroCheckBox2.Checked;
        }

        private void metroComboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            planet.TrialMaxLv = metroComboBox3.SelectedIndex + 1;
        }

        private void metroComboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            planet.ConstellationStage = metroComboBox5.SelectedIndex;
        }

        private void metroComboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            planet.ConstellationLevel = metroComboBox4.SelectedIndex + 1;
        }

        private void metroCheckBox3_CheckedChanged(object sender, EventArgs e)
        {
            planet.ConstellationFight = metroCheckBox3.Checked;
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            planet.ConstellationCount = (long)numericUpDown2.Value;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (resources == null && script != null && script.Running)
            {
                if (script.Resources.Metal > 0 || script.Resources.HE3 > 0 || script.Resources.Gold > 0 || script.Resources.MP > 0 || script.Resources.Vouchers > 0)
                {
                    resources = script.Resources;
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            botSettings.Delays = int.Parse(textBox1.Text);
        }

        private void metroCheckBox4_CheckedChanged(object sender, EventArgs e)
        {
            planet.SpinWheel = metroCheckBox4.Checked;
        }

        private void metroCheckBox5_CheckedChanged(object sender, EventArgs e)
        {
            planet.SpinWithVouchers = metroCheckBox5.Checked;
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            planet.MinVouchers = int.Parse(textBox3.Text);
        }

        private void metroButton3_Click(object sender, EventArgs e)
        {
            chrome.ShowDevTools();
        }

        private async void metroButton4_Click(object sender, EventArgs e)
        {
            if(chrome != null)
            {
                if (metroToggle1.Checked)
                {
                    metroToggle1.Checked = false;
                    Logger.LogInfo("Bot Stopped");
                    Logger.LogInfo("Unlocked browser for botting...");
                }
                if(script != null && script.Running)
                {
                    script.Stop();
                    script = null;
                    resources = null;
                }
                await Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    chrome.DestroyWindow();
                });
                ChromeContainer.Controls.Remove(chrome);
                chrome = null;
                planet.LoadPlanet = false;
                haltLabel.Visible = true;
            }
            else
            {
                RequestContextSettings alphaContext = new RequestContextSettings
                {
                    IgnoreCertificateErrors = true,
                    PersistUserPreferences = true,
                    PersistSessionCookies = true,
                    CachePath = Path.GetFullPath("cache"),
                };
                Model.SGO2.GetFrameResponse iframeUrl = await httpService.GetIFrameUrl(planet.PlanetId);
                chrome = new ChromiumWebBrowser(string.Format(url, iframeUrl.Data.UserId, iframeUrl.Data.SessionKey))
                {
                    RequestContext = new RequestContext(alphaContext),
                    FocusHandler = null
                };
                await Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    _ = chrome.RequestContext.SetPreference("profile.default_content_setting_values.plugins", 1, out string error);
                });
                chrome.MenuHandler = new CustomMenuHandler();
                chrome.Dock = DockStyle.Fill;
                ChromeContainer.Controls.Add(chrome);
                planet.LoadPlanet = true;
                haltLabel.Visible = false;
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
