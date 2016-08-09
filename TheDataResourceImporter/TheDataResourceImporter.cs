using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using TheDataResourceImporter.Models;
using TheDataResourceImporter.Utils;
using System.Data.OleDb;
using SharpCompress.Archive;
using SharpCompress.Common;
using System.Xml.Linq;
using System.Xml.XPath;

namespace TheDataResourceImporter
{
    public class ImportManger
    {

        public static string currentFile = "";
        //totalCount, handledCount, handledXMLCount, handledDirCount
        public static int totalCount = 0;
        public static int handledCount = 0;
        public static int handledXMLCount = 0;
        public static int withExceptionButExtracted = 0;
        public static int withExcepthonAndFiled2Exracted = 0;
        public static int fileCount = 0;
        public static DateTime importStartTime = System.DateTime.Now;


        public static bool forcedStop = false;

        public static int dealCount = 0;
        public static int lostCount = 0;


        public static void resetCounter()
        {
            currentFile = "";
            totalCount = 0;
            handledCount = 0;
            handledXMLCount = 0;
            withExceptionButExtracted = 0;
            withExcepthonAndFiled2Exracted = 0;
            fileCount = 0;
            //清空进度信息
            MessageUtil.DoupdateProgressIndicator(0, 0, 0, 0, "");
        }   



        public static bool BeginImport(string[] AllFilePaths, string fileType)
        {

            //importStartTime = System.DateTime.Now;
            fileCount = AllFilePaths.Length;
            foreach (string path in AllFilePaths)
            {
                //强制终止
                if (forcedStop)
                {
                    MessageUtil.DoAppendTBDetail("强制终止了插入");
                    break;
                }

                currentFile = path.Substring(path.LastIndexOf('\\') + 1);

                try
                {
                    if (File.Exists(path))
                    {

                        ImportByPath(path, fileType);

                        System.GC.Collect();
                    }
                }
                catch (Exception ex)
                {

                    if (ex.Message.Contains("对象名:“Main”"))
                    {
                        continue;
                    }

                    LogHelper.WriteLog("", "error", currentFile + ":" + ex.Message);
                    continue;
                }

            }

            return true;
        }


        public static bool ImportByPath(string filePath, string fileType)
        {
            currentFile = filePath;



            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);
            List<RecModel> list = new List<RecModel>();
            string str = "";

            RecModel rec = null;

            FileInfo selectedFileInfo = new FileInfo(filePath);

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;

            var archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            int entiresCount = archive.Entries.Count();

            totalCount = entiresCount;

            //清零
            handledCount = 0;

            MessageUtil.DoAppendTBDetail("您选择的资源类型为：" + fileType);

            MessageUtil.DoAppendTBDetail("在压缩包中发现" + entiresCount + "个条目，目录详细如下：");


