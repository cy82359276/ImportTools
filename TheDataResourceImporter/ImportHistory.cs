using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TheDataResourceImporter
{
    public partial class ImportHistory : Form
    {
        public ImportHistory()
        {
            InitializeComponent();
            DataSourceEntities entitiesDataSource = new DataSourceEntities();
            //var importSessions = from session in entitiesDataSource.IMPORT_SESSION.ToList()
            //                     orderby session.START_TIME descending
            //                     select session;

            dataGridViewImportHistory.DataSource = entitiesDataSource.IMPORT_SESSION.OrderByDescending(r => r.START_TIME).ToList();
            dataGridViewImportHistory.AllowUserToAddRows = false;

            //public string SESSION_ID { get; set; }
            //public Nullable<System.DateTime> START_TIME { get; set; }
            //public Nullable<decimal> LAST_TIME { get; set; }
            //public string HAS_ERROR { get; set; }
            //public string ZIP_OR_DIR_PATH { get; set; }
            //public string COMPLETED { get; set; }
            //public Nullable<decimal> FAILED_COUNT { get; set; }
            //public string IS_ZIP { get; set; }
            //public Nullable<decimal> ZIP_ENTRIES_COUNT { get; set; }
            //public string DATA_RES_TYPE { get; set; }
            //public Nullable<decimal> ZIP_ENTRY_POINTOR { get; set; }
            //public string ROLLED_BACK { get; set; }
            //public Nullable<decimal> ITEMS_POINT { get; set; }
            //public Nullable<decimal> TOTAL_ITEM { get; set; }
            //public string ZIP_ENTRY_PATH { get; set; }
            //public string NOTE { get; set; }

            dataGridViewImportHistory.Columns["DATA_RES_TYPE"].DisplayIndex = 0;
            dataGridViewImportHistory.Columns["DATA_RES_TYPE"].HeaderText = "资源类型";

            dataGridViewImportHistory.Columns["ZIP_OR_DIR_PATH"].DisplayIndex = 1;
            dataGridViewImportHistory.Columns["ZIP_OR_DIR_PATH"].HeaderText = "文件路径";

            dataGridViewImportHistory.Columns["IS_ZIP"].DisplayIndex = 2;
            dataGridViewImportHistory.Columns["IS_ZIP"].HeaderText = "是否是压缩包";

            dataGridViewImportHistory.Columns["ZIP_ENTRY_PATH"].DisplayIndex = 3;
            dataGridViewImportHistory.Columns["ZIP_ENTRY_PATH"].HeaderText = "压缩包内路径";

            dataGridViewImportHistory.Columns["START_TIME"].DisplayIndex = 4;
            dataGridViewImportHistory.Columns["START_TIME"].HeaderText = "导入时间";

            dataGridViewImportHistory.Columns["LAST_TIME"].DisplayIndex = 5;
            dataGridViewImportHistory.Columns["LAST_TIME"].HeaderText = "持续时间";

            dataGridViewImportHistory.Columns["COMPLETED"].DisplayIndex = 6;
            dataGridViewImportHistory.Columns["COMPLETED"].HeaderText = "是否完成";

            dataGridViewImportHistory.Columns["TOTAL_ITEM"].DisplayIndex = 7;
            dataGridViewImportHistory.Columns["TOTAL_ITEM"].HeaderText = "导入条数";

            dataGridViewImportHistory.Columns["HAS_ERROR"].DisplayIndex = 8;
            dataGridViewImportHistory.Columns["HAS_ERROR"].HeaderText = "是否有错";

            dataGridViewImportHistory.Columns["ROLLED_BACK"].DisplayIndex = 9;
            dataGridViewImportHistory.Columns["ROLLED_BACK"].HeaderText = "已回滚";

            dataGridViewImportHistory.Columns["NOTE"].DisplayIndex = 10;
            dataGridViewImportHistory.Columns["NOTE"].HeaderText = "备注";

            dataGridViewImportHistory.Columns["SESSION_ID"].Visible = false;
            dataGridViewImportHistory.Columns["ZIP_ENTRIES_COUNT"].Visible = false;
            dataGridViewImportHistory.Columns["ZIP_ENTRY_POINTOR"].Visible = false;
            dataGridViewImportHistory.Columns["ITEMS_POINT"].Visible = false;
            dataGridViewImportHistory.Columns["FAILED_COUNT"].Visible = false;

            DataGridViewButtonColumn rollBackBtn = new DataGridViewButtonColumn();
            rollBackBtn.DisplayIndex = -1;
            rollBackBtn.Text = "回滚";
            rollBackBtn.Name = "rollBackButton";
            rollBackBtn.UseColumnTextForButtonValue = true;
            dataGridViewImportHistory.Columns.Add(rollBackBtn);


            DataGridViewButtonColumn checkErrorButton = new DataGridViewButtonColumn();
            checkErrorButton.DisplayIndex = -1;
            checkErrorButton.Text = "错误详情";
            checkErrorButton.Name = "checkErrorButton";
            checkErrorButton.UseColumnTextForButtonValue = true;
            dataGridViewImportHistory.Columns.Add(checkErrorButton);

        }

        //页面加载
        private void ImportHistory_Load(object sender, EventArgs e)
        {

        }
    }
}
