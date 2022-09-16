using GO2FlashLauncher.Model;
using GO2FlashLauncher.Model.SGO2;
using GO2FlashLauncher.Service;
using MetroFramework.Forms;
using Newtonsoft.Json;
using System;
using System.IO;

namespace GO2FlashLauncher
{
    public partial class PlanetSelection : MetroForm
    {
        readonly GetPlanetResponse response;
        public int SelectedProfile = -1;
        public bool RememberMe = false;
        public PlanetSelection(GetPlanetResponse planetResponse)
        {
            this.response = planetResponse;
            InitializeComponent();
        }

        private void PlanetSelection_Load(object sender, EventArgs e)
        {
            foreach(var planet in response.Data)
            {
                comboBox1.Items.Add(planet.Username);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedProfile = comboBox1.SelectedIndex;
            metalValue.Text = response.Data[SelectedProfile].Resources.Metal.ToString("N0");
            he3Value.Text = response.Data[SelectedProfile].Resources.He3.ToString("N0");
            goldValue.Text = response.Data[SelectedProfile].Resources.Gold.ToString("N0");
            MPValue.Text = response.Data[SelectedProfile].Resources.MallPoints.ToString("N0");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(SelectedProfile >= 0)
            {
                DialogResult = System.Windows.Forms.DialogResult.OK;
                Close();
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            RememberMe = checkBox1.Checked;
        }
    }
}
