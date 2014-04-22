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
    public partial class CreateFolderForm2 : Form
    {
        public delegate void OkEventHandler(string folderName);
        public OkEventHandler OkClicked;

        public CreateFolderForm2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OkClicked(textBox1.Text);
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

    }
}
