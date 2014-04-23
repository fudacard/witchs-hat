using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace WitchsHat
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class Form1 : Form, IRemoteObject
    {
        // 表示しているタブの情報
        Dictionary<TabPage, TabInfo> tabInfos = new Dictionary<TabPage, TabInfo>();
        // ツリーノードがディレクトリかどうか
        Dictionary<int, bool> IsDirectory = new Dictionary<int, bool>();
        TabPage clickedTabPage = null;
        ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        // テンポラリプロジェクトかどうか
        bool tempproject;
        // テンポラリプロジェクトを遅延生成するフラグ
        bool CreateLater;
        bool tempprojectModify;
        EnvironmentSettings settings;
        WHServer server;
        ProjectProperty CurrentProject;

        private delegate void StartupNextInstanceDelegate(params object[] parameters);

        public Form1()
        {
            InitializeComponent();
            this.tabControl1.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                this.clickedTabPage = null;
                for (int i = 0; i < tabControl1.TabCount; i++)
                {
                    if (tabControl1.GetTabRect(i).Contains(e.X, e.Y))
                    {
                        this.clickedTabPage = (TabPage)tabControl1.GetControl(i);
                        tabControl1.SelectedTab = this.clickedTabPage;
                    }
                }
            };
            this.tabControl1.MouseUp += delegate(object sender, MouseEventArgs e)
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
            string filepath = null;
            TabPage targetTab = null;

            menuClose.Click += delegate(object sender1, EventArgs e1)
            {
                // タブを閉じる
                if (targetTab.Text.EndsWith("*"))
                {
                    DialogResult result = MessageBox.Show("変更を保存しますか？", "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.Yes)
                    {
                        // ファイル保存
                        Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)targetTab.Controls[0];
                        StreamWriter writer = new StreamWriter(filepath);
                        writer.Write(azuki.Text);
                        writer.Close();

                        this.tabControl1.TabPages.Remove(targetTab);
                        tabInfos.Remove(targetTab);
                    }
                    else if (result == DialogResult.No)
                    {
                        this.tabControl1.TabPages.Remove(targetTab);
                        tabInfos.Remove(targetTab);
                    }
                }
                else
                {
                    this.tabControl1.TabPages.Remove(targetTab);
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
            this.tabControl1.ContextMenuStrip = this.contextMenuStrip;

            settings = new EnvironmentSettings();
            // 設定ファイル読み込み
            readConfig();

            // サーバスタート
            if (settings.ServerEnable)
            {
                server = new WHServer();
                server.Start(settings.ServerPort);
            }

            string[] cmds;
            cmds = System.Environment.GetCommandLineArgs();
            if (settings.TempProjectEnable && cmds.Length == 1)
            {
                tempproject = true;
                if (HasEnchantjs())
                {
                    CreateLater = false;
                    CreateTemporaryProject();
                }
                else
                {
                    CreateLater = true;
                }
            }
            else if (cmds.Length > 1)
            {
                string filePath = cmds[1];
                OpenFile(filePath);
            }

            // AddStartpage();
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
            tabControl1.Controls.Add(tabPage);
        }

        private void CreateProject(string projectName, string projectDir, int projectTemplate)
        {
            bool create = true;

            create = NewProjectCheck();
            if (create)
            {
                CloseAllTab();
            }
            else
            {
                return;
            }
            string sourceDirName = Path.Combine(Application.StartupPath, @"Data\Templates\HelloWorldProject");
            string[] extra = { };

            System.IO.Directory.CreateDirectory(projectDir);

            switch (projectTemplate)
            {
                case 0:
                    extra = new string[1];
                    extra[0] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Witchs Hat", "enchant.js", "images", "chara1.png");
                    sourceDirName = Path.Combine(Application.StartupPath, @"Data\Templates\EnchantProject");
                    break;
                case 1:
                    sourceDirName = Path.Combine(Application.StartupPath, @"Data\Templates\BlankProject");
                    break;
                case 2:
                    sourceDirName = Path.Combine(Application.StartupPath, @"Data\Templates\HelloWorldProject");
                    break;
            }

            string[] files = System.IO.Directory.GetFiles(sourceDirName);

            foreach (string file in files)
            {
                Console.WriteLine(file);
                System.IO.File.Copy(file, Path.Combine(projectDir, Path.GetFileName(file)), true);

            }
            foreach (string file in extra)
            {
                Console.WriteLine(file);
                System.IO.File.Copy(file, Path.Combine(projectDir, Path.GetFileName(file)), true);

            }

            ProjectProperty pp = new ProjectProperty();
            pp.Name = projectName;
            pp.Dir = projectDir;
            pp.Encoding = "UTF-8";
            pp.HtmlPath = "index.html";
            ProjectProperty.WriteProjectProperty(pp);

            CurrentProject = pp;

            UpdateFileTree();

            // main.jsを開く
            if (File.Exists(Path.Combine(CurrentProject.Dir, "main.js")))
            {
                OpenTab(Path.Combine(CurrentProject.Dir, "main.js"));
            }
        }

        /// <summary>
        /// ファイルの変更を確認し保存と処理の続行の可否を返す
        /// </summary>
        /// <returns></returns>
        private bool NewProjectCheck()
        {
            bool continueFlag = true;


            if (tempproject && tempprojectModify)
            {

                DialogResult result = MessageBox.Show("プロジェクトが変更されています。\r\nプロジェクトを保存しますか？", "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                {
                    // 保存ダイアログ表示
                    continueFlag = false;
                    SaveProjectFromTemp f = new SaveProjectFromTemp();
                    f.OkClicked += delegate(string projectName, string projectDir)
                    {
                        // 保存する
                        SaveTempProject(projectName, projectDir);
                        this.Close();
                    };
                    f.ShowDialog(this);

                }
                else if (result == DialogResult.Cancel)
                {
                    continueFlag = false;
                }

            }
            else
            {
                bool modify = false;
                foreach (var pair in tabInfos)
                {
                    if (pair.Value.Modify)
                    {
                        modify = true;
                        break;
                    }
                }
                if (modify)
                {
                    DialogResult result = MessageBox.Show("ファイルが変更されています。\r\n保存しますか？", "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.Yes)
                    {
                        foreach (var pair in tabInfos)
                        {
                            Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)pair.Key.Controls[0];
                            StreamWriter writer = new StreamWriter(pair.Value.Uri);
                            writer.Write(azuki.Text);
                            writer.Close();
                        }
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        continueFlag = false;
                    }
                }
            }

            return continueFlag;
        }

        private void CreateTemporaryProject()
        {
            CreateLater = false;

            int projectTemplate = 2;
            if (HasEnchantjs())
            {
                projectTemplate = 0;
            }
            CreateProject("NoTitleProject", Path.Combine(Path.GetTempPath(), "Witchs Hat", "NoTitleProject"), projectTemplate);

        }

        private void readConfig()
        {

            // 環境設定ファイル読み込み
            settings = EnvironmentSettings.ReadEnvironmentSettings(Path.Combine(Application.StartupPath, "Data\\defaultsettings.xml"));
            if (settings.ProjectsPath == "")
            {
                settings.ProjectsPath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Witchs Hat Projects");
            }
            // ユーザー環境設定ファイル読み込み
            string userconfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Witchs Hat", "settings.xml");
            if (File.Exists(userconfig))
            {
                settings.ReadUserSettings(userconfig);
            }

        }

        public void StartupNextInstance(params object[] parameters)
        {
            this.Activate();
            string[] args = (string[])parameters[0];

            //MessageBox.Show("既に起動しています。引数1の数：" + args.Length.ToString());
            if (parameters.Length > 1)
            {
                OpenFile((string)parameters[1]);
            }
        }


        /// <summary>
        /// ファイル - 開くメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenProjectOrFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "プロジェクト ファイル (*.whprj)|*.whprj|javascriptファイル(*.js)|*.js|htmlファイル(*.html;*.html)|*.html;*.htm|画像ファイル(*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif|テキストファイル(*.txt)|*.txt|すべてのファイル(*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                OpenFile(ofd.FileName);
            }
        }

        private void OpenFile(string filePath)
        {
            if (filePath.EndsWith(".whprj"))
            {
                bool open = true;
                open = NewProjectCheck();
                if (open)
                {
                    CloseAllTab();

                    // プロジェクト設定ファイル読み込み
                    CurrentProject = ProjectProperty.ReadProjectProperty(filePath);
                    this.Text = CurrentProject.Name + " - Witch's Hat";

                    UpdateFileTree();
                }
            }
            else
            {
                OpenTab(filePath);
                this.tabControl1.SelectedTab = tabInfos.FirstOrDefault(x => x.Value.Uri == filePath).Key;
            }
        }

        private void UpdateFileTree()
        {
            this.Text = CurrentProject.Name + " - Witch's Hat";
            if (server != null)
            {
                server.RootDir = CurrentProject.Dir;
            }

            // プロジェクトツリー更新
            this.treeView1.Nodes.Clear();
            this.treeView1.Nodes.Add(CurrentProject.Dir, CurrentProject.Name);
            IsDirectory = new Dictionary<int, bool>();
            // フォルダ一覧追加
            string[] dirs = System.IO.Directory.GetDirectories(CurrentProject.Dir);
            foreach (string dir in dirs)
            {
                TreeNode treenode = this.treeView1.Nodes[0].Nodes.Add(dir, System.IO.Path.GetFileName(dir));
                IsDirectory[treenode.GetHashCode()] = true;
                // ファイル一覧追加
                string[] files0 = System.IO.Directory.GetFiles(dir);
                foreach (string file in files0)
                {
                    if (!file.EndsWith(".whprj"))
                    {
                        TreeNode treenode1 = treenode.Nodes.Add(file, System.IO.Path.GetFileName(file));
                        IsDirectory[treenode1.GetHashCode()] = false;
                    }
                }
            }
            // ファイル一覧追加
            string[] files = System.IO.Directory.GetFiles(CurrentProject.Dir);
            foreach (string file in files)
            {
                if (!file.EndsWith(".whprj"))
                {
                    TreeNode treenode = this.treeView1.Nodes[0].Nodes.Add(file, System.IO.Path.GetFileName(file));
                    IsDirectory[treenode.GetHashCode()] = false;
                }
            }
            treeView1.ExpandAll();
        }


        /// <summary>
        /// 実行メニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunOnBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = Path.Combine(CurrentProject.Dir, CurrentProject.HtmlPath);
            if (File.Exists(path))
            {
                try
                {

                    if (settings.ServerEnable)
                    {
                        Process.Start(settings.RunBrowser, "http://localhost:" + settings.ServerPort + "/" + CurrentProject.HtmlPath);
                        //OpenWebBrowserTab("http://localhost:" + settings.ServerPort + "/"+ CurrentProject.HtmlPath);
                    }
                    else
                    {
                        Process.Start(settings.RunBrowser, path);
                    }
                }
                catch (Exception e1)
                {
                    MessageBox.Show("ブラウザ " + settings.RunBrowser + " を開く際にエラーが発生しました。\r\n" + e1.Message);
                }
            }
        }

        /// <summary>
        ///  ツリーノードダブルクリック時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeViewHitTestInfo ht = treeView1.HitTest(e.Location);
            if (e.Node != treeView1.Nodes[0])
            {
                Console.WriteLine(e.Node.Text);
                if (!IsDirectory[e.Node.GetHashCode()])
                {
                    // ファイル
                    string filepath = e.Node.Name;
                    OpenTab(filepath);
                    this.tabControl1.SelectedTab = tabInfos.FirstOrDefault(x => x.Value.Uri == filepath).Key;
                }

            }
        }

        private void OpenTab(string fullpath)
        {
            string filename = Path.GetFileName(fullpath);
            string pathlower = fullpath.ToLower();
            if (!tabInfos.Any(x => x.Value.Uri == fullpath))
            {
                if (pathlower.EndsWith(".js") || pathlower.EndsWith(".txt") || pathlower.EndsWith(".html") || pathlower.EndsWith(".htm"))
                {
                    StreamReader sr = new StreamReader(fullpath);
                    string text = sr.ReadToEnd();
                    sr.Close();

                    // 新しいタブを追加して開く
                    TabPage tabPage = new TabPage(filename);
                    Sgry.Azuki.WinForms.AzukiControl azuki = new Sgry.Azuki.WinForms.AzukiControl();
                    azuki.Text = text;
                    azuki.ClearHistory();
                    azuki.TabWidth = 4;
                    azuki.AutoIndentHook = Sgry.Azuki.AutoIndentHooks.CHook;
                    azuki.Dock = DockStyle.Fill;
                    azuki.Font = new Font(settings.FontName, settings.FontSize);
                    if (fullpath.EndsWith(".js"))
                    {
                        azuki.Highlighter = Sgry.Azuki.Highlighter.Highlighters.JavaScript;
                    }
                    else if (fullpath.EndsWith(".html"))
                    {
                        azuki.Highlighter = Sgry.Azuki.Highlighter.Highlighters.Xml;
                    }

                    Console.WriteLine(azuki.Font.SizeInPoints);
                    azuki.SetKeyBind(Keys.F | Keys.Control, delegate
                    {
                        FindToolStripMenuItem_Click(null, null);
                    });
                    azuki.SetKeyBind(Keys.H | Keys.Control, delegate
                    {
                        ReplaceToolStripMenuItem_Click(null, null);
                    });
                    azuki.SetKeyBind(Keys.S | Keys.Control, delegate
                    {
                        SaveToolStripMenuItem_Click(null, null);
                    });
                    tabPage.Controls.Add(azuki);
                    azuki.TextChanged += delegate
                    {
                        tabPage.Text = filename + " *";
                        tabInfos[tabPage].Modify = true;
                        if (tempproject)
                        {
                            tempprojectModify = true;
                        }
                    };
                    this.tabControl1.TabPages.Add(tabPage);
                    this.tabInfos[tabPage] = new TabInfo();
                    this.tabInfos[tabPage].Type = TabInfo.TabTypeAzuki;
                    this.tabInfos[tabPage].Uri = fullpath;
                    this.tabInfos[tabPage].Modify = false;
                }
                else if (pathlower.EndsWith(".jpg") || pathlower.EndsWith(".jpeg") || pathlower.EndsWith(".png") || pathlower.EndsWith(".gif") || pathlower.EndsWith(".bmp"))
                {
                    // 新しいタブを追加して開く
                    TabPage tabPage = new TabPage(filename);
                    PictureBox picturebox = new PictureBox();
                    Console.WriteLine(fullpath);
                    picturebox.ImageLocation = fullpath;
                    picturebox.Dock = DockStyle.None;
                    picturebox.SizeMode = PictureBoxSizeMode.Zoom;
                    int scaleLevel = 0;
                    tabPage.Layout += delegate(Object sender, LayoutEventArgs e)
                    {
                        picturebox.Left = (tabPage.Width - picturebox.Width) / 2;
                        picturebox.Top = (tabPage.Height - picturebox.Height) / 2;
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
                        Console.WriteLine(scale);

                        picturebox.Width = (int)(picturebox.Image.Width * scale);
                        picturebox.Height = (int)(picturebox.Image.Height * scale);
                    };

                    picturebox.BackgroundImage = System.Drawing.Image.FromFile(Path.Combine(Application.StartupPath, @"Data\Resources\transback.png"));
                    tabPage.Controls.Add(picturebox);
                    this.tabControl1.TabPages.Add(tabPage);
                    this.tabInfos[tabPage] = new TabInfo();
                    this.tabInfos[tabPage].Type = TabInfo.TabTypeImage;
                    this.tabInfos[tabPage].Uri = fullpath;

                    picturebox.LoadCompleted += delegate(Object sender, AsyncCompletedEventArgs e)
                    {
                        picturebox.Width = picturebox.Image.Width;
                        picturebox.Height = picturebox.Image.Height;
                    };
                }
            }
        }

        private void OpenWebBrowserTab(string url)
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
            tabControl1.Controls.Add(tabPage);
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            bool close = true;
            bool fileModify = false;
            foreach (var tabInfo in tabInfos)
            {
                if (tabInfo.Value.Modify)
                {
                    fileModify = true;
                    break;
                }
            }

            if (tempprojectModify)
            {
                DialogResult result = MessageBox.Show("プロジェクトが変更されています。\r\nプロジェクトを保存しますか？", "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                {
                    // 保存ダイアログ表示
                    e.Cancel = true;
                    close = false;
                    SaveProjectFromTemp f = new SaveProjectFromTemp();
                    f.OkClicked += delegate(string projectName, string projectDir)
                    {
                        // 保存する
                        SaveTempProject(projectName, projectDir);
                        this.Close();
                    };
                    f.ShowDialog(this);
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    close = false;
                }
            }
            else if (fileModify)
            {
                DialogResult result = MessageBox.Show("ファイルが変更されています。\r\nファイルを保存しますか？", "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                {
                    foreach (var pair in tabInfos)
                    {
                        if (pair.Value.Modify)
                        {
                            Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)pair.Key.Controls[0];
                            StreamWriter writer = new StreamWriter(pair.Value.Uri);
                            writer.Write(azuki.Text);
                            writer.Close();
                        }
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    close = false;
                }
            }
            if (close)
            {
                if (server != null && server.IsRunning())
                {
                    server.Stop();
                }
                if (tempproject)
                {
                    Directory.Delete(CurrentProject.Dir, true);
                }
                Program.mutex.ReleaseMutex();
            }

        }

        private void SaveTempProject(string projectName, string projectDir)
        {
            // ファイルをすべて保存する
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

            // プロジェクトをコピー
            System.IO.Directory.CreateDirectory(projectDir);
            // ファイルコピー
            string sourceDirName = CurrentProject.Dir;
            string[] files = System.IO.Directory.GetFiles(sourceDirName);
            foreach (string file in files)
            {
                Console.WriteLine(file);
                string filename = System.IO.Path.GetFileName(file);
                if (System.IO.Path.GetFileName(file) == CurrentProject.Name + ".whprj")
                {
                    //                                filename = f.ProjectName + ".whprj";
                    CurrentProject.Name = projectName;
                    CurrentProject.Dir = projectDir;
                    ProjectProperty.WriteProjectProperty(CurrentProject);
                }
                else
                {
                    System.IO.File.Copy(file,
                       projectDir + "\\" + filename, true);
                }

            }
            if (!File.Exists(Path.Combine(projectDir, projectName + ".whprj")))
            {
                // プロジェクト設定ファイル生成
                CurrentProject.Name = projectName;
                CurrentProject.Dir = projectDir;
                ProjectProperty.WriteProjectProperty(CurrentProject);
            }

            tempprojectModify = false;
            if (tempproject)
            {
                Directory.Delete(CurrentProject.Dir, true);
            }
            tempproject = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            // enchant.jsがあるかどうかチェック
            if (!HasEnchantjs())
            {
                DialogResult result = MessageBox.Show("enchant.jsがありません。\r\nenchant.jsをダウンロードしますか？", "", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.OK)
                {
                    EnchantjsDownloadForm f = new EnchantjsDownloadForm();
                    f.enchantjsUrl = settings.EnchantjsUrl;
                    f.FormClosed += delegate
                    {
                        if (CreateLater)
                        {
                            CreateTemporaryProject();
                        }
                    };
                    f.ShowDialog(this);
                }
                else
                {
                    if (CreateLater)
                    {
                        CreateTemporaryProject();
                    }
                }
            }
        }

        private bool HasEnchantjs()
        {
            string enchantjsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Witchs Hat", "enchant.js", "build", "enchant.js");
            return File.Exists(enchantjsPath);
        }

        private void OpenOptionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingForm f = new SettingForm();
            f.settings = settings;
            f.OkClicked = delegate
            {
                // 設定保存
                string outputdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Witchs Hat");
                Directory.CreateDirectory(outputdir);
                settings.WriteEnvironmentSettings(Path.Combine(outputdir, "settings.xml"));

                if (settings.ServerEnable && (server == null || !server.IsRunning()))
                {
                    // サーバー起動
                    if (server == null)
                    {
                        server = new WHServer();
                    }
                    server.RootDir = CurrentProject.Dir;
                    server.Start(settings.ServerPort);
                }
                else if (!settings.ServerEnable && (server != null && server.IsRunning()))
                {
                    // サーバー終了
                    server.Stop();
                }
                if (server != null && server.IsRunning() && settings.ServerPort != server.Port)
                {
                    // ポート変更
                    server.Start(settings.ServerPort);
                }
                // フォント変更
                Font font = new Font(settings.FontName, settings.FontSize);
                foreach (var pair in tabInfos)
                {
                    if (pair.Value.Type == TabInfo.TabTypeAzuki)
                    {
                        ((Sgry.Azuki.WinForms.AzukiControl)pair.Key.Controls[0]).Font = font;
                    }
                }
            };
            f.ShowDialog(this);
        }

        private void OpenReferenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(settings.Browser, "http://enchantjs.com/ja/tutorial/lets-start-enchant-js/");
            }
            catch (Exception e1)
            {
                MessageBox.Show("ブラウザ " + settings.Browser + " を開く際にエラーが発生しました。\r\n" + e1.Message);
            }
        }

        private void OpenApidocsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(settings.Browser, "http://wise9.github.io/enchant.js/doc/plugins/ja/index.html");
            }
            catch (Exception e1)
            {
                MessageBox.Show("ブラウザ " + settings.Browser + " を開く際にエラーが発生しました。\r\n" + e1.Message);
            }
        }


        private void toolStripMenuItem1_DropDownOpening(object sender, EventArgs e)
        {
            Sgry.Azuki.WinForms.AzukiControl azuki = null;
            if (tabControl1.SelectedTab != null && tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
            }

            UndoToolStripMenuItem.Enabled = (azuki != null && azuki.CanUndo);
            RedoToolStripMenuItem.Enabled = (azuki != null && azuki.CanRedo);
            CutToolStripMenuItem.Enabled = (azuki != null && azuki.CanCut);
            CopyToolStripMenuItem.Enabled = (azuki != null && azuki.CanCopy);
            PasteToolStripMenuItem.Enabled = (azuki != null && azuki.CanPaste);
            DeleteToolStripMenuItem.Enabled = (azuki != null);
            SelectAllToolStripMenuItem.Enabled = (azuki != null);
        }

        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Undo();
            }
        }

        private void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Redo();
            }
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Cut();
            }
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Copy();
            }
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Paste();
            }
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
            azuki.Delete();
        }

        private void SelectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
            azuki.SelectAll();
        }

        private void FindToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FindForm f = new FindForm();
            f.MainForm = this;
            f.Show(this);
        }
        public Sgry.Azuki.WinForms.AzukiControl GetActiveAzuki()
        {
            Sgry.Azuki.WinForms.AzukiControl azuki = null;
            if (tabControl1.SelectedTab != null && tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
            }
            return azuki;
        }

        private void ReplaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReplaceForm f = new ReplaceForm();
            f.MainForm = this;
            f.Show(this);
        }

        private void CreateProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateProjectForm f = new CreateProjectForm();
            f.ProjectsPath = settings.ProjectsPath;
            f.OkClicked = delegate(string projectName, string projectDir, int projectTemplate)
            {
                CloseProjectToolStripMenuItem_Click(sender, e);

                CreateProject(projectName, projectDir, projectTemplate);

                this.Text = projectName + " - Witch's Hat";
                CurrentProject = new ProjectProperty();
                CurrentProject.Name = projectName;
                CurrentProject.Dir = projectDir;
                CurrentProject.HtmlPath = "index.html";
                CurrentProject.Encoding = settings.Encoding;
                UpdateFileTree();
            };
            f.ShowDialog(this);
        }

        private void CreateFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateFileForm f = new CreateFileForm();
            f.ProjectDir = CurrentProject.Dir;
            f.OkClicked = delegate(string filepath)
            {
                using (System.IO.FileStream hStream = System.IO.File.Create(filepath))
                {
                    if (hStream != null)
                    {
                        hStream.Close();
                    }
                }
                OpenTab(filepath);
                this.tabControl1.SelectedTab = tabInfos.FirstOrDefault(x => x.Value.Uri == filepath).Key;
                tempprojectModify = true;
                UpdateFileTree();
            };
            f.ShowDialog(this);
        }

        private void CreateFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateFolderForm f = new CreateFolderForm();
            f.ProjectDir = CurrentProject.Dir;
            f.OkClicked = delegate(string folderpath)
            {
                Directory.CreateDirectory(folderpath);
                tempprojectModify = true;
                UpdateFileTree();
            };
            f.ShowDialog(this);
        }

        private void EndToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void FileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            Sgry.Azuki.WinForms.AzukiControl azuki = GetActiveAzuki();
            if (azuki != null)
            {
                SaveToolStripMenuItem.Enabled = true;
                SaveAsToolStripMenuItem.Enabled = true;

            }
            else
            {
                SaveToolStripMenuItem.Text = "保存";
                SaveToolStripMenuItem.Enabled = false;
                SaveAsToolStripMenuItem.Text = "名前を付けて保存";
                SaveAsToolStripMenuItem.Enabled = false;
            }
            if (tabControl1.TabPages.Count > 0)
            {
                CloseToolStripMenuItem.Enabled = true;
            }
            else
            {
                CloseToolStripMenuItem.Enabled = false;
            }
            if (CurrentProject != null)
            {
                CreateFileToolStripMenuItem.Enabled = true;
                CreateFolderToolStripMenuItem.Enabled = true;
                CloseProjectToolStripMenuItem.Enabled = true;
            }
            else
            {
                CreateFileToolStripMenuItem.Enabled = false;
                CreateFolderToolStripMenuItem.Enabled = false;
                CloseProjectToolStripMenuItem.Enabled = false;
            }
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabInfos[tabControl1.SelectedTab].Modify)
            {
                string filename = Path.GetFileName(tabInfos[tabControl1.SelectedTab].Uri);
                DialogResult result = MessageBox.Show(filename + "は変更されています。\r\n" + filename + "を保存しますか？", "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                {
                    Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                    StreamWriter writer = new StreamWriter(tabInfos[tabControl1.SelectedTab].Uri);
                    writer.Write(azuki.Text);
                    writer.Close();
                }
                else if (result == DialogResult.No)
                {
                    tabInfos.Remove(tabControl1.SelectedTab);
                    tabControl1.TabPages.Remove(tabControl1.SelectedTab);
                }
            }
            else
            {
                tabInfos.Remove(tabControl1.SelectedTab);
                tabControl1.TabPages.Remove(tabControl1.SelectedTab);
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
            {

                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                StreamWriter writer = new StreamWriter(sfd.FileName);
                writer.Write(azuki.Text);
                writer.Close();
                tabInfos[tabControl1.SelectedTab].Uri = sfd.FileName;
            }
        }

        private void CloseProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool close = true;

            close = NewProjectCheck();

            if (close)
            {

                this.Text = "Witch's Hat";
                treeView1.Nodes.Clear();
                CloseAllTab();

                CurrentProject = null;
                if (tempproject)
                {
                    tempproject = false;
                }
            }
        }

        private void CloseAllTab()
        {
            foreach (var pair in tabInfos)
            {
                if (pair.Value.Uri.StartsWith(CurrentProject.Dir))
                {
                    tabControl1.TabPages.Remove(pair.Key);
                }
            }
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
            StreamWriter writer = new StreamWriter(tabInfos[tabControl1.SelectedTab].Uri);
            writer.Write(azuki.Text);
            writer.Close();

            tabControl1.SelectedTab.Text = Path.GetFileName(tabInfos[tabControl1.SelectedTab].Uri);
            tabInfos[tabControl1.SelectedTab].Modify = false;
        }

        public void ErrorHandler(string message, string url, int lineNumber)
        {
            Console.WriteLine(message);
            Console.WriteLine(url);
            Console.WriteLine(lineNumber);

            string filename = url.Substring(url.LastIndexOf('/') + 1);
            string fullpath = Path.Combine(CurrentProject.Dir, filename);
            var tab = tabInfos.FirstOrDefault(x => x.Value.Uri == fullpath).Key;
            if (tab != null)
            {
                tabControl1.SelectedTab = tab;
            }
            else
            {
                OpenTab(fullpath);
            }
            Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
            azuki.Document.SetCaretIndex(lineNumber - 1, 0);
        }

        private void ProjectPropertyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProjectPropertyForm f = new ProjectPropertyForm();
            f.projectProperty = CurrentProject;
            f.OkClicked = delegate
            {

            };
            f.Show();
        }

        private void ProjecttoolStripMenuItem2_DropDownOpening(object sender, EventArgs e)
        {
            if (CurrentProject != null)
            {
                ImportFileToolStripMenuItem.Enabled = true;
                ProjectPropertyToolStripMenuItem.Enabled = true;
            }
            else
            {
                ImportFileToolStripMenuItem.Enabled = false;
                ProjectPropertyToolStripMenuItem.Enabled = false;
            }
        }

        private void RunToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (CurrentProject != null)
            {
                RunOnBrowserToolStripMenuItem.Enabled = true;
            }
            else
            {
                RunOnBrowserToolStripMenuItem.Enabled = false;
            }

        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                treeView1.SelectedNode = e.Node;

                string pathlower = e.Node.Name.ToLower();
                if (e.Node.Name == CurrentProject.Dir)
                {
                    ProjectContextMenuStrip.Show(treeView1, e.Location);
                }
                else if (Directory.Exists(e.Node.Name))
                {
                    FolderContextMenuStrip.Show(treeView1, e.Location);
                }
                else if (pathlower.EndsWith(".js") || pathlower.EndsWith(".css") || pathlower.EndsWith(".md"))
                {
                    TextContextMenuStrip.Show(treeView1, e.Location);
                }
                else if (pathlower.EndsWith(".html") || pathlower.EndsWith(".htm"))
                {
                    HtmlContextMenuStrip.Show(treeView1, e.Location);
                }
                else if (pathlower.EndsWith(".png") || pathlower.EndsWith(".jpg") || pathlower.EndsWith(".jpeg") || pathlower.EndsWith(".gif"))
                {
                    ImageContextMenuStrip.Show(treeView1, e.Location);
                }
            }
        }

        private void OpenContextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string filepath = treeView1.SelectedNode.Name;
            OpenTab(filepath);
            this.tabControl1.SelectedTab = tabInfos.FirstOrDefault(x => x.Value.Uri == filepath).Key;
        }

        private void DeleteContextToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string filepath = treeView1.SelectedNode.Name;
            var tabPage = tabInfos.FirstOrDefault(x => x.Value.Uri == filepath).Key;
            if (tabPage != null)
            {
                this.tabControl1.SelectedTab = tabInfos.FirstOrDefault(x => x.Value.Uri == filepath).Key;
            }

            DialogResult result = MessageBox.Show(Path.GetFileName(filepath) + "を削除しますか？", "", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
            if (result == DialogResult.OK)
            {
                // タブで開いていたら閉じる
                if (tabPage != null)
                {
                    tabControl1.TabPages.Remove(tabPage);
                }
                if (File.Exists(filepath))
                {
                    // ファイルを消す
                    File.Delete(filepath);
                }
                else
                {
                    Directory.Delete(filepath, true);
                    foreach (var pair in tabInfos)
                    {
                        if (pair.Value.Uri.StartsWith(filepath))
                        {
                            tabControl1.TabPages.Remove(pair.Key);
                            tabInfos.Remove(pair.Key);
                        }
                    }
                }
                UpdateFileTree();
                if (tempproject)
                {
                    tempprojectModify = true;
                }
            }
        }

        private void RenameContextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string oldFilePath = treeView1.SelectedNode.Name;
            RenameForm f = new RenameForm();
            f.OldFileName = Path.GetFileName(oldFilePath);
            f.OkClicked = delegate(string newFileName)
            {
                string dir = oldFilePath.Substring(0, oldFilePath.LastIndexOf('\\'));
                string newFilePath = Path.Combine(dir, newFileName);
                if (File.Exists(oldFilePath))
                {
                    File.Move(oldFilePath, newFilePath);
                }
                else
                {
                    Directory.Move(oldFilePath, newFilePath);
                    foreach (var pair in tabInfos)
                    {
                        if (pair.Value.Uri.StartsWith(oldFilePath))
                        {
                            pair.Value.Uri.Replace(oldFilePath, newFilePath);

                        }
                    }
                }
                var tabPage = tabInfos.FirstOrDefault(x => x.Value.Uri == oldFilePath).Key;
                tabInfos[tabPage].Uri = newFilePath;
                tabPage.Text = newFileName;
                UpdateFileTree();
                if (tempproject)
                {
                    tempprojectModify = true;
                }
            };
            f.ShowDialog(this);
        }

        private void PropertyContextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProjectPropertyToolStripMenuItem_Click(sender, e);
        }

        private void CreateFolderContextToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CreateFolderForm2 f = new CreateFolderForm2();
            f.OkClicked = delegate(string folderName)
            {
                Directory.CreateDirectory(Path.Combine(treeView1.SelectedNode.Name, folderName));
                UpdateFileTree();
                if (tempproject)
                {
                    tempprojectModify = true;
                }
            };
            f.ShowDialog(this);
        }

        private void ImportFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "javascriptファイル(*.js)|*.js|htmlファイル(*.html;*.html)|*.html;*.htm|画像ファイル(*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif|テキストファイル(*.txt)|*.txt|すべてのファイル(*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string destFileName = Path.Combine(CurrentProject.Dir, Path.GetFileName(ofd.FileName));
                File.Copy(ofd.FileName, destFileName);
                OpenFile(destFileName);
                UpdateFileTree();
                if (tempproject)
                {
                    tempprojectModify = true;
                }
            }
        }


        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (CurrentProject != null)
            {
                RunOnBrowserToolStripMenuItem_Click(sender, e);
            }
        }

        private void CreateProjectToolBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateProjectToolStripMenuItem_Click(sender, e);
        }

        private void CreateFileToolBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateFileToolStripMenuItem_Click(sender, e);
        }

        private void CreateFolderToolBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateFolderToolStripMenuItem_Click(sender, e);
        }

        private void CutToolBartoolStripButton3_Click(object sender, EventArgs e)
        {
            CutToolStripMenuItem_Click(sender, e);
        }

        private void CopyToolBartoolStripButton4_Click(object sender, EventArgs e)
        {
            CopyToolStripMenuItem_Click(sender, e);
        }

        private void PasteToolBartoolStripButton5_Click(object sender, EventArgs e)
        {
            PasteToolStripMenuItem_Click(sender, e);
        }

        private void UndoToolBartoolStripButton6_Click(object sender, EventArgs e)
        {
            UndoToolStripMenuItem_Click(sender, e);
        }

        private void RedoToolBartoolStripButton7_Click(object sender, EventArgs e)
        {
            RedoToolStripMenuItem_Click(sender, e);
        }

    }

}
