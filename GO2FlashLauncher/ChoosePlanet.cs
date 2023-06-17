using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GO2FlashLauncher
{
    public partial class ChoosePlanet : MetroForm
    {
        public string SelectedPlanet { get; private set; }
        public ChoosePlanet(List<string> planets)
        {
            InitializeComponent();
            metroComboBox1.Items.AddRange(planets.ToArray());
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            if(metroComboBox1.SelectedIndex == -1)
            {
                return;
            }
            SelectedPlanet = metroComboBox1.Items[metroComboBox1.SelectedIndex].ToString();
            DialogResult = DialogResult.OK;
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
        }
    }
}
