using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WitchsHat
{
    public partial class ProjectPropertyForm : Form
    {
        public delegate void OkEventHandler();
        public OkEventHandler OkClicked;

        public ProjectProperty projectProperty { get; set; }

        public ProjectPropertyForm()
        {
            InitializeComponent();
        }

        private void ProjectPropertyForm_Load(object sender, EventArgs e)
        {
            this.Text = projectProperty.Name + " のプロパティ";
            textBox1.Text = projectProperty.Name;
            ProjectDirTextBox.Text = projectProperty.Dir;
            comboBox1.Text = projectProperty.Encoding;

            string[] files = System.IO.Directory.GetFiles(projectProperty.Dir);
            foreach (string file in files)
            {
                string lower = file.ToLower();
                if (lower.EndsWith(".html") || lower.EndsWith(".htm"))
                {
                    comboBox2.Items.Add(file.Replace(projectProperty.Dir + "\\", ""));
                }
            }
            comboBox2.Text = projectProperty.HtmlPath;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {
                return;
            }
            if (comboBox2.Text == "")
            {
                MessageBox.Show("実行するHTMLファイルを入力してください。");
                return;
            }
            OkClicked();
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
