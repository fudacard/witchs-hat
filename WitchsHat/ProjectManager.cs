using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WitchsHat
{
    public class ProjectManager
    {
        public Form1 form { get; set; }
        public TabManager tabManager { get; set; }
        // テンポラリプロジェクトかどうか
        public bool tempproject;
        public bool tempprojectModify;
        public ProjectProperty CurrentProject;
        public string projectsPath { get; set; }


        /// <summary>
        /// ファイルの変更を確認し保存と処理の続行の可否を返す
        /// </summary>
        /// <returns></returns>
        public bool NewProjectCheck()
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
                    f.ProjectsPath = projectsPath;
                    f.OkClicked += delegate(string projectName, string projectDir)
                    {
                        // 保存する
                        SaveTempProject(projectName, projectDir);

                        CloseProject();

                        form.ResetProject();
                    };
                    f.ShowDialog(form);

                }
                else if (result == DialogResult.Cancel)
                {
                    continueFlag = false;
                }

            }
            else
            {
                bool modify = false;
                foreach (var pair in tabManager.tabInfos)
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

            return continueFlag;
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

        public void CloseProject()
        {
            tabManager.CloseAllTab();

            if (tempproject)
            {
                Directory.Delete(CurrentProject.Dir, true);
                tempproject = false;
                tempprojectModify = false;
            }
            CurrentProject = null;
        }

        public void CreateProject(string projectName, string projectDir, WHTemplate projectTemplate)
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

            form.ResetProject();

            // main.jsを開く
            if (File.Exists(Path.Combine(CurrentProject.Dir, "main.js")))
            {
                form.OpenTab(Path.Combine(CurrentProject.Dir, "main.js"));
            }
        }

        public void CreateTemporaryProject()
        {

            string templateDir = "";
            WHTemplate projectTemplate = null;
            if (form.HasEnchantjs())
            {
                templateDir = Path.Combine(Application.StartupPath, @"Data\Templates\02KumaProject");
                projectTemplate = WHTemplate.ReadTemplate(Path.Combine(Application.StartupPath, @"Data\Templates\02KumaProject\KumaProject.template"));
            }
            else
            {
                templateDir = Path.Combine(Application.StartupPath, @"Data\Templates\01EnchantProject");
                projectTemplate = WHTemplate.ReadTemplate(Path.Combine(Application.StartupPath, @"Data\Templates\01EnchantProject\EnchantProject.template"));
            }
            form.FileImportDirs[0] = templateDir;
            projectTemplate.CheckFiles(form.FileImportDirs);
            CreateProject("NoTitleProject", Path.Combine(Path.GetTempPath(), "Witchs Hat", "NoTitleProject"), projectTemplate);
            tempproject = true;
        }

        public void ProjectModified()
        {
            if (tempproject)
            {
                tempprojectModify = true;
            }
        }

        public bool IsProjectOpened()
        {
            return CurrentProject != null;
        }
    }
}
