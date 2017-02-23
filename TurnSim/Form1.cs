using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace TurnSim {
    public partial class Form1 : Form {

        public SimSession Sim;

        public Form1() {
            InitializeComponent();
            Sim = new SimSession(RenderBox, txtLog, "ouput.txt", 2);

        }

        private void button1_Click(object sender, EventArgs e) {
            Sim.SetupMatch();
            Sim.StartSim();
        }

        private void ToggleVis_Click(object sender, EventArgs e) {
            Sim.Vis.Enabled = !Sim.Vis.Enabled;
        }
    }
}
