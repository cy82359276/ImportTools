namespace TheDataResourceImporter
{
    partial class errorList
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
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.sessionID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ErrorId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ZipOrDirPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.errorEntryNum = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ErrorType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ErrorMsg = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.IgnoreEror = new System.Windows.Forms.DataGridViewButtonColumn();
            this.Retry = new System.Windows.Forms.DataGridViewButtonColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.sessionID,
            this.ErrorId,
            this.ZipOrDirPath,
            this.errorEntryNum,
            this.ErrorType,
            this.ErrorMsg,
            this.IgnoreEror,
            this.Retry});
            this.dataGridView1.Location = new System.Drawing.Point(3, 13);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.Size = new System.Drawing.Size(848, 568);
            this.dataGridView1.TabIndex = 0;
            // 
            // sessionID
            // 
            this.sessionID.HeaderText = "导入操作ID";
            this.sessionID.Name = "sessionID";
            // 
            // ErrorId
            // 
            this.ErrorId.HeaderText = "错误编号";
            this.ErrorId.Name = "ErrorId";
            // 
            // ZipOrDirPath
            // 
            this.ZipOrDirPath.HeaderText = "错误压缩包或目录路径";
            this.ZipOrDirPath.Name = "ZipOrDirPath";
            // 
            // errorEntryNum
            // 
            this.errorEntryNum.HeaderText = "错误表目编号";
            this.errorEntryNum.Name = "errorEntryNum";
            // 
            // ErrorType
            // 
            this.ErrorType.HeaderText = "错误类型";
            this.ErrorType.Name = "ErrorType";
            // 
            // ErrorMsg
            // 
            this.ErrorMsg.HeaderText = "错误内容";
            this.ErrorMsg.Name = "ErrorMsg";
            // 
            // IgnoreEror
            // 
            this.IgnoreEror.HeaderText = "忽略错误";
            this.IgnoreEror.Name = "IgnoreEror";
            // 
            // Retry
            // 
            this.Retry.HeaderText = "重试";
            this.Retry.Name = "Retry";
            // 
            // errorList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(854, 595);
            this.Controls.Add(this.dataGridView1);
            this.Name = "errorList";
            this.Text = "错误列表";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn sessionID;
        private System.Windows.Forms.DataGridViewTextBoxColumn ErrorId;
        private System.Windows.Forms.DataGridViewTextBoxColumn ZipOrDirPath;
        private System.Windows.Forms.DataGridViewTextBoxColumn errorEntryNum;
        private System.Windows.Forms.DataGridViewTextBoxColumn ErrorType;
        private System.Windows.Forms.DataGridViewTextBoxColumn ErrorMsg;
        private System.Windows.Forms.DataGridViewButtonColumn IgnoreEror;
        private System.Windows.Forms.DataGridViewButtonColumn Retry;
    }
}