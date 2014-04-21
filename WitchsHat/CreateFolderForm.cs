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
    public partial class CreateFolderForm : Form
    {
        private TreeNode rootNode;

        public delegate void OkEventHandler(string folderpath);
        public OkEventHandler OkClicked;

        public CreateFolderForm()
        {
            InitializeComponent();
        }

        private void CreateFolderForm_Load(object sender, EventArgs e)
        {

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
        }

        public string ProjectDir { get; set; }

        private void button1_Click(object sender, EventArgs e)
        {

            Console.WriteLine(treeView1.SelectedNode.Name);
            string foldername = textBox1.Text;
            if (foldername == "")
            {
                MessageBox.Show("フォルダ名を入力してください。");
                return;
            }

            string folderpath = Path.Combine(treeView1.SelectedNode.Name, foldername);

            if (Directory.Exists(folderpath))
            {
                MessageBox.Show("同名のフォルダが存在します。");
                return;
            }
            OkClicked(folderpath);

            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void CreateFolderForm_Shown(object sender, EventArgs e)
        {
            treeView1.SelectedNode = rootNode;
        }
    }
}
