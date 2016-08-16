using System.Windows.Forms;

namespace TheDataResourceImporter
{
    partial class Main
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.importProgressBar = new System.Windows.Forms.ProgressBar();
            this.progressLabel = new System.Windows.Forms.Label();
            this.textBoxDetail = new System.Windows.Forms.TextBox();
            this.statusLabel = new System.Windows.Forms.Label();
            this.fileDialogLabel = new System.Windows.Forms.Label();
            this.fileTypeLabel = new System.Windows.Forms.Label();
            this.cbFileType = new System.Windows.Forms.ComboBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnAbort = new System.Windows.Forms.Button();
            this.btn_Choose = new System.Windows.Forms.Button();
            this.tb_FilePath = new System.Windows.Forms.TextBox();
            this.labelProgressMsg = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.labelStatus = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.labelcurrentArchive = new System.Windows.Forms.Label();
            this.buttonReset = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.labelelapsedTotalTime = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.labelAverageElapsedTime = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.labelImportCountPerSec = new System.Windows.Forms.Label();
            this.mainMenu = new System.Windows.Forms.MenuStrip();
            this.menuShowImportHistory = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.menuCheckHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.checkBoxClearExistData = new System.Windows.Forms.CheckBox();
            this.mainMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // importProgressBar
            // 
            this.importProgressBar.Location = new System.Drawing.Point(114, 564);
            this.importProgressBar.Name = "importProgressBar";
            this.importProgressBar.Size = new System.Drawing.Size(394, 19);
            this.importProgressBar.Step = 1;
            this.importProgressBar.TabIndex = 0;
            // 
            // progressLabel
            // 
            this.progressLabel.AutoSize = true;
            this.progressLabel.Location = new System.Drawing.Point(47, 571);
            this.progressLabel.Name = "progressLabel";
            this.progressLabel.Size = new System.Drawing.Size(41, 12);
            this.progressLabel.TabIndex = 1;
            this.progressLabel.Text = "进度：";
            // 
            // textBoxDetail
            // 
            this.textBoxDetail.Location = new System.Drawing.Point(124, 189);
            this.textBoxDetail.Multiline = true;
            this.textBoxDetail.Name = "textBoxDetail";
            this.textBoxDetail.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxDetail.Size = new System.Drawing.Size(455, 287);
            this.textBoxDetail.TabIndex = 2;
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(57, 197);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(41, 12);
            this.statusLabel.TabIndex = 3;
            this.statusLabel.Text = "详细：";
            // 
            // fileDialogLabel
            // 
            this.fileDialogLabel.AutoSize = true;
            this.fileDialogLabel.Location = new System.Drawing.Point(23, 102);
            this.fileDialogLabel.Name = "fileDialogLabel";
            this.fileDialogLabel.Size = new System.Drawing.Size(65, 12);
            this.fileDialogLabel.TabIndex = 4;
            this.fileDialogLabel.Text = "文件路径：";
            // 
            // fileTypeLabel
            // 
            this.fileTypeLabel.AutoSize = true;
            this.fileTypeLabel.Location = new System.Drawing.Point(23, 39);
            this.fileTypeLabel.Name = "fileTypeLabel";
            this.fileTypeLabel.Size = new System.Drawing.Size(65, 12);
            this.fileTypeLabel.TabIndex = 5;
            this.fileTypeLabel.Text = "文档类型：";
            // 
            // cbFileType
            // 
            this.cbFileType.FormattingEnabled = true;

            


