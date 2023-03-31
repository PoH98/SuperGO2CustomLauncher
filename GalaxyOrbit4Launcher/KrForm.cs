using CefSharp;
using CefSharp.WinForms;
using EasyTabs;
using System;
using System.IO;
using System.Windows.Forms;

namespace GalaxyOrbit4Launcher
{
    public partial class KrForm : Form
    {
        protected TitleBarTabs ParentTabs => ParentForm as TitleBarTabs;
        private ChromiumWebBrowser chrome;
        public KrForm()
        {
            InitializeComponent();
        }

        private void KrForm_Load(object sender, EventArgs e)
        {
            RequestContextSettings alphaContext = new RequestContextSettings
            {
                PersistUserPreferences = true,
                PersistSessionCookies = true,
                CachePath = Path.GetFullPath("cache"),
            };
            chrome = new ChromiumWebBrowser("https://krtools.deajae.co.uk/")
            {
                RequestContext = new RequestContext(alphaContext)
            };
            chrome.TitleChanged += Chrome_TitleChanged;
            chrome.FocusHandler = null;
            chrome.Dock = DockStyle.Fill;
            Controls.Add(chrome);
        }

        private void Chrome_TitleChanged(object sender, TitleChangedEventArgs e)
        {
            _ = Invoke((MethodInvoker)delegate
            {
                Text = e.Title;
            });

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
}