            if (fileType == "中国专利全文代码化数据")
            {


                //确认是否是存在缺失XML的情况
                HashSet<string> dirNameSet = new HashSet<string>();
                HashSet<string> entryFileParentFullPathNameSet = new HashSet<string>();


                StringBuilder sb = new StringBuilder();


                foreach (IArchiveEntry entry in archive.Entries)
                {



                    //当前条目是目录
                    if(entry.IsDirectory)
                    {
                        string entryDirPath = entry.Key;
                        //剔除目录的斜杠
                        if(entryDirPath.EndsWith("/"))
                        {
                            entryDirPath = entryDirPath.Substring(0, entryDirPath.Length - 1);
                        }
                        if(entryDirPath.Contains("/"))
                        {
                            entryDirPath = entryDirPath.Replace('/', '\\');
                        }

                        string[] pathParts = entryDirPath.Split('\\');

                        if(pathParts.Length > 1)
                        {
                            dirNameSet.Add(entryDirPath);
                        }
                    }
                    else
                    {
                        string entryFilePath = entry.Key;
                        //判断是否是XML
                        if(entryFilePath.ToUpper().EndsWith("XML"))
                        {
                            if (entryFilePath.Contains("/"))
                            {
                                entryFilePath = entryFilePath.Replace('/', '\\');
                            }

                            string[] pathParts = entryFilePath.Split('\\');

                            //拼接
                            string parentFullPath = string.Join("\\", pathParts, 0, pathParts.Length - 1);

                            entryFileParentFullPathNameSet.Add(parentFullPath);
                        }



                    }
                    sb.Append(entry.Key + Environment.NewLine);
                }

                dirNameSet.ExceptWith(entryFileParentFullPathNameSet);


                MessageUtil.DoAppendTBDetail(sb.ToString());


                //存在目录内找不到XML的情况
                if (dirNameSet.Count > 0)
                {
                    string msg = "如下压缩包中的文件夹内未发现XML文件：";

                    msg += String.Join(Environment.NewLine, dirNameSet.ToArray());

                    MessageUtil.DoAppendTBDetail(msg);

                    LogHelper.WriteErrorLog(msg);
                }



                MessageUtil.DoAppendTBDetail("当前压缩包：" + selectedFileInfo.Name);
                MessageUtil.DoAppendTBDetail("开始寻找专利XML文件：");
                foreach(IArchiveEntry entry in archive.Entries)
                {
                    handledCount++;

                    if(forcedStop)
                    {
                        MessageUtil.DoAppendTBDetail("强制终止了插入");
                        break;
                    }

                    //MessageUtil.DoAppendTBDetail("当前条目：" + entry.Key + "，是" + (entry.IsDirectory ? "目录" : "文件"));
                    var keyTemp = entry.Key;
                    //当前文件为XML文件
                    if(keyTemp.ToUpper().EndsWith(".XML"))
                    {
                        handledXMLCount++;
                        //解压当前的XML文件
                        string entryFullPath = CompressUtil.writeEntryToTemp(entry);

                        if ("" == entryFullPath.Trim())
                        {
                            MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                            continue;
                        }

                        XDocument doc = XDocument.Load(entryFullPath);

                        string insertStr = "";
                        string valuesStr = "";

                        var appl_typeEles = doc.Root.XPathSelectElements("/cn-patent-document/cn-bibliographic-data/application-reference");

                        if (appl_typeEles.Count() > 0)
                        {
                            insertStr = insertStr + "APPL_TYPE,";
                            var valueTemp = appl_typeEles.ElementAt(0).Attribute("appl-type").Value;
                            valueTemp = String.IsNullOrEmpty(valueTemp) ? "" : valueTemp;
                            valuesStr = valuesStr + "'" + valueTemp + "',";
                        }
                        var pub_countryEles = doc.Root.XPathSelectElements("/cn-patent-document/cn-bibliographic-data/cn-publication-reference/document-id/country");

                        if (pub_countryEles.Count() > 0)
                        {
                            insertStr = insertStr + "PUB_COUNTRY,";
                            var valueTemp = pub_countryEles.ElementAt(0).Value;
                            valueTemp = String.IsNullOrEmpty(valueTemp) ? "" : valueTemp;
                            valuesStr = valuesStr + "'" + valueTemp + "',";
                        }
                        var pub_numberEles = doc.Root.XPathSelectElements("/cn-patent-document/cn-bibliographic-data/cn-publication-reference/document-id/doc-number");

                        if (pub_numberEles.Count() > 0)
                        {
                            insertStr = insertStr + "PUB_NUMBER,";
                            var valueTemp = pub_numberEles.ElementAt(0).Value;
                            valueTemp = String.IsNullOrEmpty(valueTemp) ? "" : valueTemp;
                            valuesStr = valuesStr + "'" + valueTemp + "',";
                        }
                        var pub_dateEles = doc.Root.XPathSelectElements("/cn-patent-document/cn-bibliographic-data/cn-publication-reference/document-id/date");

                        if (pub_dateEles.Count() > 0)
                        {
                            insertStr = insertStr + "PUB_DATE,";
                            var valueTemp = pub_dateEles.ElementAt(0).Value;
                            valueTemp = String.IsNullOrEmpty(valueTemp) ? "" : valueTemp;
                            valuesStr = valuesStr + String.Format("TO_DATE('{0}', 'yyyymmdd')", valueTemp) + ",";
                        }
                        var pub_kindEles = doc.Root.XPathSelectElements("/cn-patent-document/cn-bibliographic-data/cn-publication-reference/document-id/kind");

                        if (pub_kindEles.Count() > 0)
                        {
                            insertStr = insertStr + "PUB_KIND,";
                            var valueTemp = pub_kindEles.ElementAt(0).Value;
                            valueTemp = String.IsNullOrEmpty(valueTemp) ? "" : valueTemp;
                            valuesStr = valuesStr + "'" + valueTemp + "',";
                        }


                        var gazette_numEles = doc.Root.XPathSelectElements("/cn-patent-document/cn-bibliographic-data/cn-publication-reference/gazette-reference/gazette-num");

                        if (gazette_numEles.Count() > 0)
                        {
                            insertStr = insertStr + "GAZETTE_NUM,";
                            var valueTemp = gazette_numEles.ElementAt(0).Value;
                            valueTemp = String.IsNullOrEmpty(valueTemp) ? "" : valueTemp;
                            valuesStr = valuesStr + "'" + valueTemp + "',";
                        }
                        var gazette_dateEles = doc.Root.XPathSelectElements("/cn-patent-document/cn-bibliographic-data/cn-publication-reference/gazette-reference/date");

                        if (gazette_dateEles.Count() > 0)
                        {
                            insertStr = insertStr + "GAZETTE_DATE,";
                            var valueTemp = gazette_dateEles.ElementAt(0).Value;
                            valueTemp = String.IsNullOrEmpty(valueTemp) ? "" : valueTemp;
                            valuesStr = valuesStr + String.Format("TO_DATE('{0}', 'yyyymmdd')", valueTemp) + ",";
                        }
                        var app_countryEles = doc.Root.XPathSelectElements("/cn-patent-document/cn-bibliographic-data/application-reference/country");

                        if (app_countryEles.Count() > 0)
                        {
                            insertStr = insertStr + "APP_COUNTRY,";
                            var valueTemp = app_countryEles.ElementAt(0).Value;
                            valueTemp = String.IsNullOrEmpty(valueTemp) ? "" : valueTemp;
                            valuesStr = valuesStr + "'" + valueTemp + "',";
                        }
                        var app_numberEles = doc.Root.XPathSelectElements("/cn-patent-document/cn-bibliographic-data/application-reference/doc-number");

                        if (app_numberEles.Count() > 0)
                        {
                            insertStr = insertStr + "APP_NUMBER,";
                            var valueTemp = app_numberEles.ElementAt(0).Value;
                            valueTemp = String.IsNullOrEmpty(valueTemp) ? "" : valueTemp;
                            valuesStr = valuesStr + "'" + valueTemp + "',";
                        }
                        var app_dateEles = doc.Root.XPathSelectElements("/cn-patent-document/cn-bibliographic-data/application-reference/date");

                        if (app_dateEles.Count() > 0)
                        {
                            insertStr = insertStr + "APP_DATE,";
                            var valueTemp = app_dateEles.ElementAt(0).Value;
                            valueTemp = String.IsNullOrEmpty(valueTemp) ? "" : valueTemp;
                            valuesStr = valuesStr = insertStr + String.Format("TO_DATE('{0}', 'yyyymmdd')", valueTemp) + ",";
                        }
                        var classification_ipcrEles = doc.Root.XPathSelectElements("/cn-patent-document/cn-bibliographic-data/classifications-ipcr/classification-ipcr[0]/text");

                        if (classification_ipcrEles.Count() > 0)
                        {
                            insertStr = insertStr + "CLASSIFICATION-IPCR,";
                            var valueTemp = classification_ipcrEles.ElementAt(0).Value;
                            valueTemp = String.IsNullOrEmpty(valueTemp) ? "" : valueTemp;
                            valuesStr = valuesStr + "'" + valueTemp + "',";
                        }
                        var invention_titleEles = doc.Root.XPathSelectElements("/cn-patent-document/cn-bibliographic-data/invention-title");

                        if (invention_titleEles.Count() > 0)
                        {
                            insertStr = insertStr + "INVENTION_TITLE,";
                            var valueTemp = invention_titleEles.ElementAt(0).Value;
                            valueTemp = String.IsNullOrEmpty(valueTemp) ? "" : valueTemp;
                            valuesStr = valuesStr + "'" + valueTemp + "',";
                        }
                        var abstractEles = doc.Root.XPathSelectElements("/cn-patent-document/cn-bibliographic-data/abstract");

                        
                        if (abstractEles.Count() > 0)
                        {
                            insertStr = insertStr + "ABSTRACT,";
                            var valueTemp = abstractEles.ElementAt(0).Value;
                            valueTemp = String.IsNullOrEmpty(valueTemp) ? "" : valueTemp;

                            //处理文本出现单引号的情况
                            valueTemp = valueTemp.Replace("'", "''");

                            valuesStr = valuesStr + "'" + valueTemp + "',";
                        }
              
                        insertStr = insertStr + "PATH_XML,";
                        valuesStr = valuesStr + "'" + entry.Key + "',";


                        insertStr = insertStr + "EXIST_XML,";

                        valuesStr = valuesStr + "'1',";

                        var id = System.Guid.NewGuid().ToString();

                        insertStr = insertStr + "ID,";

                        valuesStr = valuesStr + String.Format("'{0}',", id);

                        if(',' == insertStr.Last())
                        {
                            insertStr = insertStr.Substring(0, insertStr.Length - 1);
                        }

                        if (',' == valuesStr.Last())
                        {
                            valuesStr = valuesStr.Substring(0, valuesStr.Length - 1);
                        }

                        string sql = "insert into S_CHINA_PATENT_TEXTCODE(" + insertStr + ") values (" + valuesStr + ")";

                        MessageUtil.DoAppendTBDetail("SQL 当前插入语句：" + sql);

                        int insertedCount = OracleDB.ExecuteSql(sql);

                        if(1 == insertedCount)
                        {
                            MessageUtil.DoAppendTBDetail("记录：" + entry.Key + "插入成功!!!");
                        }
                    }

                    //更新进度信息
                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, handledXMLCount, 0, filePath);
                }
            }
            else if (fileType == "中国专利全文图像数据")
            {

            }
            else if (fileType == "中国专利标准化全文文本数据")
            {

            }
            else if (fileType == "中国专利标准化全文图像数据")
            {


            }
            else if (fileType == "中国专利公报数据")
            {


            }
            else if (fileType == "中国专利著录项目与文摘数据")
            {


            }
            else if (fileType == "中国专利法律状态数据")
            {


            }
            else if (fileType == "中国专利法律状态变更翻译数据")
            {


            }
            else if (fileType == "中国标准化简单引文数据")
            {


            }
            else if (fileType == "专利缴费数据")
            {


            }
            else if (fileType == "公司代码库")
            {


            }
            else if (fileType == "区域代码库")
            {


            }
            else if (fileType == "美国专利全文文本数据（标准化）")
            {


            }
            else if (fileType == "欧专局专利全文文本数据（标准化）")
            {


            }
            else if (fileType == "韩国专利全文代码化数据（标准化）")
            {


            }
            else if (fileType == "瑞士专利全文代码化数据（标准化）")
            {


            }
            else if (fileType == "英国专利全文代码化数据（标准化）")
            {


            }
            else if (fileType == "日本专利全文代码化数据（标准化）")
            {


            }
            else if (fileType == "中国发明申请专利数据（DI）")
            {


            }
            else if (fileType == "中国发明授权专利数据（DI）")
            {


            }
            else if (fileType == "中国实用新型专利数据（DI）")
            {


            }
            else if (fileType == "中国外观设计专利数据（DI）")
            {


            }
            else if (fileType == "中国专利生物序列数据（DI）")
            {


            }
            else if (fileType == "中国专利摘要英文翻译数据（DI）")
            {


            }
            else if (fileType == "专利同族数据（DI）")
            {


            }
            else if (fileType == "全球专利引文数据（DI）")
            {


            }
            else if (fileType == "中国专利费用信息数据（DI）")
            {


            }
            else if (fileType == "中国专利通知书数据（DI）")
            {


            }
            else if (fileType == "中国法律状态标引库（DI）")
            {


            }
            else if (fileType == "专利分类数据(分类号码)（DI）")
            {


            }
            else if (fileType == "世界法律状态数据（DI）")
            {


            }
            else if (fileType == "DOCDB数据（DI）")
            {


            }
            else if (fileType == "美国专利著录项及全文数据（US）（DI）")
            {


            }
            else if (fileType == "韩国专利著录项及全文数据（KR）（DI）")
            {


            }
            else if (fileType == "欧洲专利局专利著录项及全文数据（EP）（DI）")
            {


            }
            else if (fileType == "国际知识产权组织专利著录项及全文数据（WIPO)（DI）")
            {


            }
            else if (fileType == "加拿大专利著录项及全文数据（CA）（DI）")
            {


            }
            else if (fileType == "俄罗斯专利著录项及全文数据（RU）（DI）")
            {


            }
            else if (fileType == "英国专利全文数据（GB）（DI）")
            {


            }
            else if (fileType == "瑞士专利全文数据（CH）（DI）")
            {


            }
            else if (fileType == "日本专利著录项及全文数据（JP）（DI）")
            {


            }
            else if (fileType == "德国专利著录项及全文数据（DE）（DI）")
            {


            }
            else if (fileType == "法国专利著录项及全文数据（FR）（DI）")
            {


            }
            else if (fileType == "比利时专利全文数据（BE）（标准化）")
            {


            }
            else if (fileType == "奥地利专利全文数据（AT）（标准化）")
            {


            }
            else if (fileType == "西班牙专利全文数据（ES）（标准化）")
            {


            }
            else if (fileType == "波兰专利著录项及全文数据（PL）（标准化）")
            {


            }
            else if (fileType == "以色列专利著录项及全文数据（IL）（标准化）")
            {


            }
            else if (fileType == "新加坡专利著录项及全文数据（SG）（标准化）")
            {


            }
            else if (fileType == "台湾专利著录项及全文数据（TW）（DI）")
            {


            }
            else if (fileType == "香港专利著录项数据（HK）（DI）")
            {


            }
            else if (fileType == "澳门专利著录项数据（MO）（DI）")
            {


            }
            else if (fileType == "欧亚组织专利著录项及全文数据（EA）（DI）")
            {


            }
            else if (fileType == "美国外观设计专利数据（DI）")
            {


            }
            else if (fileType == "日本外观设计专利数据（DI）")
            {


            }
            else if (fileType == "韩国外观设计专利数据（DI）")
            {


            }
            else if (fileType == "德国外观设计专利数据（DI）")
            {


            }
            else if (fileType == "法国外观设计专利数据（DI）")
            {


            }
            else if (fileType == "俄罗斯外观设计专利数据（DI）")
            {


            }
            else if (fileType == "中国专利全文数据PDF（DI）")
            {


            }
            else if (fileType == "国外专利全文数据PDF（DI）")
            {


            }
            else if (fileType == "日本专利文摘英文翻译数据（PAJ)（DI）")
            {


            }
            else if (fileType == "韩国专利文摘英文翻译数据(KPA)（DI）")
            {


            }
            else if (fileType == "俄罗斯专利文摘英文翻译数据（DI）")
            {


            }
            else if (fileType == "中国商标")
            {


            }
            else if (fileType == "中国商标许可数据")
            {


            }
            else if (fileType == "中国商标转让数据")
            {


            }
            else if (fileType == "马德里商标进入中国")
            {


            }
            else if (fileType == "中国驰名商标数据")
            {


            }
            else if (fileType == "美国申请商标")
            {


            }
            else if (fileType == "美国转让商标")
            {


            }
            else if (fileType == "美国审判商标")
            {


            }
            else if (fileType == "社内外知识产权图书题录数据")
            {


            }
            else if (fileType == "民国书")
            {


            }
            else if (fileType == "中外期刊的著录项目与文摘数据")
            {


            }
            else if (fileType == "中国法院判例初加工数据")
            {


            }
            else if (fileType == "中国商标分类数据")
            {


            }
            else if (fileType == "美国商标图形分类数据")
            {


            }
            else if (fileType == "美国商标美国分类数据")
            {


            }
            else if (fileType == "马德里商标购买数据")
            {


            }
            else if (fileType == "中国专利代理知识产权法律法规加工数据")
            {


            }
            else if (fileType == "中国集成电路布图公告及事务数据")
            {


            }
            else if (fileType == "中国知识产权海关备案数据")
            {


            }
            else if (fileType == "国外专利生物序列加工成品数据")
            {


            }
            else if (fileType == "中国专利复审数据")
            {


            }
            else if (fileType == "中国专利无效数据")
            {


            }
            else if (fileType == "中国专利的判决书数据")
            {


            }
            else if (fileType == "中国生物序列深加工数据")
            {


            }
            else if (fileType == "中国中药专利翻译数据")
            {


            }
            else if (fileType == "中国化学药物专利深加工数据")
            {


            }
            else if (fileType == "中国中药专利深加工数据")
            {


            }











