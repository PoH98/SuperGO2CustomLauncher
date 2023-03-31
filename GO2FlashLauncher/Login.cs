using GO2FlashLauncher.Model;
using GO2FlashLauncher.Service;
using MetroFramework.Forms;
using Newtonsoft.Json;
using System;
using System.IO;

namespace GO2FlashLauncher
{
    public partial class Login : MetroForm
    {
        readonly BotSettings settings = new BotSettings();
        readonly string path = null;
        public bool IsError = false;
        public Login(string profileName)
        {
            InitializeComponent();
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            ConfigService.Instance.Config.CredentialHash = Encryption.Encrypt(metroTextBox1.Text, metroTextBox2.Text);
            ConfigService.Instance.Save();
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        private void Login_Shown(object sender, EventArgs e)
        {
            error.Visible = IsError;
        }
    }
}
