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

        public delegate void OkEventHandler(string projectName, string projectDir, WHTemplate projectTemplate, string newProjectsPath);
        public OkEventHandler OkClicked;

        public string ProjectsPath { get; set; }
        public List<WHTemplate> templates;

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

            List<string> importDirs = new List<string>();
            importDirs.Add("");
            importDirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\build"));
            importDirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\build\plugins"));
            importDirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\images"));
            importDirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\images\monster"));
            
            this.templates = new List<WHTemplate>();

            string[] templateDirs = Directory.GetDirectories(Path.Combine(Application.StartupPath, @"Data\Templates"));
            foreach (string templateDir in templateDirs)
            {
                string[] files = Directory.GetFiles(templateDir);
                foreach (string fileName in files) {
                    // フォルダ内の最初に見つかったtemplateファイルを使う
                    if (fileName.EndsWith(".template"))
                    {
                        WHTemplate temp = WHTemplate.ReadTemplate(fileName);

                        importDirs[0] = templateDir;
                        if (temp.CheckFiles(importDirs))
                        {
                            // ファイルがすべて揃っているならリストに追加
                            this.listBox1.Items.Add(temp.Name);
                            templates.Add(temp);
                        }
                        break;
                    }
                }
            }
            listBox1.DataSource = templates;
            listBox1.DisplayMember = "Name";

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
                OkClicked(projectName, projectDir, templates[projectTemplate], this.textBox2.Text);
            }
            else
            {
                OkClicked(projectName, projectDir, templates[projectTemplate], null);
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

            label3.Text = templates[projectTemplate].Description;
        }
    }
}
