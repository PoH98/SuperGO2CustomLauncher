using GalaxyOrbit4Launcher.Service;
using MetroFramework.Forms;
using System;
using System.Net.Http;
using System.Windows.Forms;

namespace GalaxyOrbit4Launcher
{
    internal partial class CreatePlanet : MetroForm
    {
        private readonly GO2HttpService httpService;
        public CreatePlanet(GO2HttpService httpService)
        {
            InitializeComponent();
            this.httpService = httpService;
        }

        private async void metroButton1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(metroTextBox1.Text.Trim()) || metroComboBox1.SelectedIndex < 0)
            {
                return;
            }
            try
            {
                await httpService.CreatePlanet(metroTextBox1.Text.Trim(), metroComboBox1.SelectedIndex + 1);
                DialogResult = DialogResult.OK;
            }
            catch (HttpRequestException ex)
            {
                _ = MessageBox.Show(ex.Message);
            }
        }
    }
}
