using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WitchsHat
{
    public partial class FileTreeView : TreeView
    {
        public FileTreeView()
        {
            InitializeComponent();
        }

        public Form1 form1 { get; set; }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.F5) == Keys.F5)
            {
                form1.UpdateFileTree();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
