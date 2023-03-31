using GalaxyOrbit4Launcher.Service;
using MetroFramework.Forms;
using System;
using System.Windows.Forms;

namespace GalaxyOrbit4Launcher
{
    public partial class Login : MetroForm
    {
        public bool IsError { get; set; }
        public string Exception { get; set; }
        public Login()
        {
            InitializeComponent();
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            ConfigService.Instance.Config.CredentialHash = Encryption.Encrypt(metroTextBox1.Text, metroTextBox2.Text);
            ConfigService.Instance.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Login_Shown(object sender, EventArgs e)
        {
            error.Visible = IsError;
            _ = Focus();
            if (IsError)
            {
                _ = MessageBox.Show(Exception, "Error!");
            }
        }
    }
}
