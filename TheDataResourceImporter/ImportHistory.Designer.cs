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
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewImportHistory)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridViewImportHistory
            // 
            this.dataGridViewImportHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewImportHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewImportHistory.Location = new System.Drawing.Point(0, 0);
            this.dataGridViewImportHistory.Name = "dataGridViewImportHistory";
            this.dataGridViewImportHistory.RowTemplate.Height = 23;
            this.dataGridViewImportHistory.Size = new System.Drawing.Size(757, 606);
            this.dataGridViewImportHistory.TabIndex = 0;
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
    }
}