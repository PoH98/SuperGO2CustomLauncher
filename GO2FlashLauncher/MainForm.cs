using CefSharp;
using CefSharp.WinForms;
using Discord;
using Discord.WebSocket;
using GO2FlashLauncher.Model;
using GO2FlashLauncher.Models;
using GO2FlashLauncher.Service;
using MetroFramework;
using MetroFramework.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GO2FlashLauncher
{
    public partial class MainForm : Form
    {
        readonly RPC rpc = new RPC();
        GO2HttpService GO2HttpService = new GO2HttpService();
        readonly string profileName = "Bot1";
        BotSettings settings = ConfigService.Instance.Config;
        DiscordSocketClient _client;
        List<BotControl> bots = new List<BotControl>();
        HttpClient hc = new HttpClient();
        bool loginError = false;
        public MainForm()
        {
            InitializeComponent();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            if (Width < 1260)
            {
                Width = 1260;
            }
            if (Height < 970)
            {
                Height = 970;
            }
            Logger.Init(richTextBox1, profileName);
            try
            {
                settings = ConfigService.Instance.Config;
                if (settings.CredentialHash == null)
                {
                    Login();
                    var profile = Encryption.Decrypt(settings.CredentialHash);
                    var credential = await GO2HttpService.Login(profile.Email, profile.Password);
                    settings.AuthKey = credential.Data.Token;
                }
                if (settings.AuthKey == null)
                {
                    var profile = Encryption.Decrypt(settings.CredentialHash);
                    var credential = await GO2HttpService.Login(profile.Email, profile.Password);
                    settings.AuthKey = credential.Data.Token;
                }
                GO2HttpService.SetToken(settings.AuthKey);
                var planet = await GO2HttpService.GetPlanets();
                if (planet.Code == 401)
                {
                    var profile = Encryption.Decrypt(settings.CredentialHash);
                    var credential = await GO2HttpService.Login(profile.Email, profile.Password);
                    settings.AuthKey = credential.Data.Token;
                    planet = await GO2HttpService.GetPlanets();
                }
                textBox1.Text = settings.DiscordBotToken;
                for (int x = 0; x < planet.Data.Count; x++)
                {
                    var tab = new MetroTabPage()
                    {
                        Text = planet.Data[x].Username,
                        Theme = MetroThemeStyle.Dark
                    };
                    if (settings.PlanetSettings.Count <= x)
                    {
                        settings.PlanetSettings.Add(new PlanetSettings());
                    }
                    metroTabControl1.Controls.Add(tab);
                    settings.PlanetSettings[x].PlanetId = planet.Data[x].UserId;
                    settings.PlanetSettings[x].PlanetName = planet.Data[x].Username;
                    var control = new BotControl(settings, settings.PlanetSettings[x], GO2HttpService, _client)
                    {
                        Size = new Size(tab.Size.Width - 10, tab.Size.Height - 10),
                        Location = new Point(5, 5),
                        Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                    };
                    bots.Add(control);
                    tab.Controls.Add(control);
                }
                var krtab = new MetroTabPage()
                {
                    Text = "Krtools",
                    Theme = MetroThemeStyle.Dark
                };
                krtab.Controls.Add(new Krtools()
                {
                    Dock = DockStyle.Fill
                });
                metroTabControl1.Controls.Add(krtab);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
                loginError = true;
                ConfigService.Instance.Save();
                MainForm_Load(sender, e);
            }
            rpc.SetPresence();
            timer1.Start();
            if (!string.IsNullOrEmpty(settings.DiscordBotToken))
            {
                button2.PerformClick();
            }
        }

        private void Login()
        {
            Login login = new Login(profileName);
            login.IsError = loginError;
            if (login.ShowDialog() == DialogResult.OK)
            {
                ConfigService.Instance.Save();
                ConfigService.Instance.Load();
                settings = ConfigService.Instance.Config;
            }
            else
            {
                MessageBox.Show("We can't log you in without your credentials! Exiting...");
                Application.Exit();
                Environment.Exit(0);
            }
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
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ConfigService.Instance.Save();
            Logger.CloseLog();
            try
            {
                if (_client != null)
                {
                    _client.LogoutAsync().Wait();
                    _client.StopAsync().Wait();
                    _client.Dispose();
                }
            }
            catch
            {

            }
            Cef.Shutdown();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            settings.DiscordBotToken = textBox1.Text;
        }

        private async Task _client_Disconnected(Exception arg)
        {
            await _client.LoginAsync(TokenType.Bot, settings.DiscordBotToken);
            await _client.StartAsync();
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                var response = await hc.GetAsync("https://api.guerradenaves.lat/metrics/online");
                var online = JsonConvert.DeserializeObject<OnlinePlayers>(await response.Content.ReadAsStringAsync());
                Text = "Not So Super GO2 | Online Players: " + online.Data.Online;
            }
            catch
            {
                Text = "Not So Super GO2 | Online Players: 0";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Your secret: " + settings.DiscordSecret + "\nPlease send this secret in discord so the bot knows you are its owner!");
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            if (_client != null)
            {
                await _client.StopAsync();
                await _client.LogoutAsync();
                _client.Dispose();
            }
            if (string.IsNullOrEmpty(settings.DiscordSecret))
            {
                settings.DiscordSecret = Guid.NewGuid().ToString().Split('-')[0];
            }
            //suppose discord token will be over 55 char to 70
            try
            {
                if (!string.IsNullOrEmpty(settings.DiscordBotToken))
                {
                    _client = new DiscordSocketClient();
                    await _client.LoginAsync(TokenType.Bot, settings.DiscordBotToken);
                    await _client.StartAsync();
                    _client.Disconnected += _client_Disconnected;
                    await _client.SetGameAsync("Not So Super GO2");
                }
                else
                {
                    _client = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\nPlease try restart the bot which might resolve the error", "Discord Bot binding error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (var bot in bots)
            {
                bot.SetDiscordBot(_client);
            }
            if (settings.DiscordUserID == 0)
            {
                MessageBox.Show("Your secret: " + settings.DiscordSecret + "\nPlease send this secret in discord so the bot knows you are its owner!");
            }
        }
    }
}