            this.cbFileType.Items.AddRange(new object[] {
            "中国专利全文代码化数据",
            "中国专利全文图像数据",
            "中国专利标准化全文文本数据",
            "中国专利标准化全文图像数据",
            "中国专利公报数据",
            "中国专利著录项目与文摘数据",
            "中国专利法律状态数据",
            "中国专利法律状态变更翻译数据",
            "中国标准化简单引文数据",
            "专利缴费数据",
            "公司代码库",
            "区域代码库",
            "美国专利全文文本数据（标准化）",
            "欧专局专利全文文本数据（标准化）",
            "韩国专利全文代码化数据（标准化）",
            "瑞士专利全文代码化数据（标准化）",
            "英国专利全文代码化数据（标准化）",
            "日本专利全文代码化数据（标准化）",
            "中国发明申请专利数据（DI）",
            "中国发明授权专利数据（DI）",
            "中国实用新型专利数据（DI）",
            "中国外观设计专利数据（DI）",
            "中国专利生物序列数据（DI）",
            "中国专利摘要英文翻译数据（DI）",
            "专利同族数据（DI）",
            "全球专利引文数据（DI）",
            "中国专利费用信息数据（DI）",
            "中国专利通知书数据（DI）",
            "中国法律状态标引库（DI）",
            "专利分类数据(分类号码)（DI）",
            "世界法律状态数据（DI）",
            "DOCDB数据（DI）",
            "美国专利著录项及全文数据（US）（DI）",
            "韩国专利著录项及全文数据（KR）（DI）",
            "欧洲专利局专利著录项及全文数据（EP）（DI）",
            "国际知识产权组织专利著录项及全文数据（WIPO)（DI）",
            "加拿大专利著录项及全文数据（CA）（DI）",
            "俄罗斯专利著录项及全文数据（RU）（DI）",
            "英国专利全文数据（GB）（DI）",
            "瑞士专利全文数据（CH）（DI）",
            "日本专利著录项及全文数据（JP）（DI）",
            "德国专利著录项及全文数据（DE）（DI）",
            "法国专利著录项及全文数据（FR）（DI）",
            "比利时专利全文数据（BE）（标准化）",
            "奥地利专利全文数据（AT）（标准化）",
            "西班牙专利全文数据（ES）（标准化）",
            "波兰专利著录项及全文数据（PL）（标准化）",
            "以色列专利著录项及全文数据（IL）（标准化）",
            "新加坡专利著录项及全文数据（SG）（标准化）",
            "台湾专利著录项及全文数据（TW）（DI）",
            "香港专利著录项数据（HK）（DI）",
            "澳门专利著录项数据（MO）（DI）",
            "欧亚组织专利著录项及全文数据（EA）（DI）",
            "美国外观设计专利数据（DI）",
            "日本外观设计专利数据（DI）",
            "韩国外观设计专利数据（DI）",
            "德国外观设计专利数据（DI）",
            "法国外观设计专利数据（DI）",
            "俄罗斯外观设计专利数据（DI）",
            "中国专利全文数据PDF（DI）",
            "国外专利全文数据PDF（DI）",
            "日本专利文摘英文翻译数据（PAJ)（DI）",
            "韩国专利文摘英文翻译数据(KPA)（DI）",
            "俄罗斯专利文摘英文翻译数据（DI）",
            "中国商标",
            "中国商标许可数据",
            "中国商标转让数据",
            "马德里商标进入中国",
            "中国驰名商标数据",
            "美国申请商标",
            "美国转让商标",
            "美国审判商标",
            "社内外知识产权图书题录数据",
            "民国书",
            "中外期刊的著录项目与文摘数据",
            "中国法院判例初加工数据",
            "中国商标分类数据",
            "美国商标图形分类数据",
            "美国商标美国分类数据",
            "马德里商标购买数据",
            "中国专利代理知识产权法律法规加工数据",
            "中国集成电路布图公告及事务数据",
            "中国知识产权海关备案数据",
            "国外专利生物序列加工成品数据",
            "中国专利复审数据",
            "中国专利无效数据",
            "中国专利的判决书数据",
            "中国生物序列深加工数据",
            "中国中药专利翻译数据",
            "中国化学药物专利深加工数据",
            "中国中药专利深加工数据"});
            this.cbFileType.Location = new System.Drawing.Point(124, 31);
            this.cbFileType.Name = "cbFileType";
            this.cbFileType.Size = new System.Drawing.Size(327, 20);
            this.cbFileType.TabIndex = 6;
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(124, 147);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 7;
            this.btnStart.Text = "开始";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnAbort
            // 
            this.btnAbort.Location = new System.Drawing.Point(244, 147);
            this.btnAbort.Name = "btnAbort";
            this.btnAbort.Size = new System.Drawing.Size(75, 23);
            this.btnAbort.TabIndex = 8;
            this.btnAbort.Text = "强制终止";
            this.btnAbort.UseVisualStyleBackColor = true;
            this.btnAbort.Click += new System.EventHandler(this.btnAbort_Click);
            // 
            // btn_Choose
            // 
            this.btn_Choose.Location = new System.Drawing.Point(457, 91);
            this.btn_Choose.Name = "btn_Choose";
            this.btn_Choose.Size = new System.Drawing.Size(75, 23);
            this.btn_Choose.TabIndex = 10;
            this.btn_Choose.Text = "选择";
            this.btn_Choose.UseVisualStyleBackColor = true;
            this.btn_Choose.Click += new System.EventHandler(this.btn_Choose_Click);
            // 
            // tb_FilePath
            // 
            this.tb_FilePath.BackColor = System.Drawing.SystemColors.Window;
            this.tb_FilePath.Location = new System.Drawing.Point(124, 92);
            this.tb_FilePath.Name = "tb_FilePath";
            this.tb_FilePath.ReadOnly = true;
            this.tb_FilePath.Size = new System.Drawing.Size(327, 21);
            this.tb_FilePath.TabIndex = 9;
            // 
            // labelProgressMsg
            // 
            this.labelProgressMsg.AutoSize = true;
            this.labelProgressMsg.Location = new System.Drawing.Point(48, 615);
            this.labelProgressMsg.Name = "labelProgressMsg";
            this.labelProgressMsg.Size = new System.Drawing.Size(89, 12);
            this.labelProgressMsg.TabIndex = 11;
            this.labelProgressMsg.Text = "              ";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(401, 615);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(0, 12);
            this.label8.TabIndex = 18;
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(536, 571);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(23, 12);
            this.labelStatus.TabIndex = 22;
            this.labelStatus.Text = "0/0";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 520);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 12);
            this.label2.TabIndex = 23;
            this.label2.Text = "当前压缩包：";
            // 
            // labelcurrentArchive
            // 
            this.labelcurrentArchive.AutoSize = true;
            this.labelcurrentArchive.Location = new System.Drawing.Point(114, 520);
            this.labelcurrentArchive.Name = "labelcurrentArchive";
            this.labelcurrentArchive.Size = new System.Drawing.Size(0, 12);
            this.labelcurrentArchive.TabIndex = 24;
            // 
            // buttonReset
            // 
            this.buttonReset.Location = new System.Drawing.Point(367, 147);
            this.buttonReset.Name = "buttonReset";
            this.buttonReset.Size = new System.Drawing.Size(75, 23);
            this.buttonReset.TabIndex = 25;
            this.buttonReset.Text = "重置";
            this.buttonReset.UseVisualStyleBackColor = true;
            this.buttonReset.Click += new System.EventHandler(this.buttonReset_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(48, 658);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 12);
            this.label4.TabIndex = 26;
            this.label4.Text = "耗时：";
            // 
            // labelelapsedTotalTime
            // 
            this.labelelapsedTotalTime.AutoSize = true;
            this.labelelapsedTotalTime.Location = new System.Drawing.Point(95, 658);
            this.labelelapsedTotalTime.Name = "labelelapsedTotalTime";
            this.labelelapsedTotalTime.Size = new System.Drawing.Size(0, 12);
            this.labelelapsedTotalTime.TabIndex = 27;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(242, 657);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(65, 12);
            this.label7.TabIndex = 28;
            this.label7.Text = "每件耗时：";
            // 
            // labelAverageElapsedTime
            // 
            this.labelAverageElapsedTime.AutoSize = true;
            this.labelAverageElapsedTime.Location = new System.Drawing.Point(317, 657);
            this.labelAverageElapsedTime.Name = "labelAverageElapsedTime";
            this.labelAverageElapsedTime.Size = new System.Drawing.Size(0, 12);
            this.labelAverageElapsedTime.TabIndex = 29;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(382, 657);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(119, 12);
            this.label5.TabIndex = 30;
            this.label5.Text = "入库速度（件/秒）：";
            // 
            // labelImportCountPerSec
            // 
            this.labelImportCountPerSec.AutoSize = true;
            this.labelImportCountPerSec.Location = new System.Drawing.Point(507, 658);
            this.labelImportCountPerSec.Name = "labelImportCountPerSec";
            this.labelImportCountPerSec.Size = new System.Drawing.Size(0, 12);
            this.labelImportCountPerSec.TabIndex = 31;
            // 
            // mainMenu
            // 
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuShowImportHistory,
            this.menuHelp});
            this.mainMenu.Location = new System.Drawing.Point(0, 0);
            this.mainMenu.Name = "mainMenu";
            this.mainMenu.Size = new System.Drawing.Size(625, 25);
            this.mainMenu.TabIndex = 32;
            this.mainMenu.Text = "menuStrip1";
            // 
            // menuShowImportHistory
            // 
            this.menuShowImportHistory.Name = "menuShowImportHistory";
            this.menuShowImportHistory.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.H)));
            this.menuShowImportHistory.Size = new System.Drawing.Size(68, 21);
            this.menuShowImportHistory.Text = "导入历史";
            this.menuShowImportHistory.Click += new System.EventHandler(this.menuShowImportHistory_Click);
            // 
            // menuHelp
            // 
            this.menuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuCheckHelp,
            this.menuAbout});
            this.menuHelp.Name = "menuHelp";
            this.menuHelp.Size = new System.Drawing.Size(44, 21);
            this.menuHelp.Text = "帮助";
            // 
            // menuCheckHelp
            // 
            this.menuCheckHelp.Enabled = false;
            this.menuCheckHelp.Name = "menuCheckHelp";
            this.menuCheckHelp.Size = new System.Drawing.Size(124, 22);
            this.menuCheckHelp.Text = "查看帮助";
            // 
            // menuAbout
            // 
            this.menuAbout.Name = "menuAbout";
            this.menuAbout.Size = new System.Drawing.Size(124, 22);
            this.menuAbout.Text = "关于";
            this.menuAbout.Click += new System.EventHandler(this.menuAbout_Click);
            // 
            // checkBoxClearExistData
            // 
            this.checkBoxClearExistData.AutoSize = true;
            this.checkBoxClearExistData.Location = new System.Drawing.Point(470, 34);
            this.checkBoxClearExistData.Name = "checkBoxClearExistData";
            this.checkBoxClearExistData.Size = new System.Drawing.Size(96, 16);
            this.checkBoxClearExistData.TabIndex = 33;
            this.checkBoxClearExistData.Text = "清空现有数据";
            this.checkBoxClearExistData.UseVisualStyleBackColor = true;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(625, 699);
            this.Controls.Add(this.checkBoxClearExistData);
            this.Controls.Add(this.labelImportCountPerSec);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.labelAverageElapsedTime);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.labelelapsedTotalTime);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.buttonReset);
            this.Controls.Add(this.labelcurrentArchive);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.labelProgressMsg);
            this.Controls.Add(this.btn_Choose);
            this.Controls.Add(this.tb_FilePath);
            this.Controls.Add(this.btnAbort);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.cbFileType);
            this.Controls.Add(this.fileTypeLabel);
            this.Controls.Add(this.fileDialogLabel);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.textBoxDetail);
            this.Controls.Add(this.progressLabel);
            this.Controls.Add(this.importProgressBar);
            this.Controls.Add(this.mainMenu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.mainMenu;
            this.Name = "Main";
            this.Text = "数据资源导入工具";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Main_FormClosing);
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar importProgressBar;
        private System.Windows.Forms.Label progressLabel;
        private System.Windows.Forms.TextBox textBoxDetail;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label fileTypeLabel;
        private System.Windows.Forms.Label fileDialogLabel;
        private System.Windows.Forms.ComboBox cbFileType;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnAbort;
        private System.Windows.Forms.Button btn_Choose;
        private System.Windows.Forms.TextBox tb_FilePath;
        private System.Windows.Forms.Label labelProgressMsg;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelcurrentArchive;
        private System.Windows.Forms.Button buttonReset;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelelapsedTotalTime;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label labelAverageElapsedTime;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label labelImportCountPerSec;
        private System.Windows.Forms.MenuStrip mainMenu;
        private System.Windows.Forms.ToolStripMenuItem menuShowImportHistory;
        private System.Windows.Forms.ToolStripMenuItem menuHelp;
        private System.Windows.Forms.ToolStripMenuItem menuCheckHelp;
        private System.Windows.Forms.ToolStripMenuItem menuAbout;
        private System.Windows.Forms.CheckBox checkBoxClearExistData;
    }
}

