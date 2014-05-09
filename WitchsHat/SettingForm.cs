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

namespace WitchsHat
{
    public partial class SettingForm : Form
    {
        public delegate void OkEventHandler();
        public OkEventHandler OkClicked;
        public EnvironmentSettings settings;

        public SettingForm()
        {
            InitializeComponent();

            List<Browser> browserList = new List<Browser>();
            browserList.Add(new Browser("Internet Explorer", "iexplore"));
            browserList.Add(new Browser("Chrome", "chrome"));
            browserList.Add(new Browser("Firefox", "firefox"));
            browserList.Add(new Browser("Opera", "opera"));
            RunBrowserComboBox.DisplayMember = "Name";
            RunBrowserComboBox.ValueMember = "Command";
            RunBrowserComboBox.DataSource = new List<Browser>(browserList);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            settings.Encoding = EncodingComboBox.Text;
            settings.ProjectsPath = ProjectsPathTextBox.Text;
            settings.TempProjectEnable = TempProjectCheckBox.Checked;
            settings.ServerEnable = ServerCheckBox.Checked;
            settings.ServerPort = int.Parse(ServerPortTextBox.Text);
            settings.RunBrowser = RunBrowserComboBox.SelectedValue.ToString();
            settings.EnchantjsDownload = EnchantjsDownloadcheckBox.Checked;
            settings.SuggestEnable = SuggestCheckBox.Checked;
            settings.IndentUseTab = IndentTabRadioButton.Checked;
            OkClicked();
            this.Close();
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Text == "全般" || e.Node.Text == "動作")
            {
                TitleLabel.Text = "動作";
                panel1.Visible = true;
                panel2.Visible = false;
                panel3.Visible = false;
                panel4.Visible = false;
            }
            else if (e.Node.Text == "ブラウザー")
            {
                TitleLabel.Text = "ブラウザー";
                panel1.Visible = false;
                panel2.Visible = true;
                panel3.Visible = false;
                panel4.Visible = false;
            }
            else if (e.Node.Text == "エディタ")
            {
                TitleLabel.Text = "エディタ";
                panel1.Visible = false;
                panel2.Visible = false;
                panel3.Visible = true;
                panel4.Visible = false;
            }
            else if (e.Node.Text == "上級者設定")
            {
                TitleLabel.Text = "上級者設定";
                panel1.Visible = false;
                panel2.Visible = false;
                panel3.Visible = false;
                panel4.Visible = true;
            }
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
            panel1.Visible = true;
            panel2.Visible = false;
            panel3.Visible = false;
            panel4.Visible = false;
            treeView1.ExpandAll();
            // 動作
            EncodingComboBox.Text = settings.Encoding;
            ProjectsPathTextBox.Text = settings.ProjectsPath;
            TempProjectCheckBox.Checked = settings.TempProjectEnable;
            ServerCheckBox.Checked = settings.ServerEnable;
            ServerPortTextBox.Text = settings.ServerPort.ToString();
            EnchantjsDownloadcheckBox.Checked = settings.EnchantjsDownload;
            SuggestCheckBox.Checked = settings.SuggestEnable;
            // ブラウザー
            RunBrowserComboBox.SelectedValue = settings.RunBrowser;
            // エディタ
            TitleLabel.Text = "動作";
            FontPreviewTextBox.Font = new Font(settings.FontName, settings.FontSize);
            FontNameLabel.Text = "フォント名 " + settings.FontName;
            FontSizeLabel.Text = "フォントサイズ " + settings.FontSize;
            if (settings.IndentUseTab)
            {
                IndentTabRadioButton.Checked = true;
            }
            else
            {
                IndentSpaceRadioButton.Checked = true;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog ofd = new CommonOpenFileDialog();
            ofd.InitialDirectory = ProjectsPathTextBox.Text;
            ofd.IsFolderPicker = true;
            if (ofd.ShowDialog(this.Handle) == CommonFileDialogResult.Ok)
            {
                Console.WriteLine(ofd.FileName);
                ProjectsPathTextBox.Text = ofd.FileName;
            }
        }

        private void FontSelectButton_Click(object sender, EventArgs e)
        {
            FontDialog f = new FontDialog();
            f.Font = new Font(settings.FontName, settings.FontSize);
            DialogResult result = f.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                Console.WriteLine(f.Font.Name);
                Console.WriteLine("Unit: " + f.Font.Unit);
                Console.WriteLine("Size: " + f.Font.Size);
                Console.WriteLine("Size: " + f.Font.SizeInPoints);
                settings.FontName = f.Font.Name;
                settings.FontSize = f.Font.Size;
                FontPreviewTextBox.Font = f.Font;
                FontNameLabel.Text = "フォント名 " + settings.FontName;
                FontSizeLabel.Text = "フォントサイズ " + settings.FontSize;
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }
    }

    class Browser
    {
        public string Name { get; set; }
        public string Command { get; set; }
        public Browser(string name, string command)
        {
            this.Name = name;
            this.Command = command;
        }
    }
}
