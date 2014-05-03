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
        bool tempproject;
        // テンポラリプロジェクトを遅延生成するフラグ
        bool CreateLater;
        bool tempprojectModify;
        EnvironmentSettings settings;
        WHServer server;
        ProjectProperty CurrentProject;
        List<string> FileImportDirs;
        TabManager tabManager;
        PopupWindow popupWindow;
        TaskScheduler taskScheduler;

        private delegate void StartupNextInstanceDelegate(params object[] parameters);

        public Form1()
        {
            InitializeComponent();

            taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            tabManager = new TabManager(this.tabControl1, tabInfos);

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
                tempproject = true;
                if (HasEnchantjs() || !settings.EnchantjsDownload)
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

        private void CreateProject(string projectName, string projectDir, WHTemplate projectTemplate)
        {
            bool create = true;

            create = NewProjectCheck();
            if (create)
            {
                tabManager.CloseAllTab();
            }
            else
            {
                return;
            }

            System.IO.Directory.CreateDirectory(projectDir);

            foreach (WHFile file in projectTemplate.Files)
            {
                Directory.CreateDirectory(Path.Combine(projectDir, file.DestDir));
                string fileName = Path.GetFileName(file.Src);
                File.Copy(file.FullSrc, Path.Combine(projectDir, file.DestDir, fileName), true);
            }

            ProjectProperty pp = new ProjectProperty();
            pp.Name = projectName;
            pp.Dir = projectDir;
            pp.Encoding = "UTF-8";
            pp.HtmlPath = "index.html";
            ProjectProperty.WriteProjectProperty(pp);

            CurrentProject = pp;

            ResetProject();

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
                    f.ProjectsPath = settings.ProjectsPath;
                    f.OkClicked += delegate(string projectName, string projectDir)
                    {
                        // 保存する
                        SaveTempProject(projectName, projectDir);

                        CloseProject();
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
                        tabManager.SaveAllFiles();
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        continueFlag = false;
                    }
                }
            }

            tempproject = false;

            return continueFlag;
        }

        private void CreateTemporaryProject()
        {
            CreateLater = false;

            string templateDir = "";
            WHTemplate projectTemplate = null;
            if (HasEnchantjs())
            {
                templateDir = Path.Combine(Application.StartupPath, @"Data\Templates\02KumaProject");
                projectTemplate = WHTemplate.ReadTemplate(Path.Combine(Application.StartupPath, @"Data\Templates\02KumaProject\KumaProject.template"));
            }
            else
            {
                templateDir = Path.Combine(Application.StartupPath, @"Data\Templates\01EnchantProject");
                projectTemplate = WHTemplate.ReadTemplate(Path.Combine(Application.StartupPath, @"Data\Templates\01EnchantProject\EnchantProject.template"));
            }
            FileImportDirs[0] = templateDir;
            projectTemplate.CheckFiles(FileImportDirs);
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
                open = NewProjectCheck();
                if (CurrentProject != null)
                {
                    CloseProject();
                }
                if (open)
                {
                    tabManager.CloseAllTab();

                    // プロジェクト設定ファイル読み込み
                    CurrentProject = ProjectProperty.ReadProjectProperty(filePath);

                    ResetProject();
                }
            }
            else
            {
                OpenTab(filePath);
                this.tabControl1.SelectedTab = tabInfos.FirstOrDefault(x => x.Value.Uri == filePath).Key;
            }
        }



        /// <summary>
        /// 実行メニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunOnBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path;
            if (tabInfos[tabControl1.SelectedTab].Uri.EndsWith(".html") || tabInfos[tabControl1.SelectedTab].Uri.EndsWith(".htm"))
            {
                path = tabInfos[tabControl1.SelectedTab].Uri;
            }
            else
            {
                path = Path.Combine(CurrentProject.Dir, CurrentProject.HtmlPath);
            }
            RunOnBrowser(path);
        }

        private void RunOnBrowser(string path)
        {
            if (File.Exists(path))
            {
                bool useServer = false;
                if (CurrentProject != null && path.StartsWith(CurrentProject.Dir))
                {
                    useServer = true;
                }

                try
                {
                    if (settings.ServerEnable && useServer)
                    {
                        Process.Start(settings.RunBrowser, "http://localhost:" + settings.ServerPort + "/" + CurrentProject.HtmlPath);
                        //OpenWebBrowserTab("http://localhost:" + settings.ServerPort + "/"+ CurrentProject.HtmlPath);
                    }
                    else
                    {
                        Process.Start(settings.RunBrowser, "\"" + path + "\"");
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
                if (!treeView1.IsDirectory(e.Node))
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
                        if (tempproject)
                        {
                            tempprojectModify = true;
                        }
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
            close = NewProjectCheck();
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

                Properties.Settings.Default.Save();
            }

        }

        private void SaveTempProject(string projectName, string projectDir)
        {
            // ファイルをすべて保存する
            tabManager.SaveAllFiles();

            string tempprojectDir = CurrentProject.Dir;
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
                Directory.Delete(tempprojectDir, true);
            }
            tempproject = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
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
                    if (pair.Value.Type == TabInfo.TabTypeAzuki)
                    {
                        pair.Value.suggestionManager.Enable = settings.SuggestEnable;
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
                if (HasEnchantjs())
                {
                    string path = "\"" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\doc\plugins\ja\index.html") + "\"";
                    Process.Start(settings.Browser, path);
                }
                else
                {
                    Process.Start(settings.Browser, "http://wise9.github.io/enchant.js/doc/plugins/ja/index.html");
                }
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
            if (CurrentProject != null && tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Undo();
            }
        }

        private void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentProject != null && tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Redo();
            }
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentProject != null && tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Cut();
            }
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentProject != null && tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Copy();
            }
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentProject != null && tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
            {
                Sgry.Azuki.WinForms.AzukiControl azuki = (Sgry.Azuki.WinForms.AzukiControl)tabControl1.SelectedTab.Controls[0];
                azuki.Paste();
            }
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentProject != null && tabInfos[tabControl1.SelectedTab].Type == TabInfo.TabTypeAzuki)
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

                CreateProject(projectName, projectDir, projectTemplate);

                if (newProjectsPath != null)
                {
                    settings.ProjectsPath = newProjectsPath;
                    // 設定保存
                    string outputdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Witchs Hat");
                    Directory.CreateDirectory(outputdir);
                    settings.WriteEnvironmentSettings(Path.Combine(outputdir, "settings.xml"));
                }

                CurrentProject = new ProjectProperty();
                CurrentProject.Name = projectName;
                CurrentProject.Dir = projectDir;
                CurrentProject.HtmlPath = "index.html";
                CurrentProject.Encoding = settings.Encoding;

                ResetProject();
            };
            f.ShowDialog(this);
        }

        private void CreateFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentProject != null)
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

                    ResetProject();
                };
                f.ShowDialog(this);
            }
        }

        private void CreateFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentProject != null)
            {
                CreateFolderForm f = new CreateFolderForm();
                f.ProjectDir = CurrentProject.Dir;
                f.OkClicked = delegate(string folderpath)
                {
                    Directory.CreateDirectory(folderpath);
                    tempprojectModify = true;

                    ResetProject();
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
            if (CurrentProject != null)
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
            if (CurrentProject != null)
            {
                sfd.InitialDirectory = CurrentProject.Dir;
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

            close = NewProjectCheck();

            if (close)
            {
                CloseProject();
            }
        }

        private void CloseProject()
        {
            this.Text = "Witch's Hat";
            treeView1.Nodes.Clear();
            tabManager.CloseAllTab();

            if (tempproject)
            {
                Directory.Delete(CurrentProject.Dir, true);
                tempproject = false;
                tempprojectModify = false;
            }
            CurrentProject = null;
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
                // プロジェクトファイル保存
                ProjectProperty.WriteProjectProperty(CurrentProject);
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

            RunOnBrowserToolStripMenuItem.Enabled = false;

            if (CurrentProject != null)
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

                ResetProject();

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
                if (tabPage != null)
                {
                    tabInfos[tabPage].Uri = newFilePath;
                    tabPage.Text = newFileName;
                }

                ResetProject();

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
            f.Dir = treeView1.SelectedNode.Name;
            f.OkClicked = delegate(string folderName)
            {
                Directory.CreateDirectory(Path.Combine(treeView1.SelectedNode.Name, folderName));

                ResetProject();

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
            ofd.FilterIndex = 5;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string destFileName = Path.Combine(CurrentProject.Dir, Path.GetFileName(ofd.FileName));
                File.Copy(ofd.FileName, destFileName);
                OpenFile(destFileName);

                ResetProject();

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
            if (CurrentProject != null)
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

                string tempprojectDir = CurrentProject.Dir;
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
                    CurrentProject.Name = projectName;
                    CurrentProject.Dir = projectDir;
                    ProjectProperty.WriteProjectProperty(CurrentProject);
                }
                treeView1.Nodes[0].Name = CurrentProject.Dir;

                tempprojectModify = false;
                if (tempproject)
                {
                    Directory.Delete(tempprojectDir, true);
                }
                tempproject = false;
            };
            f.Show();
        }

        private void ResetProject()
        {
            this.Text = CurrentProject.Name + " - Witch's Hat";

            treeView1.ProjectName = CurrentProject.Name;
            treeView1.ProjectDir = CurrentProject.Dir;
            if (server != null)
            {
                server.RootDir = CurrentProject.Dir;
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
    }

}
