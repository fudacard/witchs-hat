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
    public partial class CreateFolderForm2 : Form
    {
        public delegate void OkEventHandler(string folderName);
        public OkEventHandler OkClicked;

        public string Dir { get; set; }

        public CreateFolderForm2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string foldername = textBox1.Text;
            if (foldername == "")
            {
                MessageBox.Show("フォルダ名を入力してください。");
                return;
            }

            if (foldername.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("フォルダ名に使用できない文字が含まれています。");
                return;
            }

            string folderpath = Path.Combine(Dir, foldername);

            if (Directory.Exists(folderpath))
            {
                MessageBox.Show("同名のフォルダが存在します。");
                return;
            }

            OkClicked(textBox1.Text);
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

    }
}
