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
        public Login(string profileName)
        {
            path = "Profile\\" + profileName + "\\config.json";
            if (File.Exists(path))
            {
                try
                {
                    settings = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText(path));
                }
                catch
                {

                }
            }
            InitializeComponent();
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            settings.CredentialHash = Encryption.Encrypt(metroTextBox1.Text, metroTextBox2.Text);
            File.WriteAllText(path, JsonConvert.SerializeObject(settings));
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }
    }
}
