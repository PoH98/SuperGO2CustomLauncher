using GO2FlashLauncher.Service;
using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;

namespace GO2FlashLauncher
{
    public partial class Login : MetroForm
    {
        public Login()
        {
            InitializeComponent();
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            File.WriteAllText(Path.GetFullPath("cache\\credential.settings"), Encryption.Encrypt(metroTextBox1.Text, metroTextBox2.Text) );
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }
    }
}
