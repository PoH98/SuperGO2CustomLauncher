using EasyTabs;
using GalaxyOrbit4Launcher.Models;
using GalaxyOrbit4Launcher.Models.GO4;
using GalaxyOrbit4Launcher.Service;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Security.Authentication;
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
            Resize += AppContainer_Resize;
            InitializeComponent();
            AeroPeekEnabled = true;
            TabRenderer = new ChromeTabRenderer(this);
            ExitOnLastTabClose = false;
        }

        private void AppContainer_Resize(object sender, EventArgs e)
        {
            if (Width < 600)
            {
                Width = 600;
            }
            if (Height < 700)
            {
                Height = 700;
            }
            ResizeTabContents();
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
                        LoginResponse credential = await GO2HttpService.Login(profile.Email, profile.Password);
                        settings.AuthKey = credential.Data.Token;
                    }
                    if (settings.AuthKey == null)
                    {
                        Profile profile = Encryption.Decrypt(settings.CredentialHash);
                        LoginResponse credential = await GO2HttpService.Login(profile.Email, profile.Password);
                        settings.AuthKey = credential.Data.Token;
                    }
                    GO2HttpService.SetToken(settings.AuthKey);
                    planet = await GO2HttpService.GetPlanets();
                    if (planet.Code == 401)
                    {
                        Profile profile = Encryption.Decrypt(settings.CredentialHash);
                        LoginResponse credential = await GO2HttpService.Login(profile.Email, profile.Password);
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
                            LoginResponse credential = await GO2HttpService.Login(profile.Email, profile.Password);
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
                        Invoke((MethodInvoker)delegate
                        {
                            SelectedTabIndex = 0;
                        });
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
