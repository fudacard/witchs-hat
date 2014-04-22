using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WitchsHat
{
    public partial class EnchantjsDownloadForm : Form
    {
        bool running;
        System.Net.WebClient downloadClient = null;
        string filepath;
        string outputdir;
        TaskScheduler taskScheduler;

        public EnchantjsDownloadForm()
        {
            InitializeComponent();
        }

        public string enchantjsUrl
        {
            get;
            set;
        }

        private void EnchantjsDownloadForm_Load(object sender, EventArgs e)
        {
            Console.WriteLine("ダウンロード開始");
            running = true;
            button1.Enabled = true;

            Uri u = new Uri(enchantjsUrl);

            //ダウンロードしたファイルの保存先
            outputdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Witchs Hat");
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Witchs Hat", u.ToString().Substring(u.ToString().LastIndexOf('/') + 1));
            running = true;
            this.progressBar1.Maximum = 100;
            filepath = path;
            taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            // ファイルがすでにあるかどうか
            Console.WriteLine(path);
            if (File.Exists(path))
            {
                textBox1.Text = "ファイルがすでにあります。\r\n";
                //extract();
            }
            else
            {
                textBox1.Text = "ファイルをダウンロードします。\r\n";

                System.IO.Directory.CreateDirectory(outputdir);
                textBox1.Text += u + "\r\n";

                //WebClientの作成
                if (downloadClient == null)
                {
                    downloadClient = new System.Net.WebClient();
                    //イベントハンドラの作成
                    downloadClient.DownloadProgressChanged +=
                        new System.Net.DownloadProgressChangedEventHandler(
                            downloadClient_DownloadProgressChanged);
                    downloadClient.DownloadFileCompleted +=
                        new System.ComponentModel.AsyncCompletedEventHandler(
                            downloadClient_DownloadFileCompleted);
                }
                //非同期ダウンロードを開始する
                downloadClient.DownloadFileAsync(u, path);

            }
        }
        private void downloadClient_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {

            Task.Factory.StartNew(() =>
            {
                if (running)
                {
                    progressBar1.Value = (e.ProgressPercentage / 2);
                    label1.Text = "ダウンロードしています。 (" + e.ProgressPercentage + "%)";
                }
            }, System.Threading.CancellationToken.None, TaskCreationOptions.None, taskScheduler);
        }

        private void downloadClient_DownloadFileCompleted(object sender,
            System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error != null && !e.Cancelled)
            {
                Console.WriteLine("エラー:{0}", e.Error.Message);
                running = false;
                Task.Factory.StartNew(() =>
                {
                    textBox1.Text += e.Error.Message + "\r\n";
                    textBox1.Text += "エラーによりダウンロードを中止します。\r\n";
                    label1.Text = "エラーが発生しました。";
                }, System.Threading.CancellationToken.None, TaskCreationOptions.None, taskScheduler);
                File.Delete(filepath);
                progressBar1.Value = 0;
            }

            if (e.Cancelled)
            {
                Console.WriteLine("キャンセルされました。");
                File.Delete(filepath);
                progressBar1.Value = 0;
                //label2.Text = "0%";
                Task.Factory.StartNew(() =>
                {
                    label1.Text = "ダウンロードがキャンセルされました。";
                    textBox1.Text += "ダウンロードをキャンセルします。\r\n";
                }, System.Threading.CancellationToken.None, TaskCreationOptions.None, taskScheduler);
                File.Delete(filepath);
            }
            if (e.Error == null && !e.Cancelled)
            {
                Console.WriteLine("ダウンロードが完了しました。");
                Task.Factory.StartNew(() =>
                {
                    textBox1.Text += "ダウンロードが完了しました。\r\n";
                }, System.Threading.CancellationToken.None, TaskCreationOptions.None, taskScheduler);
                extract();
            }

        }

        private void extract()
        {
            Task.Factory.StartNew(() =>
            {
                textBox1.Text += "ファイルを展開しています。\r\n";
            }, System.Threading.CancellationToken.None, TaskCreationOptions.None, taskScheduler);
            var t = Task.Factory.StartNew(() =>
            {
                ZipFile zip = ZipFile.Read(filepath);
                string zipfilename = zip.ElementAt(0).FileName;
                zipfilename = zipfilename.Substring(0, zipfilename.Length - 1);
                Console.WriteLine(zip.Count);
                int i = 0;
                foreach (ZipEntry entry in zip.Entries)
                {
                    entry.Extract(outputdir);
                    Task.Factory.StartNew(() =>
                    {
                        int percentage = 50 + (i * 50 / zip.Count);
                        progressBar1.Value = percentage;
                        label1.Text = "ファイルを展開しています。 " + (i * 100 / zip.Count) + "%";
                    }, System.Threading.CancellationToken.None, TaskCreationOptions.None, taskScheduler);

                    i++;
                    if (!running)
                    {
                        break;
                    }
                }
                if (running)
                {
                    zip.Dispose();
                    Directory.Move(Path.Combine(outputdir, zipfilename), Path.Combine(outputdir, "enchant.js"));
                    running = false;
                    Task.Factory.StartNew(() =>
                    {
                        textBox1.Text += "ファイルの展開が完了しました。\r\n";
                        label1.Text = "ダウンロードが完了しました。";
                        if (checkBox1.Checked)
                        {
                            Close();
                        }
                    }, System.Threading.CancellationToken.None, TaskCreationOptions.None, taskScheduler);
                }
                else
                {
                    zip.Dispose();
                    File.Delete(filepath);
                    Directory.Delete(Path.Combine(outputdir, zipfilename), true);
                    Task.Factory.StartNew(() =>
                    {
                        label1.Text = "ダウンロードがキャンセルされました。";
                        textBox1.Text += "ファイルの展開がキャンセルされました。\r\n";
                        progressBar1.Value = 0;
                    }, System.Threading.CancellationToken.None, TaskCreationOptions.None, taskScheduler);
                }
            });



        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (downloadClient != null && running)
            {
                DialogResult result = MessageBox.Show("ダウンロードをキャンセルしますか？", "", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                {
                    downloadClient.CancelAsync();
                    downloadClient = null;
                    running = false;
                    button1.Enabled = false;
                }
            }
        }

        private void EnchantjsDownloadForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (downloadClient != null && running)
            {
                DialogResult result = MessageBox.Show("ダウンロードをキャンセルしますか？", "", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                {
                    downloadClient.CancelAsync();
                    downloadClient = null;
                    running = false;
                    button1.Enabled = false;
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
