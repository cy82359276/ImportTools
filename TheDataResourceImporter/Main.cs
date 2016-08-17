using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TheDataResourceImporter.Utils;


namespace TheDataResourceImporter
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();


            DataSourceEntities entitiesDataSource = new DataSourceEntities();
            //绑定数据类型 下拉列表
            var availableDataTypes = from dataType in entitiesDataSource.S_DATA_RESOURCE_TYPES_DETAIL.Where(dataType => "Y".Equals(dataType.IMPLEMENTED_IMPORT_LOGIC)).ToList()
                                     orderby dataType.ID ascending
                                     select new {diplayName = dataType.ID + "-" + dataType.CHINESE_NAME, selectedValue = dataType.CHINESE_NAME };
            cbFileType.DisplayMember = "diplayName";
            cbFileType.ValueMember = "selectedValue";

            cbFileType.DataSource = availableDataTypes.ToList();


            //MessageUtil.SetMessage = SetLabelMsg;
            MessageUtil.setTbDetail = SetTextBoxDetail;
            MessageUtil.appendTbDetail = appendTextBoxDetail;
            //添加日志输出
            //MessageUtil.appendTbDetail += LogHelper.WriteLog;

            MessageUtil.updateProgressIndicator = updateProgressIndicator;
            //cbFileType.SelectedIndex = 0;
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true); // 禁止擦除背景.  
            SetStyle(ControlStyles.DoubleBuffer, true); // 双缓冲  

        }
        string[] filePaths = null;
        private void btn_Choose_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "任意文件(*.*)或目录|*";
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filePaths = null;
                tb_FilePath.Text = string.Empty;

                filePaths = dialog.FileNames;
                foreach (string filePath in filePaths)
                {
                    tb_FilePath.Text += (filePath + ";");
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {

            //清空进度信息
            //ImportManger.currentFile = "";
            //ImportManger.totalCount = 0;
            //ImportManger.handledCount = 0;
            //ImportManger.handledXMLCount = 0;
            //ImportManger.withExceptionButExtracted = 0;
            //ImportManger.withExcepthonAndFiled2Exracted = 0;
            //ImportManger.fileCount = 0;

            //MessageUtil.DoupdateProgressIndicator(0, 0, 0, 0, "");
            ImportManger.resetCounter();


            //清空强制终止标识
            ImportManger.forcedStop = false;

            MessageUtil.setTbDetail("");

            MessageUtil.appendTbDetail("开始导入：");

            if (string.IsNullOrEmpty(tb_FilePath.Text))
            {
                MessageBox.Show("请选择至少选择一个文件！");
                return;
            }

            //var fileType = cbFileType.Text;
            var fileType = cbFileType.SelectedValue.ToString();

            //未选中文件类型
            if (String.IsNullOrEmpty(fileType))
            {
                MessageBox.Show("请选择数据类型！");
                return;
            }



            SetEnabled(btn_Choose, false);
            SetEnabled(btnStart, false);



            Func<string[],string, bool> func = TheDataResourceImporter.ImportManger.BeginImport;

            ImportManger.importStartTime = System.DateTime.Now;


            func.BeginInvoke(filePaths,fileType, 
                delegate(IAsyncResult ia)
                {
                    bool result = func.EndInvoke(ia);
                    if (result)
                    {
                        double totalSeconds = System.DateTime.Now.Subtract(ImportManger.importStartTime).TotalSeconds;

                        MessageUtil.DoAppendTBDetail(String.Format("\r\n导入结束!共运行了{0:0.##}秒, 成功入库{1}件，平均用时：{2:0.#######}", totalSeconds, ImportManger.handledCount, totalSeconds / ImportManger.handledCount));
                    }

                    SetEnabled(btn_Choose, true);
                    SetEnabled(btnStart, true);
                }, null); 
        }



        delegate void SetTextBoxDetailHander(string msg);
        public void SetTextBoxDetail(string msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new SetTextBoxDetailHander(SetTextBoxDetail), msg);
            }
            else
            {
               //lock (textBoxDetail)
                {
                    textBoxDetail.Text = msg;
                }
            }
        }



        delegate void updateProgressIndicatorHander(int totalCount, int handledCount, int handledXMLCount, int handledDirCount, string achievePath);
        public void updateProgressIndicator(int totalCount, int handledCount, int handledXMLCount, int handledDirCount, string achievePath)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new updateProgressIndicatorHander(updateProgressIndicator), totalCount, handledCount, handledXMLCount, handledDirCount, achievePath);
            }
            else
            {

                //lock (importProgressBar)
                {
                    labelcurrentArchive.Text = achievePath;
                    string status = handledCount + "/" + totalCount;

                    labelStatus.Text = status;

                    string progressMsg = $"发现待入库{totalCount}件条目，已入库{handledCount}件，剩余{totalCount - handledCount}件";

                    labelProgressMsg.Text = progressMsg;

                    int currentPercentage = 0;
                    if (totalCount > 0)
                    {
                        //计算百分比
                        currentPercentage =  (Int32)Math.Floor((double)handledCount * 100 / totalCount);
                    }

                    //当前进度百分比
                    importProgressBar.Value = currentPercentage;

                    //更新耗时信息
                    double totalSecCount = System.DateTime.Now.Subtract(ImportManger.importStartTime).TotalSeconds;

                    double averageTime = totalSecCount / ImportManger.handledCount;

                    double importCountPerSec = ImportManger.handledCount / totalSecCount;

                    labelelapsedTotalTime.Text = totalSecCount.ToString("0.####") + "S";

                    labelAverageElapsedTime.Text = averageTime.ToString("0.####") + "S";

                    labelImportCountPerSec.Text = importCountPerSec.ToString("0.####" + "件/S");
                }
            }
        }



        delegate void appendTextBoxDetailHander(string msg);
        public void appendTextBoxDetail(string msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new appendTextBoxDetailHander(appendTextBoxDetail), msg);
            }
            else
            {
                //textBoxDetail.Text = textBoxDetail.Text + msg;
                //lock (textBoxDetail)
                {
                    textBoxDetail.SelectionStart = textBoxDetail.Text.Length;
                    //textBoxDetail.AppendText(msg);
                    textBoxDetail.Text = msg;
                    textBoxDetail.ScrollToCaret();
                }
            }
        }



        delegate void SetEnabledHander(Control control, bool flag);

        public void SetEnabled(Control control, bool flag)
        {

            if (this.InvokeRequired)
            {
                this.Invoke(new SetEnabledHander(SetEnabled), control, flag);
            }
            else
            {
                control.Enabled = flag;
            }
        }


        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("确定退出？", "退出确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == System.Windows.Forms.DialogResult.No)
            {
                e.Cancel = true;
            }
        }


        private void btnAbort_Click(object sender, EventArgs e)
        {
            //强制终止
            ImportManger.forcedStop = true;
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            //重置
            ImportManger.resetCounter();
        }

        private void menuAbout_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("技术支持 内网分机8323");
            new AboutBoxUS().ShowDialog();
        }

        private void menuShowImportHistory_Click(object sender, EventArgs e)
        {
            new ImportHistoryForm().Show();
        }
    }
}
