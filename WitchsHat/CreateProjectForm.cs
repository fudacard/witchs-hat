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
using System.Xml;

namespace WitchsHat
{
    public partial class CreateProjectForm : Form
    {

        public delegate void OkEventHandler(string projectName, string projectDir, int projectTemplate, string newProjectsPath);
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

            if (projectName == "")
            {
                MessageBox.Show("プロジェクト名を入力してください。");
                return;
            }

            if (projectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("プロジェクト名に使用できない文字が含まれています。");
                return;
            }

            var projectDir = Path.Combine(this.textBox2.Text, projectName);
            if (Directory.Exists(projectDir))
            {
                MessageBox.Show("同名のプロジェクトがすでにあります。");
                return;
            }
            int projectTemplate = 0;
            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                if (this.listBox1.GetSelected(i))
                {
                    projectTemplate = i;
                    break;
                }
            }

            if (checkBox1.Checked)
            {
                OkClicked(projectName, projectDir, projectTemplate, this.textBox2.Text);
            }
            else
            {
                OkClicked(projectName, projectDir, projectTemplate, null);
            }

            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog ofd = new CommonOpenFileDialog();
            ofd.InitialDirectory = textBox2.Text;
            ofd.IsFolderPicker = true;
            if (ofd.ShowDialog(this.Handle) == CommonFileDialogResult.Ok)
            {
                Console.WriteLine(ofd.FileName);
                this.textBox2.Text = ofd.FileName;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int projectTemplate = 0;
            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                if (this.listBox1.GetSelected(i))
                {
                    projectTemplate = i;
                    break;
                }
            }
            switch (projectTemplate)
            {
                case 0:
                    label3.Text = "enchant.js本体、表示用HTML、実行用初期コードが含まれたプロジェクトを作成します。";
                    break;
                case 1:
                    label3.Text = "何もファイルが含まれていない空のプロジェクトを作成します。";
                    break;
            }
        }


    }
}
