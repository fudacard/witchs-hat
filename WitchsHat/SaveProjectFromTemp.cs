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
        public delegate void OkEventHandler(string projectName, string projectDir);
        public OkEventHandler OkClicked;

        public string ProjectName;
        public string ProjectDir;
        public string ProjectsPath;

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
            ProjectName = this.textBox1.Text;

            if (ProjectName == "")
            {
                MessageBox.Show("プロジェクト名を入力してください。");
                return;
            }

            if (ProjectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("プロジェクト名に使用できない文字が含まれています。");
                return;
            }

            if (textBox2.Text == "")
            {
                MessageBox.Show("プロジェクトを作るディレクトリを入力してください。");
                return;
            }

            if (!Directory.Exists(textBox2.Text))
            {
                MessageBox.Show("ディレクトリが見つかりません。");
                return;
            }

            var projectDir = Path.Combine(this.textBox2.Text, ProjectName);
            if (Directory.Exists(projectDir))
            {
                MessageBox.Show("同名のプロジェクトがすでにあります。");
                return;
            }

            ProjectDir = Path.Combine(textBox2.Text, ProjectName);
            this.Close();
            OkClicked(ProjectName, ProjectDir);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SaveProjectFromTemp_Load(object sender, EventArgs e)
        {
            textBox2.Text = ProjectsPath;
        }
    }
}
