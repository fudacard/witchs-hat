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
        // 表示しているタブの情報
        Dictionary<TabPage, TabInfo> tabInfos = new Dictionary<TabPage, TabInfo>();
        // ツリーノードがディレクトリかどうか
        Dictionary<int, bool> IsDirectory = new Dictionary<int, bool>();
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
        // テンポラリプロジェクトかどうか
        bool tempproject;
        // テンポラリプロジェクトを遅延生成するフラグ
        bool CreateLater;
        bool projectModify;
        string encoding = "utf-8";
        EnvironmentSettings settings;

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
            if (serverEnable)
            {
                serverThread = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadFunction));
                serverThread.Start();
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

        private void CreateProject(string projectName, string projectDir, int projectTemplate)
        {
            string sourceDirName = @"Data\Templates\HelloWorldProject";
            string[] extra = { };

            System.IO.Directory.CreateDirectory(projectDir);

            switch (projectTemplate)
            {
                case 0:
                    extra = new string[1];
                    extra[0] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WitchsHat", "enchant.js", "images", "chara1.png");
                    sourceDirName = @"Data\Templates\EnchantProject";
                    break;
                case 1:
                    sourceDirName = @"Data\Templates\BlankProject";
                    break;
                case 2:
                    sourceDirName = @"Data\Templates\HelloWorldProject";
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
            this.projectName = projectName;
            this.projectDir = projectDir;

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            XmlWriter writer = XmlWriter.Create(projectDir + "\\" + projectName + ".whprj", settings);
            writer.WriteElementString(projectName, projectName);
            writer.Flush();
            writer.Close();

            loadProject();

            // main.jsを開く
            if (File.Exists(Path.Combine(this.projectDir, "main.js")))
            {
                OpenTab(Path.Combine(this.projectDir, "main.js"));
            }
        }

        private void CreateTemporaryProject()
        {
            CreateLater = false;

            int projectTemplate = 2;
            if (HasEnchantjs())
            {
                projectTemplate = 0;
            }
            CreateProject("NoTitleProject", Path.Combine(Path.GetTempPath(), "WitchsHat", "NoTitleProject"), projectTemplate);

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
                                    settings.TempProjectEnable = true;
                                }
                                else
                                {
                                    settings.TempProjectEnable = false;
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
                                case "tempproject":
                                    string tempproject = reader.ReadString();
                                    Console.WriteLine("[" + tempproject + "]");
                                    if (tempproject != "")
                                    {
                                        if (tempproject == "on")
                                        {
                                            settings.TempProjectEnable = true;
                                        }
                                        else
                                        {
                                            settings.TempProjectEnable = false;
                                        }
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
            this.treeView1.Nodes.Add(this.projectDir, this.projectName);
            // フォルダ一覧追加
            string[] dirs = System.IO.Directory.GetDirectories(this.projectDir);
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
            string[] files = System.IO.Directory.GetFiles(this.projectDir);
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

            if (!tabInfos.Any(x => x.Value.Uri == fullpath))
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
                    azuki.TabWidth = 4;
                    azuki.Dock = DockStyle.Fill;
                    if (fullpath.EndsWith(".js"))
                    {
                        azuki.Highlighter = Sgry.Azuki.Highlighter.Highlighters.JavaScript;
                    }
                    else if (fullpath.EndsWith(".html"))
                    {
                        azuki.Highlighter = Sgry.Azuki.Highlighter.Highlighters.Xml;
                    }

                    //azuki.Font = new Font("Courier New", 10);
                    azuki.Font = new Font("ＭＳ ゴシック", 10);
                    Console.WriteLine(azuki.Font.SizeInPoints);
                    azuki.SetKeyBind(Keys.F | Keys.Control, delegate
                    {
                        FindToolStripMenuItem_Click(null, null);
                    });
                    azuki.SetKeyBind(Keys.H | Keys.Control, delegate
                    {
                        ReplaceToolStripMenuItem_Click(null, null);
                    });
                    tabPage.Controls.Add(azuki);
                    azuki.TextChanged += delegate
                    {
                        tabPage.Text = filename + " *";
                        tabInfos[tabPage].Modify = true;
                        projectModify = true;
                    };
                    this.tabControl1.TabPages.Add(tabPage);
                    this.tabInfos[tabPage] = new TabInfo();
                    this.tabInfos[tabPage].Type = TabInfo.TabTypeAzuki;
                    this.tabInfos[tabPage].Uri = fullpath;
                    this.tabInfos[tabPage].Modify = false;
                }
                else if (fullpath.EndsWith(".jpg") || fullpath.EndsWith(".png") || fullpath.EndsWith(".gif") || fullpath.EndsWith(".bmp"))
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
                    tabPage.MouseEnter += delegate {
                        tabPage.Focus();

                    };
                    tabPage.MouseWheel += delegate(object sender, MouseEventArgs e)
                    {
                        scaleLevel += e.Delta / 120;
                        double scale = 1.0;
                        if (scaleLevel > 0)
                        {
                            for (int i = 0; i < scaleLevel;i++ )
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
                    
                    picturebox.BackgroundImage = System.Drawing.Image.FromFile(Path.Combine(System.Environment.CurrentDirectory, @"Data\Resources\transback.png"));
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

        void tabPage_MouseWheel(object sender, MouseEventArgs e)
        {
            throw new NotImplementedException();
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
            f.settings = settings;
            f.OkClicked = delegate
            {
                // 設定保存

                XmlWriterSettings xmlsettings = new XmlWriterSettings();
                xmlsettings.Indent = true;

                string outputdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WitchsHat");
                Directory.CreateDirectory(outputdir);

                XmlWriter writer = XmlWriter.Create(Path.Combine(outputdir, "config.xml"), xmlsettings);
                writer.WriteElementString("tempproject", this.settings.TempProjectEnable ? "on" : "off");
                writer.Flush();
                writer.Close();
            };
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
            f.OkClicked = delegate(string projectName, string projectDir, int projectTemplate)
            {
                CloseProjectToolStripMenuItem_Click(sender, e);

                CreateProject(projectName, projectDir, projectTemplate);

                this.Text = projectName + " - Witch's Hat";
                this.projectName = projectName;
                this.projectDir = projectDir;
                loadProject();
            };
            f.ShowDialog(this);
        }

        private void CreateFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateFileForm f = new CreateFileForm();
            f.ProjectDir = projectDir;
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
                projectModify = true;
                loadProject();
            };
            f.ShowDialog(this);
        }

        private void CreateFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateFolderForm f = new CreateFolderForm();
            f.ProjectDir = projectDir;
            f.OkClicked = delegate(string folderpath)
            {
                Directory.CreateDirectory(folderpath);
                projectModify = true;
                loadProject();
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
            this.Text = "Witch's Hat";

            this.treeView1.Nodes.Clear();

            foreach (var pair in tabInfos)
            {
                if (pair.Value.Uri.StartsWith(projectDir))
                {
                    tabControl1.TabPages.Remove(pair.Key);
                }
            }
        }

    }

}
