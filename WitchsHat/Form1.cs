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
        // プロジェクト名
        string projectName;
        // プロジェクトのフルパス
        string projectDir;
        // 開いているファイルのパスと表示しているタブ
        Dictionary<string, TabPage> tabOpenFiles = new Dictionary<string, TabPage>();
        Dictionary<TabPage, TabInfo> tabInfos = new Dictionary<TabPage, TabInfo>();
        // ツリーノードがディレクトリかどうか
        Dictionary<int, bool> IsDirectory = new Dictionary<int, bool>();
        // ツリーノードのファイルのパス
        Dictionary<int, string> FileFullPath = new Dictionary<int, string>();
        string browser;
        TabPage clickedTabPage = null;
        ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        bool serverEnable;
        string serverPort;

        // サーバスレッド
        System.Threading.Thread serverThread;
        // サーバー動作中
        bool running;
        // ビルドされたenchant.jsのURL
        string enchantjsUrl;
        System.Net.HttpListener listener;
        // テンポラリプロジェクトを生成するかどうか
        private bool TempProjectEnable;
        // テンポラリプロジェクトかどうか
        bool tempproject;
        // テンポラリプロジェクトを遅延生成するフラグ
        bool CreateLater;
        bool projectModify;
        string encoding = "utf-8";

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
                        tabOpenFiles.Remove(filepath);
                    }
                    else if (result == DialogResult.No)
                    {
                        this.tabControl1.TabPages.Remove(targetTab);
                        tabOpenFiles.Remove(filepath);
                    }
                }
                else
                {
                    this.tabControl1.TabPages.Remove(targetTab);
                    if (tabInfos[targetTab].Type != TabInfo.TabTypeBrowser)
                    {
                        tabOpenFiles.Remove(filepath);
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
            };
            contextMenuStrip.Opening += delegate(object sender, CancelEventArgs e)
            {
                ContextMenuStrip menu = (ContextMenuStrip)sender;
                if (this.clickedTabPage != null)
                {
                    menu.Items.Clear();
                    if (tabInfos[clickedTabPage].Type == TabInfo.TabTypeAzuki)
                    {

                        filepath = tabOpenFiles.FirstOrDefault(x => x.Value == this.clickedTabPage).Key;
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

            // 設定ファイル読み込み
            readConfig();

            // サーバスタート
            if (serverEnable)
            {
                serverThread = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadFunction));
                serverThread.Start();
            }

            string[] cmds;
            cmds = System.Environment.GetCommandLineArgs();
            if (TempProjectEnable && cmds.Length == 1)
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

            // AddStartpage();
        }

        private void AddStartpage()
        {
            TabPage tabPage = new TabPage();
            WebBrowser webbrowser = new WebBrowser();
            webbrowser.Dock = DockStyle.Fill;
            string path = Path.Combine(System.Environment.CurrentDirectory, @"Data\Pages\startpage.html");
            webbrowser.Navigate(path);
            Console.WriteLine(path);
            tabPage.Controls.Add(webbrowser);
            tabInfos[tabPage] = new TabInfo();
            tabInfos[tabPage].Type = TabInfo.TabTypeBrowser;
            tabControl1.Controls.Add(tabPage);
        }

        private void CreateTemporaryProject()
        {
            CreateLater = false;
            string path = Path.Combine(Path.GetTempPath(), "WitchsHat", "NoTitleProject");
            System.IO.Directory.CreateDirectory(path);

            // enchant.jsプロジェクト
            string sourceDirName = @"Data\Templates\HelloWorldProject";
            if (HasEnchantjs())
            {
                sourceDirName = @"Data\Templates\EnchantProject";
            }
            string[] files = System.IO.Directory.GetFiles(sourceDirName);
            string[] extra = { };
            if (HasEnchantjs())
            {
                extra = new string[1];
                extra[0] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WitchsHat", "enchant.js", "images", "chara1.png");
            }
            foreach (string file in files)
            {
                Console.WriteLine(file);
                System.IO.File.Copy(file,
                   path + "\\" + System.IO.Path.GetFileName(file), true);

            }
            foreach (string file in extra)
            {
                Console.WriteLine(file);
                System.IO.File.Copy(file,
                   path + "\\" + System.IO.Path.GetFileName(file), true);

            }
            this.projectName = "NoTitleProject";
            this.projectDir = path;
            loadProject();

            // main.jsを開く
            if (File.Exists(Path.Combine(this.projectDir, "main.js")))
            {
                OpenTab(Path.Combine(this.projectDir, "main.js"));
            }
        }

        private void readConfig()
        {

            // プロジェクト設定ファイル読み込み
            using (XmlReader reader = XmlReader.Create("Data\\defaultconfig.xml"))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.LocalName)
                        {
                            case "enchantjsbuild":
                                string url = reader.ReadString();
                                Console.WriteLine("[" + url + "]");
                                enchantjsUrl = url;
                                break;
                            case "server":
                                string server = reader.ReadString();
                                Console.WriteLine("[" + server + "]");
                                if (server == "on")
                                {
                                    serverEnable = true;
                                }
                                else
                                {
                                    serverEnable = false;
                                }
                                break;
                            case "serverport":
                                string port = reader.ReadString();
                                Console.WriteLine("[" + port + "]");
                                serverPort = port;
                                break;
                            case "tempproject":
                                string tempproject = reader.ReadString();
                                Console.WriteLine("[" + tempproject + "]");
                                if (tempproject == "on")
                                {
                                    this.TempProjectEnable = true;
                                }
                                else
                                {
                                    this.TempProjectEnable = false;
                                }
                                break;
                            case "browser":
                                string b = reader.ReadString();
                                Console.WriteLine("[" + b + "]");
                                browser = b;
                                break;
                        }
                    }
                }

            }
            // ユーザー設定ファイル読み込み
            string userconfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WitchsHat", "config.xml");
            if (File.Exists(userconfig))
            {
                using (XmlReader reader = XmlReader.Create(userconfig))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.LocalName)
                            {
                                case "enchantjsbuild":
                                    string url = reader.ReadString();
                                    Console.WriteLine("[" + url + "]");
                                    if (url != "")
                                    {
                                        enchantjsUrl = url;
                                    }
                                    break;
                                case "server":
                                    string server = reader.ReadString();
                                    Console.WriteLine("[" + server + "]");
                                    if (server == "on")
                                    {
                                        serverEnable = true;
                                    }
                                    else if (server == "off")
                                    {
                                        serverEnable = false;
                                    }
                                    break;
                                case "serverport":
                                    string port = reader.ReadString();
                                    Console.WriteLine("[" + port + "]");
                                    if (port != "")
                                    {
                                        serverPort = port;
                                    }
                                    break;
                                case "browser":
                                    string b = reader.ReadString();
                                    Console.WriteLine("[" + b + "]");
                                    if (b != "")
                                    {
                                        browser = b;
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

        }

        public void StartupNextInstance(params object[] parameters)
        {
            this.Activate();
            string[] args = (string[])parameters[0];

            MessageBox.Show("既に起動しています。引数1の数：" + args.Length.ToString());
        }

        /// <summary>
        /// 新しいプロジェクトメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateProjectForm f = new CreateProjectForm();
            f.RefreshEvent += delegate(object sender1, ProjectEventArgs e1)
            {
                this.Text = e1.ProjectName + " - Witch's Hat";
                this.projectName = e1.ProjectName;
                this.projectDir = e1.ProjectDir;
                loadProject();
            };
            f.ShowDialog(this);
        }

        /// <summary>
        /// プロジェクトを開くメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "プロジェクト ファイル (*.whprj)|*.whprj";
            string projectName = null;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                // プロジェクト設定ファイル読み込み
                using (XmlReader reader = XmlReader.Create(ofd.FileName))
                {
                    while (reader.Read())
                    {
                        reader.ReadToFollowing("ProjectName");
                        projectName = reader.ReadString();
                        Console.WriteLine(projectName);

                    }
                }
                this.projectDir = ofd.FileName.Substring(0, ofd.FileName.LastIndexOf('\\'));
                Console.WriteLine(this.projectDir);
                this.Text = projectName + " - Witch's Hat";
                this.projectName = projectName;

                loadProject();
            }
        }

        private void loadProject()
        {
            this.Text = projectName + " - Witch's Hat";

            // プロジェクトツリー更新
            this.treeView1.Nodes.Clear();
            this.treeView1.Nodes.Add(this.projectName);
            // フォルダ一覧追加
            string[] dirs = System.IO.Directory.GetDirectories(this.projectDir);
            foreach (string dir in dirs)
            {
                TreeNode treenode = this.treeView1.Nodes[0].Nodes.Add(this.projectName, System.IO.Path.GetFileName(dir));
                IsDirectory[treenode.GetHashCode()] = true;
                // ファイル一覧追加
                string[] files0 = System.IO.Directory.GetFiles(dir);
                foreach (string file in files0)
                {
                    if (!file.EndsWith(".whprj"))
                    {
                        TreeNode treenode1 = treenode.Nodes.Add(this.projectName, System.IO.Path.GetFileName(file));
                        IsDirectory[treenode1.GetHashCode()] = false;
                        FileFullPath[treenode1.GetHashCode()] = file;
                    }
                }
            }
            // ファイル一覧追加
            string[] files = System.IO.Directory.GetFiles(this.projectDir);
            foreach (string file in files)
            {
                if (!file.EndsWith(".whprj"))
                {
                    TreeNode treenode = this.treeView1.Nodes[0].Nodes.Add(this.projectName, System.IO.Path.GetFileName(file));
                    IsDirectory[treenode.GetHashCode()] = false;
                    FileFullPath[treenode.GetHashCode()] = file;
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
            string path = projectDir + "\\" + "index.html";
            if (File.Exists(path))
            {
                // Process hProcess = Process.Start(browser, projectDir + "\\" + "index.html");
                Process hProcess = Process.Start(browser, "http://localhost:" + serverPort + "/index.html");
            }
            //Process hProcess = Process.Start("chrome.exe", projectDir + "\\" + "index.html");
            //Process hProcess = Process.Start("firefox.exe", projectDir + "\\" + "index.html");
        }

        /// <summary>
        ///  ツリーノードダブルクリック時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeViewHitTestInfo ht = treeView1.HitTest(e.Location);
            //if (ht.Location == TreeViewHitTestLocations.Label && e.Node != treeView1.Nodes[0])
            if (e.Node != treeView1.Nodes[0])
            {
                Console.WriteLine(e.Node.Text);
                if (!IsDirectory[e.Node.GetHashCode()])
                {
                    // ファイル
                    //string filepath = this.projectDir + "\\" + e.Node.Text;
                    string filepath = FileFullPath[e.Node.GetHashCode()];
                    OpenTab(filepath);
                    this.tabControl1.SelectedTab = tabOpenFiles[filepath];
                }

            }
        }

        private void OpenTab(string fullpath)
        {
            string filename = Path.GetFileName(fullpath);
            if (!tabOpenFiles.ContainsKey(fullpath))
            {
                if (fullpath.EndsWith(".js") || fullpath.EndsWith(".txt") || fullpath.EndsWith(".html"))
                {
                    StreamReader sr = new StreamReader(fullpath, Encoding.GetEncoding(encoding));
                    string text = sr.ReadToEnd();
                    sr.Close();

                    // 新しいタブを追加して開く
                    TabPage tabPage = new TabPage(filename);
                    Sgry.Azuki.WinForms.AzukiControl azuki = new Sgry.Azuki.WinForms.AzukiControl();
                    azuki.Text = text;
                    azuki.ClearHistory();
                    azuki.Dock = DockStyle.Fill;
                    azuki.SetKeyBind(Keys.F | Keys.Control, delegate
                    {
                        FindToolStripMenuItem_Click(null, null);
                    });
                    azuki.SetKeyBind(Keys.H | Keys.Control, delegate
                    {
                        ReplaceToolStripMenuItem_Click(null, null);
                    });
                    tabPage.Controls.Add(azuki);
                    /*
                    TextBox textbox = new TextBox();
                    textbox.Multiline = true;
                    textbox.ScrollBars = ScrollBars.Both;
                    textbox.WordWrap = false;
                    textbox.Text = text;
                    textbox.Dock = DockStyle.Fill;
                    tabPage.Controls.Add(textbox);
                   */
                    azuki.TextChanged += delegate
                    {
                        tabPage.Text = filename + " *";
                        projectModify = true;
                    };
                    this.tabControl1.TabPages.Add(tabPage);
                    tabOpenFiles.Add(fullpath, tabPage);
                    this.tabInfos[tabPage] = new TabInfo();
                    this.tabInfos[tabPage].Type = TabInfo.TabTypeAzuki;
                    this.tabInfos[tabPage].Uri = fullpath;
                }
                else if (fullpath.EndsWith(".jpg") || fullpath.EndsWith(".png") || fullpath.EndsWith(".gif") || fullpath.EndsWith(".bmp"))
                {
                    // 新しいタブを追加して開く
                    TabPage tabPage = new TabPage(filename);
                    PictureBox picturebox = new PictureBox();
                    Console.WriteLine(fullpath);
                    picturebox.ImageLocation = fullpath;
                    picturebox.Dock = DockStyle.Fill;
                    picturebox.SizeMode = PictureBoxSizeMode.Zoom;
                    tabPage.Controls.Add(picturebox);
                    this.tabControl1.TabPages.Add(tabPage);
                    tabOpenFiles.Add(fullpath, tabPage);
                    this.tabInfos[tabPage] = new TabInfo();
                    this.tabInfos[tabPage].Type = TabInfo.TabTypeImage;
                    this.tabInfos[tabPage].Uri = fullpath;
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            bool close = true;
            if (projectModify)
            {
                DialogResult result = MessageBox.Show("プロジェクトが変更されています。\r\nプロジェクトを保存しますか？", "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                {
                    // 保存ダイアログ表示
                    e.Cancel = true;
                    close = false;
                    SaveProjectFromTemp f = new SaveProjectFromTemp();
                    f.OkClicked += delegate()
                    {

                        System.IO.Directory.CreateDirectory(f.ProjectDir);
                        // ファイルコピー
                        string sourceDirName = this.projectDir;
                        string[] files = System.IO.Directory.GetFiles(sourceDirName);
                        foreach (string file in files)
                        {
                            Console.WriteLine(file);
                            string filename = System.IO.Path.GetFileName(file);
                            if (System.IO.Path.GetFileName(file) == this.projectName + ".whprj")
                            {
                                filename = f.ProjectName + ".whprj";
                            }
                            System.IO.File.Copy(file,
                               f.ProjectDir + "\\" + filename, true);

                        }
                        if (!File.Exists(Path.Combine(f.ProjectDir, f.ProjectName + ".whprj")))
                        {
                            // プロジェクト設定ファイル生成
                            XmlWriterSettings settings = new XmlWriterSettings();
                            settings.Indent = true;
                            XmlWriter writer = XmlWriter.Create(f.ProjectDir + "\\" + f.ProjectName + ".whprj", settings);
                            writer.WriteElementString("ProjectName", f.ProjectName);
                            writer.Flush();
                            writer.Close();
                        }

                        projectModify = false;
                        if (tempproject)
                        {
                            Directory.Delete(projectDir, true);
                        }
                        tempproject = false;

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
            if (close)
            {
                running = false;
                if (listener != null)
                {
                    listener.Close();
                }
                if (tempproject)
                {
                    Directory.Delete(projectDir, true);
                }
                Program.mutex.ReleaseMutex();
            }


        }

        void ThreadFunction()
        {
            running = true;
            string prefix = "http://localhost:" + serverPort + "/"; // 受け付けるURL

            listener = new System.Net.HttpListener();
            listener.Prefixes.Add(prefix); // プレフィックスの登録
            listener.Start();
            try
            {
                while (running)
                {

                    System.Net.HttpListenerContext context = listener.GetContext();
                    System.Net.HttpListenerRequest req = context.Request;
                    System.Net.HttpListenerResponse res = context.Response;

                    Console.WriteLine(req.RawUrl);

                    // リクエストされたURLからファイルのパスを求める
                    string path = this.projectDir + req.RawUrl.Replace("/", "\\");
                    Console.WriteLine(path);

                    // ファイルが存在すればレスポンス・ストリームに書き出す
                    if (File.Exists(path))
                    {
                        byte[] content = File.ReadAllBytes(path);
                        res.OutputStream.Write(content, 0, content.Length);
                    }
                    res.Close();

                }
            }
            catch
            {

            }
            listener.Close();
            Console.WriteLine("server close");
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
                    f.enchantjsUrl = enchantjsUrl;
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
            string enchantjsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WitchsHat", "enchant.js", "build", "enchant.js");
            return File.Exists(enchantjsPath);
        }

        private void OpenOptionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingForm f = new SettingForm();
            f.ShowDialog(this);
        }

        private void OpenReferenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(browser, "http://enchantjs.com/ja/tutorial/lets-start-enchant-js/");
        }

        private void OpenApidocsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(browser, "http://wise9.github.io/enchant.js/doc/plugins/ja/index.html");
        }

        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
            azuki.Undo();
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

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
            azuki.Cut();
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
            azuki.Copy();
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
            azuki.Paste();
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
            Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
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
            Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
            f.MainForm = this;
            f.Show(this);
        }
    }


    public class ProjectEventArgs : EventArgs
    {
        public string ProjectName;
        public int ProjectType;
        public string ProjectDir;
    }
}
