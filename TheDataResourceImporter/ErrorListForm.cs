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
    public partial class ErrorListForm : Form
    {
        public string SessionId { get; set; }
        int pageSize = 15;     //每页显示行数
        int nMax = 0;         //总记录数
        int pageCount = 0;    //页数＝总记录数/每页显示行数
        int pageCurrent = 0;   //当前页号
        int nCurrent = 0;      //当前记录行
        DataSourceEntities entitiesDataSource = new DataSourceEntities();
        public ErrorListForm()
        {
            InitializeComponent();
        }

        public ErrorListForm(string sessionId)
        {
            SessionId = sessionId;
            InitializeComponent();
            showPage(1, entitiesDataSource);
        }

        public void showPage(int pageNum, DataSourceEntities entitiesDataSource)
        {

            dataGridViewErrorList.Columns.Clear();

            var sessionArray = entitiesDataSource.IMPORT_ERROR.Where(r => r.SESSION_ID == SessionId).OrderByDescending(r => r.OCURREDTIME);

            //总记录数
            nMax = sessionArray.Count();

            //页数
            pageCount = (int)Math.Ceiling(nMax * 1.0 / pageSize);

            pageCurrent = pageNum > pageCount ? pageCurrent : pageNum;

            //总页数
            labelTotal.Text = pageCount.ToString();
            labelCurrentPage.Text = pageCurrent.ToString();

            int StartPosition = pageSize * (pageNum - 1);

            if(StartPosition < 0)
            {
                StartPosition = 0;
            }

            dataGridViewErrorList.AutoGenerateColumns = false;

            var pageArray = sessionArray.Skip(StartPosition).Take(pageSize).ToList();
            dataGridViewErrorList.DataSource = pageArray;

            dataGridViewErrorList.AllowUserToAddRows = false;
            dataGridViewErrorList.AllowUserToResizeColumns = true;
            dataGridViewErrorList.AllowUserToResizeRows = true;

            //public string ID { get; set; }
            //public string SESSION_ID { get; set; }
            //public string ZIP_OR_DIR_PATH { get; set; }
            //public string ISZIP { get; set; }
            //public Nullable<decimal> POINTOR { get; set; }
            //public string ZIP_PATH { get; set; }
            //public string IGNORED { get; set; }
            //public string REIMPORTED { get; set; }
            //public Nullable<System.DateTime> OCURREDTIME { get; set; }
            //public string ERROR_MESSAGE { get; set; }
            //public string ERROR_DETAIL { get; set; }




            DataGridViewTextBoxColumn dGVResType = new DataGridViewTextBoxColumn();
            dGVResType.Name = "ZIP_OR_DIR_PATH";
            dGVResType.ReadOnly = true;
            dGVResType.DataPropertyName = "ZIP_OR_DIR_PATH";
            dGVResType.DisplayIndex = 0;
            dGVResType.HeaderText = "文件路径";
            dGVResType.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dGVResType.Resizable = DataGridViewTriState.True;
            dataGridViewErrorList.Columns.Add(dGVResType);

            //dataGridViewErrorList.Columns["DATA_RES_TYPE"].DisplayIndex = 0;
            //dataGridViewErrorList.Columns["DATA_RES_TYPE"].HeaderText = "资源类型";

            DataGridViewTextBoxColumn dGVDirPath = new DataGridViewTextBoxColumn();
            dGVDirPath.Name = "OCURREDTIME";
            dGVDirPath.ReadOnly = true;
            dGVDirPath.DataPropertyName = "OCURREDTIME";
            dGVDirPath.DisplayIndex = 1;
            dGVDirPath.HeaderText = "发生时间";
            //dGVDirPath.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dGVDirPath.Resizable = DataGridViewTriState.True;
            dataGridViewErrorList.Columns.Add(dGVDirPath);

            //dataGridViewErrorList.Columns["ZIP_OR_DIR_PATH"].DisplayIndex = 1;
            //dataGridViewErrorList.Columns["ZIP_OR_DIR_PATH"].HeaderText = "文件路径";

            DataGridViewTextBoxColumn dGVIsZip = new DataGridViewTextBoxColumn();
            dGVIsZip.Name = "ISZIP";
            dGVIsZip.ReadOnly = true;
            dGVIsZip.DataPropertyName = "ISZIP";
            dGVIsZip.DisplayIndex = 2;
            dGVIsZip.HeaderText = "是否是压缩包";
            //dGVIsZip.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dGVIsZip.Resizable = DataGridViewTriState.True;
            dataGridViewErrorList.Columns.Add(dGVIsZip);

            //dataGridViewErrorList.Columns["IS_ZIP"].DisplayIndex = 2;
            //dataGridViewErrorList.Columns["IS_ZIP"].HeaderText = "是否是压缩包";

            DataGridViewTextBoxColumn dGVStartTime = new DataGridViewTextBoxColumn();
            dGVStartTime.Name = "ZIP_PATH";
            dGVStartTime.ReadOnly = true;
            dGVStartTime.DataPropertyName = "ZIP_PATH";
            dGVStartTime.DisplayIndex = 3;
            dGVStartTime.HeaderText = "压缩包内路径";
            //dGVStartTime.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dGVStartTime.Resizable = DataGridViewTriState.True;
            dataGridViewErrorList.Columns.Add(dGVStartTime);

            //dataGridViewErrorList.Columns["START_TIME"].DisplayIndex = 3;
            //dataGridViewErrorList.Columns["START_TIME"].HeaderText = "导入时间";

            DataGridViewTextBoxColumn dGVLastTime = new DataGridViewTextBoxColumn();
            dGVLastTime.Name = "IGNORED";
            dGVLastTime.ReadOnly = true;
            dGVLastTime.DataPropertyName = "IGNORED";
            dGVLastTime.DisplayIndex = 4;
            dGVLastTime.HeaderText = "已忽略";
            //dGVLastTime.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dGVLastTime.Resizable = DataGridViewTriState.True;
            dataGridViewErrorList.Columns.Add(dGVLastTime);

            //dataGridViewErrorList.Columns["LAST_TIME"].DisplayIndex = 4;
            //dataGridViewErrorList.Columns["LAST_TIME"].HeaderText = "持续时间";

            DataGridViewTextBoxColumn dGVCompleted = new DataGridViewTextBoxColumn();
            dGVCompleted.Name = "REIMPORTED";
            dGVCompleted.ReadOnly = true;
            dGVCompleted.DataPropertyName = "REIMPORTED";
            dGVCompleted.DisplayIndex = 5;
            dGVCompleted.HeaderText = "已处理";
            //dGVCompleted.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dGVCompleted.Resizable = DataGridViewTriState.True;
            dataGridViewErrorList.Columns.Add(dGVCompleted);


            //dataGridViewErrorList.Columns["ROLLED_BACK"].DisplayIndex = 8;
            //dataGridViewErrorList.Columns["ROLLED_BACK"].HeaderText = "已回滚";

            DataGridViewTextBoxColumn dGVNote = new DataGridViewTextBoxColumn();
            dGVNote.Name = "ERROR_MESSAGE";
            dGVNote.ReadOnly = true;
            dGVNote.DataPropertyName = "ERROR_MESSAGE";
            dGVNote.DisplayIndex = 6;
            dGVNote.HeaderText = "错误消息";
            //dGVNote.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dGVNote.Resizable = DataGridViewTriState.True;
            dataGridViewErrorList.Columns.Add(dGVNote);

            DataGridViewTextBoxColumn dGVSessionId = new DataGridViewTextBoxColumn();
            dGVSessionId.Name = "ID";
            dGVSessionId.ReadOnly = true;
            dGVSessionId.DataPropertyName = "ID";
            dGVSessionId.DisplayIndex = 7;
            dGVSessionId.HeaderText = "ID";
            dGVSessionId.Visible = false;
            //dGVSessionId.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dGVSessionId.Resizable = DataGridViewTriState.True;
            dataGridViewErrorList.Columns.Add(dGVSessionId);


            //dataGridViewErrorList.Columns["NOTE"].DisplayIndex = 9;
            //dataGridViewErrorList.Columns["NOTE"].HeaderText = "备注";
            //dataGridViewErrorList.Columns["SESSION_ID"].Visible = false;
            //dataGridViewErrorList.Columns["ZIP_ENTRIES_COUNT"].Visible = false;
            //dataGridViewErrorList.Columns["ZIP_ENTRY_POINTOR"].Visible = false;
            //dataGridViewErrorList.Columns["ITEMS_POINT"].Visible = false;
            //dataGridViewErrorList.Columns["FAILED_COUNT"].Visible = false;
            //dataGridViewErrorList.Columns["ZIP_ENTRY_PATH"].Visible = false;

            DataGridViewButtonColumn ignoredBtn = new DataGridViewButtonColumn();
            ignoredBtn.DisplayIndex = 10;
            ignoredBtn.Text = "忽略";
            ignoredBtn.Name = "ignoreButton";
            ignoredBtn.HeaderText = "";
            ignoredBtn.UseColumnTextForButtonValue = true;
            dataGridViewErrorList.Columns.Add(ignoredBtn);

            DataGridViewButtonColumn handledButton = new DataGridViewButtonColumn();
            handledButton.DisplayIndex = 11;
            handledButton.Text = "已处理";
            handledButton.Name = "handledButton";
            handledButton.HeaderText = "";
            handledButton.UseColumnTextForButtonValue = true;
            dataGridViewErrorList.Columns.Add(handledButton);
        }

        private void dataGridViewErrorList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //选中的列的 名称
            var targetColName = dataGridViewErrorList.Columns[e.ColumnIndex].Name;
            var id = dataGridViewErrorList.Rows[e.RowIndex].Cells["ID"].Value.ToString();
            if ("ignoreButton".Equals(targetColName))//回滚
            {
                var errorEntity = entitiesDataSource.IMPORT_ERROR.Find(id);
                errorEntity.IGNORED = "Y";
                entitiesDataSource.SaveChanges();
                //刷新DataGridView
                refreshDataGrid();
            }
            else if ("handledButton".Equals(targetColName))//查看详情
            {
                var errorEntity = entitiesDataSource.IMPORT_ERROR.Find(id);
                errorEntity.REIMPORTED = "Y";
                entitiesDataSource.SaveChanges();
                refreshDataGrid();
            }
        }

        private void refreshDataGrid()
        {
            string currentPageStr = labelCurrentPage.Text;
            int currentPageTemp = 1;
            if (Int32.TryParse(currentPageStr, out currentPageTemp))
            {

            }
            showPage(currentPageTemp, new DataSourceEntities());
        }

        private void bindingNavigatorMoveFirstItem_Click(object sender, EventArgs e)
        {
            showPage(1, entitiesDataSource);
        }

        private void bindingNavigatorMoveLastItem_Click(object sender, EventArgs e)
        {
            showPage(pageCount, entitiesDataSource);
        }

        private void bindingNavigatorMovePreviousItem_Click(object sender, EventArgs e)
        {
            string currentPageStr = labelCurrentPage.Text;
            int currentPageTemp = 1;
            if (Int32.TryParse(currentPageStr, out currentPageTemp))
            {
                if (currentPageTemp - 1 <= 0)
                {
                    currentPageTemp = 1;
                }
                else
                {
                    currentPageTemp = currentPageTemp - 1;
                }
            }
            showPage(currentPageTemp, new DataSourceEntities());
        }

        private void bindingNavigatorMoveNextItem_Click(object sender, EventArgs e)
        {
            string currentPageStr = labelCurrentPage.Text;
            int currentPageTemp = 1;
            if (Int32.TryParse(currentPageStr, out currentPageTemp))
            {
                if (currentPageTemp + 1 >= pageCount)
                {
                    currentPageTemp = pageCount;
                }
                else
                {
                    currentPageTemp = currentPageTemp + 1;
                }
            }
            showPage(currentPageTemp, new DataSourceEntities());
        }

    }
}
