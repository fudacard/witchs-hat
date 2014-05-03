using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WitchsHat
{
    class TabManager
    {
        TabControl tabControl { get; set; }
        public TabPage clickedTabPage;
        ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        Dictionary<TabPage, TabInfo> tabInfos;

        public TabManager(TabControl tabControl, Dictionary<TabPage, TabInfo> tabInfos)
        {
            this.tabControl = tabControl;
            this.tabInfos = tabInfos;

            tabControl.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                this.clickedTabPage = null;
                for (int i = 0; i < tabControl.TabCount; i++)
                {
                    if (tabControl.GetTabRect(i).Contains(e.X, e.Y))
                    {
                        this.clickedTabPage = (TabPage)tabControl.GetControl(i);
                        tabControl.SelectedTab = this.clickedTabPage;
                    }
                }
            };
            tabControl.MouseUp += delegate(object sender, MouseEventArgs e)
            {
                if (this.clickedTabPage != null)
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        contextMenuStrip.Show((TabControl)sender, e.Location);
                    }
                }
            };
            ToolStripMenuItem menuClose = new ToolStripMenuItem("閉じる");
            TabPage targetTab = null;
            string filepath = null;
            menuClose.Click += delegate(object sender1, EventArgs e1)
            {
                // タブを閉じる
                if (tabInfos[targetTab].Modify)
                {
                    DialogResult result = MessageBox.Show("変更を保存しますか？", "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.Yes)
                    {
                        // ファイル保存
                        Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)targetTab.Controls[0];
                        StreamWriter writer = new StreamWriter(filepath);
                        writer.Write(azuki.Text);
                        writer.Close();

                        tabControl.TabPages.Remove(targetTab);
                        tabInfos.Remove(targetTab);
                    }
                    else if (result == DialogResult.No)
                    {
                        tabControl.TabPages.Remove(targetTab);
                        tabInfos.Remove(targetTab);
                    }
                }
                else
                {
                    tabControl.TabPages.Remove(targetTab);
                    if (tabInfos[targetTab].Type != TabInfo.TabTypeBrowser)
                    {
                        tabInfos.Remove(targetTab);
                    }
                }
            };

            ToolStripMenuItem menuSave = new ToolStripMenuItem("保存");
            menuSave.Click += delegate(object sender1, EventArgs e1)
            {
                // ファイル保存
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)targetTab.Controls[0];
                StreamWriter writer = new StreamWriter(filepath);
                writer.Write(azuki.Text);
                writer.Close();

                targetTab.Text = Path.GetFileName(filepath);
                tabInfos[targetTab].Modify = false;
            };
            contextMenuStrip.Opening += delegate(object sender, CancelEventArgs e)
            {
                ContextMenuStrip menu = (ContextMenuStrip)sender;
                if (this.clickedTabPage != null)
                {
                    menu.Items.Clear();
                    if (tabInfos[clickedTabPage].Type == TabInfo.TabTypeAzuki)
                    {

                        filepath = tabInfos[clickedTabPage].Uri;
                        // if (filepath.EndsWith(".js") || filepath.EndsWith(".html") || filepath.EndsWith(".txt"))
                        // {
                        menu.Items.Add(menuSave);
                        // }
                    }
                    menu.Items.Add(menuClose);
                    targetTab = this.clickedTabPage;
                    this.clickedTabPage = null;
                }
                else
                {
                    e.Cancel = true;
                }
            };
            contextMenuStrip.Items.Add(new ToolStripMenuItem());
            tabControl.ContextMenuStrip = this.contextMenuStrip;

        }

        public TabPage AddEditorTab(string fullpath)
        {
            StreamReader sr = new StreamReader(fullpath);
            string text = sr.ReadToEnd();
            sr.Close();

            // 新しいタブを追加して開く
            TabPage tabPage = new TabPage();
            SetTabText(tabPage, Path.GetFileName(fullpath), false);
            Sgry.Azuki.WinForms.AzukiControl azuki = new Sgry.Azuki.WinForms.AzukiControl();
            azuki.Text = text;
            azuki.ClearHistory();
            azuki.TabWidth = 4;
            azuki.AutoIndentHook = Sgry.Azuki.AutoIndentHooks.CHook;
            azuki.Dock = DockStyle.Fill;
            if (fullpath.EndsWith(".js"))
            {
                azuki.Highlighter = Sgry.Azuki.Highlighter.Highlighters.JavaScript;
            }
            else if (fullpath.EndsWith(".html") || fullpath.EndsWith(".htm"))
            {
                azuki.Highlighter = Sgry.Azuki.Highlighter.Highlighters.Xml;
            }
            tabPage.Controls.Add(azuki);


            azuki.TextChanged += delegate
            {
                SetTabText(tabPage, Path.GetFileName(fullpath), true);
//                tabPage.Text = Path.GetFileName(fullpath) + " *";
                tabInfos[tabPage].Modify = true;
            };

            this.tabControl.TabPages.Add(tabPage);
            this.tabInfos[tabPage] = new TabInfo();
            this.tabInfos[tabPage].Type = TabInfo.TabTypeAzuki;
            this.tabInfos[tabPage].Uri = fullpath;
            this.tabInfos[tabPage].Modify = false;

            return tabPage;
        }

        public TabPage AddImageTab(string fullpath)
        {
            // 新しいタブを追加して開く
            TabPage tabPage = new TabPage();
            SetTabText(tabPage, Path.GetFileName(fullpath), false);
            Panel panel = new Panel();
            panel.Height = 100;
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Dock = DockStyle.Bottom;
            tabPage.Controls.Add(panel);

            Label label = new Label();
            label.AutoSize = true;
            label.Top = 14;
            label.Text = "横幅, 縦幅";
            panel.Controls.Add(label);

            TextBox textBox = new TextBox();
            textBox.Top = 10;
            textBox.Left = 70;
            textBox.ReadOnly = true;

            panel.Controls.Add(textBox);

            PictureBox picturebox = new PictureBox();
            Console.WriteLine(fullpath);
            picturebox.ImageLocation = fullpath;
            picturebox.Dock = DockStyle.None;
            picturebox.SizeMode = PictureBoxSizeMode.Zoom;
            int scaleLevel = 0;
            tabPage.Layout += delegate(Object sender, LayoutEventArgs e)
            {
                picturebox.Left = (tabPage.Width - picturebox.Width) / 2;
                picturebox.Top = (tabPage.Height - panel.Height - picturebox.Height) / 2;
            };
            tabPage.MouseEnter += delegate
            {
                tabPage.Focus();

            };
            picturebox.MouseEnter += delegate
            {
                tabPage.Focus();

            };
            tabPage.MouseWheel += delegate(object sender, MouseEventArgs e)
            {
                scaleLevel += e.Delta / 120;
                double scale = 1.0;
                if (scaleLevel > 0)
                {
                    for (int i = 0; i < scaleLevel; i++)
                    {
                        scale += scale / 10;
                    }
                }
                else
                {
                    for (int i = 0; i < -scaleLevel; i++)
                    {
                        scale -= scale / 10;
                    }
                }

                picturebox.Width = (int)(picturebox.Image.Width * scale);
                picturebox.Height = (int)(picturebox.Image.Height * scale);
            };

            picturebox.BackgroundImage = System.Drawing.Image.FromFile(Path.Combine(Application.StartupPath, @"Data\Resources\transback.png"));
            tabPage.Controls.Add(picturebox);
            this.tabControl.TabPages.Add(tabPage);
            this.tabInfos[tabPage] = new TabInfo();
            this.tabInfos[tabPage].Type = TabInfo.TabTypeImage;
            this.tabInfos[tabPage].Uri = fullpath;

            picturebox.LoadCompleted += delegate(Object sender, AsyncCompletedEventArgs e)
            {
                picturebox.Width = picturebox.Image.Width;
                picturebox.Height = picturebox.Image.Height;
                textBox.Text = picturebox.Image.Width + ", " + picturebox.Image.Height;
            };

            return tabPage;
        }

        public void OpenWebBrowserTab(string url)
        {
            TabPage tabPage = new TabPage();
            tabPage.Text = url;
            WebBrowser webbrowser = new WebBrowser();
            webbrowser.ObjectForScripting = this;
            // webbrowser.ScriptErrorsSuppressed = false;
            webbrowser.Dock = DockStyle.Fill;
            webbrowser.Navigate(url);
            webbrowser.DocumentCompleted += delegate
            {
                webbrowser.Document.InvokeScript("javascript:window.onerror = function(message, url, lineNumber) { window.external.ErrorHandler(message, url, lineNumber);return true;};");
            };
            Console.WriteLine(url);
            tabPage.Controls.Add(webbrowser);
            tabInfos[tabPage] = new TabInfo();
            tabInfos[tabPage].Type = TabInfo.TabTypeBrowser;
            tabInfos[tabPage].Uri = url;
            tabControl.Controls.Add(tabPage);
        }

        private void AddStartpage()
        {
            TabPage tabPage = new TabPage();
            WebBrowser webbrowser = new WebBrowser();
            webbrowser.Dock = DockStyle.Fill;
            string path = Path.Combine(Application.StartupPath, @"Data\Pages\startpage.html");
            webbrowser.Navigate(path);
            Console.WriteLine(path);
            tabPage.Controls.Add(webbrowser);
            tabInfos[tabPage] = new TabInfo();
            tabInfos[tabPage].Type = TabInfo.TabTypeBrowser;
            tabControl.Controls.Add(tabPage);
        }

        public void SaveAllFiles()
        {
            foreach (var pair in tabInfos)
            {
                if (pair.Value.Type == TabInfo.TabTypeAzuki)
                {
                    Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)pair.Key.Controls[0];
                    StreamWriter writer = new StreamWriter(pair.Value.Uri);
                    writer.Write(azuki.Text);
                    writer.Close();
                }
            }
        }

        public void CloseAllTab()
        {
            List<TabPage> removes = new List<TabPage>();
            foreach (var pair in tabInfos)
            {
                tabControl.TabPages.Remove(pair.Key);
                removes.Add(pair.Key);
            }
            foreach (TabPage removePage in removes)
            {
                tabInfos.Remove(removePage);
            }
        }
        public void SetTabText(TabPage tabPage, string text, bool modify)
        {
            if (!modify)
            {
                tabPage.Text = text;
            }
            else
            {
                tabPage.Text = text + " *";
            }
        }
    }
}
