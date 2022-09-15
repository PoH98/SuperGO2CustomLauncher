using CefSharp;
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
        Thread redirector;
        string scriptKey = "";
        HttpClient hc = new HttpClient();
        CancellationTokenSource cancellation;
        MainScript script;
        string profileName = "Bot1";
        BotSettings settings = new BotSettings();
        readonly RPC rpc = new RPC();
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
            scriptKey = Encryption.RandomString(10);
            Logger.Init(richTextBox1, profileName);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
#if !DEBUG
            metroButton3.Hide();      
#endif
            if (File.Exists("debug.log"))
            {
                File.Delete("debug.log");
            }
            WindowState = FormWindowState.Maximized;
            metroTabControl1.SelectedIndex = 0;
            var settings = new CefSettings();
            settings.CachePath = Path.GetFullPath("cache");
            settings.CefCommandLineArgs.Add("enable-system-flash", "1");
            settings.CefCommandLineArgs.Add("ppapi-flash-path", Path.Combine(Directory.GetCurrentDirectory(), "libs\\pepflashplayer.dll"));
            settings.CefCommandLineArgs.Add("ppapi-flash-version", "28.0.0.137");
            settings.CefCommandLineArgs.Add("disable-gpu", "1");
            settings.CefCommandLineArgs.Add("disable-gpu-compositing", "1");
            settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");
            settings.CefCommandLineArgs.Add("disable-gpu-vsync", "1");
            settings.CefCommandLineArgs["plugin-policy"] = "allow";
            settings.CefCommandLineArgs.Add("allow-outdated-plugins");
            settings.CefCommandLineArgs.Add("use-angle", "gl");
            var alphaContext = new RequestContextSettings
            {
                IgnoreCertificateErrors = true,
                PersistUserPreferences = true,
                PersistSessionCookies = true,
                CachePath = Path.GetFullPath("cache")
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
            alpha.IsBrowserInitializedChanged += Alpha_IsBrowserInitializedChanged;
            alpha.LoadingStateChanged += ChromiumWebBrowser_LoadingStateChanged;
            beta.LoadingStateChanged += ChromiumWebBrowser_LoadingStateChanged;
            alpha.ConsoleMessage += ChromiumWebBrowser_ConsoleMessage;
            timer1_Tick(null, null);
            timer1.Start();
            discordRPC_Tick(null, null);
            instanceSelection.SelectedIndex = this.settings.Instance - 1;
            metroToggle1.Checked = this.settings.RunBot;
            numericUpDown1.Value = this.settings.HaltOn;
            RenderFleets();
#if !DEBUG
            metroTabControl1.Controls.Remove(metroTabPage3);
#endif
        }

        private void Alpha_IsBrowserInitializedChanged(object sender, EventArgs e)
        {
            if (!File.Exists(Path.GetFullPath("cache\\config.settings")))
            {
                alpha.GetDevToolsClient().Emulation.SetUserAgentOverrideAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) supergo2-beta/1.0.0-beta Chrome/85.0.4183.121 Electron/10.1.3 Safari/537.36");
                alpha.Load("https://beta.supergo2.com/");
            }
            else
            {
                alpha.GetDevToolsClient().Emulation.SetUserAgentOverrideAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) supergo2-beta/1.0.0-beta Chrome/85.0.4183.121 Electron/10.1.3 Safari/537.36");
                alpha.Load(File.ReadAllText(Path.GetFullPath("cache\\config.settings")));
            }
        }

        private void Beta_IsBrowserInitializedChanged(object sender, EventArgs e)
        {
            if (!File.Exists(Path.GetFullPath("cache\\config.beta.settings")))
            {
                beta.GetDevToolsClient().Emulation.SetUserAgentOverrideAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) supergo2-btr/1.0.0-btr Chrome/85.0.4183.121 Electron/10.1.3 Safari/537.36");
                beta.Load("http://149.56.143.181:3000/");
            }
            else
            {
                beta.GetDevToolsClient().Emulation.SetUserAgentOverrideAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) supergo2-btr/1.0.0-btr Chrome/85.0.4183.121 Electron/10.1.3 Safari/537.36");
                beta.Load(File.ReadAllText(Path.GetFullPath("cache\\config.beta.settings")));
            }
        }

        private void ChromiumWebBrowser_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            var chrome = (ChromiumWebBrowser)sender;
            if (e.Message == "back to home!")
            {
                if (redirector != null)
                {
                    if (redirector.IsAlive)
                        return;
                }
                redirector = new Thread(() =>
                {
                    Thread.Sleep(5000);
                    chrome.Load("https://beta.supergo2.com/");
                });
                redirector.Start();
            }
            else if (e.Message == "LAAL")
            {
                if (redirector != null)
                {
                    redirector.Abort();
                    redirector = null;
                }
            }
            else if (e.Message.StartsWith("Login result:"))
            {
                if (!e.Message.Contains("Invalid"))
                {
                    chrome.ExecuteScriptAsync("document.querySelector(\"a[href = '/myplanets']\").click()");
                }
            }
            else if (e.Message == scriptKey)
            {
                RunScript(chrome);
            }
        }

        private void ChromiumWebBrowser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            var chrome = (ChromiumWebBrowser)sender;
            if (chrome.Address == "blank")
            {
                return;
            }
            var uri = new Uri(chrome.Address);
            var host = uri.Scheme + "://" + uri.Host;
            if (!uri.IsDefaultPort)
            {
                host += ":" + uri.Port;
            }
            if (!host.EndsWith("/"))
            {
                host += "/";
            }

            if (!e.IsLoading)
            {
                if (chrome.Address.Contains("igg"))
                {
                    chrome.Back();
                    return;
                }
                else if (chrome.Address == host && File.Exists(Path.GetFullPath("cache\\credential.settings")))
                {
                    LoginWeb(chrome);
                }
                else if (chrome.Address.StartsWith(host + "play") && alpha.CanExecuteJavascriptInMainFrame)
                {
                    //player in game
                    File.WriteAllText(Path.GetFullPath("cache\\config.settings"), alpha.Address);
                    chrome.ExecuteScriptAsync(@"(function () {
var iv = setInterval(()=>{
try
{ 
if(document.getElementsByTagName('iframe')){
   document.getElementById('wrapper').style.overflow = 'hidden';
   document.getElementsByTagName('iframe')[0].height = '" + (chrome.Size.Height - 110) + @"';
   document.getElementsByTagName('iframe')[0].width = '" + (chrome.Size.Width > 1920 ? 1920 : chrome.Size.Width) + @"';
   document.getElementsByTagName('iframe')[0].style.minHeight = '" + (chrome.Size.Height - 110) + @"px';
   document.getElementsByTagName('iframe')[0].style.minWidth = '" + (chrome.Size.Width > 1920 ? 1920 : chrome.Size.Width) + @"px';
   document.getElementsByTagName('iframe')[0].style.maxWidth = '1920px';
   document.getElementsByTagName('iframe')[0].style.marginLeft = 'auto';
   document.getElementsByTagName('iframe')[0].style.marginRight = 'auto';
   document.getElementsByTagName('iframe')[0].style.marginTop = '15px';
   document.getElementsByTagName('iframe')[0].style.marginBottom = '15px';
   console.log('" + scriptKey + @"');
   document.querySelector('#wrapper .row').style.display = 'none';
   clearInterval(iv);
}
}
catch
{ 
   console.log('back to home!') 
}
}, 1000);
})();");
                    RunScript(chrome);
                }
                if (!File.Exists(Path.GetFullPath("cache\\background.settings")) && chrome.CanExecuteJavascriptInMainFrame)
                {
                    chrome.ExecuteScriptAsync("document.body.style.backgroundColor = 'black'; document.body.style.backgroundImage = 'none'");
                }
                else if (chrome.CanExecuteJavascriptInMainFrame)
                {
                    chrome.ExecuteScriptAsync("document.body.style.backgroundColor = 'black'; document.body.style.backgroundImage = 'url(data:image/png;base64," + ConvertImage(File.ReadAllText(Path.GetFullPath("cache\\background.settings"))) + ")'");
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
            _ = script.Run(cancellation.Token, chrome).ConfigureAwait(false);
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
                alpha.ExecuteScriptAsync("document.body.style.backgroundColor = 'black'; document.body.style.backgroundImage = 'url(data:image/png;base64," + ConvertImage(openFileDialog.FileName) + ")'");
            }
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            alpha.Reload(true);
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

        private void metroButton4_Click(object sender, EventArgs e)
        {
            Login login = new Login(profileName);
            login.Show();
            login.FormClosed += Login_FormClosed;
        }

        private void Login_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (File.Exists("Profile\\" + profileName + "\\config.json"))
            {
                settings = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText("Profile\\" + profileName + "\\config.json"));
            }
            LoginWeb(alpha);
            LoginWeb(beta);
        }

        private async void LoginWeb(ChromiumWebBrowser web)
        {
            var hashed = settings.CredentialHash;
            if(hashed == null)
            {
                richTextBox1.Invoke((MethodInvoker)delegate
                {
                    richTextBox1.AppendText("\nNo auto login creditials detected!\n");
                });
                return;
            }
            var login = Encryption.Decrypt(hashed);
            try
            {
                web.ExecuteScriptAsync("document.querySelector('#navbarDropdown').click();");
                web.ExecuteScriptAsync(@"(function(){
var text = '" + login.Email + @"';
var input = document.querySelector('input[name=" + "\"username\"" + @"]');
var nativeTextAreaValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
nativeTextAreaValueSetter.call(input, text);

const event = new Event('input', { bubbles: true });
input.dispatchEvent(event);
})()");
                await Task.Delay((login.Email.Length * 50) + 100);
                web.ExecuteScriptAsync(@"(function(){
var text = '" + login.Password + @"';
var input = document.querySelector('input[name=" + "\"password\"" + @"]');
var nativeTextAreaValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
nativeTextAreaValueSetter.call(input, text);

const event = new Event('input', { bubbles: true });
input.dispatchEvent(event);
})()");
                await Task.Delay((login.Password.Length * 50) + 100);
                web.ExecuteScriptAsync("document.querySelector('.loginBox__button.btn-primary').click();");
                await Task.Delay(3000);
                web.ExecuteScriptAsync("console.log(\"Login result:\"+document.querySelector('#swal2-html-container').innerText)");
            }
            catch(Exception ex)
            {
                if(ex.Message.Contains("Unable to execute javascript at this time"))
                {
                    while (!web.CanExecuteJavascriptInMainFrame)
                    {
                        await Task.Delay(1000);
                    }
                    LoginWeb(web);
                }
                richTextBox1.Invoke((MethodInvoker)delegate
                {
                    richTextBox1.SelectionStart = richTextBox1.TextLength;
                    richTextBox1.SelectionLength = 0;
                    richTextBox1.SelectionColor = Color.Red;
                    richTextBox1.AppendText("\n" + "[" + DateTime.Now.ToString("HH:mm") + "]: " + ex.Message);
                    richTextBox1.Focus();
                    richTextBox1.Select(richTextBox1.TextLength, 0);
                    richTextBox1.ScrollToCaret();
                });
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
            Cef.Shutdown();
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
            Task.Run(() =>
            {
                do
                {
                    Thread.Sleep(200);
                }
                while (!alpha.CanExecuteJavascriptInMainFrame);
                alpha.ExecuteScriptAsync(@"
   document.getElementsByTagName('iframe')[0].height = '" + (alpha.Size.Height - 110) + @"';
   document.getElementsByTagName('iframe')[0].width = '" + alpha.Size.Width + @"';
   document.getElementsByTagName('iframe')[0].style.minHeight = '" + (alpha.Size.Height - 110) + @"px';
   document.getElementsByTagName('iframe')[0].style.minWidth = '" + alpha.Size.Width + @"px';
");
            });
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

        private void RemoveFleet_Click(object sender, EventArgs e)
        {
            var name = (sender as MetroButton).Name.Replace("btnRem_", "");
            var data = settings.Fleets.First(x => x.Name == name);
            settings.Fleets.Remove(data);
            RenderFleets();
        }

        private void Input_ValueChanged(object sender, EventArgs e)
        {
            settings.Fleets.First(x => x.Name == (sender as NumericUpDown).Name).Order = (int)Math.Round((sender as NumericUpDown).Value);

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
