namespace TheDataResourceImporter
{
    partial class ImportBatchHistoryForm
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
            this.labelTotal = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.labelCurrentPage = new System.Windows.Forms.Label();
            this.buttonLast = new System.Windows.Forms.Button();
            this.buttonNext = new System.Windows.Forms.Button();
            this.buttonPrevious = new System.Windows.Forms.Button();
            this.buttonFirst = new System.Windows.Forms.Button();
            this.dataGridViewImportHistory = new System.Windows.Forms.DataGridView();
            this.buttonPgSize50 = new System.Windows.Forms.Button();
            this.buttonPgSize100 = new System.Windows.Forms.Button();
            this.buttonPgSize200 = new System.Windows.Forms.Button();
            this.buttonPgSize500 = new System.Windows.Forms.Button();
            this.buttonPgSize1000 = new System.Windows.Forms.Button();
            this.LabelPageSize = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxResType = new System.Windows.Forms.TextBox();
            this.buttonFiliterResType = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewImportHistory)).BeginInit();
            this.SuspendLayout();
            // 
            // labelTotal
            // 
            this.labelTotal.AutoSize = true;
            this.labelTotal.Location = new System.Drawing.Point(307, 899);
            this.labelTotal.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelTotal.Name = "labelTotal";
            this.labelTotal.Size = new System.Drawing.Size(39, 15);
            this.labelTotal.TabIndex = 15;
            this.labelTotal.Text = "    ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(284, 899);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(15, 15);
            this.label2.TabIndex = 14;
            this.label2.Text = "/";
            // 
            // labelCurrentPage
            // 
            this.labelCurrentPage.AutoSize = true;
            this.labelCurrentPage.Location = new System.Drawing.Point(237, 899);
            this.labelCurrentPage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelCurrentPage.Name = "labelCurrentPage";
            this.labelCurrentPage.Size = new System.Drawing.Size(39, 15);
            this.labelCurrentPage.TabIndex = 13;
            this.labelCurrentPage.Text = "    ";
            // 
            // buttonLast
            // 
            this.buttonLast.Location = new System.Drawing.Point(168, 892);
            this.buttonLast.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonLast.Name = "buttonLast";
            this.buttonLast.Size = new System.Drawing.Size(41, 29);
            this.buttonLast.TabIndex = 12;
            this.buttonLast.Text = ">>";
            this.buttonLast.UseVisualStyleBackColor = true;
            this.buttonLast.Click += new System.EventHandler(this.bindingNavigatorMoveLastItem_Click);
            // 
            // buttonNext
            // 
            this.buttonNext.Location = new System.Drawing.Point(117, 892);
            this.buttonNext.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonNext.Name = "buttonNext";
            this.buttonNext.Size = new System.Drawing.Size(43, 29);
            this.buttonNext.TabIndex = 11;
            this.buttonNext.Text = ">";
            this.buttonNext.UseVisualStyleBackColor = true;
            this.buttonNext.Click += new System.EventHandler(this.bindingNavigatorMoveNextItem_Click);
            // 
            // buttonPrevious
            // 
            this.buttonPrevious.Location = new System.Drawing.Point(67, 892);
            this.buttonPrevious.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonPrevious.Name = "buttonPrevious";
            this.buttonPrevious.Size = new System.Drawing.Size(43, 29);
            this.buttonPrevious.TabIndex = 10;
            this.buttonPrevious.Text = "<";
            this.buttonPrevious.UseVisualStyleBackColor = true;
            this.buttonPrevious.Click += new System.EventHandler(this.bindingNavigatorMovePreviousItem_Click);
            // 
            // buttonFirst
            // 
            this.buttonFirst.Location = new System.Drawing.Point(17, 892);
            this.buttonFirst.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonFirst.Name = "buttonFirst";
            this.buttonFirst.Size = new System.Drawing.Size(41, 29);
            this.buttonFirst.TabIndex = 9;
            this.buttonFirst.Text = "<<";
            this.buttonFirst.UseVisualStyleBackColor = true;
            this.buttonFirst.Click += new System.EventHandler(this.bindingNavigatorMoveFirstItem_Click);
            // 
            // dataGridViewImportHistory
            // 
            this.dataGridViewImportHistory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewImportHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewImportHistory.Location = new System.Drawing.Point(9, 56);
            this.dataGridViewImportHistory.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dataGridViewImportHistory.Name = "dataGridViewImportHistory";
            this.dataGridViewImportHistory.RowTemplate.Height = 23;
            this.dataGridViewImportHistory.Size = new System.Drawing.Size(1096, 812);
            this.dataGridViewImportHistory.TabIndex = 8;
            this.dataGridViewImportHistory.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewImportHistory_CellClick);
            // 
            // buttonPgSize50
            // 
            this.buttonPgSize50.Location = new System.Drawing.Point(601, 891);
            this.buttonPgSize50.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonPgSize50.Name = "buttonPgSize50";
            this.buttonPgSize50.Size = new System.Drawing.Size(56, 29);
            this.buttonPgSize50.TabIndex = 16;
            this.buttonPgSize50.Text = "50";
            this.buttonPgSize50.UseVisualStyleBackColor = true;
            this.buttonPgSize50.Click += new System.EventHandler(this.buttonPgSize50_Click);
            // 
            // buttonPgSize100
            // 
            this.buttonPgSize100.Location = new System.Drawing.Point(683, 891);
            this.buttonPgSize100.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonPgSize100.Name = "buttonPgSize100";
            this.buttonPgSize100.Size = new System.Drawing.Size(56, 29);
            this.buttonPgSize100.TabIndex = 16;
            this.buttonPgSize100.Text = "100";
            this.buttonPgSize100.UseVisualStyleBackColor = true;
            this.buttonPgSize100.Click += new System.EventHandler(this.buttonPgSize100_Click);
            // 
            // buttonPgSize200
            // 
            this.buttonPgSize200.Location = new System.Drawing.Point(764, 892);
            this.buttonPgSize200.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonPgSize200.Name = "buttonPgSize200";
            this.buttonPgSize200.Size = new System.Drawing.Size(56, 29);
            this.buttonPgSize200.TabIndex = 16;
            this.buttonPgSize200.Text = "200";
            this.buttonPgSize200.UseVisualStyleBackColor = true;
            this.buttonPgSize200.Click += new System.EventHandler(this.buttonPgSize200_Click);
            // 
            // buttonPgSize500
            // 
            this.buttonPgSize500.Location = new System.Drawing.Point(848, 892);
            this.buttonPgSize500.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonPgSize500.Name = "buttonPgSize500";
            this.buttonPgSize500.Size = new System.Drawing.Size(56, 29);
            this.buttonPgSize500.TabIndex = 16;
            this.buttonPgSize500.Text = "500";
            this.buttonPgSize500.UseVisualStyleBackColor = true;
            this.buttonPgSize500.Click += new System.EventHandler(this.buttonPgSize500_Click);
            // 
            // buttonPgSize1000
            // 
            this.buttonPgSize1000.Location = new System.Drawing.Point(935, 892);
            this.buttonPgSize1000.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonPgSize1000.Name = "buttonPgSize1000";
            this.buttonPgSize1000.Size = new System.Drawing.Size(56, 29);
            this.buttonPgSize1000.TabIndex = 16;
            this.buttonPgSize1000.Text = "1000";
            this.buttonPgSize1000.UseVisualStyleBackColor = true;
            this.buttonPgSize1000.Click += new System.EventHandler(this.buttonPgSize1000_Click);
            // 
            // LabelPageSize
            // 
            this.LabelPageSize.AutoSize = true;
            this.LabelPageSize.Location = new System.Drawing.Point(448, 899);
            this.LabelPageSize.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.LabelPageSize.Name = "LabelPageSize";
            this.LabelPageSize.Size = new System.Drawing.Size(143, 15);
            this.LabelPageSize.TabIndex = 17;
            this.LabelPageSize.Text = "页大小（默认15）：";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 26);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 15);
            this.label1.TabIndex = 18;
            this.label1.Text = "资源类型";
            // 
            // textBoxResType
            // 
            this.textBoxResType.Location = new System.Drawing.Point(95, 22);
            this.textBoxResType.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBoxResType.Name = "textBoxResType";
            this.textBoxResType.Size = new System.Drawing.Size(217, 25);
            this.textBoxResType.TabIndex = 19;
            // 
            // buttonFiliterResType
            // 
            this.buttonFiliterResType.Location = new System.Drawing.Point(321, 20);
            this.buttonFiliterResType.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonFiliterResType.Name = "buttonFiliterResType";
            this.buttonFiliterResType.Size = new System.Drawing.Size(100, 29);
            this.buttonFiliterResType.TabIndex = 20;
            this.buttonFiliterResType.Text = "过滤";
            this.buttonFiliterResType.UseVisualStyleBackColor = true;
            this.buttonFiliterResType.Click += new System.EventHandler(this.buttonFiliterResType_Click);
            // 
            // ImportBatchHistoryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1111, 950);
            this.Controls.Add(this.buttonFiliterResType);
            this.Controls.Add(this.textBoxResType);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.LabelPageSize);
            this.Controls.Add(this.buttonPgSize1000);
            this.Controls.Add(this.buttonPgSize500);
            this.Controls.Add(this.buttonPgSize200);
            this.Controls.Add(this.buttonPgSize100);
            this.Controls.Add(this.buttonPgSize50);
            this.Controls.Add(this.labelTotal);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.labelCurrentPage);
            this.Controls.Add(this.buttonLast);
            this.Controls.Add(this.buttonNext);
            this.Controls.Add(this.buttonPrevious);
            this.Controls.Add(this.buttonFirst);
            this.Controls.Add(this.dataGridViewImportHistory);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "ImportBatchHistoryForm";
            this.Text = "批次历史";
            this.Load += new System.EventHandler(this.ImportBatchHistoryForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewImportHistory)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelTotal;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelCurrentPage;
        private System.Windows.Forms.Button buttonLast;
        private System.Windows.Forms.Button buttonNext;
        private System.Windows.Forms.Button buttonPrevious;
        private System.Windows.Forms.Button buttonFirst;
        private System.Windows.Forms.DataGridView dataGridViewImportHistory;
        private System.Windows.Forms.Button buttonPgSize50;
        private System.Windows.Forms.Button buttonPgSize100;
        private System.Windows.Forms.Button buttonPgSize200;
        private System.Windows.Forms.Button buttonPgSize500;
        private System.Windows.Forms.Button buttonPgSize1000;
        private System.Windows.Forms.Label LabelPageSize;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxResType;
        private System.Windows.Forms.Button buttonFiliterResType;
    }
}