using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Management.HyperV;

namespace VM1
{
    public partial class vm_control : UserControl
    {
        public Form1 frm;

        public vm_control()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            frm.vms.doAction(lblName.Text, "start",this);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show(frm.vms.CreateVirtualSystemSnapshot(lblName.Text,this));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            frm.vms.doAction(lblName.Text, "stop", this);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            frm.vms.doAction(lblName.Text, "reset", this);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            frm.vms.doAction(lblName.Text, "suspend", this);
        }
    }
}
