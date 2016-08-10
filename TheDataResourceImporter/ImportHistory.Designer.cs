namespace TheDataResourceImporter
{
    partial class ImportHistory
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
            this.dataGridViewImportHistory = new System.Windows.Forms.DataGridView();
            this.importSessionId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.importTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.totalEntriesCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.importedCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.错误数 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.checkError = new System.Windows.Forms.DataGridViewButtonColumn();
            this.cancelThisSession = new System.Windows.Forms.DataGridViewButtonColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewImportHistory)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridViewImportHistory
            // 
            this.dataGridViewImportHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewImportHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.importSessionId,
            this.importTime,
            this.totalEntriesCount,
            this.importedCount,
            this.错误数,
            this.checkError,
            this.cancelThisSession});
            this.dataGridViewImportHistory.Location = new System.Drawing.Point(12, 12);
            this.dataGridViewImportHistory.Name = "dataGridViewImportHistory";
            this.dataGridViewImportHistory.RowTemplate.Height = 23;
            this.dataGridViewImportHistory.Size = new System.Drawing.Size(743, 525);
            this.dataGridViewImportHistory.TabIndex = 0;
            // 
            // importSessionId
            // 
            this.importSessionId.HeaderText = "操作编号";
            this.importSessionId.Name = "importSessionId";
            // 
            // importTime
            // 
            this.importTime.HeaderText = "操作时间";
            this.importTime.Name = "importTime";
            // 
            // totalEntriesCount
            // 
            this.totalEntriesCount.HeaderText = "总条目数";
            this.totalEntriesCount.Name = "totalEntriesCount";
            // 
            // importedCount
            // 
            this.importedCount.HeaderText = "已入库条目数";
            this.importedCount.Name = "importedCount";
            // 
            // 错误数
            // 
            this.错误数.HeaderText = "错误数";
            this.错误数.Name = "错误数";
            // 
            // checkError
            // 
            this.checkError.HeaderText = "查看错误";
            this.checkError.Name = "checkError";
            this.checkError.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.checkError.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // cancelThisSession
            // 
            this.cancelThisSession.HeaderText = "回滚";
            this.cancelThisSession.Name = "cancelThisSession";
            this.cancelThisSession.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.cancelThisSession.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // ImportHistory
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(757, 606);
            this.Controls.Add(this.dataGridViewImportHistory);
            this.Name = "ImportHistory";
            this.Text = "导入历史";
            this.Load += new System.EventHandler(this.ImportHistory_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewImportHistory)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridViewImportHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn importSessionId;
        private System.Windows.Forms.DataGridViewTextBoxColumn importTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn totalEntriesCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn importedCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn 错误数;
        private System.Windows.Forms.DataGridViewButtonColumn checkError;
        private System.Windows.Forms.DataGridViewButtonColumn cancelThisSession;
    }
}