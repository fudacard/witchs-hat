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
    public partial class CreateFileForm : Form
    {
        private TreeNode rootNode;

        public delegate void OkEventHandler(string filepath);
        public OkEventHandler OkClicked;

        public CreateFileForm()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine(treeView1.SelectedNode.Name);
            string filename = textBox1.Text;
            if (filename == "")
            {
                MessageBox.Show("ファイル名を入力してください。");
                return;
            }

            if (filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("ファイル名に使用できない文字が含まれています。");
                return;
            }

            string filepath = Path.Combine(treeView1.SelectedNode.Name, filename);

            if (File.Exists(filepath))
            {
                MessageBox.Show("同名のファイルが存在します。");
                return;
            }
            OkClicked(filepath);

            Close();
        }

        private void CreateFileForm_Load(object sender, EventArgs e)
        {

            comboBox1.SelectedIndex = 0;
            List<FileExt> extlist = new List<FileExt>();
            extlist.Add(new FileExt("javascript ソースファイル", "js"));
            extlist.Add(new FileExt("HTML ファイル", "html"));
            extlist.Add(new FileExt("CSS ファイル", "css"));
            extlist.Add(new FileExt("テキストファイル", "txt"));
            comboBox1.DisplayMember = "Name";
            comboBox1.ValueMember = "Ext";
            comboBox1.DataSource = extlist;

            // プロジェクトツリー更新
            treeView1.Nodes.Clear();
            rootNode = treeView1.Nodes.Add(ProjectDir, Path.GetFileName(ProjectDir));
            // フォルダ一覧追加
            string[] dirs = System.IO.Directory.GetDirectories(ProjectDir);
            foreach (string dir in dirs)
            {
                TreeNode treenode = treeView1.Nodes[0].Nodes.Add(dir, System.IO.Path.GetFileName(dir));
                // ファイル一覧追加
                string[] files0 = System.IO.Directory.GetFiles(dir);
                foreach (string file in files0)
                {
                    if (!file.EndsWith(".whprj"))
                    {
                        TreeNode treenode1 = treenode.Nodes.Add(file, System.IO.Path.GetFileName(file));

                    }
                }
            }
            // ファイル一覧追加
            string[] files = System.IO.Directory.GetFiles(ProjectDir);
            foreach (string file in files)
            {
                if (!file.EndsWith(".whprj"))
                {
                    TreeNode treenode = this.treeView1.Nodes[0].Nodes.Add(file, System.IO.Path.GetFileName(file));
                }
            }
            treeView1.ExpandAll();

            //textBox1.Text = "subscript.js";

        }

        public string ProjectDir { get; set; }

        private void CreateFileForm_Shown(object sender, EventArgs e)
        {
            treeView1.SelectedNode = rootNode;
        }

        private void treeView1_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (System.IO.File.Exists(e.Node.Name))
            {
                e.Cancel = true;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string fileName = "";
            if (textBox1.Text != "" && textBox1.Text.IndexOf(".") >= 0)
            {
                fileName = textBox1.Text.Substring(0, textBox1.Text.LastIndexOf("."));
                textBox1.Text = fileName + "." + comboBox1.SelectedValue;
            }
            else if (textBox1.Text.IndexOf(".") == -1)
            {
                textBox1.Text += "." + comboBox1.SelectedValue;
            }
        }
    }

    class FileExt
    {
        public string Ext { get; set; }
        public string Name { get; set; }

        public FileExt(string name, string ext)
        {
            this.Name = name;
            this.Ext = ext;
        }
    }
}
