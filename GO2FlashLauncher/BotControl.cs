using CefSharp;
using CefSharp.WinForms;
using GO2FlashLauncher.Model;
using GO2FlashLauncher.Script;
using GO2FlashLauncher.Service;
using MetroFramework.Controls;
using MetroFramework;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Linq;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Text;
using GO2FlashLauncher.Models;
using Newtonsoft.Json;
using System.Drawing.Imaging;
using GO2FlashLauncher.Script.GameLogic;

namespace GO2FlashLauncher
{
    internal partial class BotControl : UserControl
    {
        private ChromiumWebBrowser chrome;
        private readonly string url = "https://beta-client.supergo2.com/?userId={0}&sessionKey={1}";
        private readonly GO2HttpService httpService;
        private PlanetSettings planet;
        private BotSettings botSettings;
        private AbstractScript script;
        private DiscordSocketClient client;
        private BaseResources resources;

        public BotControl(BotSettings botSettings, PlanetSettings planet, GO2HttpService httpService, DiscordSocketClient client)
        {
            this.planet = planet;
            this.botSettings = botSettings;
            this.httpService = httpService;
            this.client = client;
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
            if (!Cef.Initialize(settings, true))
            {
                throw new Exception("Unable to Initialize Cef");
            }
            InitializeComponent();
            timer2.Start();
            if(client != null)
            {
                client.MessageReceived += Client_MessageReceived;
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

                var replyOnce = botSettings.PlanetSettings.IndexOf(planet) == 0;
                if (botSettings.DiscordUserID == ulong.Parse("0"))
                {
                    if (replyOnce)
                    {
                        if (arg.Content.Trim() == "bind " + botSettings.DiscordSecret)
                        {
                            botSettings.DiscordUserID = arg.Author.Id;
                            await arg.Channel.SendMessageAsync("User bind successfully");
                        }
                        else if (arg.Content.Trim() == "help")
                        {
                            await arg.Channel.SendMessageAsync("bind - bind you to this bot\nhelp - this message\n");
                        }
                        else
                        {
                            await arg.Channel.SendMessageAsync("You are not authorized to use this bot. Plase use \"/bind <secret>\" to bind your user with this bot");
                        }
                    }
                }
                else
                {
                    if (arg.Author.Id != botSettings.DiscordUserID)
                    {
                        if (replyOnce)
                        {
                            await arg.Channel.SendMessageAsync("You are not authorized to use this bot.");
                        }

                    }
                    else
                    {
                        switch (arg.Content.Trim())
                        {
                            case "help":
                                if (replyOnce)
                                {
                                    await arg.Channel.SendMessageAsync(
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
                                break;
                            case "list":
                                if (replyOnce)
                                {
                                    StringBuilder sb = new StringBuilder();
                                    foreach (var planet in botSettings.PlanetSettings)
                                    {
                                        sb.AppendLine(planet.PlanetName);
                                    }
                                    await arg.Channel.SendMessageAsync("Listed planet list: \n" + sb.ToString());
                                }
                                break;
                            default:
                                if (arg.Content.Trim() == "resource " + planet.PlanetName)
                                {
                                    await arg.Channel.SendMessageAsync("Current planet resources: \n" + script.Resources.ToString());
                                }
                                else if (arg.Content.Trim() == "gain " + planet.PlanetName)
                                {
                                    if (script == null || !script.Running)
                                    {
                                        await arg.Channel.SendMessageAsync("Bot is not started.");
                                    }
                                    if (resources == null)
                                    {
                                        await arg.Channel.SendMessageAsync("Bot not loaded resources details yet.");
                                    }
                                    await arg.Channel.SendMessageAsync("Current planet gained resources: \nMetal:" + (script.Resources.Metal - resources.Metal) +
                                        "\nHE3: " + (script.Resources.HE3 - resources.HE3) + "\nGold: " + (script.Resources.Gold - resources.Gold));
                                }
                                else if (arg.Content.Trim().Contains("start " + planet.PlanetName))
                                {
                                    metroToggle1.Invoke((MethodInvoker)delegate()
                                    {
                                        metroToggle1.Checked = true;
                                    });
                                    await arg.Channel.SendMessageAsync("Bot Started");
                                }
                                else if (arg.Content.Trim().Contains("stop " + planet.PlanetName))
                                {
                                    metroToggle1.Invoke((MethodInvoker)delegate ()
                                    {
                                        metroToggle1.Checked = false;
                                    });
                                    await arg.Channel.SendMessageAsync("Bot Stopped");
                                }
                                else if(arg.Content.Trim().Contains("refresh " + planet.PlanetName))
                                {
                                    metroButton2.Invoke((MethodInvoker)delegate
                                    {
                                        metroButton2.PerformClick();
                                    });
                                    await arg.Channel.SendMessageAsync("Refreshing browser...");
                                }
                                else if(arg.Content.Trim().Contains("img " + planet.PlanetName))
                                {
                                    if(script == null)
                                    {
                                        await arg.Channel.SendMessageAsync("Bot not started");
                                        return;
                                    }
                                    if(script.lastbmp == null)
                                    {
                                        await arg.Channel.SendMessageAsync("Bot unable to fetch screenshot now, please try again later");
                                        return;
                                    }
                                    using(MemoryStream stream= new MemoryStream())
                                    {
                                        script.lastbmp.Save(stream, ImageFormat.Png);
                                        await arg.Channel.SendFileAsync(stream, "screenshot.png");
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                await arg.Channel.SendMessageAsync(ex.Message);
            }

        }

        public void SetDiscordBot(DiscordSocketClient client)
        {
            this.client = client;
            if(client!= null)
            {
                client.MessageReceived += Client_MessageReceived;
            }        
        }

        private async void BotControl_Load(object sender, EventArgs e)
        {
            var alphaContext = new RequestContextSettings
            {
                IgnoreCertificateErrors = true,
                PersistUserPreferences = true,
                PersistSessionCookies = true,
                CachePath = Path.GetFullPath("cache"),
            };
            var iframeUrl = await httpService.GetIFrameUrl(planet.PlanetId);
            chrome = new ChromiumWebBrowser(string.Format(url, iframeUrl.Data.UserId, iframeUrl.Data.SessionKey));
            chrome.RequestContext = new RequestContext(alphaContext);
            chrome.FocusHandler = null;
            await Cef.UIThreadTaskFactory.StartNew(delegate
            {
                chrome.RequestContext.SetPreference("profile.default_content_setting_values.plugins", 1, out string error);
            });
            foreach (var constellations in Enum.GetNames(typeof(Constellations)))
            {
                metroComboBox5.Items.Add(constellations);
            }
            chrome.MenuHandler = new CustomMenuHandler();
            chrome.Dock = DockStyle.Fill;
            ChromeContainer.Controls.Add(chrome);
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
            var tabCount = planet.Fleets.Count / 9;
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
            for (int y = (planet.Fleets.Count - planet.Fleets.Count % 9); y < planet.Fleets.Count; y++)
            {
                var group = new GroupBox
                {
                    Text = planet.Fleets[y].Name,
                    Height = 45,
                    Width = 170,
                    ForeColor = Color.White
                };
                var input = new NumericUpDown
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
                var removeFleet = new MetroButton
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
            var name = (sender as MetroButton).Name.Replace("btnRem_", "");
            var data = planet.Fleets.First(x => x.Name == name);
            planet.Fleets.Remove(data);
            RenderFleets();
        }

        private void Input_ValueChanged(object sender, EventArgs e)
        {
            var name = string.Join("_", (sender as NumericUpDown).Name.Split('_').Take((sender as NumericUpDown).Name.Split('_').Length - 1));
            var type = Convert.ToInt32((sender as NumericUpDown).Name.Split('_').Last());
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
                        Text = planet.Fleets[y].Name,
                        Height = 45,
                        Width = 170,
                        ForeColor = Color.White
                    };
                    var input = new NumericUpDown
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
                    var removeFleet = new MetroButton
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
            var iframeUrl = await httpService.GetIFrameUrl(planet.PlanetId);
            chrome.Load(string.Format(url, iframeUrl.Data.UserId, iframeUrl.Data.SessionKey));
        }

        private void metroToggle1_CheckedChanged(object sender, EventArgs e)
        {
            if(metroToggle1.Checked)
            {
                if(script == null || !script.Running)
                {
                    script = new InstanceScript(botSettings, planet, client);
                    _ = script.Run(chrome, planet.PlanetId, httpService);
                }
            }
            else
            {
                script.Stop();
                script = null;
                resources = null;
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
            planet.HaltOn = decimal.Parse(textBox2.Text);
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
            if(resources == null && script != null && script.Running)
            {
                resources = script.Resources;
            }
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
