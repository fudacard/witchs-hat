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
    public partial class RenameForm : Form
    {
        public delegate void OkEventHandler(string fileName);
        public OkEventHandler OkClicked;
        public string OldFileName;

        public RenameForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "") {
                MessageBox.Show("ファイル名を入力してください");
                return;
            }
            OkClicked(textBox1.Text);
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void RenameForm_Load(object sender, EventArgs e)
        {
            textBox1.Text = OldFileName;
        }
    }
}
