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
using System.Threading.Tasks;
using System.Xml;

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
            withExceptionButExtracted = 0;
            withExcepthonAndFiled2Exracted = 0;
            fileCount = 0;
            ImportManger.importStartTime = System.DateTime.Now;
            //清空进度信息
            MessageUtil.DoupdateProgressIndicator(0, 0, 0, 0, "");
        }



        public static bool BeginImport(string[] AllFilePaths, string fileType)
        {

            //importStartTime = System.DateTime.Now;
            fileCount = AllFilePaths.Length;
            using (DataSourceEntities dataSourceEntites = new DataSourceEntities())
            {
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
                            ImportByPath(path, fileType, dataSourceEntites);
                            System.GC.Collect();
                        }
                    }
                    catch (Exception ex)
                    {

                        if (ex.Message.Contains("对象名:“Main”"))
                        {
                            continue;
                        }

                        //LogHelper.WriteLog("", "error", currentFile + ":" + ex.Message);
                        LogHelper.WriteErrorLog($"导入文件{currentFile}时发生错误,错误消息:{ex.Message}");
                        continue;
                    }
                }
            }

            return true;
        }


        public static bool ImportByPath(string filePath, string fileType, DataSourceEntities entiesContext)
        {
            currentFile = filePath;

            MessageUtil.DoAppendTBDetail("您选择的资源类型为：" + fileType);

            MessageUtil.DoAppendTBDetail("当前文件：" + filePath);



            //导入操作信息
            IMPORT_SESSION importSession = MiscUtil.getNewImportSession(fileType, filePath);
            entiesContext.IMPORT_SESSION.Add(importSession);
            entiesContext.SaveChanges();
            //判断是否是
            #region 分文件类型进行处理
            #region 01 中国专利全文代码化数据
            //压缩包内解析XML
            //目前监测了XML文件缺失的情况
            if (fileType == "中国专利全文代码化数据")
            {

                importSession.TABLENAME = "S_CHINA_PATENT_TEXTCODE";
                entiesContext.SaveChanges();

                importStartTime = System.DateTime.Now;

                //清零
                handledCount = 0;


                SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;

                IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

                importSession.IS_ZIP = "Y";
                entiesContext.SaveChanges();

                //总条目数
                totalCount = archive.Entries.Count();
                importSession.ZIP_ENTRIES_COUNT = totalCount;

                #region 检查目录内无XML的情况
                var dirNameSetEntires = (from entry in archive.Entries.AsParallel()
                                         where entry.IsDirectory && CompressUtil.getDirEntryDepth(entry.Key) == 2
                                         select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();


                var xmlEntryParentDirEntries = (from entry in archive.Entries.AsParallel()
                                                where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML")
                                                select CompressUtil.getFileEntryParentPath(entry.Key)).Distinct();

                var dirEntriesWithoutXML = dirNameSetEntires.Except(xmlEntryParentDirEntries);

                //发现存在XML不存在的情况
                if (dirEntriesWithoutXML.Count() > 0)
                {
                    string msg = "如下压缩包中的文件夹内未发现XML文件：";
                    msg += String.Join(Environment.NewLine, dirEntriesWithoutXML.ToArray());
                    MessageUtil.DoAppendTBDetail(msg);
                    LogHelper.WriteErrorLog(msg);

                    foreach (string entryKey in dirEntriesWithoutXML)
                    {
                        importSession.HAS_ERROR = "Y";
                        IMPORT_ERROR importError = new IMPORT_ERROR() { ID = System.Guid.NewGuid().ToString(), SESSION_ID = importSession.SESSION_ID, IGNORED = "N", ISZIP = "Y", POINTOR = handledCount, ZIP_OR_DIR_PATH = filePath, REIMPORTED = "N", ZIP_PATH = entryKey, OCURREDTIME = System.DateTime.Now, ERROR_MESSAGE = "文件夹中不存在XML" };
                        importSession.FAILED_COUNT++;
                        entiesContext.IMPORT_ERROR.Add(importError);
                        entiesContext.SaveChanges();
                    }
                }
                #endregion

                #region ----待删除 检查目录内无XML的情况 已Linq重构
                /***
                //确认是否是存在缺失XML的情况
                HashSet<string> dirNameSet = new HashSet<string>();
                HashSet<string> entryFileParentFullPathNameSet = new HashSet<string>();

                StringBuilder sb = new StringBuilder();

                foreach (IArchiveEntry entry in archive.Entries)
                {
                    //当前条目是目录
                    if (entry.IsDirectory)
                    {
                        string entryDirPath = entry.Key;

                        if (CompressUtil.getDirEntryDepth(entryDirPath) > 1)
                        {
                            dirNameSet.Add(entryDirPath);
                        }
                    }
                    else
                    {
                        string entryFilePath = entry.Key;
                        //判断是否是XML
                        if (entryFilePath.ToUpper().EndsWith("XML"))
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
                **/
                #endregion

                MessageUtil.DoAppendTBDetail("开始寻找专利XML文件：");

                var allXMLEntires = from entry in archive.Entries.AsParallel()
                                    where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML")
                                    select entry;

                totalCount = allXMLEntires.Count();

                MessageUtil.DoAppendTBDetail("在压缩包中发现" + totalCount + "个待导入XML条目");

                //已处理计数清零
                handledCount = 0;
                #region 循环入库
                foreach (IArchiveEntry entry in allXMLEntires)
                {
                    //计数变量
                    handledCount++;

                    if (forcedStop)
                    {
                        MessageUtil.DoAppendTBDetail("强制终止了插入");
                        importSession.NOTE = "用户强制终止了本次插入";
                        entiesContext.SaveChanges();
                        break;
                    }

                    var keyTemp = entry.Key;

                    //解压当前的XML文件
                    string entryFullPath = CompressUtil.writeEntryToTemp(entry);

                    if ("" == entryFullPath.Trim())
                    {
                        MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                        LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                        importSession.FAILED_COUNT++;
                        IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                        entiesContext.IMPORT_ERROR.Add(errorTemp);
                        entiesContext.SaveChanges();
                        continue;
                    }

                    S_CHINA_PATENT_TEXTCODE sCNPatentTextCode = new S_CHINA_PATENT_TEXTCODE() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                    sCNPatentTextCode.ARCHIVE_INNER_PATH = entry.Key;
                    sCNPatentTextCode.FILE_PATH = filePath;
                    //sCNPatentTextCode.SESSION_INDEX = handledCount;
                    entiesContext.S_CHINA_PATENT_TEXTCODE.Add(sCNPatentTextCode);
                    //entiesContext.SaveChanges();

                    XDocument doc = XDocument.Load(entryFullPath);

                    #region 具体的入库操作,EF
                    //获取所有字段名， 获取字段的配置信息， 对字段值进行复制， 
                    //appl-type
                    var rootElement = doc.Root;

                    var appl_type = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/application-reference", "appl-type");
                    sCNPatentTextCode.APPL_TYPE = appl_type;
                    entiesContext.SaveChanges();

                    var pub_country = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/cn-publication-reference/document-id/country");
                    sCNPatentTextCode.PUB_COUNTRY = pub_country;

                    var pub_number = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/cn-publication-reference/document-id/doc-number");
                    sCNPatentTextCode.PUB_NUMBER = pub_number;

                    var pub_date = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/cn-publication-reference/document-id/date");

                    try
                    {
                        sCNPatentTextCode.PUB_DATE = DateTime.ParseExact(pub_date, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
                    }
                    catch (Exception)
                    {
                    }


                    var pub_kind = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/cn-publication-reference/document-id/kind");
                    sCNPatentTextCode.PUB_KIND = pub_kind;

                    var gazette_num = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/cn-publication-reference/gazette-reference/gazette-num");
                    sCNPatentTextCode.GAZETTE_NUM = gazette_num;

                    var gazette_date = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/cn-publication-reference/gazette-reference/date");

                    try
                    {
                        sCNPatentTextCode.GAZETTE_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(gazette_date);
                    }
                    catch (Exception)
                    {
                    }

                    var app_country = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/application-reference/document-id/country");
                    sCNPatentTextCode.APP_COUNTRY = app_country;

                    var app_number = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/application-reference/document-id/doc-number");
                    sCNPatentTextCode.APP_NUMBER = app_number;


                    var app_date = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/application-reference/document-id/date");
                    try
                    {
                        sCNPatentTextCode.APP_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(app_date);
                    }
                    catch (Exception)
                    {

                    }

                    var classification_ipcr = MiscUtil.getXElementValueByTagNameaAndChildTabName(rootElement, "main-classification");

                    if (String.IsNullOrEmpty(classification_ipcr))
                    {
                        classification_ipcr = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/classifications-ipcr/classification-ipcr/text");
                    }

                    sCNPatentTextCode.CLASSIFICATION_IPCR = classification_ipcr;

                    var invention_title = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/invention-title");
                    sCNPatentTextCode.INVENTION_TITLE = invention_title;

                    var abstractEle = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/abstract");
                    sCNPatentTextCode.ABSTRACT = abstractEle;

                    sCNPatentTextCode.PATH_XML = entry.Key;

                    sCNPatentTextCode.EXIST_XML = "1";

                    sCNPatentTextCode.IMPORT_TIME = System.DateTime.Now;

                    entiesContext.SaveChanges();

                    //输出插入记录
                    var currentValue = MiscUtil.jsonSerilizeObject(sCNPatentTextCode);

                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");

                    /**
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

                    if (',' == insertStr.Last())
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

                    if (1 == insertedCount)
                    {
                        MessageUtil.DoAppendTBDetail("记录：" + entry.Key + "插入成功!!!");
                    }
                    **/
                    #endregion

                    #region 解析XML,插入数据库 已使用EF重构
                    /**
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

                    if (',' == insertStr.Last())
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

                    if (1 == insertedCount)
                    {
                        MessageUtil.DoAppendTBDetail("记录：" + entry.Key + "插入成功!!!");
                    }
                    **/
                    #endregion


                    //更新进度信息
                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                }
                #endregion 循环入库

                if (0 == allXMLEntires.Count())
                {
                    MessageUtil.DoAppendTBDetail("没有找到XML");
                    importSession.NOTE = "没有找到XML";
                    //添加错误信息
                    entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", filePath, "", ""));
                    entiesContext.SaveChanges();
                }

            }
            #endregion

            #region 02 中国专利全文图像数据
            else if (fileType == "中国专利全文图像数据")
            {
                importSession.TABLENAME = "s_China_Patent_Textimage".ToUpper();
                entiesContext.SaveChanges();


                importStartTime = System.DateTime.Now;

                FileInfo selectedFileInfo = new FileInfo(filePath);


                SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;

                IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

                importSession.IS_ZIP = "Y";
                int zipEntriesCount = archive.Entries.Count();
                importSession.ZIP_ENTRIES_COUNT = zipEntriesCount;
                entiesContext.SaveChanges();

                //总条目数

                //S_CHINA_PATENT_TEXTIMAGE sCNPatTxtImg = new S_CHINA_PATENT_TEXTIMAGE();

                var APPL_TYPE = "";
                try
                {
                    string appl_type = selectedFileInfo.Directory.Parent.Name;
                    APPL_TYPE = appl_type;
                }
                catch (Exception)
                {

                }

                var pub_dateEntry = (from entry in archive.Entries.AsParallel()
                                     where entry.IsDirectory && CompressUtil.getDirEntryDepth(entry.Key) == 1
                                     select CompressUtil.removeDirEntrySlash(entry.Key)).FirstOrDefault();

                DateTime? PUB_DATE = System.DateTime.Now;
                if (null != pub_dateEntry)
                {
                    PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(pub_dateEntry);
                }



                //所有的待导入条目
                var dirNameSetEntires = (from entry in archive.Entries.AsParallel()
                                         where entry.IsDirectory && CompressUtil.getDirEntryDepth(entry.Key) == 2
                                         select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();


                //所有包含Tif的条目
                var tifEntryParentDirEntries = (from entry in archive.Entries.AsParallel()
                                                where !entry.IsDirectory&& entry.Key.ToUpper().EndsWith(".TIF")
                                                select CompressUtil.getFileEntryParentPath(entry.Key)).Distinct();



                //不包含tif的目录
                var dirEntiresWithoutTif = dirNameSetEntires.Except(tifEntryParentDirEntries);

                totalCount = dirEntiresWithoutTif.Count() + tifEntryParentDirEntries.Count();

                handledCount = 0;

                //包含tif
                Parallel.ForEach<string>(tifEntryParentDirEntries, key => {
                    lock (typeof(ImportManger))
                    {
                        handledCount++;
                        string importedMsg = ImportLogicUtil.importS_China_Patent_TextImage(entiesContext, filePath, importSession.SESSION_ID, APPL_TYPE, PUB_DATE, key, "1");
                        MessageUtil.appendTbDetail($"记录:{importedMsg}插入成功");
                        MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                    }
                });

                //不包含tif
                Parallel.ForEach<string>(dirEntiresWithoutTif, key => {
                    lock (typeof(ImportManger))
                    {
                        handledCount++;
                        string importedMsg = ImportLogicUtil.importS_China_Patent_TextImage(entiesContext, filePath, importSession.SESSION_ID, APPL_TYPE, PUB_DATE, key, "0");
                        MessageUtil.appendTbDetail($"记录:{importedMsg}插入成功");
                     }
                });
                
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);

            }
            #endregion

            #region 03 中国专利标准化全文文本数据 
            //有疑问: XML结构不同, 文件路径不确定
            else if(fileType == "中国专利标准化全文文本数据")
            {

                handledCount = 0;
                importStartTime = importSession.START_TIME.Value;

                importSession.TABLENAME = "S_China_Patent_StandardFullTxt".ToUpper();
                entiesContext.SaveChanges();

                SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
                IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

                //总条目数
                importSession.IS_ZIP = "Y";
                totalCount = archive.Entries.Count();
                importSession.ZIP_ENTRIES_COUNT = totalCount;
                entiesContext.SaveChanges();

                #region 检查目录内无XML的情况
                var dirNameSetEntires = (from entry in archive.Entries.AsParallel()
                                         where entry.IsDirectory && CompressUtil.getDirEntryDepth(entry.Key) == 2
                                         select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();


                //排除压缩包中无关XML
                var xmlEntryParentDirEntries = (from entry in archive.Entries.AsParallel()
                                                where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getDirEntryDepth(entry.Key) == 3
                                                select CompressUtil.getFileEntryParentPath(entry.Key)).Distinct();

                var dirEntriesWithoutXML = dirNameSetEntires.Except(xmlEntryParentDirEntries);

                //发现存在XML不存在的情况
                if (dirEntriesWithoutXML.Count() > 0)
                {
                    string msg = "如下压缩包中的文件夹内未发现XML文件：";
                    msg += String.Join(Environment.NewLine, dirEntriesWithoutXML.ToArray());
                    MessageUtil.DoAppendTBDetail(msg);
                    LogHelper.WriteErrorLog(msg);

                    foreach (string entryKey in dirEntriesWithoutXML)
                    {
                        importSession.HAS_ERROR = "Y";
                        IMPORT_ERROR importError = new IMPORT_ERROR() { ID = System.Guid.NewGuid().ToString(), SESSION_ID = importSession.SESSION_ID, IGNORED = "N", ISZIP = "Y", POINTOR = handledCount, ZIP_OR_DIR_PATH = filePath, REIMPORTED = "N", ZIP_PATH = entryKey, OCURREDTIME = System.DateTime.Now, ERROR_MESSAGE = "文件夹中不存在XML" };
                        importSession.FAILED_COUNT++;
                        entiesContext.IMPORT_ERROR.Add(importError);
                        entiesContext.SaveChanges();
                    }
                }
                #endregion


                MessageUtil.DoAppendTBDetail("开始寻找'中国专利标准化全文文本数据'XML文件：");

                var allXMLEntires = from entry in archive.Entries.AsParallel()
                                    where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getDirEntryDepth(entry.Key) == 3
                                    select entry;

                totalCount = allXMLEntires.Count();

                MessageUtil.DoAppendTBDetail("在压缩包中发现" + totalCount + "个待导入XML条目");

                //已处理计数清零
                handledCount = 0;
                #region 循环入库
                foreach (IArchiveEntry entry in allXMLEntires)
                {
                    //计数变量
                    handledCount++;

                    if (forcedStop)
                    {
                        MessageUtil.DoAppendTBDetail("强制终止了插入");
                        importSession.NOTE = "用户强制终止了本次插入";
                        entiesContext.SaveChanges();
                        break;
                    }

                    var keyTemp = entry.Key;

                    //解压当前的XML文件
                    string entryFullPath = CompressUtil.writeEntryToTemp(entry);

                    if ("" == entryFullPath.Trim())
                    {
                        MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                        LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                        importSession.FAILED_COUNT++;
                        IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                        entiesContext.IMPORT_ERROR.Add(errorTemp);
                        entiesContext.SaveChanges();
                        continue;
                    }

                    S_CHINA_PATENT_STANDARDFULLTXT entityObject = new S_CHINA_PATENT_STANDARDFULLTXT() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                    entityObject.ARCHIVE_INNER_PATH = entry.Key;
                    entityObject.FILE_PATH = filePath;
                    //sCNPatentTextCode.SESSION_INDEX = handledCount;
                    entiesContext.S_CHINA_PATENT_STANDARDFULLTXT.Add(entityObject);
                    //entiesContext.SaveChanges();

                    XDocument doc = XDocument.Load(entryFullPath);

                    #region 具体的入库操作,EF
                    //获取所有字段名， 获取字段的配置信息， 对字段值进行复制， 

                    //定义命名空间
                    XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.CreateReader().NameTable);
                    namespaceManager.AddNamespace("base", "http://www.sipo.gov.cn/XMLSchema/base");
                    namespaceManager.AddNamespace("business", "http://www.sipo.gov.cn/XMLSchema/business");
                    //namespaceManager.AddNamespace("m", "http://www.w3.org/1998/Math/MathML");
                    //namespaceManager.AddNamespace("tbl", "http://oasis-open.org/specs/soextblx");

                    var rootElement = doc.Root;
                    //entityObject.STA_PUB_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/business:PublicationReference", "appl-type");
                    entityObject.STA_PUB_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                    entityObject.STA_PUB_NUMBER = MiscUtil.getXElementValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:DocNumber", "", namespaceManager);
                    entityObject.STA_PUB_KIND = MiscUtil.getXElementValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:Kind", "", namespaceManager);
                    entityObject.STA_PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:Date", "", namespaceManager));


                    entityObject.ORI_PUB_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                    entityObject.ORI_PUB_NUMBER = MiscUtil.getXElementValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:DocNumber", "", namespaceManager);
                    entityObject.ORI_PUB_KIND = MiscUtil.getXElementValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:Kind", "", namespaceManager);
                    entityObject.ORI_PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:Date", "", namespaceManager));


                    entityObject.STA_APP_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);;
                    entityObject.STA_APP_NUMBER = MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:DocNumber", "", namespaceManager);
                    entityObject.STA_APP_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:Date", "", namespaceManager));


                    entityObject.ORI_APP_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                    entityObject.ORI_APP_NUMBER = MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:DocNumber", "", namespaceManager);
                    entityObject.ORI_APP_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:Date", "", namespaceManager));


                    entityObject.DESIGN_PATENTNUMBER = MiscUtil.getXElementValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBibliographicData/business:PatentNumber", "", namespaceManager);

                    entityObject.CLASSIFICATIONIPCR = MiscUtil.getXElementValueByXPath(rootElement, "//business:ClassificationIPCRDetails/business:ClassificationIPCR[@sequence='1']/base:Text", "", namespaceManager);

                    entityObject.CLASSIFICATIONLOCARNO = MiscUtil.getXElementValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBibliographicData/business:ClassificationLocarno", "", namespaceManager);

                    entityObject.INVENTIONTITLE = MiscUtil.getXElementValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:BibliographicData/business:InventionTitle", "", namespaceManager);

                    entityObject.ABSTRACT = MiscUtil.getXElementValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:Abstract/base:Paragraphs", "", namespaceManager);

                    entityObject.DESIGNBRIEFEXPLANATION = MiscUtil.getXElementValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBriefExplanation", "", namespaceManager);
                    entityObject.FULLDOCIMAGE_NUMBEROFFIGURES = MiscUtil.getXElementValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:FullDocImagenumberOfFigures", "", namespaceManager);
                    entityObject.FULLDOCIMAGE_TYPE = MiscUtil.getXElementValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:FullDocImage/type", "", namespaceManager);

                    
                    entityObject.PATH_STA_FULLTEXT = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + CompressUtil.getFileEntryParentPath(entry.Key);

                    entityObject.EXIST_STA_FULLTEXT = "1";

                    //entityObject.PATH_DI_ABS_BIB = null;

                    //entityObject.PATH_DI_CLA_DES_DRA = null;

                    //entityObject.PATH_DI_BRI_DBI = null;

                    //entityObject.EXIST_DI_ABS_BIB = "0";

                    //entityObject.EXIST_DI_CLA_DES_DRA = "0";

                    //entityObject.EXIST_DI_BRI_DBI = "0";

                    //entityObject.PATH_FULLTEXT = null;

                    //entityObject.EXIST_FULLTEXT = "0";

                    entiesContext.SaveChanges();


                    //输出插入记录
                    var currentValue = MiscUtil.jsonSerilizeObject(entityObject);

                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");

                    #endregion

                    //更新进度信息
                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                }
                #endregion 循环入库

                if (0 == allXMLEntires.Count())
                {
                    MessageUtil.DoAppendTBDetail("没有找到XML");
                    importSession.NOTE = "没有找到XML";
                    //添加错误信息
                    entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", filePath, "", ""));
                    entiesContext.SaveChanges();
                }
            }

            #endregion

            #region 04 中国专利标准化全文图像数据

            else if (fileType == "中国专利标准化全文图像数据")
            {







            }

            #endregion

            #region 05 中国专利公报数据
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
            #endregion
            #endregion

            importSession.LAST_TIME = new Decimal(importSession.START_TIME != null ? DateTime.Now.Subtract(importSession.START_TIME.Value).TotalSeconds : 0);

            //是否发生错误
            importSession.HAS_ERROR = importSession.FAILED_COUNT > 0 ? "Y" : "N";

            importSession.ZIP_ENTRY_POINTOR = handledCount;

            importSession.COMPLETED = totalCount == handledCount ? "Y" : "N";

            importSession.ITEMS_POINT = handledCount;

            importSession.TOTAL_ITEM = totalCount;

            entiesContext.SaveChanges();

            return true;
        }
    }
}
