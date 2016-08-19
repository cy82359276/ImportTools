using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TheDataResourceImporter.Utils
{
    class TRSUtil
    {
        private static Regex regex = new Regex(@"^<(.+?)>=(.*)$");
        /***
         * TRS文件入库
         * **/
        public static List<Dictionary<string, string>> paraseTrsRecord(string filePath)
        {
            List<Dictionary<string, string>> resultList = new List<Dictionary<string, string>>();

            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);

            string str = "";

            string fieldName = "";

            string fieldValue = "";

            Dictionary<string, string> recDict = new Dictionary<string, string>();
            while ((str = sr.ReadLine()) != null)
            {
                try
                {
                    if (str.Equals("<REC>"))//标记新的记录开始
                    {
                        if (!string.IsNullOrEmpty(fieldName))//排除第一行REC, 此时还无记录
                        {
                            recDict.Add(fieldName, fieldValue);//入库当前记录的最后发现的字段
                        }
                        if (recDict.Count > 0) //排除第一行REC
                        {
                            resultList.Add(recDict);
                        }

                        
                        fieldName = "";
                        fieldValue = "";

                        recDict = new Dictionary<string, string>();//起新纪录

                        continue;
                    }

                   //if (newRec)
                    //{
                    //    //如果当前记录不为空, 入库
                    //    if (recDict.Count > 0) //排除第一行REC
                    //    {
                    //        resultList.Add(recDict);
                    //    }
                    //    newRec = false; //重置新纪录标记
                    //}
                    //if (!string.IsNullOrEmpty(str))
                    {

                        Match match = regex.Match(str); //解析字段名称和值

                        string fieldOName = match.Groups[1].Value; //字段名

                        string fieldOValue = match.Groups[2].Value; //当前字段取值 可能是部分值
                        //如果解析出新的名字, 说明是新的字段
                        if (!string.IsNullOrEmpty(fieldOName))
                        {
                            //上一个字段值需要入库: 
                            if (!string.IsNullOrEmpty(fieldName))//排除第一行REC, 此时还无记录
                            {
                                recDict.Add(fieldName, fieldValue);//入库当前记录的最后发现的字段
                            }

                            fieldName = fieldOName;
                            fieldValue = fieldOValue;//重新赋字段值
                        }
                        else //没有成功解析出字段名,当前行为字段值的后续值
                        {
                            fieldValue = fieldValue + Environment.NewLine + str;
                        }

                        //string[] recParts = str.Split("=".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        //if(recParts.Length >= 2)
                        //{
                        //    string key = recParts[0];
                        //    key = key.Trim().Trim("<".ToCharArray()).Trim(">".ToCharArray());//移除前后的<, >
                        //    string value = string.Join("=", recParts.Skip(1).ToArray());
                        //    recDict.Add(key, value);
                        //}
                    }
                }
                catch (Exception ex)
                {
                    continue;
                }
            }


            //入库最后一条记录
            if (!string.IsNullOrEmpty(fieldName))//排除第一行REC, 此时还无记录
            {
                recDict.Add(fieldName, fieldValue);//入库当前记录的最后发现的字段
            }
            if (recDict.Count > 0) //排除第一行REC
            {
                resultList.Add(recDict);
            }

            return resultList;
        }
    }
}
