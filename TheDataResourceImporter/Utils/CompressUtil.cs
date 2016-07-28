using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpCompress.Archive;
using System.Configuration;
using SharpCompress.Common;
using System.IO;

namespace TheDataResourceImporter.Utils
{
    class CompressUtil
    {

        public  static string tempDir = ConfigurationManager.AppSettings["tempDir"];
        /***
         * 将当前的entry写到指定目录,保留文件信息,如果文件存在, 覆盖文件
         * 返回临时目录
         * */ 
        public static string writeEntryToTemp(IArchiveEntry entry)
        {
            //string tempDir = ConfigurationManager.AppSettings["tempDir"];
            bool successed = false;
            try
            {
                entry.WriteToDirectory(tempDir, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                successed = true;
            }
            catch(Exception ex)
            {
                if(File.Exists(Path.Combine(tempDir, entry.Key)))
                {
                    string msg = "!!!!!!!!!!发生异常, 但是文件解压成功：" + ex.Message + ex.StackTrace;
                    MessageUtil.DoAppendTBDetail(msg);
                    LogHelper.WriteErrorLog(msg);
                    successed = true;
                }
                else
                {
                    string msg = "!!!!!!!!!!发生异常, 解压失败：" + ex.Message + ex.StackTrace;
                    MessageUtil.DoAppendTBDetail(msg);
                    LogHelper.WriteErrorLog(msg);
                    successed = false;   
                }
            }
                if(successed)
            {
               return Path.Combine(tempDir, entry.Key);
            }
                else
            {
                return "";
            }
        }
        
        /***
         * 解压压缩包的所有条目到指定目录, 返回临时目录
         **/
        public static string extractAllEntiresInArchive(IArchive archive)
        {
            archive.WriteToDirectory(tempDir, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite | ExtractOptions.PreserveAttributes | ExtractOptions.PreserveFileTime);
            return tempDir;
        }
        
        /**
         * 返回当前entry解压后的路径
         * */
        public static string getExtractedFullPath(IArchiveEntry entry)
        {
            return Path.Combine(tempDir, entry.Key);
        }
        /**
         * 删除指定Entry对象的临时文件 只删除文件, 不操作目录
         * */
        public static bool removeEntryTempFile(IArchiveEntry entry)
        {
            bool deleted = true;
            try
            {
                string fullPath = Path.Combine(tempDir, entry.Key);
                FileInfo entryFile = new FileInfo(fullPath);
                if(entryFile.Exists)
                {
                    entryFile.Delete();
                }
            }
            catch(Exception ex)
            {
                deleted = false;
            }
            return deleted;
        }


    }
}
