using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace TheDataResourceImporter.Utils
{
    public class LogHelper
    {

        /// <summary>
        /// 改成一批次一日志文件
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="txtName"></param>
        /// <param name="text"></param>
        public static void WriteLog(string dest, string txtName, string text)
        {
            if (string.IsNullOrEmpty(ImportManger.bathId))
            {
                ImportManger.bathId = "";
            }


            if (!Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
            }
            try
            {
                dest = dest + "\\" + txtName + ImportManger.bathId + ".log";
                using (StreamWriter sw = new StreamWriter(dest, true, Encoding.Default))
                {
                    sw.WriteLine(text);
                }
            }
            catch (Exception ec)
            {
                MessageBox.Show("写记录出错！" + ec.Message);
            }
        }

        public static void WriteLog(string msg)
        {
            string logDir = ConfigurationManager.AppSettings["tempDir"];


            //var task = new Task(() =>
            //{
            //    lock (typeof(LogHelper))
            //    {
            //        WriteLog(logDir, "Import", msg);
            //    } });
            //task.Start();
            WriteLog(logDir, "Import", msg);
        }


        public static void WriteErrorLog(string msg)
        {
            //添加时间标识
            DateTime now = System.DateTime.Now;
            string timeStamp = now.ToLocalTime().ToString() + " " + now.Millisecond;
            //添加消息换行
            msg = Environment.NewLine + timeStamp + Environment.NewLine + msg;

            string logDir = ConfigurationManager.AppSettings["tempDir"];

            var task = new Task(() =>
            {
                lock (typeof(LogHelper))
                {
                    WriteLog(logDir, "Import_Error", msg);
                }
            });
            task.Start();
        }
    }
}
