using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Windows.Forms;

namespace TheDataResourceImporter.Utils
{
    public class LogHelper
    {

        public static void WriteLog(string dest, string txtName, string text)
        {
            if (!Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
            }
                try
                {
                    dest = dest + "\\" + txtName + ".log";
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

            WriteLog(logDir, "Import_Error", msg);
        }
    }
}
