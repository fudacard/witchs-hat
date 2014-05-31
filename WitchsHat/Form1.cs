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
        // テンポラリプロジェクトかどうか
        //bool tempproject;
        // テンポラリプロジェクトを遅延生成するフラグ
        bool CreateLater;
        //bool tempprojectModify;
        EnvironmentSettings settings;
        WHServer server;
        //ProjectProperty CurrentProject;
        public List<string> FileImportDirs;
        TabManager tabManager;
        PopupWindow popupWindow;
        TaskScheduler taskScheduler;
        ProjectManager projectManager;
        public TabPage CurrentTab;

        private delegate void StartupNextInstanceDelegate(params object[] parameters);

        public Form1()
        {
            InitializeComponent();

            taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            tabManager = new TabManager(this.tabControl1, tabInfos);

            projectManager = new ProjectManager();
            projectManager.form = this;
            projectManager.tabManager = tabManager;

            treeView1.form1 = this;
            treeView1.ImageList = this.imageList1;

            FileImportDirs = new List<string>();
            FileImportDirs.Add("");
            FileImportDirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\build"));
            FileImportDirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\build\plugins"));
            FileImportDirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\images"));
            FileImportDirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\images\monster"));


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
                if (HasEnchantjs() || !settings.EnchantjsDownload)
                {
                    CreateLater = false;
                    projectManager.CreateTemporaryProject();
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
            projectManager.projectsPath = settings.ProjectsPath;

        }

        public void StartupNextInstance(params object[] parameters)
        {
            Task.Factory.StartNew(() =>
            {
                this.Activate();
                string[] args = (string[])parameters[0];

                if (args.Length > 1)
                {
                    OpenFile((string)args[1]);
                }
            }, System.Threading.CancellationToken.None, TaskCreationOptions.None, taskScheduler);
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
            ofd.FilterIndex = 6;
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
                open = projectManager.NewProjectCheck();
                if (open)
                {
                    OpenProject(filePath);
                }
            }
            else
            {
                OpenTab(filePath);
                this.tabControl1.SelectedTab = tabInfos.FirstOrDefault(x => x.Value.Uri == filePath).Key;
            }
        }

        private void OpenProject(string filePath)
        {
            if (projectManager.CurrentProject != null)
            {
                this.Text = "Witch's Hat";
                treeView1.Nodes.Clear();
                projectManager.CloseProject();
            }

            tabManager.CloseAllTab();

            // プロジェクト設定ファイル読み込み
            projectManager.CurrentProject = ProjectProperty.ReadProjectProperty(filePath);

            ResetProject();
        }

        /// <summary>
        /// 実行メニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunOnBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path;
            if (tabControl1.SelectedTab != null && (tabInfos[tabControl1.SelectedTab].Uri.EndsWith(".html") || tabInfos[tabControl1.SelectedTab].Uri.EndsWith(".htm")))
            {
                path = tabInfos[tabControl1.SelectedTab].Uri;
            }
            else
            {
                path = Path.Combine(projectManager.CurrentProject.Dir, projectManager.CurrentProject.HtmlPath);
            }
            RunOnBrowser(path);
        }

        private void RunOnBrowser(string path, string browser = null)
        {
            if (File.Exists(path))
            {
                if (browser == null)
                {
                    browser = settings.RunBrowser;
                }
                // 開いているファイルを保存する
                tabManager.SaveAllFiles();

                bool useServer = false;
                if (projectManager.CurrentProject != null && path.StartsWith(projectManager.CurrentProject.Dir))
                {
                    useServer = true;
                }

                try
                {
                    if (settings.ServerEnable && useServer)
                    {
                        Process.Start(browser, "http://localhost:" + settings.ServerPort + "/" + projectManager.CurrentProject.HtmlPath);
                        //OpenWebBrowserTab("http://localhost:" + settings.ServerPort + "/"+ CurrentProject.HtmlPath);
                    }
                    else
                    {
                        Process.Start(browser, "\"" + path + "\"");
                    }
                }
                catch (Exception e1)
                {
                    MessageBox.Show("ブラウザ " + browser + " を開く際にエラーが発生しました。\r\n" + e1.Message);
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
                if (!treeView1.IsDirectory(e.Node))
                {
                    // ファイル
                    string filepath = e.Node.Name;
                    OpenTab(filepath);
                    this.tabControl1.SelectedTab = tabInfos.FirstOrDefault(x => x.Value.Uri == filepath).Key;
                }

            }
        }

        public void OpenTab(string fullpath)
        {
            string filename = Path.GetFileName(fullpath);
            string pathlower = fullpath.ToLower();
            if (!tabInfos.Any(x => x.Value.Uri == fullpath))
            {
                if (pathlower.EndsWith(".js") || pathlower.EndsWith(".txt") || pathlower.EndsWith(".html") || pathlower.EndsWith(".htm") || pathlower.EndsWith(".css"))
                {
                    TabPage tabPage = tabManager.AddEditorTab(fullpath);
                    Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabPage.Controls[0];
                    azuki.Font = new Font(settings.FontName, settings.FontSize);
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
                    azuki.TextChanged += delegate
                    {
                        projectManager.ProjectModified();
                    };
                    azuki.ContextMenuStrip = AzukiContextMenuStrip;

                    if (popupWindow == null)
                    {
                        popupWindow = new PopupWindow();
                    }
                    if (pathlower.EndsWith(".js"))
                    {
                        SuggestionManager suggestionManager = new SuggestionManager(tabPage, azuki, listBox1, this, popupWindow);
                        tabInfos[tabPage].suggestionManager = suggestionManager;
                        suggestionManager.Enable = settings.SuggestEnable;
                        if (settings.SuggestEnable)
                        {
                            suggestionManager.Analyze();
                        }
                    }
                    azuki.UsesTabForIndent = settings.IndentUseTab;

                }
                else if (pathlower.EndsWith(".jpg") || pathlower.EndsWith(".jpeg") || pathlower.EndsWith(".png") || pathlower.EndsWith(".gif") || pathlower.EndsWith(".bmp"))
                {
                    // 新しいタブを追加して開く
                    tabManager.AddImageTab(fullpath);
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            bool close = true;
            close = projectManager.NewProjectCheck();
            if (close)
            {
                if (server != null && server.IsRunning())
                {
                    server.Stop();
                }
                if (projectManager.tempproject)
                {
                    Directory.Delete(projectManager.CurrentProject.Dir, true);
                }
                Program.mutex.ReleaseMutex();

                Properties.Settings.Default.Save();
            }
            else
            {
                e.Cancel = true;
            }

        }


        private void Form1_Load(object sender, EventArgs e)
        {
            Properties.Settings.Default.SettingChanging += new System.Configuration.SettingChangingEventHandler(Default_SettingChanging);
            Console.WriteLine(GetDefaultBrowserPath());
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            // enchant.jsがあるかどうかチェック
            if (!HasEnchantjs() && settings.EnchantjsDownload)
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
                            CreateLater = false;
                            projectManager.CreateTemporaryProject();
                        }
                    };
                    f.ShowDialog(this);
                }
                else
                {
                    if (CreateLater)
                    {
                        CreateLater = false;
                        projectManager.CreateTemporaryProject();
                    }
                }
            }
        }

        public bool HasEnchantjs()
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
                    server.RootDir = projectManager.CurrentProject.Dir;
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
                    server.Stop();
                    //server = new WHServer();
                    //server.RootDir = CurrentProject.Dir;
                    server.Start(settings.ServerPort);
                }

                Font font = new Font(settings.FontName, settings.FontSize);
                foreach (var pair in tabInfos)
                {
                    if (pair.Value.Type == TabInfo.TabTypeAzuki)
                    {
                        Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)pair.Key.Controls[0];
                        // フォント変更
                        azuki.Font = font;
                        // インデント設定変更
                        azuki.UsesTabForIndent = settings.IndentUseTab;
                    }
                }
                // サジェスト機能有効切り替え
                foreach (var pair in tabInfos)
                {
                    if (pair.Value.Type == TabInfo.TabTypeAzuki && pair.Value.suggestionManager != null)
                    {
                        pair.Value.suggestionManager.Enable = settings.SuggestEnable;
                    }
                }
            };
            f.ShowDialog(this);
        }

        private void OpenReferenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenBrowser("http://enchantjs.com/ja/tutorial/lets-start-enchant-js/");
        }

        private void OpenApidocsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (HasEnchantjs())
            {
                string path = "\"" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\doc\plugins\ja\index.html") + "\"";
                OpenBrowser(path);
            }
            else
            {
                OpenBrowser("http://wise9.github.io/enchant.js/doc/plugins/ja/index.html");
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
            if (projectManager.CurrentProject != null && tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Undo();
            }
        }

        private void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (projectManager.CurrentProject != null && tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Redo();
            }
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (projectManager.CurrentProject != null && tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Cut();
            }
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (projectManager.CurrentProject != null && tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Copy();
            }
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (projectManager.CurrentProject != null && tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Paste();
            }
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (projectManager.CurrentProject != null && tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Delete();
            }
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
            f.FileImportDirs = this.FileImportDirs;
            f.OkClicked = delegate(string projectName, string projectDir, WHTemplate projectTemplate, string newProjectsPath)
            {
                CloseProjectToolStripMenuItem_Click(sender, e);

                projectManager.CreateProject(projectName, projectDir, projectTemplate);

                if (newProjectsPath != null)
                {
                    settings.ProjectsPath = newProjectsPath;
                    projectManager.projectsPath = newProjectsPath;
                    // 設定保存
                    string outputdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Witchs Hat");
                    Directory.CreateDirectory(outputdir);
                    settings.WriteEnvironmentSettings(Path.Combine(outputdir, "settings.xml"));
                }

                projectManager.CurrentProject = new ProjectProperty();
                projectManager.CurrentProject.Name = projectName;
                projectManager.CurrentProject.Dir = projectDir;
                projectManager.CurrentProject.HtmlPath = "index.html";
                projectManager.CurrentProject.Encoding = settings.Encoding;

                ResetProject();
            };
            f.ShowDialog(this);
        }

        private void CreateFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (projectManager.CurrentProject != null)
            {
                CreateFileForm f = new CreateFileForm();
                f.ProjectDir = projectManager.CurrentProject.Dir;
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

                    ResetProject();
                    projectManager.ProjectModified();
                };
                f.ShowDialog(this);
            }
        }

        private void CreateFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (projectManager.CurrentProject != null)
            {
                CreateFolderForm f = new CreateFolderForm();
                f.ProjectDir = projectManager.CurrentProject.Dir;
                f.OkClicked = delegate(string folderpath)
                {
                    Directory.CreateDirectory(folderpath);

                    ResetProject();
                    projectManager.ProjectModified();
                };
                f.ShowDialog(this);
            }
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
            if (projectManager.CurrentProject != null)
            {
                CreateFileToolStripMenuItem.Enabled = true;
                CreateFolderToolStripMenuItem.Enabled = true;
                CloseProjectToolStripMenuItem.Enabled = true;
                SaveAsProjectToolStripMenuItem.Enabled = true;
            }
            else
            {
                CreateFileToolStripMenuItem.Enabled = false;
                CreateFolderToolStripMenuItem.Enabled = false;
                CloseProjectToolStripMenuItem.Enabled = false;
                SaveAsProjectToolStripMenuItem.Enabled = false;
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
            if (projectManager.CurrentProject != null)
            {
                sfd.InitialDirectory = projectManager.CurrentProject.Dir;
            }
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

            close = projectManager.NewProjectCheck();

            if (close)
            {
                this.Text = "Witch's Hat";
                treeView1.Nodes.Clear();
                projectManager.CloseProject();
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
            string fullpath = Path.Combine(projectManager.CurrentProject.Dir, filename);
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
            f.projectProperty = projectManager.CurrentProject;
            f.OkClicked = delegate(string executeHtml)
            {
                projectManager.CurrentProject.HtmlPath = executeHtml;
                // プロジェクトファイル保存
                ProjectProperty.WriteProjectProperty(projectManager.CurrentProject);
            };
            f.Show();
        }

        private void ProjecttoolStripMenuItem2_DropDownOpening(object sender, EventArgs e)
        {
            if (projectManager.CurrentProject != null)
            {
                ImportFileToolStripMenuItem.Enabled = true;
                SpecialImportToolStripMenuItem.Enabled = true;
                ProjectPropertyToolStripMenuItem.Enabled = true;
            }
            else
            {
                ImportFileToolStripMenuItem.Enabled = false;
                SpecialImportToolStripMenuItem.Enabled = false;
                ProjectPropertyToolStripMenuItem.Enabled = false;
            }
        }

        private void RunToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {

            RunOnBrowserToolStripMenuItem.Enabled = false;

            if (projectManager.CurrentProject != null)
            {
                RunOnBrowserToolStripMenuItem.Enabled = true;
            }
            else if (tabInfos.Count > 0)
            {
                string path = tabInfos[tabControl1.SelectedTab].Uri;
                if (path.EndsWith(".html") || path.EndsWith(".htm"))
                {
                    RunOnBrowserToolStripMenuItem.Enabled = true;
                }
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                treeView1.SelectedNode = e.Node;

                string pathlower = e.Node.Name.ToLower();
                if (e.Node.Name == projectManager.CurrentProject.Dir)
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

                ResetProject();

                projectManager.ProjectModified();
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
                if (tabPage != null)
                {
                    tabInfos[tabPage].Uri = newFilePath;
                    tabPage.Text = newFileName;
                }

                ResetProject();

                projectManager.ProjectModified();
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
            f.Dir = treeView1.SelectedNode.Name;
            f.OkClicked = delegate(string folderName)
            {
                Directory.CreateDirectory(Path.Combine(treeView1.SelectedNode.Name, folderName));

                ResetProject();

                projectManager.ProjectModified();
            };
            f.ShowDialog(this);
        }

        private void ImportFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectImportFile(null);
        }


        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (projectManager.IsProjectOpened())
            {
                RunOnBrowserToolStripMenuItem_Click(sender, e);
            }
            else if (tabInfos.Count > 0)
            {
                string path = tabInfos[tabControl1.SelectedTab].Uri;
                if (path.EndsWith(".html") || path.EndsWith(".htm"))
                {
                    RunOnBrowserToolStripMenuItem_Click(sender, e);
                }
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

        private void CreateToolBartoolStripDropDownButton1_DropDownOpening(object sender, EventArgs e)
        {
            if (projectManager.IsProjectOpened())
            {
                CreateFileToolBarToolStripMenuItem.Enabled = true;
                CreateFolderToolBarToolStripMenuItem.Enabled = true;
            }
            else
            {
                CreateFileToolBarToolStripMenuItem.Enabled = false;
                CreateFolderToolBarToolStripMenuItem.Enabled = false;
                CreateFileToolStripMenuItem.Enabled = false;
            }
        }

        private void RunOnBrowserContextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunOnBrowser(treeView1.SelectedNode.Name);
        }

        private void OpenExplorerContextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("EXPLORER.EXE", @"/e, " + treeView1.SelectedNode.Name);
        }

        private void OpenToolBartoolStripButton1_Click(object sender, EventArgs e)
        {
            OpenProjectOrFileToolStripMenuItem_Click(sender, e);
        }

        private void FindToolBartoolStripButton8_Click(object sender, EventArgs e)
        {
            FindToolStripMenuItem_Click(sender, e);
        }

        private void SaveAsProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveProjectFromTemp f = new SaveProjectFromTemp();
            f.ProjectsPath = settings.ProjectsPath;
            f.OkClicked = delegate(string projectName, string projectDir)
            {

                string tempprojectDir = projectManager.CurrentProject.Dir;
                // プロジェクトをコピー
                System.IO.Directory.CreateDirectory(projectDir);
                // ファイルコピー
                string sourceDirName = projectManager.CurrentProject.Dir;
                string[] files = System.IO.Directory.GetFiles(sourceDirName);
                foreach (string file in files)
                {
                    Console.WriteLine(file);
                    string filename = System.IO.Path.GetFileName(file);
                    if (System.IO.Path.GetFileName(file) == projectManager.CurrentProject.Name + ".whprj")
                    {
                        //                                filename = f.ProjectName + ".whprj";
                        projectManager.CurrentProject.Name = projectName;
                        projectManager.CurrentProject.Dir = projectDir;
                        ProjectProperty.WriteProjectProperty(projectManager.CurrentProject);
                    }
                    else
                    {
                        KeyValuePair<TabPage, TabInfo> pair = tabInfos.FirstOrDefault(x => x.Value.Uri == file);
                        if (pair.Key == null)
                        {
                            System.IO.File.Copy(file, Path.Combine(projectDir, filename), true);
                        }
                        else
                        {
                            pair.Value.Uri = Path.Combine(projectDir, filename);

                            if (pair.Value.Type == TabInfo.TabTypeAzuki)
                            {
                                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)pair.Key.Controls[0];
                                StreamWriter writer = new StreamWriter(pair.Value.Uri);
                                writer.Write(azuki.Text);
                                writer.Close();
                                pair.Value.Modify = false;
                                pair.Key.Text = filename;
                            }
                            else
                            {
                                System.IO.File.Copy(file, pair.Value.Uri, true);
                            }
                        }
                    }

                }
                if (!File.Exists(Path.Combine(projectDir, projectName + ".whprj")))
                {
                    // プロジェクト設定ファイル生成
                    projectManager.CurrentProject.Name = projectName;
                    projectManager.CurrentProject.Dir = projectDir;
                    ProjectProperty.WriteProjectProperty(projectManager.CurrentProject);
                }
                ResetProject();

                projectManager.tempprojectModify = false;
                if (projectManager.tempproject)
                {
                    Directory.Delete(tempprojectDir, true);
                }
                projectManager.tempproject = false;
            };
            f.Show();
        }

        public void ResetProject()
        {
            if (projectManager.IsProjectOpened())
            {
                this.Text = projectManager.CurrentProject.Name + " - Witch's Hat";

                treeView1.ProjectName = projectManager.CurrentProject.Name;
                treeView1.ProjectDir = projectManager.CurrentProject.Dir;
                if (server != null)
                {
                    server.RootDir = projectManager.CurrentProject.Dir;
                }
            }
            else
            {
                this.Text = "Witch's Hat";

                treeView1.ProjectName = "";
                treeView1.ProjectDir = "";
                if (server != null)
                {
                    server.RootDir = "";
                }
            }

            treeView1.UpdateFileTree();
        }

        private void CutAzukiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CutToolStripMenuItem_Click(sender, e);
        }

        private void CopyAzukiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyToolStripMenuItem_Click(sender, e);
        }

        private void PasteAzukiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PasteToolStripMenuItem_Click(sender, e);
        }


        internal int GetListBoxLeft()
        {
            return this.Left + splitContainer1.Left + splitContainer1.Panel2.Left + listBox1.Left;//+ tabControl1.Left +tabControl1.SelectedTab.Left + tabControl1.SelectedTab.Controls[0].Left;
        }
        internal int GetListBoxTop()
        {
            return this.Top + splitContainer1.Top + splitContainer1.Panel2.Top + listBox1.Top;//+ tabControl1.Left +tabControl1.SelectedTab.Left + tabControl1.SelectedTab.Controls[0].Left;
        }

        private void DocumentImportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectImportFile(System.Environment.GetFolderPath(Environment.SpecialFolder.Personal));
        }

        private void EnchantjsImportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectImportFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\images"));
        }

        private void SpecialImportToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (projectManager.IsProjectOpened() && Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\images")))
            {
                DocumentImportToolStripMenuItem.Enabled = true;
                EnchantjsImportToolStripMenuItem.Enabled = true;
            }
            else
            {
                DocumentImportToolStripMenuItem.Enabled = false;
                EnchantjsImportToolStripMenuItem.Enabled = false;
            }
        }

        private void SelectImportFile(string initialDirectory)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "javascriptファイル(*.js)|*.js|htmlファイル(*.html;*.html)|*.html;*.htm|画像ファイル(*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif|テキストファイル(*.txt)|*.txt|すべてのファイル(*.*)|*.*";
            ofd.FilterIndex = 5;
            ofd.InitialDirectory = initialDirectory;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string destFileName = Path.Combine(projectManager.CurrentProject.Dir, Path.GetFileName(ofd.FileName));
                if (File.Exists(Path.Combine(projectManager.CurrentProject.Dir, Path.GetFileName(destFileName))))
                {
                    MessageBox.Show("同名のファイルがプロジェクトに含まれています。");
                    return;
                }
                File.Copy(ofd.FileName, destFileName);
                OpenFile(destFileName);

                ResetProject();

                projectManager.ProjectModified();
            }
        }

        void Default_SettingChanging(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            if (this.WindowState != FormWindowState.Normal)
            {
                if ((e.SettingName == "MyClientSize") || (e.SettingName == "MyLocation"))
                {
                    e.Cancel = true;
                }
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentTab = tabControl1.SelectedTab;
        }

        private void VersionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm form = new AboutForm();
            form.ShowDialog(this);
        }

        private void OpenBrowser(string url)
        {
            string browserPath = GetDefaultBrowserPath();
            if (browserPath != "")
            {
                try
                {
                    Process.Start(browserPath, url);
                }
                catch (Exception e1)
                {
                    MessageBox.Show("ブラウザ " + browserPath + " を開く際にエラーが発生しました。\r\n" + e1.Message);
                }
            }
            else
            {
                Process.Start(url);
            }
        }

        private string GetDefaultBrowserPath()
        {
            string path = "";
            Microsoft.Win32.RegistryKey rKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"http\shell\open\command");
            if (rKey != null)
            {
                string command = (string)rKey.GetValue(String.Empty);
                if (command == null)
                {
                    return path;
                }

                command = command.Trim();
                if (command.Length == 0)
                {
                    return path;
                }

                if (command[0] == '"')
                {
                    int endIndex = command.IndexOf('"', 1);
                    if (endIndex != -1)
                    {
                        path = command.Substring(1, endIndex - 1);
                    }
                }
                else
                {
                    int endIndex = command.IndexOf(' ');
                    if (endIndex != -1)
                    {
                        path = command.Substring(0, endIndex);
                    }
                    else
                    {
                        path = command;
                    }
                }
            }

            return path;

        }

        private void HelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://fudacard.github.io/witchs-hat/help.html");
        }

        private void tabControl1_ControlAdded(object sender, ControlEventArgs e)
        {
            CurrentTab = tabControl1.SelectedTab;
        }

        private void tabControl1_ControlRemoved(object sender, ControlEventArgs e)
        {
            CurrentTab = tabControl1.SelectedTab;
        }

        private void internetExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunOnBrowserCurrent("iexplore");
        }

        private void chromeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunOnBrowserCurrent("chrome");
        }

        private void firefoxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunOnBrowserCurrent("firefox");
        }

        private void operaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunOnBrowserCurrent("opera");
        }

        private void RunOnBrowserCurrent(string browser = null)
        {
            string path;
            if (tabControl1.SelectedTab != null && (tabInfos[tabControl1.SelectedTab].Uri.EndsWith(".html") || tabInfos[tabControl1.SelectedTab].Uri.EndsWith(".htm")))
            {
                path = tabInfos[tabControl1.SelectedTab].Uri;
            }
            else
            {
                path = Path.Combine(projectManager.CurrentProject.Dir, projectManager.CurrentProject.HtmlPath);
            }
            RunOnBrowser(path, browser);
        }

        private void OpenProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool open = true;
            open = projectManager.NewProjectCheck();
            if (open)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "プロジェクト ファイル (*.whprj)|*.whprj|すべてのファイル(*.*)|*.*";
                if (Directory.Exists(settings.ProjectsPath))
                {
                    ofd.InitialDirectory = settings.ProjectsPath;
                }
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    OpenProject(ofd.FileName);
                }
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Sgry.Azuki.WinForms.AzukiControl azuki = GetActiveAzuki();
            if (azuki != null)
            {
                SaveToolStripMenuItem_Click(null, null);
            }
        }
    }

}
