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
        public delegate void RefreshEventHandler(object sender, ProjectEventArgs e);
        public event RefreshEventHandler RefreshEvent;
        public CreateProjectForm()
        {
            InitializeComponent();
        }

        private void CreateProjectForm_Load(object sender, EventArgs e)
        {
            int number = 1;
            string ProjectsPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\WitchsHatProject";
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
            string ProjectName = this.textBox1.Text;
            var path = this.textBox2.Text + "\\" + ProjectName;
            System.IO.DirectoryInfo di = System.IO.Directory.CreateDirectory(path);
            // プロジェクトテンプレートコピー
            if (this.listBox1.GetSelected(0))
            {
                // enchant.jsプロジェクト
                string sourceDirName = @"Data\Templates\EnchantProject";
                string[] files = System.IO.Directory.GetFiles(sourceDirName);
                foreach (string file in files)
                {
                    Console.WriteLine(file);
                    System.IO.File.Copy(file,
                       path + "\\" + System.IO.Path.GetFileName(file), true);

                }
            }
            else
            {
                // 空のプロジェクト
            }

            var p = new ProjectEventArgs();
            p.ProjectName = ProjectName;
            p.ProjectDir = path;
            this.RefreshEvent(this, p);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            XmlWriter writer = XmlWriter.Create(path + "\\" + ProjectName + ".whprj", settings);
            writer.WriteElementString("ProjectName", ProjectName);
            writer.Flush();
            writer.Close();

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
