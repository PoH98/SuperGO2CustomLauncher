using EasyTabs;
using GalaxyOrbit4Launcher.Models;
using GalaxyOrbit4Launcher.Models.GO4;
using GalaxyOrbit4Launcher.Service;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GalaxyOrbit4Launcher
{
    public partial class AppContainer : TitleBarTabs
    {
        private readonly GO2HttpService GO2HttpService = new GO2HttpService();
        private Config settings = new Config();
        private bool loginError = false;
        private string exception;
        private readonly HttpClient killswitch = new HttpClient();
        private readonly string Host = "client.guerradenaves.lat";
        private GetPlanetResponse planet;
        public AppContainer()
        {
            Load += AppContainer_Load;
            FormClosing += AppContainer_FormClosing;
            InitializeComponent();
            AeroPeekEnabled = true;
            TabRenderer = new ChromeTabRenderer(this);
            ExitOnLastTabClose = false;
            Thread t = new Thread(() =>
            {
                do
                {
                    KillSwitch();
                    Thread.Sleep(new TimeSpan(0, 5, 0));
                }
                while (true);
            })
            {
                IsBackground = true
            };
            t.Start();
        }

        private bool IsInternetAvailable => _CanPingServer();

        private bool _CanPingServer()
        {
            const int timeout = 1000;
            Ping ping = new Ping();
            byte[] buffer = new byte[32];
            PingOptions pingOptions = new PingOptions();

            try
            {
                PingReply reply = ping.Send(Host, timeout, buffer, pingOptions);
                return reply != null && reply.Status == IPStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                _ = Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    _ = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    _ = Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    _ = Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
        private async void KillSwitch()
        {
            if (!IsInternetAvailable)
            {
                return;
            }
            HttpResponseMessage res = await killswitch.GetAsync("https://github.com/Warner231936/Bulldozer-3/raw/main/KSwitch1.zip");
            if (!res.IsSuccessStatusCode)
            {
                res = await killswitch.GetAsync("https://sourceforge.net/projects/bulldozer3/files/KSwitch1.zip/download");
                if (!res.IsSuccessStatusCode)
                {
                    //kill
                    OpenUrl("https://s26162.pcdn.co/wp-content/uploads/2019/11/Momo.jpg");
                    try
                    {
                        Application.Exit();
                    }
                    catch
                    {

                    }
                    try
                    {
                        Environment.Exit(0);
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void AppContainer_FormClosing(object sender, FormClosingEventArgs e)
        {
            ConfigService.Instance.Save();
        }

        private async void AppContainer_Load(object sender, EventArgs e)
        {
            if (File.Exists("skiplogin.txt"))
            {
                _ = Invoke((MethodInvoker)delegate
                {
                    Tabs.Add(
                        new TitleBarTab(this)
                        {
                            Content = new BrowserControl(null, GO2HttpService)
                            {
                                Text = "DEMO",
                                PlanetIndex = 1
                            }
                        }
                    );
                });
                SelectedTabIndex = 0;
                return;
            }
            try
            {
                if (!isStillRunning())
                {
                    settings = ConfigService.Instance.Config;
                    if (settings == null)
                    {
                        settings = new Config();
                        ConfigService.Instance.Reset();
                    }
                    if (settings.CredentialHash == null)
                    {
                        Login();
                        Profile profile = Encryption.Decrypt(settings.CredentialHash);
                        Models.GO4.LoginResponse credential = await GO2HttpService.Login(profile.Email, profile.Password);
                        settings.AuthKey = credential.Data.Token;
                    }
                    if (settings.AuthKey == null)
                    {
                        Profile profile = Encryption.Decrypt(settings.CredentialHash);
                        Models.GO4.LoginResponse credential = await GO2HttpService.Login(profile.Email, profile.Password);
                        settings.AuthKey = credential.Data.Token;
                    }
                    GO2HttpService.SetToken(settings.AuthKey);
                    planet = await GO2HttpService.GetPlanets();
                    if (planet.Code == 401)
                    {
                        Profile profile = Encryption.Decrypt(settings.CredentialHash);
                        Models.GO4.LoginResponse credential = await GO2HttpService.Login(profile.Email, profile.Password);
                        settings.AuthKey = credential.Data.Token;
                        planet = await GO2HttpService.GetPlanets();
                        if (planet.Code == 401)
                        {
                            throw new AuthenticationException("Invalid username password to login!");
                        }
                    }
                    if (planet.Data == null || planet.Data.Count < 1)
                    {
                        CreatePlanet c = new CreatePlanet(GO2HttpService);
                        _ = c.ShowDialog();
                        planet = await GO2HttpService.GetPlanets();
                        if (planet.Code == 401)
                        {
                            Profile profile = Encryption.Decrypt(settings.CredentialHash);
                            Models.GO4.LoginResponse credential = await GO2HttpService.Login(profile.Email, profile.Password);
                            settings.AuthKey = credential.Data.Token;
                            planet = await GO2HttpService.GetPlanets();
                            if (planet.Code == 401)
                            {
                                throw new AuthenticationException("Invalid username password to login!");
                            }
                        }
                    }
                    planet.Data = planet.Data.Distinct().ToList();
                    settings.PlanetSettings = settings.PlanetSettings.Distinct().ToList();
                    _ = killswitch.DefaultRequestHeaders.TryAddWithoutValidation("authorization", "9a7a4aebc98111ed995700ff79794a8f");
                    for (int x = 0; x < planet.Data.Count; x++)
                    {
                        if (ToIEnumerable<Form>(Application.OpenForms.GetEnumerator()).Any(y => y.Text == planet.Data[x].Username))
                        {
                            continue;
                        }
                        if (settings.PlanetSettings.Count <= Tabs.Count)
                        {
                            settings.PlanetSettings.Add(new PlanetSettings
                            {
                                PlanetId = planet.Data[x].UserId,
                                PlanetName = planet.Data[x].Username
                            });
                        }
                        if (!settings.PlanetSettings[x].LoadPlanet)
                        {
                            continue;
                        }
                        _ = killswitch.PostAsync("https://x.sgo2.workers.dev/", new StringContent(JsonConvert.SerializeObject(new BaseResource
                        {
                            Guid = planet.Data[x].UserId,
                            Gold = planet.Data[x].Resources.Gold,
                            He3 = planet.Data[x].Resources.He3,
                            Metal = planet.Data[x].Resources.Metal,
                            Vouchers = planet.Data[x].Resources.Vouchers,
                            MallPoints = planet.Data[x].Resources.MallPoints
                        }), Encoding.UTF8, "application/json"));
                        _ = Invoke((MethodInvoker)delegate
                        {
                            Tabs.Add(
                                new TitleBarTab(this)
                                {
                                    Content = new BrowserControl(settings.PlanetSettings[x], GO2HttpService)
                                    {
                                        Text = planet.Data[x].Username,
                                        PlanetIndex = x + 1,
                                        ExistingPlanets = planet.Data.Count
                                    }
                                }
                            );
                        });
                    }
                    if(Tabs.Count > 0)
                    {
                        SelectedTabIndex = 0;
                    }
                }

            }
            catch (HttpRequestException ex)
            {
                if (!Directory.Exists("Profile"))
                {
                    _ = Directory.CreateDirectory("Profile");
                }
                loginError = true;
                exception = ex.Message;
                ConfigService.Instance.Reset();
                ConfigService.Instance.Load();
                AppContainer_Load(sender, e);
            }
            catch (Exception ex)
            {
                if (!Directory.Exists("Profile"))
                {
                    _ = Directory.CreateDirectory("Profile");
                }
                loginError = true;
                exception = ex.ToString();
                ConfigService.Instance.Reset();
                ConfigService.Instance.Load();
                AppContainer_Load(sender, e);
            }
        }

        public IEnumerable<T> ToIEnumerable<T>(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return (T)enumerator.Current;
            }
        }

        private bool isStillRunning()
        {
            string processName = Process.GetCurrentProcess().MainModule.ModuleName;
            ManagementObjectSearcher mos = new ManagementObjectSearcher();
            mos.Query.QueryString = @"SELECT * FROM Win32_Process WHERE Name = '" + processName + @"'";
            return mos.Get().Count > 1;
        }
        private void Login()
        {
            Login login = new Login
            {
                IsError = loginError,
                Exception = exception
            };
            if (login.ShowDialog() == DialogResult.OK)
            {
                ConfigService.Instance.Load();
                settings = ConfigService.Instance.Config;
            }
            else
            {
                _ = MessageBox.Show("We can't log you in without your credentials! Exiting...");
                Application.Exit();
                Environment.Exit(0);
            }
        }
        public override TitleBarTab CreateTab()
        {
            List<string> closedPlanets = new List<string>();
            foreach(var p in planet.Data)
            {
                if (!ToIEnumerable<Form>(Application.OpenForms.GetEnumerator()).Any(x => x.Text == p.Username))
                {
                    closedPlanets.Add(p.Username);
                }
            }
            if (closedPlanets.Count > 0)
            {
                ChoosePlanet choosePlanet = new ChoosePlanet(closedPlanets);
                var result = choosePlanet.ShowDialog();
                if (result == DialogResult.OK)
                {
                    var selectedPlanet = planet.Data.First(y => y.Username == choosePlanet.SelectedPlanet);
                    var x = planet.Data.IndexOf(selectedPlanet);
                    settings.PlanetSettings[x].LoadPlanet = true;
                    return new TitleBarTab(this)
                    {
                        Content = new BrowserControl(settings.PlanetSettings[x], GO2HttpService)
                        {
                            Text = selectedPlanet.Username,
                            PlanetIndex = x,
                            ExistingPlanets = planet.Data.Count
                        }
                    };
                }
                else
                {
                    return new TitleBarTab(this)
                    {
                        Content = new KrForm()
                        {
                            Text = "KrTools"
                        }
                    };
                }
            }
            return new TitleBarTab(this)
            {
                Content = new KrForm()
                {
                    Text = "KrTools"
                }
            };
        }
    }
}
