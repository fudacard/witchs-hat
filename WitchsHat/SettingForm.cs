﻿using Microsoft.WindowsAPICodePack.Dialogs;
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
            BrowserComboBox.DisplayMember = "Name";
            BrowserComboBox.ValueMember = "Command";
            BrowserComboBox.DataSource = browserList;
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
            settings.Browser = BrowserComboBox.SelectedValue.ToString();
            settings.RunBrowser = RunBrowserComboBox.SelectedValue.ToString();
            OkClicked();
            this.Close();
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Text == "全般" || e.Node.Text == "動作")
            {
                panel1.Visible = true;
                panel2.Visible = false;
            }
            else if (e.Node.Text == "ブラウザー")
            {
                panel1.Visible = false;
                panel2.Visible = true;
            }
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
            EncodingComboBox.Text = settings.Encoding;
            ProjectsPathTextBox.Text = settings.ProjectsPath;
            TempProjectCheckBox.Checked = settings.TempProjectEnable;
            ServerCheckBox.Checked = settings.ServerEnable;
            ServerPortTextBox.Text = settings.ServerPort.ToString();
            BrowserComboBox.SelectedValue = settings.Browser;
            RunBrowserComboBox.SelectedValue = settings.RunBrowser;
            panel1.Visible = true;
            panel2.Visible = false;
            treeView1.ExpandAll();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {

            CommonOpenFileDialog ofd = new CommonOpenFileDialog();
            ofd.IsFolderPicker = true;
            if (ofd.ShowDialog(this.Handle) == CommonFileDialogResult.Ok)
            {
                Console.WriteLine(ofd.FileName);
                ProjectsPathTextBox.Text = ofd.FileName;
            }
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
