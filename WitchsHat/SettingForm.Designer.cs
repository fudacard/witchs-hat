namespace WitchsHat
{
    partial class SettingForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("動作");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("ブラウザー");
            System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("全般", new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2});
            System.Windows.Forms.TreeNode treeNode4 = new System.Windows.Forms.TreeNode("エディタ");
            System.Windows.Forms.TreeNode treeNode5 = new System.Windows.Forms.TreeNode("上級者設定");
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.SuggestCheckBox = new System.Windows.Forms.CheckBox();
            this.EnchantjsDownloadcheckBox = new System.Windows.Forms.CheckBox();
            this.TempProjectCheckBox = new System.Windows.Forms.CheckBox();
            this.button3 = new System.Windows.Forms.Button();
            this.ProjectsPathTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.EncodingComboBox = new System.Windows.Forms.ComboBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.RunBrowserComboBox = new System.Windows.Forms.ComboBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.IndentSpaceRadioButton = new System.Windows.Forms.RadioButton();
            this.IndentTabRadioButton = new System.Windows.Forms.RadioButton();
            this.FontSizeLabel = new System.Windows.Forms.Label();
            this.FontNameLabel = new System.Windows.Forms.Label();
            this.FontPreviewTextBox = new System.Windows.Forms.TextBox();
            this.FontSelectButton = new System.Windows.Forms.Button();
            this.TitleLabel = new System.Windows.Forms.Label();
            this.panel4 = new System.Windows.Forms.Panel();
            this.ServerPortTextBox = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.ServerCheckBox = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel4.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(509, 354);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(590, 354);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "キャンセル";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // treeView1
            // 
            this.treeView1.FullRowSelect = true;
            this.treeView1.Location = new System.Drawing.Point(13, 13);
            this.treeView1.Name = "treeView1";
            treeNode1.Name = "ノード1";
            treeNode1.Text = "動作";
            treeNode2.Name = "ノード2";
            treeNode2.Text = "ブラウザー";
            treeNode3.Name = "ノード0";
            treeNode3.Text = "全般";
            treeNode4.Name = "ノード3";
            treeNode4.Text = "エディタ";
            treeNode5.Name = "ノード0";
            treeNode5.Text = "上級者設定";
            this.treeView1.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode3,
            treeNode4,
            treeNode5});
            this.treeView1.ShowLines = false;
            this.treeView1.ShowRootLines = false;
            this.treeView1.Size = new System.Drawing.Size(145, 364);
            this.treeView1.TabIndex = 2;
            this.treeView1.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.SuggestCheckBox);
            this.panel1.Controls.Add(this.EnchantjsDownloadcheckBox);
            this.panel1.Controls.Add(this.TempProjectCheckBox);
            this.panel1.Controls.Add(this.button3);
            this.panel1.Controls.Add(this.ProjectsPathTextBox);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.EncodingComboBox);
            this.panel1.Location = new System.Drawing.Point(186, 46);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(479, 302);
            this.panel1.TabIndex = 3;
            // 
            // SuggestCheckBox
            // 
            this.SuggestCheckBox.AutoSize = true;
            this.SuggestCheckBox.Location = new System.Drawing.Point(7, 182);
            this.SuggestCheckBox.Name = "SuggestCheckBox";
            this.SuggestCheckBox.Size = new System.Drawing.Size(124, 16);
            this.SuggestCheckBox.TabIndex = 7;
            this.SuggestCheckBox.Text = "入力候補を表示する";
            this.SuggestCheckBox.UseVisualStyleBackColor = true;
            // 
            // EnchantjsDownloadcheckBox
            // 
            this.EnchantjsDownloadcheckBox.AutoSize = true;
            this.EnchantjsDownloadcheckBox.Location = new System.Drawing.Point(7, 159);
            this.EnchantjsDownloadcheckBox.Name = "EnchantjsDownloadcheckBox";
            this.EnchantjsDownloadcheckBox.Size = new System.Drawing.Size(203, 16);
            this.EnchantjsDownloadcheckBox.TabIndex = 6;
            this.EnchantjsDownloadcheckBox.Text = "起動時にenchant.jsをダウンロードする";
            this.EnchantjsDownloadcheckBox.UseVisualStyleBackColor = true;
            // 
            // TempProjectCheckBox
            // 
            this.TempProjectCheckBox.AutoSize = true;
            this.TempProjectCheckBox.Location = new System.Drawing.Point(7, 136);
            this.TempProjectCheckBox.Name = "TempProjectCheckBox";
            this.TempProjectCheckBox.Size = new System.Drawing.Size(150, 16);
            this.TempProjectCheckBox.TabIndex = 5;
            this.TempProjectCheckBox.Text = "起動時にプロジェクトを作る";
            this.TempProjectCheckBox.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(335, 26);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 4;
            this.button3.Text = "参照";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // ProjectsPathTextBox
            // 
            this.ProjectsPathTextBox.Location = new System.Drawing.Point(7, 28);
            this.ProjectsPathTextBox.Name = "ProjectsPathTextBox";
            this.ProjectsPathTextBox.Size = new System.Drawing.Size(312, 19);
            this.ProjectsPathTextBox.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(5, 13);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(90, 12);
            this.label3.TabIndex = 2;
            this.label3.Text = "プロジェクトの場所";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 78);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(279, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "デフォルトの文字コード (プロジェクトの設定が優先されます)";
            // 
            // EncodingComboBox
            // 
            this.EncodingComboBox.Enabled = false;
            this.EncodingComboBox.FormattingEnabled = true;
            this.EncodingComboBox.Items.AddRange(new object[] {
            "UTF-8"});
            this.EncodingComboBox.Location = new System.Drawing.Point(7, 94);
            this.EncodingComboBox.Name = "EncodingComboBox";
            this.EncodingComboBox.Size = new System.Drawing.Size(121, 20);
            this.EncodingComboBox.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label4);
            this.panel2.Controls.Add(this.RunBrowserComboBox);
            this.panel2.Location = new System.Drawing.Point(185, 46);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(479, 302);
            this.panel2.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 13);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(117, 12);
            this.label4.TabIndex = 2;
            this.label4.Text = "実行に使用するブラウザ";
            // 
            // RunBrowserComboBox
            // 
            this.RunBrowserComboBox.FormattingEnabled = true;
            this.RunBrowserComboBox.Location = new System.Drawing.Point(8, 29);
            this.RunBrowserComboBox.Name = "RunBrowserComboBox";
            this.RunBrowserComboBox.Size = new System.Drawing.Size(186, 20);
            this.RunBrowserComboBox.TabIndex = 1;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.IndentSpaceRadioButton);
            this.panel3.Controls.Add(this.IndentTabRadioButton);
            this.panel3.Controls.Add(this.FontSizeLabel);
            this.panel3.Controls.Add(this.FontNameLabel);
            this.panel3.Controls.Add(this.FontPreviewTextBox);
            this.panel3.Controls.Add(this.FontSelectButton);
            this.panel3.Location = new System.Drawing.Point(186, 46);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(479, 302);
            this.panel3.TabIndex = 6;
            // 
            // IndentSpaceRadioButton
            // 
            this.IndentSpaceRadioButton.AutoSize = true;
            this.IndentSpaceRadioButton.Location = new System.Drawing.Point(7, 161);
            this.IndentSpaceRadioButton.Name = "IndentSpaceRadioButton";
            this.IndentSpaceRadioButton.Size = new System.Drawing.Size(143, 16);
            this.IndentSpaceRadioButton.TabIndex = 6;
            this.IndentSpaceRadioButton.TabStop = true;
            this.IndentSpaceRadioButton.Text = "インデントにスペースを使う";
            this.IndentSpaceRadioButton.UseVisualStyleBackColor = true;
            // 
            // IndentTabRadioButton
            // 
            this.IndentTabRadioButton.AutoSize = true;
            this.IndentTabRadioButton.Location = new System.Drawing.Point(7, 137);
            this.IndentTabRadioButton.Name = "IndentTabRadioButton";
            this.IndentTabRadioButton.Size = new System.Drawing.Size(122, 16);
            this.IndentTabRadioButton.TabIndex = 5;
            this.IndentTabRadioButton.TabStop = true;
            this.IndentTabRadioButton.Text = "インデントにタブを使う";
            this.IndentTabRadioButton.UseVisualStyleBackColor = true;
            // 
            // FontSizeLabel
            // 
            this.FontSizeLabel.AutoSize = true;
            this.FontSizeLabel.Location = new System.Drawing.Point(9, 28);
            this.FontSizeLabel.Name = "FontSizeLabel";
            this.FontSizeLabel.Size = new System.Drawing.Size(35, 12);
            this.FontSizeLabel.TabIndex = 3;
            this.FontSizeLabel.Text = "label7";
            // 
            // FontNameLabel
            // 
            this.FontNameLabel.AutoSize = true;
            this.FontNameLabel.Location = new System.Drawing.Point(7, 4);
            this.FontNameLabel.Name = "FontNameLabel";
            this.FontNameLabel.Size = new System.Drawing.Size(35, 12);
            this.FontNameLabel.TabIndex = 2;
            this.FontNameLabel.Text = "label2";
            // 
            // FontPreviewTextBox
            // 
            this.FontPreviewTextBox.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.FontPreviewTextBox.Location = new System.Drawing.Point(7, 62);
            this.FontPreviewTextBox.Multiline = true;
            this.FontPreviewTextBox.Name = "FontPreviewTextBox";
            this.FontPreviewTextBox.ReadOnly = true;
            this.FontPreviewTextBox.Size = new System.Drawing.Size(295, 58);
            this.FontPreviewTextBox.TabIndex = 1;
            this.FontPreviewTextBox.Text = "enchant.jsでゲーム製作";
            // 
            // FontSelectButton
            // 
            this.FontSelectButton.Location = new System.Drawing.Point(227, 24);
            this.FontSelectButton.Name = "FontSelectButton";
            this.FontSelectButton.Size = new System.Drawing.Size(75, 23);
            this.FontSelectButton.TabIndex = 0;
            this.FontSelectButton.Text = "変更";
            this.FontSelectButton.UseVisualStyleBackColor = true;
            this.FontSelectButton.Click += new System.EventHandler(this.FontSelectButton_Click);
            // 
            // TitleLabel
            // 
            this.TitleLabel.AutoSize = true;
            this.TitleLabel.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.TitleLabel.Location = new System.Drawing.Point(185, 13);
            this.TitleLabel.Name = "TitleLabel";
            this.TitleLabel.Size = new System.Drawing.Size(46, 16);
            this.TitleLabel.TabIndex = 7;
            this.TitleLabel.Text = "label2";
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.ServerPortTextBox);
            this.panel4.Controls.Add(this.label6);
            this.panel4.Controls.Add(this.ServerCheckBox);
            this.panel4.Location = new System.Drawing.Point(185, 46);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(480, 302);
            this.panel4.TabIndex = 8;
            // 
            // ServerPortTextBox
            // 
            this.ServerPortTextBox.Location = new System.Drawing.Point(79, 33);
            this.ServerPortTextBox.Name = "ServerPortTextBox";
            this.ServerPortTextBox.Size = new System.Drawing.Size(100, 19);
            this.ServerPortTextBox.TabIndex = 11;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(16, 36);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(57, 12);
            this.label6.TabIndex = 10;
            this.label6.Text = "ポート番号";
            // 
            // ServerCheckBox
            // 
            this.ServerCheckBox.AutoSize = true;
            this.ServerCheckBox.Location = new System.Drawing.Point(18, 13);
            this.ServerCheckBox.Name = "ServerCheckBox";
            this.ServerCheckBox.Size = new System.Drawing.Size(140, 16);
            this.ServerCheckBox.TabIndex = 9;
            this.ServerCheckBox.Text = "内部サーバーを起動する";
            this.ServerCheckBox.UseVisualStyleBackColor = true;
            // 
            // SettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(677, 390);
            this.Controls.Add(this.TitleLabel);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel1);
            this.Name = "SettingForm";
            this.Text = "オプション";
            this.Load += new System.EventHandler(this.SettingForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox EncodingComboBox;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.TextBox ProjectsPathTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox RunBrowserComboBox;
        private System.Windows.Forms.CheckBox TempProjectCheckBox;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label TitleLabel;
        private System.Windows.Forms.TextBox FontPreviewTextBox;
        private System.Windows.Forms.Button FontSelectButton;
        private System.Windows.Forms.Label FontSizeLabel;
        private System.Windows.Forms.Label FontNameLabel;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.TextBox ServerPortTextBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox ServerCheckBox;
        private System.Windows.Forms.CheckBox EnchantjsDownloadcheckBox;
        private System.Windows.Forms.CheckBox SuggestCheckBox;
        private System.Windows.Forms.RadioButton IndentSpaceRadioButton;
        private System.Windows.Forms.RadioButton IndentTabRadioButton;
    }
}