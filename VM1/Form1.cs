using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Management;
using System.Globalization;
using System.Management.HyperV;
using System.Threading;


namespace VM1
{
    public partial class Form1 : Form
    {
        public VM vms;
        Thread th;
        public string app_path = Application.StartupPath;   //for other modules

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            vms = new VM(this);
            UI_VM_CONTAINER.Controls.Clear();
            th = new Thread(new ThreadStart(vms.getRemoteVM));
            th.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
         
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!th.IsAlive)
            {
                th = new Thread(new ThreadStart(vms.getRemoteVM));
                th.Start();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Enabled = false;
            if (th.IsAlive)
                th.Abort();

            th = null;
            vms = null;
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void optionsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            frmMachine frm = new frmMachine();
            frm.ShowDialog();
        }

        private void communicationToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
