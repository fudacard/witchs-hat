using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace WitchsHat
{
    public partial class FileTreeView : TreeView
    {
        public string ProjectName { get; set; }
        public string ProjectDir { get; set; }
        // ツリーノードがディレクトリかどうか
        Dictionary<int, bool> isDirectory = new Dictionary<int, bool>();

        public FileTreeView()
        {
            InitializeComponent();
        }

        public Form1 form1 { get; set; }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.F5) == Keys.F5)
            {
                UpdateFileTree();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public void UpdateFileTree()
        {

            // プロジェクトツリー更新
            this.Nodes.Clear();
            this.Nodes.Add(ProjectDir, ProjectName, 2, 2);
            isDirectory = new Dictionary<int, bool>();
            // フォルダ一覧追加
            string[] dirs = System.IO.Directory.GetDirectories(ProjectDir);
            foreach (string dir in dirs)
            {
                TreeNode treenode = this.Nodes[0].Nodes.Add(dir, System.IO.Path.GetFileName(dir), 1, 1);
                isDirectory[treenode.GetHashCode()] = true;
                // ファイル一覧追加
                string[] files0 = System.IO.Directory.GetFiles(dir);
                foreach (string file in files0)
                {
                    if (!file.EndsWith(".whprj"))
                    {
                        int icon = GetTreeViewIcon(Path.GetExtension(file));
                        TreeNode treenode1 = treenode.Nodes.Add(file, System.IO.Path.GetFileName(file), icon, icon);
                        isDirectory[treenode1.GetHashCode()] = false;
                    }
                }
            }
            // ファイル一覧追加
            string[] files = System.IO.Directory.GetFiles(ProjectDir);
            foreach (string file in files)
            {
                if (!file.EndsWith(".whprj"))
                {
                    int icon = GetTreeViewIcon(Path.GetExtension(file));
                    TreeNode treenode = this.Nodes[0].Nodes.Add(file, System.IO.Path.GetFileName(file), icon, icon);
                    isDirectory[treenode.GetHashCode()] = false;
                }
            }
            ExpandAll();
        }

        private int GetTreeViewIcon(string ext)
        {
            if (ext == ".js")
            {
                return 3;
            }
            else if (ext == ".html" || ext == ".htm")
            {
                return 4;
            }
            else if (ext == ".png")
            {
                return 5;
            }
            else if (ext == ".jpg" || ext == ".jpeg")
            {
                return 6;
            }
            else if (ext == ".gif")
            {
                return 7;
            }
            else if (ext == ".css")
            {
                return 8;
            }
            else
            {
                return 0;
            }
        }

        public bool IsDirectory(TreeNode treeNode)
        {
            return isDirectory[treeNode.GetHashCode()];
        }
    }
}
