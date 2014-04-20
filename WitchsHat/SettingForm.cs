using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WitchsHat
{
    public partial class SettingForm : Form
    {
        public SettingForm()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Text == "全般" || e.Node.Text == "動作")
            {
                panel1.Visible = true;
                panel2.Visible = false;
            }
            else if (e.Node.Text == "ブラウザー")
            {
                panel1.Visible = false;
                panel2.Visible = true;
            }
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {

            panel1.Visible = true;
            panel2.Visible = false;
            treeView1.ExpandAll();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