            /**
             * 判断文档的类型
             * */























            /**
             * 解析XML入库
             * */

            //while ((str = sr.ReadLine()) != null)
            //{
            //}
            //最后一个REC

            //数据库Test
            //DataTable dt = OracleDB.GetDT("select * from S_CHINA_Patent_TextCode t"); ;


            //importRec(rec);


            //MessageUtil.SetMessage(currentFile + "  已处理件数:" + dealCount + "  缺失件数:" + lostCount);

            sr.Close();

            fs.Close();
            return true;
        }














        public static bool importRec(RecModel rec)
        {
            try
            {
                string tableName = "";
                dealCount++;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("", "error", currentFile + ": " + rec.SWPUBDATE + "-" + rec.AN + "-" + rec.FLZT + "-" + rec.FLZTInfo + "--" + ex.Message);
                return true;
            }

            return false;
        }



        public static bool RecordExist(RecModel rec, string tableName)
        {
            try
            {
                string sql = @"SELECT APPLYNO FROM {0}
                                WHERE AUXIDX='{1}'";

                string auxidx = rec.SWPUBDATE + rec.AN + rec.FLZT + rec.FLZTInfo;

                sql = string.Format(sql, tableName, auxidx);

                if (OracleDB.GetDT(sql).Rows.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("", "error", currentFile + ": " + rec.SWPUBDATE + "-" + rec.AN + "-" + rec.FLZT + "-" + rec.FLZTInfo + "--" + ex.Message);
                return true;
            }

        }
    }
}
