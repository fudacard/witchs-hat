using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace WitchsHat
{
    public partial class CreateProjectForm : Form
    {

        public delegate void OkEventHandler(string projectName, string projectDir, int projectTemplate);
        public OkEventHandler OkClicked;

        public string ProjectsPath { get; set; }

        public CreateProjectForm()
        {
            InitializeComponent();
        }

        private void CreateProjectForm_Load(object sender, EventArgs e)
        {
            int number = 1;
            if (System.IO.Directory.Exists(ProjectsPath))
            {
                string[] dirs = System.IO.Directory.GetDirectories(ProjectsPath);
                number = dirs.Count() + 1;
            }
            this.textBox1.Text = "Project" + number;
            this.textBox2.Text = ProjectsPath;

            this.listBox1.SetSelected(0, true);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string projectName = this.textBox1.Text;
            var projectDir = this.textBox2.Text + "\\" + projectName;
            int projectTemplate = 0;
            for (int i = 0; i < listBox1.Items.Count; i++ )
            {
                if (this.listBox1.GetSelected(i))
                {
                    projectTemplate = i;
                    break;
                }
            }
            
            OkClicked(projectName, projectDir, projectTemplate);


            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog ofd = new CommonOpenFileDialog();
            ofd.IsFolderPicker = true;
            if (ofd.ShowDialog(this.Handle) == CommonFileDialogResult.Ok)
            {
                Console.WriteLine(ofd.FileName);
                this.textBox2.Text = ofd.FileName;
            }
        }


    }
}
