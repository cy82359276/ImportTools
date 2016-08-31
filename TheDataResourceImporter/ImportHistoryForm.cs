﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TheDataResourceImporter
{
    public partial class ImportHistoryForm : Form
    {

        int pageSize = 15;     //每页显示行数
        int nMax = 0;         //总记录数
        int pageCount = 0;    //页数＝总记录数/每页显示行数
        int pageCurrent = 0;   //当前页号
        int nCurrent = 0;      //当前记录行
        string bathId = "";//批次编号
        DataSourceEntities entitiesDataSource = new DataSourceEntities();

        public ImportHistoryForm()
        {
            InitializeComponent();
            DataSourceEntities entitiesDataSource = new DataSourceEntities();
            //var importSessions = from session in entitiesDataSource.IMPORT_SESSION.ToList()
            //                     orderby session.START_TIME descending
            //                     select session;
            dataGridViewImportHistory.AutoGenerateColumns = false;
            showPage(1, entitiesDataSource);
        }

        public ImportHistoryForm(string bathID)
        {
            bathId = bathID;
            InitializeComponent();
            DataSourceEntities entitiesDataSource = new DataSourceEntities();
            //var importSessions = from session in entitiesDataSource.IMPORT_SESSION.ToList()
            //                     orderby session.START_TIME descending
            //                     select session;
            dataGridViewImportHistory.AutoGenerateColumns = false;
            showPage(1, entitiesDataSource);
        }

        public void showPage(int pageNum, DataSourceEntities entitiesDataSource)
        {

            dataGridViewImportHistory.Columns.Clear();


            var sessionArray = entitiesDataSource.IMPORT_SESSION.OrderByDescending(r => r.START_TIME);

            //存在批次编号
            if (!string.IsNullOrEmpty(bathId))
            {
                sessionArray = entitiesDataSource.IMPORT_SESSION.Where(r => r.BATCH_ID == bathId).OrderByDescending(r => r.START_TIME);
            }


            //总记录数
            nMax = sessionArray.Count();

            //页数
            pageCount = (int)Math.Ceiling(nMax * 1.0 / pageSize);

            pageCurrent = pageNum > pageCount ? pageCurrent : pageNum;

            //总页数
            labelTotal.Text = pageCount.ToString();
            labelCurrentPage.Text = pageCurrent.ToString();

            int StartPosition = pageSize * (pageNum - 1);

            dataGridViewImportHistory.AutoGenerateColumns = false;

            var pageArray = sessionArray.Skip(StartPosition).Take(pageSize).ToList();
            dataGridViewImportHistory.DataSource = pageArray;

            dataGridViewImportHistory.AllowUserToAddRows = false;
            dataGridViewImportHistory.AllowUserToResizeColumns = true;
            dataGridViewImportHistory.AllowUserToResizeRows = true;

            DataGridViewTextBoxColumn dGVResType = new DataGridViewTextBoxColumn();
            dGVResType.Name = "DATA_RES_TYPE";
            dGVResType.ReadOnly = true;
            dGVResType.DataPropertyName = "DATA_RES_TYPE";
            dGVResType.DisplayIndex = 0;
            dGVResType.HeaderText = "资源类型";
            dGVResType.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dGVResType.Resizable = DataGridViewTriState.True;
            dataGridViewImportHistory.Columns.Add(dGVResType);

            //dataGridViewImportHistory.Columns["DATA_RES_TYPE"].DisplayIndex = 0;
            //dataGridViewImportHistory.Columns["DATA_RES_TYPE"].HeaderText = "资源类型";

            DataGridViewTextBoxColumn dGVDirPath = new DataGridViewTextBoxColumn();
            dGVDirPath.Name = "ZIP_OR_DIR_PATH";
            dGVDirPath.ReadOnly = true;
            dGVDirPath.DataPropertyName = "ZIP_OR_DIR_PATH";
            dGVDirPath.DisplayIndex = 1;
            dGVDirPath.HeaderText = "文件路径";
            //dGVDirPath.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dGVDirPath.Resizable = DataGridViewTriState.True;
            dataGridViewImportHistory.Columns.Add(dGVDirPath);

            //dataGridViewImportHistory.Columns["ZIP_OR_DIR_PATH"].DisplayIndex = 1;
            //dataGridViewImportHistory.Columns["ZIP_OR_DIR_PATH"].HeaderText = "文件路径";

            DataGridViewTextBoxColumn dGVIsZip = new DataGridViewTextBoxColumn();
            dGVIsZip.Name = "IS_ZIP";
            dGVIsZip.ReadOnly = true;
            dGVIsZip.DataPropertyName = "IS_ZIP";
            dGVIsZip.DisplayIndex = 2;
            dGVIsZip.HeaderText = "是否是压缩包";
            //dGVIsZip.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dGVIsZip.Resizable = DataGridViewTriState.True;
            dataGridViewImportHistory.Columns.Add(dGVIsZip);

            //dataGridViewImportHistory.Columns["IS_ZIP"].DisplayIndex = 2;
            //dataGridViewImportHistory.Columns["IS_ZIP"].HeaderText = "是否是压缩包";

            DataGridViewTextBoxColumn dGVStartTime = new DataGridViewTextBoxColumn();
            dGVStartTime.Name = "START_TIME";
            dGVStartTime.ReadOnly = true;
            dGVStartTime.DataPropertyName = "START_TIME";
            dGVStartTime.DisplayIndex = 2;
            dGVStartTime.HeaderText = "导入时间";
            //dGVStartTime.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dGVStartTime.Resizable = DataGridViewTriState.True;
            dataGridViewImportHistory.Columns.Add(dGVStartTime);

            //dataGridViewImportHistory.Columns["START_TIME"].DisplayIndex = 3;
            //dataGridViewImportHistory.Columns["START_TIME"].HeaderText = "导入时间";

            DataGridViewTextBoxColumn dGVLastTime = new DataGridViewTextBoxColumn();
            dGVLastTime.Name = "LAST_TIME";
            dGVLastTime.ReadOnly = true;
            dGVLastTime.DataPropertyName = "LAST_TIME";
            dGVLastTime.DisplayIndex = 4;
            dGVLastTime.HeaderText = "持续时间";
            //dGVLastTime.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dGVLastTime.Resizable = DataGridViewTriState.True;
            dataGridViewImportHistory.Columns.Add(dGVLastTime);

            //dataGridViewImportHistory.Columns["LAST_TIME"].DisplayIndex = 4;
            //dataGridViewImportHistory.Columns["LAST_TIME"].HeaderText = "持续时间";

            DataGridViewTextBoxColumn dGVCompleted = new DataGridViewTextBoxColumn();
            dGVCompleted.Name = "COMPLETED";
            dGVCompleted.ReadOnly = true;
            dGVCompleted.DataPropertyName = "COMPLETED";
            dGVCompleted.DisplayIndex = 5;
            dGVCompleted.HeaderText = "是否完成";
            //dGVCompleted.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dGVCompleted.Resizable = DataGridViewTriState.True;
            dataGridViewImportHistory.Columns.Add(dGVCompleted);

            //dataGridViewImportHistory.Columns["COMPLETED"].DisplayIndex = 5;
            //dataGridViewImportHistory.Columns["COMPLETED"].HeaderText = "是否完成";

            DataGridViewTextBoxColumn dGVTotalItem = new DataGridViewTextBoxColumn();
            dGVTotalItem.Name = "TOTAL_ITEM";
            dGVTotalItem.ReadOnly = true;
            dGVTotalItem.DataPropertyName = "TOTAL_ITEM";
            dGVTotalItem.DisplayIndex = 6;
            dGVTotalItem.HeaderText = "导入条数";
            //dGVTotalItem.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dGVTotalItem.Resizable = DataGridViewTriState.True;
            dataGridViewImportHistory.Columns.Add(dGVTotalItem);

            //dataGridViewImportHistory.Columns["TOTAL_ITEM"].DisplayIndex = 6;
            //dataGridViewImportHistory.Columns["TOTAL_ITEM"].HeaderText = "导入条数";

            DataGridViewTextBoxColumn dGVHasError = new DataGridViewTextBoxColumn();
            dGVHasError.Name = "HAS_ERROR";
            dGVHasError.ReadOnly = true;
            dGVHasError.DataPropertyName = "HAS_ERROR";
            dGVHasError.DisplayIndex = 7;
            dGVHasError.HeaderText = "是否有错";
            //dGVHasError.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dGVHasError.Resizable = DataGridViewTriState.True;
            dataGridViewImportHistory.Columns.Add(dGVHasError);

            //dataGridViewImportHistory.Columns["HAS_ERROR"].DisplayIndex = 7;
            //dataGridViewImportHistory.Columns["HAS_ERROR"].HeaderText = "是否有错";

            DataGridViewTextBoxColumn dGVRolledBack = new DataGridViewTextBoxColumn();
            dGVRolledBack.Name = "ROLLED_BACK";
            dGVRolledBack.ReadOnly = true;
            dGVRolledBack.DataPropertyName = "ROLLED_BACK";
            dGVRolledBack.DisplayIndex = 8;
            dGVRolledBack.HeaderText = "是否已回滚";
            dGVRolledBack.Resizable = DataGridViewTriState.True;
            dataGridViewImportHistory.Columns.Add(dGVRolledBack);

            //dataGridViewImportHistory.Columns["ROLLED_BACK"].DisplayIndex = 8;
            //dataGridViewImportHistory.Columns["ROLLED_BACK"].HeaderText = "已回滚";

            DataGridViewTextBoxColumn dGVNote = new DataGridViewTextBoxColumn();
            dGVNote.Name = "NOTE";
            dGVNote.ReadOnly = true;
            dGVNote.DataPropertyName = "NOTE";
            dGVNote.DisplayIndex = 9;
            dGVNote.HeaderText = "备注";
            //dGVNote.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dGVNote.Resizable = DataGridViewTriState.True;
            dataGridViewImportHistory.Columns.Add(dGVNote);

            DataGridViewTextBoxColumn dGVSessionId = new DataGridViewTextBoxColumn();
            dGVSessionId.Name = "SESSION_ID";
            dGVSessionId.ReadOnly = true;
            dGVSessionId.DataPropertyName = "SESSION_ID";
            dGVSessionId.DisplayIndex = 12;
            dGVSessionId.HeaderText = "SESSION_ID";
            dGVSessionId.Visible = false;
            //dGVSessionId.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dGVSessionId.Resizable = DataGridViewTriState.True;
            dataGridViewImportHistory.Columns.Add(dGVSessionId);


            //dataGridViewImportHistory.Columns["NOTE"].DisplayIndex = 9;
            //dataGridViewImportHistory.Columns["NOTE"].HeaderText = "备注";
            //dataGridViewImportHistory.Columns["SESSION_ID"].Visible = false;
            //dataGridViewImportHistory.Columns["ZIP_ENTRIES_COUNT"].Visible = false;
            //dataGridViewImportHistory.Columns["ZIP_ENTRY_POINTOR"].Visible = false;
            //dataGridViewImportHistory.Columns["ITEMS_POINT"].Visible = false;
            //dataGridViewImportHistory.Columns["FAILED_COUNT"].Visible = false;
            //dataGridViewImportHistory.Columns["ZIP_ENTRY_PATH"].Visible = false;

            DataGridViewButtonColumn rollBackBtn = new DataGridViewButtonColumn();
            rollBackBtn.DisplayIndex = 10;
            rollBackBtn.Text = "回滚";
            rollBackBtn.Name = "rollBackButton";
            rollBackBtn.HeaderText = "";
            rollBackBtn.UseColumnTextForButtonValue = true;
            dataGridViewImportHistory.Columns.Add(rollBackBtn);

            DataGridViewButtonColumn checkErrorButton = new DataGridViewButtonColumn();
            checkErrorButton.DisplayIndex = 11;
            checkErrorButton.Text = "错误详情";
            checkErrorButton.Name = "checkErrorButton";
            checkErrorButton.HeaderText = "";
            checkErrorButton.UseColumnTextForButtonValue = true;
            dataGridViewImportHistory.Columns.Add(checkErrorButton);
        }

        //页面加载
        private void ImportHistory_Load(object sender, EventArgs e)
        {

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

        private void refreshDataGrid()
        {
            string currentPageStr = labelCurrentPage.Text;
            int currentPageTemp = 1;
            if (Int32.TryParse(currentPageStr, out currentPageTemp))
            {

            }
            showPage(currentPageTemp, new DataSourceEntities());
        }

        private void dataGridViewImportHistory_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var session_Id = dataGridViewImportHistory.Rows[e.RowIndex].Cells["SESSION_ID"].Value.ToString();
            var hasError = dataGridViewImportHistory.Rows[e.RowIndex].Cells["HAS_ERROR"].Value.ToString();

            //有错 弹出错误详情
            if ("Y".Equals(hasError))
            {
                var errorList = new ErrorListForm(session_Id);
                errorList.Show();
            }
            else
            {
                MessageBox.Show("本次导入没有错误");
            }
        }

        private void dataGridViewImportHistory_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex < 0)
            {
                return;
            }

            //选中的列的 名称
            var targetColName = dataGridViewImportHistory.Columns[e.ColumnIndex].Name;
            var session_Id = dataGridViewImportHistory.Rows[e.RowIndex].Cells["SESSION_ID"].Value.ToString();
            var hasError = dataGridViewImportHistory.Rows[e.RowIndex].Cells["HAS_ERROR"].Value.ToString();
            var selectedSession = entitiesDataSource.IMPORT_SESSION.Find(session_Id);
            string tableName = selectedSession.TABLENAME;
            if ("rollBackButton".Equals(targetColName))//回滚
            {
                var messageBoxResult = MessageBox.Show("确定回滚本次导入的全部记录么?", "是否回滚", MessageBoxButtons.YesNo);

                if (messageBoxResult == System.Windows.Forms.DialogResult.Yes)
                {
                    var sqlCommand = "";
                    try
                    {
                        if (!string.IsNullOrEmpty(tableName))
                        {
                            tableName = tableName.Trim();
                            sqlCommand = $"delete from {tableName} where import_session_id='{session_Id}'";
                            //删除本session的记录
                            entitiesDataSource.Database.ExecuteSqlCommand(sqlCommand);
                        }
                        sqlCommand = $"delete from import_error where session_id ='{session_Id}'";
                        //删除错误记录
                        entitiesDataSource.Database.ExecuteSqlCommand(sqlCommand);

                        sqlCommand = $"delete from IMPORT_SESSION t where t.session_id ='{session_Id}'";
                        //删除错误记录
                        entitiesDataSource.Database.ExecuteSqlCommand(sqlCommand);
                    }
                    catch (Exception ex)
                    {

                    }

                    //刷新DataGridView
                    refreshDataGrid();
                }
            }
            else if ("checkErrorButton".Equals(targetColName))//查看详情
            {
                //有错 弹出错误详情
                if ("Y".Equals(hasError))
                {
                    var errorList = new ErrorListForm(session_Id);
                    errorList.Show();
                }
                else
                {
                    MessageBox.Show("本次导入没有错误");
                }
            }
        }
    }
}
