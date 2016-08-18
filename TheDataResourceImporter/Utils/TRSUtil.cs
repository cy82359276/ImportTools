using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDataResourceImporter.Utils
{
    class TRSUtil
    {

        /***
         * 目前只考虑了一行记录一条的情况
         * **/
        public static List<Dictionary<string, string>> paraseTrsRecords(string filePath, int columnCount)
        {
            List<Dictionary<string, string>> resultList = new List<Dictionary<string, string>>();

            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);

            string str = "";

            Dictionary<string, string> recDict = new Dictionary<string, string>();
            bool newRec = false;//是否是新记录
            while ((str = sr.ReadLine()) != null)
            {
                try
                {
                    if (str.Equals("<REC>"))//标记新的记录开始
                    {
                        newRec = true; 
                        continue;
                    }


                    if(newRec)
                    {
                        //如果当前记录不为空, 入库
                        if(recDict.Count > 0)
                        {
                            resultList.Add(recDict);
                        }
                        recDict = new Dictionary<string, string>();//起新纪录
                        newRec = false; //重置新纪录标记
                    }


                    if (!string.IsNullOrEmpty(str))//空行继续
                    {
                        string[] recParts = str.Split("=".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        if(recParts.Length >= 2)
                        {
                            string key = recParts[0];
                            key = key.Trim().Trim("<".ToCharArray()).Trim(">".ToCharArray());//移除前后的<, >
                            string value = string.Join("=", recParts.Skip(1).ToArray());
                            recDict.Add(key, value);
                        }
                    }
                }
                catch (Exception ex)
                {

                    continue;
                }
            }
            return resultList;
        }
    }
}
