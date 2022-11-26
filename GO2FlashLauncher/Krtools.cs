using CefSharp;
using CefSharp.WinForms;
using System;
using System.Windows.Forms;

namespace GO2FlashLauncher
{
    public partial class Krtools : UserControl
    {
        private ChromiumWebBrowser chromium;
        public Krtools()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            chromium.Reload(true);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            chromium.Forward();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            chromium.Back();
        }

        private void Krtools_Load(object sender, EventArgs e)
        {
            chromium = new ChromiumWebBrowser("https://krtools.deajae.co.uk/")
            {
                Dock = DockStyle.Fill
            };
            panel1.Controls.Add(chromium);
        }
    }
}
