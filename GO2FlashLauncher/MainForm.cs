using CefSharp;
using CefSharp.WinForms;
using GO2FlashLauncher.Service;
using MetroFramework.Forms;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GO2FlashLauncher
{
    public partial class MainForm : MetroForm
    {
        ChromiumWebBrowser chromiumWebBrowser;
        Thread redirector;
        string scriptKey = "";
        public MainForm()
        {
            InitializeComponent();
            scriptKey = Encryption.RandomString(10);

        }

        private void Form1_Load(object sender, EventArgs e)
        {
#if !DEBUG
            metroButton3.Hide();
#endif
            var settings = new CefSettings();
            settings.CachePath = Path.GetFullPath("cache");
            settings.CefCommandLineArgs.Add("enable-system-flash", "1");
            settings.CefCommandLineArgs.Add("ppapi-flash-path",Path.Combine(Directory.GetCurrentDirectory(), "libs\\pepflashplayer.dll"));
            settings.CefCommandLineArgs.Add("ppapi-flash-version", "28.0.0.137");
            settings.CefCommandLineArgs.Add("disable-gpu", "1");
            settings.CefCommandLineArgs.Add("disable-gpu-compositing", "1");
            settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");
            settings.CefCommandLineArgs.Add("disable-gpu-vsync", "1");
            settings.CefCommandLineArgs["plugin-policy"] = "allow";
            settings.CefCommandLineArgs.Add("allow-outdated-plugins");

            if (!Cef.Initialize(settings, true))
            {
                throw new Exception("Unable to Initialize Cef");
            }

            if (!File.Exists(Path.GetFullPath("cache\\config.settings")))
            {
                chromiumWebBrowser = new ChromiumWebBrowser("https://beta.supergo2.com/");
            }
            else
            {
                chromiumWebBrowser = new ChromiumWebBrowser(File.ReadAllText(Path.GetFullPath("cache\\config.settings")));
            }
            chromiumWebBrowser.BrowserSettings.Plugins = CefState.Enabled;
            chromiumWebBrowser.RequestContext = new RequestContext();
            Cef.UIThreadTaskFactory.StartNew(delegate {
                chromiumWebBrowser.RequestContext.SetPreference("profile.default_content_setting_values.plugins", 1, out string error);
            });
            chromiumWebBrowser.MenuHandler = new CustomMenuHandler();
            panel1.Controls.Add(chromiumWebBrowser);
            chromiumWebBrowser.Dock = DockStyle.Fill;
            chromiumWebBrowser.LoadingStateChanged += ChromiumWebBrowser_LoadingStateChanged;
            chromiumWebBrowser.ConsoleMessage += ChromiumWebBrowser_ConsoleMessage;
        }

        private void ChromiumWebBrowser_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            if(e.Message == "back to home!")
            {
                if (redirector != null)
                {
                    if(redirector.IsAlive)
                        return;
                }
                redirector = new Thread(() =>
                {
                    Thread.Sleep(5000);
                    chromiumWebBrowser.Load("https://beta.supergo2.com/");
                });
                redirector.Start();                
            }
            else if(e.Message == "LAAL")
            {
                if (redirector != null)
                {
                    redirector.Abort();
                    redirector = null;
                }
            }
            else if(e.Message == scriptKey)
            {
                RunScript();
            }
        }

        private void ChromiumWebBrowser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                if (!chromiumWebBrowser.Address.StartsWith("https://beta.supergo2.com/"))
                {
                    chromiumWebBrowser.Load("https://beta.supergo2.com/");
                    return;
                }
                else if (chromiumWebBrowser.Address == "https://beta.supergo2.com/" && File.Exists(Path.GetFullPath("cache\\credential.settings")))
                {
                    LoginWeb();
                }
                else if (chromiumWebBrowser.Address.StartsWith("https://beta.supergo2.com/play"))
                {
                    //player in game
                    File.WriteAllText(Path.GetFullPath("cache\\config.settings"), chromiumWebBrowser.Address);
                    chromiumWebBrowser.ExecuteScriptAsync(@"
setInterval(()=>{
try
{ 
   document.getElementsByTagName('iframe')[0].height = '800';
   document.getElementsByTagName('iframe')[0].style.minHeight = '800px';
   console.log('"+ scriptKey + @"');
}
catch
{ 
   console.log('back to home!') 
}
}, 1000);");
                    RunScript();
                }
                if (!File.Exists(Path.GetFullPath("cache\\background.settings")))
                {
                    chromiumWebBrowser.ExecuteScriptAsync("document.body.style.backgroundColor = 'black'; document.body.style.backgroundImage = 'none'");
                }
                else
                {
                    chromiumWebBrowser.ExecuteScriptAsync("document.body.style.backgroundColor = 'black'; document.body.style.backgroundImage = 'url(data:image/png;base64," + ConvertImage(File.ReadAllText(Path.GetFullPath("cache\\background.settings"))) +")'");
                }
            }
        }

        private void RunScript()
        {
            //Run script
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
                chromiumWebBrowser.ExecuteScriptAsync("document.body.style.backgroundColor = 'black'; document.body.style.backgroundImage = 'url(data:image/png;base64," + ConvertImage(openFileDialog.FileName) + ")'");
            }
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            chromiumWebBrowser.Reload(true);
        }

        private void metroButton3_Click(object sender, EventArgs e)
        {
            chromiumWebBrowser.ShowDevTools();
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
            if(WindowState == FormWindowState.Normal)
            {
                WindowState = FormWindowState.Maximized;
                maximizeBtn.Text = "\U0001F5D7";
            }
            else
            {
                WindowState = FormWindowState.Normal;
                maximizeBtn.Text = "\U0001F5D6";
            }
        }

        private void metroButton4_Click(object sender, EventArgs e)
        {
            Login login = new Login();
            login.Show();
            login.FormClosed += Login_FormClosed;
        }

        private void Login_FormClosed(object sender, FormClosedEventArgs e)
        {
            LoginWeb();
        }

        private async void LoginWeb()
        {
            var hashed = File.ReadAllText(Path.GetFullPath("cache\\credential.settings"));
            var login = Encryption.Decrypt(hashed);
            chromiumWebBrowser.ExecuteScriptAsync("document.querySelector('#navbarDropdown').click();");
            chromiumWebBrowser.ExecuteScriptAsync(@"
var text = '"+ login.Email + @"';
var input = document.querySelector('input[name=" + "\"username\"" + @"]');
var nativeTextAreaValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
nativeTextAreaValueSetter.call(input, text);

const event = new Event('input', { bubbles: true });
input.dispatchEvent(event);
");
            await Task.Delay((login.Email.Length * 50) + 100);
            chromiumWebBrowser.ExecuteScriptAsync(@"
var text = '" + login.Password + @"';
var input = document.querySelector('input[name=" + "\"password\"" + @"]');
var nativeTextAreaValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
nativeTextAreaValueSetter.call(input, text);

input.dispatchEvent(event);
");
            await Task.Delay((login.Password.Length * 50) + 100);
            chromiumWebBrowser.ExecuteScriptAsync("document.querySelector('.loginBox__button.btn-primary').click();");
            await Task.Delay(5000);
            chromiumWebBrowser.ExecuteScriptAsync("document.querySelector(\"a[href = '/myplanets']\").click()");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cef.Shutdown();
        }

        private void metroButton5_Click(object sender, EventArgs e)
        {
            if (File.Exists(Path.GetFullPath("cache\\background.settings")))
            {
                File.Delete(Path.GetFullPath("cache\\background.settings"));
            }
            chromiumWebBrowser.ExecuteScriptAsync("document.body.style.backgroundColor = 'black'; document.body.style.backgroundImage = 'none'");
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
