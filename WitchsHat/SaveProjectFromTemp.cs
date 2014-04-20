using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WitchsHat
{
    public partial class SaveProjectFromTemp : Form
    {
        public delegate void OkEventHandler();
        public OkEventHandler OkClicked;

        public string ProjectName;
        public string ProjectDir;

        public SaveProjectFromTemp()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog ofd = new CommonOpenFileDialog();
            ofd.IsFolderPicker = true;
            if (ofd.ShowDialog(this.Handle) == CommonFileDialogResult.Ok)
            {
                Console.WriteLine(ofd.FileName);
                this.textBox2.Text = ofd.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "" && textBox2.Text != "")
            {
                ProjectName = textBox1.Text;
                ProjectDir = Path.Combine(textBox2.Text, ProjectName);
                this.Close();
                OkClicked();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SaveProjectFromTemp_Load(object sender, EventArgs e)
        {

        }
    }
}
