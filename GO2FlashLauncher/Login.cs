using GO2FlashLauncher.Service;
using MetroFramework.Forms;
using System;

namespace GO2FlashLauncher
{
    public partial class Login : MetroForm
    {
        public bool IsError = false;
        public Login()
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
