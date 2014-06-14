using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace VM1
{
    public partial class frmMachine : Form
    {
        bool invalid = false;

        public frmMachine()
        {
            InitializeComponent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                textBox2.Enabled = true;
                textBox3.Enabled = true;
            }
            else
            {
                textBox2.Enabled = false;
                textBox3.Enabled = false;
            }
        }

        private void frmMachine_Load(object sender, EventArgs e)
        {   
            string path = Application.StartupPath + "\\options.bin";

            listBox1.SelectedIndex = 0;

            if (File.Exists(path))
            {
                using (StreamReader rd = new StreamReader(path))
                {
                    textBox1.Text = rd.ReadLine();
                    if (!rd.EndOfStream)
                    {
                        checkBox1.Checked = true;
                        textBox2.Text = rd.ReadLine();
                        textBox3.Text = rd.ReadLine();
                        textBox4.Text = rd.ReadLine();
                    }
                    else
                        checkBox1.Checked = false;
                }
            }




            path = Application.StartupPath + "\\comunication.bin";

            if (File.Exists(path))
            {
                using (StreamReader rd = new StreamReader(path))
                {
                    string g="";

                    lstEmails.Items.Clear();
                    while (g != "@" && !rd.EndOfStream)
                    {
                        g = rd.ReadLine();
                        if(g!="@")  lstEmails.Items.Add(g);
                    }

                    txtSMTP.Text = rd.ReadLine();

                    if (!rd.EndOfStream)
                    {
                        checkBox1.Checked = true;    
                        txtUserID.Text = rd.ReadLine();
                        txtPass.Text = rd.ReadLine();
                    }
                    else
                        checkBox2.Checked = false;
                }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (listBox1.SelectedIndex)
            {
                case 0: pnlRemote.Visible = true;  pnlComm.Visible = false; break;
                case 1: pnlRemote.Visible = false; pnlComm.Visible = true; break;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string path = Application.StartupPath + "\\options.bin";

                using (StreamWriter wr = new StreamWriter(path))
                {
                    wr.WriteLine(textBox1.Text);
                    if (checkBox1.Checked)
                    {
                        wr.WriteLine(textBox2.Text);
                        wr.WriteLine(textBox3.Text);
                        wr.WriteLine(textBox4.Text);
                    }
                }
        }

        private string DomainMapper(Match match)
        {
            // IdnMapping class with default property values.
            IdnMapping idn = new IdnMapping();

            string domainName = match.Groups[2].Value;
            try
            {
                domainName = idn.GetAscii(domainName);
            }
            catch (ArgumentException)
            {
                invalid = true;
            }
            return match.Groups[1].Value + domainName;
        }

        public bool IsValidEmail(string strIn)
        {
            

            invalid = false;
            if (String.IsNullOrEmpty(strIn))
                return false;

            // Use IdnMapping class to convert Unicode domain names. 
            try
            {
                strIn = Regex.Replace(strIn, @"(@)(.+)$", this.DomainMapper,
                                      RegexOptions.None);
            }
            catch
            {
                return false;
            }

            if (invalid)
                return false;

            // Return true if strIn is in valid e-mail format. 
            try
            {
                return Regex.IsMatch(strIn,
                      @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                      @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$",
                      RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }



        private void button3_Click(object sender, EventArgs e)
        {
            //Check if is an real email
            if (IsValidEmail(txtEmail.Text))
            {
                lstEmails.Items.Add(txtEmail.Text);
                txtEmail.Text = "";
            }
            else
            {
                MessageBox.Show("Please, insert a valid email.");
            }
        }

        private void eliminaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstEmails.SelectedIndex > -1)
            {
                if (DialogResult.Yes == MessageBox.Show(this, "Are you sure to delete this email from system?","Delete?", MessageBoxButtons.YesNo, MessageBoxIcon.Information))
                {
                    lstEmails.Items.RemoveAt(lstEmails.SelectedIndex);

                }
            }
        }

        private void pnlComm_Paint(object sender, PaintEventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                txtUserID.Enabled = true;
                txtPass.Enabled = true;
            }
            else
            {
                txtUserID.Enabled = false;
                txtPass.Enabled = false;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string path = Application.StartupPath + "\\comunication.bin";

            using (StreamWriter wr = new StreamWriter(path))
            {
                for(int i=0;i<lstEmails.Items.Count;i++)
                    wr.WriteLine(lstEmails.Items[i].ToString());

                wr.WriteLine("@");

                wr.WriteLine(txtSMTP.Text);
                if (checkBox2.Checked)
                {
                    wr.WriteLine(txtUserID.Text);
                    wr.WriteLine(txtPass.Text);
                }
            }
        }
    }
}
