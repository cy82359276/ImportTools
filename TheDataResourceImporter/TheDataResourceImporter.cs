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
using UpdateDataFromExcel.Utils;
using System.Windows.Forms;

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

        public static string bathId = "";

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
                var bath = MiscUtil.getNewImportBathObject(fileType);
                bathId = bath.ID;

                #region 文件夹模式 解析符合条件的文件

                if (!Main.showFileDialog)//文件夹模式
                {
                    if (AllFilePaths.Length != 1) //文件夹模式只有一个文件夹路径
                    {
                        var message = $"{MiscUtil.jsonSerilizeObject(AllFilePaths)}文件夹路径不正确";
                        MessageUtil.DoAppendTBDetail(message);
                        LogHelper.WriteErrorLog(message);
                        return true;
                    }

                    string dirPath = AllFilePaths[0];

                    if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath))//路径为空, 路径对应的文件夹不存在
                    {
                        var message = $"文件夹路径{dirPath}不正确";
                        MessageUtil.DoAppendTBDetail(message);
                        LogHelper.WriteErrorLog(message);
                        return true;
                    }

                    string suffixFilter = "";

                    if (
                        "中国专利法律状态变更翻译数据".Equals(fileType) ||
                        "中国中药专利翻译数据".Equals(fileType) ||
                        "中国化学药物专利深加工数据".Equals(fileType) ||
                        "中国中药专利深加工数据".Equals(fileType)
                        )
                    {
                        //MDB
                        suffixFilter = "*.mdb";



                    }
                    else if ("中国专利公报数据".Equals(fileType) || "专利缴费数据".Equals(fileType))
                    {
                        //TRS
                        suffixFilter = "*.trs";


                    }
                    else if ("中国专利法律状态数据".Equals(fileType))
                    {
                        //TXT
                        suffixFilter = "*.txt";


                    }
                    else if ("中国商标分类数据".Equals(fileType) || "美国商标图形分类数据".Equals(fileType) || "美国商标美国分类数据".Equals(fileType))
                    {
                        //EXCEL
                        suffixFilter = "*.xlsx";


                    }
                    else //默认是zip包
                    {
                        //Zip XML
                        suffixFilter = "*.zip";
                    }


                    // List<FileInfo> fileInfos = MiscUtil.getFileInfosByDirPathRecuriouslyWithMultiSearchPattern(dirPath, new string[] { suffixFilter.ToLower(), suffixFilter.ToUpper()});
                    List<FileInfo> fileInfos = MiscUtil.getFileInfosByDirPathRecuriouslyWithSingleSearchPattern(dirPath, suffixFilter);



                    var allFoundFilePaths = (from fileTemp in fileInfos
                                             select fileTemp.FullName).Distinct().ToArray();

                    if (allFoundFilePaths.Count() == 0)
                    {
                        MessageBox.Show("没有找到指定的文件，请选择正确的路径！");
                        LogHelper.WriteErrorLog("没有找到指定的文件");
                        return true;
                    }
                    else
                    {
                        MessageUtil.DoAppendTBDetail($"发现{allFoundFilePaths.Count()}个符合条件的文件,他们是{MiscUtil.jsonSerilizeObject(allFoundFilePaths)}");
                        AllFilePaths = allFoundFilePaths;
                        bath.IS_DIR_MODE = "Y";
                        bath.DIR_PATH = dirPath;
                    }
                }

                #endregion


                bath.FILECOUNT = AllFilePaths.Count();
                dataSourceEntites.S_IMPORT_BATH.Add(bath);
                dataSourceEntites.SaveChanges();



                #region 对指定的或发现的路径进行处理

                foreach (string path in AllFilePaths)//遍历处理需要处理的路径
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
                            ImportByPath(path, fileType, dataSourceEntites, bath);
                            System.GC.Collect();
                        }
                        else
                        {
                            MessageBox.Show($"指定的文件不存在{path}");
                        }
                    }
                    catch (Exception ex)
                    {

                        if (ex.Message.Contains("对象名:“Main”"))
                        {
                            continue;
                        }

                        //LogHelper.WriteLog("", "error", currentFile + ":" + ex.Message);
                        LogHelper.WriteErrorLog($"导入文件{currentFile}时发生错误,错误消息:{ex.Message}详细信息{ex.StackTrace}");
                        continue;
                    }
                }
                #endregion


                var lastTime = DateTime.Now.Subtract(bath.START_TIME.Value).TotalSeconds;
                bath.LAST_TIME = new decimal(lastTime);
                bath.ISCOMPLETED = "Y";

                MessageUtil.DoAppendTBDetail($"当前批次运行完毕，处理了{bath.FILECOUNT}个文件，入库了{bath.HANDLED_ITEM_COUNT}条目，总耗时{bath.LAST_TIME}秒");

                dataSourceEntites.SaveChanges();
            }

            return true;
        }

        public static bool ImportByPath(string filePath, string fileType, DataSourceEntities entiesContext, S_IMPORT_BATH bath)
        {
            //fileType = fileType.Trim();

            #region 导入前准备 新建session对象
            currentFile = filePath;
            MessageUtil.DoAppendTBDetail("您选择的资源类型为：" + fileType);
            MessageUtil.DoAppendTBDetail("当前文件：" + filePath);


            //导入操作信息
            IMPORT_SESSION importSession = MiscUtil.getNewImportSession(fileType, filePath, bath);
            entiesContext.IMPORT_SESSION.Add(importSession);
            entiesContext.SaveChanges();
            #endregion



            //判断是否是
            #region 分文件类型进行处理
            #region 01 中国专利全文代码化数据
            //压缩包内解析XML
            //目前监测了XML文件缺失的情况
            if (fileType == "中国专利全文代码化数据")
            {
                parseZip01(filePath, entiesContext, importSession);

            }
            #endregion

            #region 02 中国专利全文图像数据
            else if (fileType == "中国专利全文图像数据")
            {
                parseZip02(filePath, entiesContext, importSession);

            }
            #endregion

            #region 03 中国专利标准化全文文本数据 通用字段
            //有疑问: XML结构不同, 文件路径不确定
            else if (fileType == "中国专利标准化全文文本数据")
            {
                parseZip03(filePath, entiesContext, importSession);
            }

            #endregion

            #region 04 中国专利标准化全文图像数据 XML

            else if (fileType == "中国专利标准化全文图像数据")
            {
                //未根据Index文件进行完整性校验
                parseZip04(filePath, entiesContext, importSession);
            }

            #endregion
            #region 05 中国专利公报数据 TRS
            else if (fileType == "中国专利公报数据")
            {
                parseTRS05(filePath, entiesContext, importSession);
            }

            #endregion
            #region 06 中国专利著录项目与文摘数据 通用字段 未完成
            else if (fileType == "中国专利著录项目与文摘数据")
            {
                //parse06(filePath, entiesContext, importSession);
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC, typeof(S_CHINA_PATENT_BIBLIOGRAPHIC));
            }
            #endregion
            #region 10 中国专利数据法律状态数据 TRS
            else if (fileType == "中国专利法律状态数据")
            {
                parseTRS10(filePath, entiesContext, importSession);

            }
            #endregion
            #region  11 中国专利法律状态变更翻译数据 mdb文件
            else if (fileType == "中国专利法律状态变更翻译数据")
            {
                parseMDB11(filePath, entiesContext, importSession);
            }
            #endregion
            #region 13 中国标准化简单引文数据 通用字段
            else if (fileType == "中国标准化简单引文数据")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_CHINA_STANDARD_SIMPCITATION, typeof(S_CHINA_STANDARD_SIMPCITATION));
            }
            #endregion
            #region 14 专利缴费数据 TRS
            else if (fileType == "专利缴费数据")
            {
                parseTRS14(filePath, entiesContext, importSession);

            }
            #endregion
            #region 16  公司代码库 未完成 无样例
            else if (fileType == "公司代码库")
            {


            }
            #endregion
            #region 17 区域代码库 未完成 无样例
            else if (fileType == "区域代码库")
            {


            }
            #endregion
            #region 50 美国专利全文文本数据（标准化） 通用 未完成 未建库
            else if (fileType == "美国专利全文文本数据（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_AMERICAN_PATENT_FULLTEXT, typeof(S_AMERICAN_PATENT_FULLTEXT));
            }
            #endregion
            #region 51 欧专局专利全文文本数据（标准化） 通用 未完成 未建库
            else if (fileType == "欧专局专利全文文本数据（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_EUROPEAN_PATENT_FULLTEXT, typeof(S_EUROPEAN_PATENT_FULLTEXT));
            }
            #endregion
            #region  52 韩国专利全文代码化数据（标准化） 通用 未完成 未建库 
            else if (fileType == "韩国专利全文代码化数据（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_KOREAN_PATENT_FULLTEXTCODE, typeof(S_KOREAN_PATENT_FULLTEXTCODE));
            }
            #endregion
            #region  53 瑞士专利全文代码化数据（标准化）通用 未完成 未建库
            else if (fileType == "瑞士专利全文代码化数据（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_SWISS_PATENT_FULLTEXTCODE, typeof(S_SWISS_PATENT_FULLTEXTCODE));
            }
            #endregion
            #region 54 英国专利全文代码化数据（标准化）通用 未完成 未建库
            else if (fileType == "英国专利全文代码化数据（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_BRITISH_PATENT_FULLTEXTCODE, typeof(S_BRITISH_PATENT_FULLTEXTCODE));
            }
            #endregion
            #region 55 日本专利全文代码化数据（标准化）通用 未完成 未建库
            else if (fileType == "日本专利全文代码化数据（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_JAPAN_PATENT_FULLTEXTCODE, typeof(S_JAPAN_PATENT_FULLTEXTCODE));
            }
            #endregion
            #region
            else if (fileType == "中国发明申请专利数据（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "中国发明授权专利数据（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "中国实用新型专利数据（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "中国外观设计专利数据（DI）")
            {


            }
            #endregion
            #region 76 中国专利生物序列数据（DI）未完成 无样例
            else if (fileType == "中国专利生物序列数据（DI）")
            {


            }
            #endregion
            #region 
            else if (fileType == "中国专利摘要英文翻译数据（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "专利同族数据（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "全球专利引文数据（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "中国专利费用信息数据（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "中国专利通知书数据（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "中国法律状态标引库（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "专利分类数据(分类号码)（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "世界法律状态数据（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "DOCDB数据（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "美国专利著录项及全文数据（US）（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "韩国专利著录项及全文数据（KR）（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "欧洲专利局专利著录项及全文数据（EP）（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "国际知识产权组织专利著录项及全文数据（WIPO)（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "加拿大专利著录项及全文数据（CA）（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "俄罗斯专利著录项及全文数据（RU）（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "英国专利全文数据（GB）（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "瑞士专利全文数据（CH）（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "日本专利著录项及全文数据（JP）（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "德国专利著录项及全文数据（DE）（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "法国专利著录项及全文数据（FR）（DI）")
            {


            }
            #endregion
            #region 103 比利时专利全文数据（BE）（标准化） 通用字段 未完成
            else if (fileType == "比利时专利全文数据（BE）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_BELGIAN_PATENT_FULLTEXT, typeof(S_BELGIAN_PATENT_FULLTEXT));
            }
            #endregion 未完成
            #region 104 奥地利专利全文数据（AT）（标准化） 通用字段 未完成
            else if (fileType == "奥地利专利全文数据（AT）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_AUSTRIA_PATENT_FULLTEXT, typeof(S_AUSTRIA_PATENT_FULLTEXT));
            }
            #endregion
            #region 105 西班牙专利全文数据（ES）（标准化） 通用字段 未完成
            else if (fileType == "西班牙专利全文数据（ES）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_SPANISH_PATENT_FULLTEXT, typeof(S_SPANISH_PATENT_FULLTEXT));
            }
            #endregion
            #region 106 波兰专利著录项及全文数据（PL）（标准化） 通用字段 未完成
            else if (fileType == "波兰专利著录项及全文数据（PL）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_POLAND_PATENT_DESCRIPTION, typeof(S_POLAND_PATENT_DESCRIPTION));
            }
            #endregion
            #region 107 以色列专利著录项及全文数据（IL）（标准化） 通用字段 未完成
            else if (fileType == "以色列专利著录项及全文数据（IL）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_ISRAEL_PATENT_DESCRIPTION, typeof(S_ISRAEL_PATENT_DESCRIPTION));
            }
            #endregion
            #region 108 新加坡专利著录项及全文数据（SG）（标准化） 通用字段 未完成
            else if (fileType == "新加坡专利著录项及全文数据（SG）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_SINGAPORE_PATENT_DESCRIPTION, typeof(S_SINGAPORE_PATENT_DESCRIPTION));
            }
            #endregion
            #region 
            else if (fileType == "台湾专利著录项及全文数据（TW）（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "香港专利著录项数据（HK）（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "澳门专利著录项数据（MO）（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "欧亚组织专利著录项及全文数据（EA）（DI）")
            {


            }
            #endregion
            #region 113 美国外观设计专利数据（DI）通用 未完成
            else if (fileType == "美国外观设计专利数据（DI）")
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
                                         where entry.IsDirectory && CompressUtil.getEntryDepth(entry.Key) == 2
                                         select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();


                //排除压缩包中无关XML
                var xmlEntryParentDirEntries = (from entry in archive.Entries.AsParallel()
                                                where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
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
                                    where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
                                    select entry;

                totalCount = allXMLEntires.Count();

                MessageUtil.DoAppendTBDetail("在压缩包中发现" + totalCount + "个待导入XML条目");

                //已处理计数清零
                handledCount = 0;
                if (0 == allXMLEntires.Count())
                {
                    MessageUtil.DoAppendTBDetail("没有找到XML");
                    importSession.NOTE = "没有找到XML";
                    //添加错误信息
                    entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", filePath, "", ""));
                    entiesContext.SaveChanges();
                }
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


                    entityObject.STA_APP_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager); ;
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

                    entityObject.IMPORT_TIME = System.DateTime.Now;

                    entiesContext.SaveChanges();


                    //输出插入记录
                    var currentValue = MiscUtil.jsonSerilizeObject(entityObject);

                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");

                    #endregion

                    //更新进度信息
                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                }
                #endregion 循环入库

            }
            #endregion
            #region 日本外观设计专利数据（DI）通用 未完成
            else if (fileType == "日本外观设计专利数据（DI）")
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
                                         where entry.IsDirectory && CompressUtil.getEntryDepth(entry.Key) == 2
                                         select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();


                //排除压缩包中无关XML
                var xmlEntryParentDirEntries = (from entry in archive.Entries.AsParallel()
                                                where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
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
                                    where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
                                    select entry;

                totalCount = allXMLEntires.Count();

                MessageUtil.DoAppendTBDetail("在压缩包中发现" + totalCount + "个待导入XML条目");

                //已处理计数清零
                handledCount = 0;
                if (0 == allXMLEntires.Count())
                {
                    MessageUtil.DoAppendTBDetail("没有找到XML");
                    importSession.NOTE = "没有找到XML";
                    //添加错误信息
                    entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", filePath, "", ""));
                    entiesContext.SaveChanges();
                }
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


                    entityObject.STA_APP_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager); ;
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

                    entityObject.IMPORT_TIME = System.DateTime.Now;

                    entiesContext.SaveChanges();


                    //输出插入记录
                    var currentValue = MiscUtil.jsonSerilizeObject(entityObject);

                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");

                    #endregion

                    //更新进度信息
                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                }
                #endregion 循环入库

            }
            #endregion
            #region 115 韩国外观设计专利数据（DI）通用 未完成
            else if (fileType == "韩国外观设计专利数据（DI）")
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
                                         where entry.IsDirectory && CompressUtil.getEntryDepth(entry.Key) == 2
                                         select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();


                //排除压缩包中无关XML
                var xmlEntryParentDirEntries = (from entry in archive.Entries.AsParallel()
                                                where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
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
                                    where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
                                    select entry;

                totalCount = allXMLEntires.Count();

                MessageUtil.DoAppendTBDetail("在压缩包中发现" + totalCount + "个待导入XML条目");

                //已处理计数清零
                handledCount = 0;
                if (0 == allXMLEntires.Count())
                {
                    MessageUtil.DoAppendTBDetail("没有找到XML");
                    importSession.NOTE = "没有找到XML";
                    //添加错误信息
                    entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", filePath, "", ""));
                    entiesContext.SaveChanges();
                }
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


                    entityObject.STA_APP_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager); ;
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

                    entityObject.IMPORT_TIME = System.DateTime.Now;

                    entiesContext.SaveChanges();


                    //输出插入记录
                    var currentValue = MiscUtil.jsonSerilizeObject(entityObject);

                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");

                    #endregion

                    //更新进度信息
                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                }
                #endregion 循环入库

            }
            #endregion
            #region
            else if (fileType == "德国外观设计专利数据（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "法国外观设计专利数据（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "俄罗斯外观设计专利数据（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "中国专利全文数据PDF（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "国外专利全文数据PDF（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "日本专利文摘英文翻译数据（PAJ)（DI）")
            {


            }
            #endregion
            #region
            else if (fileType == "韩国专利文摘英文翻译数据(KPA)（DI）")
            {


            }
            #endregion
            #region 127 俄罗斯专利文摘英文翻译数据（DI） 通用字段 未完成
            else if (fileType == "俄罗斯专利文摘英文翻译数据（DI）")
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
                                         where entry.IsDirectory && CompressUtil.getEntryDepth(entry.Key) == 2
                                         select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();


                //排除压缩包中无关XML
                var xmlEntryParentDirEntries = (from entry in archive.Entries.AsParallel()
                                                where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
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
                                    where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
                                    select entry;

                totalCount = allXMLEntires.Count();

                MessageUtil.DoAppendTBDetail("在压缩包中发现" + totalCount + "个待导入XML条目");

                //已处理计数清零
                handledCount = 0;
                if (0 == allXMLEntires.Count())
                {
                    MessageUtil.DoAppendTBDetail("没有找到XML");
                    importSession.NOTE = "没有找到XML";
                    //添加错误信息
                    entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", filePath, "", ""));
                    entiesContext.SaveChanges();
                }
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


                    entityObject.STA_APP_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager); ;
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

                    entityObject.IMPORT_TIME = System.DateTime.Now;

                    entiesContext.SaveChanges();


                    //输出插入记录
                    var currentValue = MiscUtil.jsonSerilizeObject(entityObject);

                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");

                    #endregion

                    //更新进度信息
                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                }
                #endregion 循环入库


            }
            #endregion
            #region 132 中国商标 XML
            else if (fileType == "中国商标")
            {


            }
            #endregion
            #region 133 中国商标许可数据 XML
            else if (fileType == "中国商标许可数据")
            {


            }
            #endregion
            #region 134 中国商标转让数据 XML
            else if (fileType == "中国商标转让数据")
            {


            }
            #endregion
            #region 136 马德里商标进入中国 XML
            else if (fileType == "马德里商标进入中国")
            {


            }
            #endregion
            #region 137  中国驰名商标数据 XML
            else if (fileType == "中国驰名商标数据")
            {


            }
            #endregion
            #region 138 美国申请商标 XML
            else if (fileType == "美国申请商标")
            {


            }
            #endregion
            #region 139 美国转让商标 XML
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
            #endregion
            #region 153 中外期刊的著录项目与文摘数据 XML
            else if (fileType == "中外期刊的著录项目与文摘数据")
            {


            }
            #endregion
            #region 162 中国法院判例初加工数据 XML
            else if (fileType == "中国法院判例初加工数据")
            {


            }
            #endregion
            #region 168 中国商标分类数据 EXCEL
            else if (fileType == "中国商标分类数据")
            {
                Dictionary<string, int> headers = new Dictionary<string, int>();
                headers.Add("CLNO", 1);
                headers.Add("F_CLNO", 2);
                headers.Add("GOODS_SERVICE_CN", 3);
                headers.Add("GOODS_SERVICE_EN", 4);
                headers.Add("ZHUSHI_CN", 5);
                headers.Add("ZHUSHI_EN", 6);

                var result = ExcelUtil.parseExcelWithEEPlus(filePath, 4, 1, headers);

                MessageUtil.DoAppendTBDetail($"发现{result.Count}条记录");

                handledCount = 0;
                importStartTime = importSession.START_TIME.Value;
                totalCount = result.Count();
                importSession.TOTAL_ITEM = totalCount;
                importSession.TABLENAME = "S_CHINA_BRAND_CLASSIFICATION".ToUpper();
                importSession.IS_ZIP = "N";
                entiesContext.SaveChanges();

                var parsedEntites = from rec in result
                                    select new S_CHINA_BRAND_CLASSIFICATION()
                                    {
                                        ID = System.Guid.NewGuid().ToString(),
                                        CLNO = MiscUtil.getDictValueOrDefaultByKey(rec, "CLNO"),
                                        F_CLNO = MiscUtil.getDictValueOrDefaultByKey(rec, "F_CLNO"),
                                        GOODS_SERVICE_CN = MiscUtil.getDictValueOrDefaultByKey(rec, "GOODS_SERVICE_CN"),
                                        GOODS_SERVICE_EN = MiscUtil.getDictValueOrDefaultByKey(rec, "GOODS_SERVICE_EN"),
                                        ZHUSHI_CN = MiscUtil.getDictValueOrDefaultByKey(rec, "ZHUSHI_CN"),
                                        ZHUSHI_EN = MiscUtil.getDictValueOrDefaultByKey(rec, "ZHUSHI_EN"),
                                        FILE_PATH = filePath,
                                        IMPORT_SESSION_ID = importSession.SESSION_ID,
                                        IMPORT_TIME = System.DateTime.Now
                                    };

                foreach (var entityObject in parsedEntites)
                {
                    handledCount++;
                    entiesContext.S_CHINA_BRAND_CLASSIFICATION.Add(entityObject);

                    if (handledCount % 100 == 0)
                    {
                        MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                        //每500条, 提交下
                        if (handledCount % 500 == 0)
                        {
                            entiesContext.SaveChanges();
                        }
                    }
                }
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                entiesContext.SaveChanges();

            }
            #endregion
            #region 169 美国商标图形分类数据 EXCEL
            else if (fileType == "美国商标图形分类数据")
            {
                Dictionary<string, int> headers = new Dictionary<string, int>();
                headers.Add("DESIGN_CODE", 1);
                headers.Add("DESIGN_F_CODE", 2);
                headers.Add("DESIGN_CODE_NOTE", 3);

                var result = ExcelUtil.parseExcelWithEEPlus(filePath, 4, 1, headers);

                MessageUtil.DoAppendTBDetail($"发现{result.Count}条记录");

                handledCount = 0;
                importStartTime = importSession.START_TIME.Value;
                totalCount = result.Count();
                importSession.TOTAL_ITEM = totalCount;
                importSession.TABLENAME = "S_AMERICAN_BRAND_GRAPHCLASSIFY".ToUpper();
                importSession.IS_ZIP = "N";
                entiesContext.SaveChanges();

                var parsedEntites = from rec in result
                                    select new S_AMERICAN_BRAND_GRAPHCLASSIFY()
                                    {
                                        ID = System.Guid.NewGuid().ToString(),
                                        DESIGN_CODE = MiscUtil.getDictValueOrDefaultByKey(rec, "DESIGN_CODE"),
                                        DESIGN_F_CODE = MiscUtil.getDictValueOrDefaultByKey(rec, "DESIGN_F_CODE"),
                                        DESIGN_CODE_NOTE = MiscUtil.getDictValueOrDefaultByKey(rec, "DESIGN_CODE_NOTE"),
                                        FILE_PATH = filePath,
                                        IMPORT_SESSION_ID = importSession.SESSION_ID,
                                        IMPORT_TIME = System.DateTime.Now
                                    };

                foreach (var entityObject in parsedEntites)
                {
                    handledCount++;
                    entiesContext.S_AMERICAN_BRAND_GRAPHCLASSIFY.Add(entityObject);

                    if (handledCount % 100 == 0)
                    {
                        MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                        //每500条, 提交下
                        if (handledCount % 500 == 0)
                        {
                            entiesContext.SaveChanges();
                        }
                    }
                }
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                entiesContext.SaveChanges();





            }
            #endregion
            #region 170 美国商标美国分类数据 EXCEL
            else if (fileType == "美国商标美国分类数据")
            {
                Dictionary<string, int> headers = new Dictionary<string, int>();
                headers.Add("ID", 1);
                headers.Add("CLNO", 2);
                headers.Add("ZHUSHI", 3);

                var result = ExcelUtil.parseExcelWithEEPlus(filePath, 3, 1, headers);

                MessageUtil.DoAppendTBDetail($"发现{result.Count}条记录");

                handledCount = 0;
                importStartTime = importSession.START_TIME.Value;
                totalCount = result.Count();
                importSession.TOTAL_ITEM = totalCount;
                importSession.TABLENAME = "S_AMERICAN_BRAND_USCLASSIFY".ToUpper();
                importSession.IS_ZIP = "N";
                entiesContext.SaveChanges();

                var parsedEntites = from rec in result
                                    select new S_AMERICAN_BRAND_USCLASSIFY()
                                    {
                                        ID = MiscUtil.getDictValueOrDefaultByKey(rec, "ID"),
                                        CLNO = MiscUtil.getDictValueOrDefaultByKey(rec, "CLNO"),
                                        ZHUSHI = MiscUtil.getDictValueOrDefaultByKey(rec, "ZHUSHI"),
                                        FILE_PATH = filePath,
                                        IMPORT_SESSION_ID = importSession.SESSION_ID,
                                        IMPORT_TIME = System.DateTime.Now
                                    };

                foreach (var entityObject in parsedEntites)
                {
                    handledCount++;
                    entiesContext.S_AMERICAN_BRAND_USCLASSIFY.Add(entityObject);

                    if (handledCount % 100 == 0)
                    {
                        MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                        //每500条, 提交下
                        if (handledCount % 500 == 0)
                        {
                            entiesContext.SaveChanges();
                        }
                    }
                }
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                entiesContext.SaveChanges();

            }
            #endregion
            #region 172 马德里商标购买数据 XML
            else if (fileType == "马德里商标购买数据")
            {


            }
            #endregion
            #region 180 中国专利代理知识产权法律法规加工数据 XML
            else if (fileType == "中国专利代理知识产权法律法规加工数据")
            {


            }
            #endregion
            #region 183 中国集成电路布图公告及事务数据 XML
            else if (fileType == "中国集成电路布图公告及事务数据")
            {


            }
            #endregion
            #region 184 中国知识产权海关备案数据 XML

            else if (fileType == "中国知识产权海关备案数据")
            {


            }
            #endregion
            #region 184 国外专利生物序列加工成品数据 XML
            else if (fileType == "国外专利生物序列加工成品数据")
            {


            }



            else if (fileType == "中国专利复审数据")
            {


            }
            else if (fileType == "中国专利无效数据")
            {


            }



            #endregion
            #region 196 中国专利的判决书数据 XML

            else if (fileType == "中国专利的判决书数据")
            {


            }

            #endregion
            #region 209 中国生物序列深加工数据

            else if (fileType == "中国生物序列深加工数据")
            {


            }

            #endregion
            #region 210 中国中药专利翻译数据 mdb

            else if (fileType == "中国中药专利翻译数据")
            {
                parseMDB210(filePath, entiesContext, importSession);
            }

            #endregion
            #region 211 中国化学药物专利深加工数据 mdb

            else if (fileType == "中国化学药物专利深加工数据")
            {
                parseMDB211(filePath, entiesContext, importSession);

            }

            #endregion
            #region 212 中国中药专利深加工数据 mdb

            else if (fileType == "中国中药专利深加工数据")
            {
                parseMDB212(filePath, entiesContext, importSession);

            }
            #endregion
            #region 213 中国专利摘要英文翻译数据（标准化） XML 通用字段 未完成
            else if (fileType == "中国专利摘要英文翻译数据（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_CHINA_PATENT_ABSTRACTS, typeof(S_CHINA_PATENT_ABSTRACTS));
            }
            #endregion
            #region 214 DOCDB数据（标准化） XML
            else if (fileType == "DOCDB数据（标准化）")
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
                                         where entry.IsDirectory && CompressUtil.getEntryDepth(entry.Key) == 2
                                         select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();


                //排除压缩包中无关XML
                var xmlEntryParentDirEntries = (from entry in archive.Entries.AsParallel()
                                                where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
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
                                    where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
                                    select entry;

                totalCount = allXMLEntires.Count();

                MessageUtil.DoAppendTBDetail("在压缩包中发现" + totalCount + "个待导入XML条目");

                //已处理计数清零
                handledCount = 0;
                if (0 == allXMLEntires.Count())
                {
                    MessageUtil.DoAppendTBDetail("没有找到XML");
                    importSession.NOTE = "没有找到XML";
                    //添加错误信息
                    entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", filePath, "", ""));
                    entiesContext.SaveChanges();
                }
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


                    entityObject.STA_APP_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager); ;
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

                    entityObject.IMPORT_TIME = System.DateTime.Now;

                    entiesContext.SaveChanges();


                    //输出插入记录
                    var currentValue = MiscUtil.jsonSerilizeObject(entityObject);

                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");

                    #endregion

                    //更新进度信息
                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                }
                #endregion 循环入库


            }
            #endregion
            #region 215 国际知识产权组织专利著录项及全文数据（WIPO)（标准化） XML  通用字段 未完成
            else if (fileType == "国际知识产权组织专利著录项及全文数据（WIPO)（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_WIPO_PATENT_DESCRIPTION, typeof(S_WIPO_PATENT_DESCRIPTION));
            }
            #endregion
            #region 216 加拿大专利著录项及全文数据（CA）（标准化） XML  通用字段 未完成
            else if (fileType == "加拿大专利著录项及全文数据（CA）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_CANADIAN_PATENT_DESCRIPTION, typeof(S_CANADIAN_PATENT_DESCRIPTION));
            }
            #endregion
            #region 217 俄罗斯专利著录项及全文数据（RU）（标准化） XML  通用字段 未完成
            else if (fileType == "俄罗斯专利著录项及全文数据（RU）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_RUSSIAN_PATENT_DESCRIPTION, typeof(S_RUSSIAN_PATENT_DESCRIPTION));
            }
            #endregion
            #region 218 澳大利亚专利全文文本数据（AU）（标准化） XML  通用字段 未完成
            else if (fileType == "澳大利亚专利全文文本数据（AU）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_AUSTRALIAN_PATENT_FULLTEXT, typeof(S_AUSTRALIAN_PATENT_FULLTEXT));
            }
            #endregion
            #region 219 德国专利著录项及全文数据（DE）（标准化） XML  通用字段 未完成
            else if (fileType == "德国专利著录项及全文数据（DE）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_GERMAN_PATENT_DESCRIPTION, typeof(S_GERMAN_PATENT_DESCRIPTION));
            }
            #endregion
            #region 220 法国专利著录项及全文数据（FR）（标准化） XML  通用字段 未完成
            else if (fileType == "法国专利著录项及全文数据（FR）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_FRENCH_PATENT_DESCRIPTION, typeof(S_FRENCH_PATENT_DESCRIPTION));
            }
            #endregion
            #region 221 台湾专利著录项及全文数据（TW）（标准化） XML  通用字段 未完成
            else if (fileType == "台湾专利著录项及全文数据（TW）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_TAIWAN_PATENT_DESCRIPTION, typeof(S_TAIWAN_PATENT_DESCRIPTION));
            }
            #endregion
            #region 222 香港专利著录项数据（HK）（标准化） XML  通用字段 未完成
            else if (fileType == "香港专利著录项数据（HK）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_HONGKONG_PATENT_DESCRIPTION, typeof(S_HONGKONG_PATENT_DESCRIPTION));
            }
            #endregion
            #region 223 澳门专利著录项数据（MO）（标准化） XML  通用字段 未完成
            else if (fileType == "澳门专利著录项数据（MO）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_MACAO_PATENT_DESCRIPTION, typeof(S_MACAO_PATENT_DESCRIPTION));
            }
            #endregion
            #region 224 欧亚组织专利著录项及全文数据（EA）（标准化） XML  通用字段 未完成
            else if (fileType == "欧亚组织专利著录项及全文数据（EA）（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_EURASIAN_PATENT_DESCRIPTION, typeof(S_EURASIAN_PATENT_DESCRIPTION));
            }
            #endregion
            #region 225 日本外观设计专利数据（标准化） XML  通用字段 未完成
            else if (fileType == "日本外观设计专利数据（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_JAPAN_DESIGN_PATENT, typeof(S_JAPAN_DESIGN_PATENT));
            }
            #endregion
            #region 226 德国外观设计专利数据（标准化） XML  通用字段 未完成
            else if (fileType == "德国外观设计专利数据（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_GERMAN_DESIGN_PATENT, typeof(S_GERMAN_DESIGN_PATENT));
            }
            #endregion
            #region 227 法国外观设计专利数据（标准化） XML  通用字段 未完成
            else if (fileType == "法国外观设计专利数据（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_FRENCH_DESIGN_PATENT, typeof(S_FRENCH_DESIGN_PATENT));
            }
            #endregion
            #region 228 俄罗斯外观设计专利数据（标准化） XML  通用字段 未完成
            else if (fileType == "俄罗斯外观设计专利数据（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_RUSSIAN_DESIGN_PATENT, typeof(S_RUSSIAN_DESIGN_PATENT));
            }
            #endregion
            #region 229 日本专利文摘英文翻译数据（PAJ)（标准化） XML  通用字段 未完成
            else if (fileType == "日本专利文摘英文翻译数据（PAJ)（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_JAPAN_PATENT_ABSTRACTS, typeof(S_JAPAN_PATENT_ABSTRACTS));
            }
            #endregion
            #region 230 韩国专利文摘英文翻译数据(KPA)（标准化） XML  通用字段 未完成
            else if (fileType == "韩国专利文摘英文翻译数据(KPA)（标准化）")
            {
                parseZipUniversalSTA(filePath, entiesContext, importSession, entiesContext.S_KOREA_PATENT_ABSTRACTS, typeof(S_KOREA_PATENT_ABSTRACTS));
            }
            #endregion

            #endregion


            #region 导入后处理 写入导入session信息
            importSession.LAST_TIME = new Decimal(importSession.START_TIME != null ? DateTime.Now.Subtract(importSession.START_TIME.Value).TotalSeconds : 0);
            //是否发生错误
            importSession.HAS_ERROR = importSession.FAILED_COUNT > 0 ? "Y" : "N";
            importSession.ZIP_ENTRY_POINTOR = handledCount;
            importSession.COMPLETED = totalCount == handledCount ? "Y" : "N";
            importSession.ITEMS_POINT = handledCount;
            importSession.TOTAL_ITEM = totalCount;
            bath.HANDLED_ITEM_COUNT = bath.HANDLED_ITEM_COUNT + totalCount;
            entiesContext.SaveChanges();
            #endregion
            return true;
        }

        #region 入库逻辑
        private static void parseMDB212(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
        {
            string sql = @"SELECT 
                            t1.TI as T1_TI, t1.AP as T1_AP, t1.AD as T1_AD, t1.PN as T1_PN, t1.PD as T1_PD, t1.PA as T1_PA, t1.PAC as T1_PAC, t1.ADDR as T1_ADDR, t1.INR as T1_INR, t1.IC0 as T1_IC0, t1.IC1 as T1_IC1, t1.IC2 as T1_IC2, t1.AB as T1_AB, t1.PHC as T1_PHC, t1.ANA as T1_ANA, t1.BIO as T1_BIO, t1.EXT as T1_EXT, t1.PHY as T1_PHY, t1.GAL as T1_GAL, t1.MIX as T1_MIX, t1.CHE as T1_CHE, t1.NUS as T1_NUS, t1.ANEF as T1_ANEF, t1.THEF as T1_THEF, t1.DINT as T1_DINT, t1.TOXI as T1_TOXI, t1.DIAG as T1_DIAG, t2.AP as T2_AP, t2.FORMULA as T2_FORMULA, t3.AP as T3_AP, t3.CMNO as T3_CMNO, t3.NOM1 as T3_NOM1, t3.NOM2 as T3_NOM2, t3.NOM3 as T3_NOM3, t3.CN as T3_CN, t3.RN as T3_RN, t3.ROLES as T3_ROLES, t3.FS as T3_FS, t3.NOTE as T3_NOTE
                            FROM  
                            (
                            [INDEX] as t1 left join FORMULA_INDEX as t2 
                            on t1.AP = t2.AP
                            )
                            left join CHEMICAL_INDEX as t3
                            on t2.AP = t3.AP
                            ";



            AccessUtil accUtil = new AccessUtil(filePath);
            DataTable allRecsDt = accUtil.SelectToDataTable(sql);
            totalCount = allRecsDt.Rows.Count;
            MessageUtil.DoAppendTBDetail($"发现{allRecsDt.Rows.Count}条记录");

            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;
            importSession.TOTAL_ITEM = totalCount;
            importSession.TABLENAME = "S_CHINA_MEDICINE_PATENT_HANDLE".ToUpper();
            importSession.IS_ZIP = "N";
            entiesContext.SaveChanges();

            foreach (DataRow dr in allRecsDt.Rows)
            {
                handledCount++;

                var entityObject = new S_CHINA_MEDICINE_PATENT_HANDLE()
                {
                    ID = System.Guid.NewGuid().ToString(),
                    FILE_PATH = filePath,
                    IMPORT_SESSION_ID = importSession.SESSION_ID,
                    IMPORT_TIME = System.DateTime.Now,

                    T1_TI = dr["T1_TI"] as string,
                    T1_AP = dr["T1_AP"] as string,
                    T1_AD = dr["T1_AD"] as DateTime?,
                    T1_PN = dr["T1_PN"] as string,
                    T1_PD = dr["T1_PD"] as DateTime?,
                    T1_PA = dr["T1_PA"] as string,
                    T1_PAC = dr["T1_PAC"] as string,
                    T1_ADDR = dr["T1_ADDR"] as string,
                    T1_INR = dr["T1_INR"] as string,
                    T1_IC0 = dr["T1_IC0"] as string,
                    T1_IC1 = dr["T1_IC1"] as string,
                    T1_IC2 = dr["T1_IC2"] as string,
                    T1_AB = dr["T1_AB"] as string,
                    T1_PHC = dr["T1_PHC"] as string,
                    T1_ANA = dr["T1_ANA"] as string,
                    T1_BIO = dr["T1_BIO"] as string,
                    T1_EXT = dr["T1_EXT"] as string,
                    T1_PHY = dr["T1_PHY"] as string,
                    T1_GAL = dr["T1_GAL"] as string,
                    T1_MIX = dr["T1_MIX"] as string,
                    T1_CHE = dr["T1_CHE"] as string,
                    T1_NUS = dr["T1_NUS"] as string,
                    T1_ANEF = dr["T1_ANEF"] as string,
                    T1_THEF = dr["T1_THEF"] as string,
                    T1_DINT = dr["T1_DINT"] as string,
                    T1_TOXI = dr["T1_TOXI"] as string,
                    T1_DIAG = dr["T1_DIAG"] as string,
                    T2_AP = dr["T2_AP"] as string,
                    T2_FORMULA = dr["T2_FORMULA"] as string,
                    T3_AP = dr["T3_AP"] as string,
                    T3_CMNO = dr["T3_CMNO"] as string,
                    T3_NOM1 = dr["T3_NOM1"] as string,
                    T3_NOM2 = dr["T3_NOM2"] as string,
                    T3_NOM3 = dr["T3_NOM3"] as string,
                    T3_CN = dr["T3_CN"] as string,
                    T3_RN = dr["T3_RN"] as string,
                    T3_ROLES = dr["T3_ROLES"] as string,
                    T3_FS = dr["T3_FS"] as string,
                    T3_NOTE = dr["T3_NOTE"] as string,
                };

                entiesContext.S_CHINA_MEDICINE_PATENT_HANDLE.Add(entityObject);

                if (0 == handledCount % 10)
                {
                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                    if (0 == handledCount % 50) //每插入500条记录写库, 更新进度
                    {
                        entiesContext.SaveChanges();
                    }
                }

            }

            entiesContext.SaveChanges();
            MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);

            accUtil.Close();//关闭数据库                
        }

        private static void parseMDB211(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
        {
            string sql = @"SELECT t1.TI as T1_TI, t1.AP as T1_AP, t1.AD as T1_AD, t1.PN as T1_PN, t1.PD as T1_PD, t1.PA as T1_PA, t1.PAC as T1_PAC, t1.ADDR as T1_ADDR, t1.INR as T1_INR, t1.IC0 as T1_IC0, t1.IC1 as T1_IC1, t1.IC2 as T1_IC2, t1.AB as T1_AB, t1.PHC as T1_PHC, t1.ANA as T1_ANA, t1.BIO as T1_BIO, t1.EXT as T1_EXT, t1.PHY as T1_PHY, t1.GAL as T1_GAL, t1.MIX as T1_MIX, t1.CHE as T1_CHE, t1.NUS as T1_NUS, t1.ANEF as T1_ANEF, t1.THEF as T1_THEF, t1.DINT as T1_DINT, t1.TOXI as T1_TOXI, t1.DIAG as T1_DIAG, t2.AP as T2_AP, t2.FORMULA as T2_FORMULA, t3.AP as T3_AP, t3.CMNO as T3_CMNO, t3.NOM1 as T3_NOM1, t3.NOM2 as T3_NOM2, t3.NOM3 as T3_NOM3, t3.CN as T3_CN, t3.RN as T3_RN, t3.ROLES as T3_ROLES, t3.FS as T3_FS, t3.NOTE as T3_NOTE
                            FROM  
                            (
                            [INDEX] as t1 left join FORMULA_INDEX as t2 
                            on t1.AP = t2.AP
                            )
                            left join CHEMICAL_INDEX as t3
                            on t1.AP = t3.AP;
                            ";



            AccessUtil accUtil = new AccessUtil(filePath);
            DataTable allRecsDt = accUtil.SelectToDataTable(sql);
            totalCount = allRecsDt.Rows.Count;
            MessageUtil.DoAppendTBDetail($"发现{allRecsDt.Rows.Count}条记录");

            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;
            importSession.TOTAL_ITEM = totalCount;
            importSession.TABLENAME = "S_CHINA_PHARMACEUTICAL_PATENT".ToUpper();
            importSession.IS_ZIP = "N";
            entiesContext.SaveChanges();

            foreach (DataRow dr in allRecsDt.Rows)
            {
                handledCount++;

                var entityObject = new S_CHINA_PHARMACEUTICAL_PATENT()
                {
                    ID = System.Guid.NewGuid().ToString(),
                    FILE_PATH = filePath,
                    IMPORT_SESSION_ID = importSession.SESSION_ID,
                    IMPORT_TIME = System.DateTime.Now,

                    T1_TI = dr["T1_TI"] as string,
                    T1_AP = dr["T1_AP"] as string,
                    T1_AD = dr["T1_AD"] as DateTime?,
                    T1_PN = dr["T1_PN"] as string,
                    T1_PD = dr["T1_PD"] as DateTime?,
                    T1_PA = dr["T1_PA"] as string,
                    T1_PAC = dr["T1_PAC"] as string,
                    T1_ADDR = dr["T1_ADDR"] as string,
                    T1_INR = dr["T1_INR"] as string,
                    T1_IC0 = dr["T1_IC0"] as string,
                    T1_IC1 = dr["T1_IC1"] as string,
                    T1_IC2 = dr["T1_IC2"] as string,
                    T1_AB = dr["T1_AB"] as string,
                    T1_PHC = dr["T1_PHC"] as string,
                    T1_ANA = dr["T1_ANA"] as string,
                    T1_BIO = dr["T1_BIO"] as string,
                    T1_EXT = dr["T1_EXT"] as string,
                    T1_PHY = dr["T1_PHY"] as string,
                    T1_GAL = dr["T1_GAL"] as string,
                    T1_MIX = dr["T1_MIX"] as string,
                    T1_CHE = dr["T1_CHE"] as string,
                    T1_NUS = dr["T1_NUS"] as string,
                    T1_ANEF = dr["T1_ANEF"] as string,
                    T1_THEF = dr["T1_THEF"] as string,
                    T1_DINT = dr["T1_DINT"] as string,
                    T1_TOXI = dr["T1_TOXI"] as string,
                    T1_DIAG = dr["T1_DIAG"] as string,
                    T2_AP = dr["T2_AP"] as string,
                    T2_FORMULA = dr["T2_FORMULA"] as string,
                    T3_AP = dr["T3_AP"] as string,
                    T3_CMNO = dr["T3_CMNO"] as string,
                    T3_NOM1 = dr["T3_NOM1"] as string,
                    T3_NOM2 = dr["T3_NOM2"] as string,
                    T3_NOM3 = dr["T3_NOM3"] as string,
                    T3_CN = dr["T3_CN"] as string,
                    T3_RN = dr["T3_RN"] as string,
                    T3_ROLES = dr["T3_ROLES"] as string,
                    T3_FS = dr["T3_FS"] as string,
                    T3_NOTE = dr["T3_NOTE"] as string,
                };

                entiesContext.S_CHINA_PHARMACEUTICAL_PATENT.Add(entityObject);

                if (0 == handledCount % 10)
                {
                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                    if (0 == handledCount % 50) //每插入500条记录写库, 更新进度
                    {
                        entiesContext.SaveChanges();
                    }
                }

            }

            entiesContext.SaveChanges();
            MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);

            accUtil.Close();//关闭数据库                
        }

        private static void parseMDB210(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
        {
            //SELECT 
            //t1.ETI as T1_ETI, t1.AP as T1_AP,t2.AP as T2_AP,t2.EFORMULA as T2_EFORMULA, t1.AD as T1_AD, t1.PN as T1_PN, t1.PD as T1_PD, t1.EPA as T1_EPA, t1.EPAC as T1_EPAC, t1.EADDR as T1_EADDR, t1.EINR as T1_EINR, t1.IC0 as T1_IC0, t1.IC1 as T1_IC1, t1.IC2 as T1_IC2, t1.EAB as T1_EAB, t1.PHC as T1_PHC, t1.EANA as T1_EANA, t1.EBIO as T1_EBIO, t1.EEXT as T1_EEXT, t1.EPHY as T1_EPHY, t1.EGAL as T1_EGAL, t1.EMIX as T1_EMIX, t1.ECHE as T1_ECHE, t1.ENUS as T1_ENUS, t1.EANEF as T1_EANEF, t1.ETHEF as T1_ETHEF, t1.EDINT as T1_EDINT, t1.ETOXI as T1_ETOXI, t1.EDIAG as T1_EDIAG
            //FROM[INDEX] as t1 left join FORMULA_INDEX as t2
            //on t1.AP = t2.AP
            //;

            string sql = "select t1.ETI as T1_ETI, t1.AP as T1_AP,t2.AP as T2_AP,t2.EFORMULA as T2_EFORMULA, t1.AD as T1_AD, t1.PN as T1_PN, t1.PD as T1_PD, t1.EPA as T1_EPA, t1.EPAC as T1_EPAC, t1.EADDR as T1_EADDR, t1.EINR as T1_EINR, t1.IC0 as T1_IC0, t1.IC1 as T1_IC1, t1.IC2 as T1_IC2, t1.EAB as T1_EAB, t1.PHC as T1_PHC, t1.EANA as T1_EANA, t1.EBIO as T1_EBIO, t1.EEXT as T1_EEXT, t1.EPHY as T1_EPHY, t1.EGAL as T1_EGAL, t1.EMIX as T1_EMIX, t1.ECHE as T1_ECHE, t1.ENUS as T1_ENUS, t1.EANEF as T1_EANEF, t1.ETHEF as T1_ETHEF, t1.EDINT as T1_EDINT, t1.ETOXI as T1_ETOXI, t1.EDIAG as T1_EDIAG FROM[INDEX] as t1 left join FORMULA_INDEX as t2 on t1.AP = t2.AP";
            AccessUtil accUtil = new AccessUtil(filePath);
            DataTable allRecsDt = accUtil.SelectToDataTable(sql);
            totalCount = allRecsDt.Rows.Count;
            MessageUtil.DoAppendTBDetail($"发现{allRecsDt.Rows.Count}条记录");

            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;
            importSession.TOTAL_ITEM = totalCount;
            importSession.TABLENAME = "S_CHINA_MEDICINE_PATENT_TRANS".ToUpper();
            importSession.IS_ZIP = "N";
            entiesContext.SaveChanges();

            foreach (DataRow dr in allRecsDt.Rows)
            {
                handledCount++;

                var entityObject = new S_CHINA_MEDICINE_PATENT_TRANS()
                {
                    ID = System.Guid.NewGuid().ToString(),
                    FILE_PATH = filePath,
                    IMPORT_SESSION_ID = importSession.SESSION_ID,
                    IMPORT_TIME = System.DateTime.Now,
                };

                entiesContext.S_CHINA_MEDICINE_PATENT_TRANS.Add(entityObject);

                if (0 == handledCount % 10)
                {

                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                    if (0 == handledCount % 50) //每插入500条记录写库, 更新进度
                    {
                        entiesContext.SaveChanges();
                    }
                }

            }

            entiesContext.SaveChanges();
            MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);

            accUtil.Close();//关闭数据库                
        }

        private static void parseTRS14(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
        {
            MessageUtil.DoAppendTBDetail($"正在解析TRS文件");

            List<Dictionary<string, string>> result = TRSUtil.paraseTrsRecord(filePath, System.Text.Encoding.Default);

            MessageUtil.DoAppendTBDetail($"发现{result.Count}条记录");

            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;
            totalCount = result.Count();
            importSession.TOTAL_ITEM = totalCount;
            importSession.TABLENAME = "S_PATENT_PAYMENT".ToUpper();
            importSession.IS_ZIP = "N";
            entiesContext.SaveChanges();

            var parsedEntites = from rec in result
                                select new S_PATENT_PAYMENT()
                                {
                                    ID = System.Guid.NewGuid().ToString(),



                                    APPLYNUM = MiscUtil.getDictValueOrDefaultByKey(rec, "ApplyNum"),
                                    EN_FEETYPE = MiscUtil.getDictValueOrDefaultByKey(rec, "EN_FeeType"),
                                    FEE = MiscUtil.getDictValueOrDefaultByKey(rec, "Fee"),
                                    FEETYPE = MiscUtil.getDictValueOrDefaultByKey(rec, "FeeType"),
                                    EN_STATE = MiscUtil.getDictValueOrDefaultByKey(rec, "EN_State"),
                                    HKDATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getDictValueOrDefaultByKey(rec, "HKDate"), "yyyy.MM.dd"),
                                    HKINFO = MiscUtil.getDictValueOrDefaultByKey(rec, "HKInfo"),
                                    PAYMENTUNITTYPE = MiscUtil.getDictValueOrDefaultByKey(rec, "PaymentUnitType"),
                                    RECEIPTION = MiscUtil.getDictValueOrDefaultByKey(rec, "Receiption"),
                                    RECEIPTIONDATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getDictValueOrDefaultByKey(rec, "ReceiptionDate"), "yyyy.MM.dd"),
                                    REGISTERCODE = MiscUtil.getDictValueOrDefaultByKey(rec, "RegisterCode"),
                                    STATE = MiscUtil.getDictValueOrDefaultByKey(rec, "State"),
                                    APPLYNUM_NEW = MiscUtil.getDictValueOrDefaultByKey(rec, "ApplyNum_new"),



                                    FILE_PATH = filePath,
                                    IMPORT_SESSION_ID = importSession.SESSION_ID,
                                    IMPORT_TIME = System.DateTime.Now
                                };

            foreach (var entityObject in parsedEntites)
            {
                handledCount++;
                entiesContext.S_PATENT_PAYMENT.Add(entityObject);

                if (handledCount % 100 == 0)
                {
                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                    //每500条, 提交下
                    if (handledCount % 500 == 0)
                    {
                        entiesContext.SaveChanges();
                    }
                }
            }
            MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            entiesContext.SaveChanges();
        }

        private static void parseMDB11(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
        {
            string sql = "select ap, pd, flztinfoenrlt from [Legal_status]";
            AccessUtil accUtil = new AccessUtil(filePath);
            DataTable allRecsDt = accUtil.SelectToDataTable(sql);
            totalCount = allRecsDt.Rows.Count;
            MessageUtil.DoAppendTBDetail($"发现{allRecsDt.Rows.Count}条记录");

            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;
            importSession.TOTAL_ITEM = totalCount;
            importSession.TABLENAME = "S_CHINA_PATENT_LAWSTATE_CHANGE".ToUpper();
            importSession.IS_ZIP = "N";
            entiesContext.SaveChanges();

            foreach (DataRow dr in allRecsDt.Rows)
            {
                handledCount++;
                var ap = dr["ap"].ToString();
                var pd = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(dr["pd"].ToString(), "yyyy.MM.dd");
                var flztinfoenrlt = dr["flztinfoenrlt"].ToString();
                var entityObject = new S_CHINA_PATENT_LAWSTATE_CHANGE()
                {
                    ID = System.Guid.NewGuid().ToString(),
                    FILE_PATH = filePath,
                    IMPORT_SESSION_ID = importSession.SESSION_ID,
                    IMPORT_TIME = System.DateTime.Now,

                    AP = ap,
                    PD = pd,
                    FLZTINFOENRLT = flztinfoenrlt
                };

                entiesContext.S_CHINA_PATENT_LAWSTATE_CHANGE.Add(entityObject);

                if (0 == handledCount % 100)
                {

                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                    if (0 == handledCount % 500) //每插入500条记录写库, 更新进度
                    {
                        entiesContext.SaveChanges();
                    }

                }

            }

            entiesContext.SaveChanges();
            MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);

            accUtil.Close();//关闭数据库
        }

        private static void parseTRS10(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
        {
            MessageUtil.DoAppendTBDetail($"正在解析TRS文件");

            List<Dictionary<string, string>> result = TRSUtil.paraseTrsRecord(filePath, System.Text.Encoding.Default);

            MessageUtil.DoAppendTBDetail($"发现{result.Count}条记录");

            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;
            totalCount = result.Count();
            importSession.TOTAL_ITEM = totalCount;
            importSession.TABLENAME = "S_CHINA_PATENT_GAZETTE".ToUpper();
            importSession.IS_ZIP = "N";
            entiesContext.SaveChanges();

            var parsedEntites = from rec in result
                                select new S_CHINA_PATENT_LAWSTATE()
                                {
                                    ID = System.Guid.NewGuid().ToString(),
                                    APP_NUMBER = MiscUtil.getDictValueOrDefaultByKey(rec, "申请号"),
                                    PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getDictValueOrDefaultByKey(rec, "法律状态公告日"), "yyyy.MM.dd"),
                                    LAW_STATE = MiscUtil.getDictValueOrDefaultByKey(rec, "法律状态"),
                                    LAW_STATE_INFORMATION = MiscUtil.getDictValueOrDefaultByKey(rec, "法律状态信息"),
                                    FILE_PATH = filePath,
                                    IMPORT_SESSION_ID = importSession.SESSION_ID,
                                    IMPORT_TIME = System.DateTime.Now
                                };

            foreach (var entityObject in parsedEntites)
            {
                handledCount++;
                entiesContext.S_CHINA_PATENT_LAWSTATE.Add(entityObject);

                if (handledCount % 100 == 0)
                {
                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                    //每500条, 提交下
                    if (handledCount % 500 == 0)
                    {
                        entiesContext.SaveChanges();
                    }
                }
            }
            MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            entiesContext.SaveChanges();
        }

        /// <summary>
        /// 标准化 通用字段 数据 入库 忽略DI字段
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="entiesContext"></param>
        /// <param name="importSession"></param>
        /// <param name="dbSet"></param>
        /// <param name="entityObjectType"></param>
        private static void parseZipUniversalSTA(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession, dynamic dbSet, Type entityObjectType)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = entityObjectType.Name;//设置表名
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
                                     where entry.IsDirectory && CompressUtil.getEntryDepth(entry.Key) == 2
                                     select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();


            //排除压缩包中无关XML
            var xmlEntryParentDirEntries = (from entry in archive.Entries.AsParallel()
                                            where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
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
                                where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
                                select entry;

            totalCount = allXMLEntires.Count();

            MessageUtil.DoAppendTBDetail("在压缩包中发现" + totalCount + "个待导入XML条目");

            //已处理计数清零
            handledCount = 0;
            if (0 == allXMLEntires.Count())
            {
                MessageUtil.DoAppendTBDetail("没有找到XML");
                importSession.NOTE = "没有找到XML";
                //添加错误信息
                entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", filePath, "", ""));
                entiesContext.SaveChanges();
            }
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

                //S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                dynamic entityObject = Activator.CreateInstance(entityObjectType);
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                dbSet.Add(entityObject);
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


                entityObject.STA_APP_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager); ;
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

                entityObject.IMPORT_TIME = System.DateTime.Now;

                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);
                try
                {
                    entiesContext.SaveChanges();
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");
                }
                catch (Exception ex)
                {
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入失败!!!");
                    throw ex;
                }





                //输出插入记录


                #endregion

                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }
            #endregion 循环入库
        }

        private static void parseZip162(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
        {
            //entiesContext.S_CHINA_COURTCASE_PROCESS
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = "S_CHINA_COURTCASE_PROCESS".ToUpper();
            entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            //totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = archive.Entries.Count(); ;
            entiesContext.SaveChanges();

            #region 检查目录内无XML的情况
            var dirNameSetEntires = (from entry in archive.Entries.AsParallel()
                                     where entry.IsDirectory && CompressUtil.getEntryDepth(entry.Key) == 2
                                     select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();

            totalCount = dirNameSetEntires.Count();

            //排除压缩包中无关XML
            var xmlEntryParentDirEntries = (from entry in archive.Entries.AsParallel()
                                            where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
                                            select CompressUtil.getFileEntryParentPath(entry.Key)).Distinct();

            var dirEntriesWithoutXML = dirNameSetEntires.Except(xmlEntryParentDirEntries);


            /***
             * 入库逻辑:
             * 1. 入没有XML的数据
             *        判断有无PDF文件
             * 2. 入有XML的数据
             *       解析XML信息 入库
             *       判断有无PDF       
             * **/
    


            //发现存在XML不存在的情况
            if (dirEntriesWithoutXML.Count() > 0)
            {
                //string msg = "如下压缩包中的文件夹内未发现XML文件：";
                //msg += String.Join(Environment.NewLine, dirEntriesWithoutXML.ToArray());
                //MessageUtil.DoAppendTBDetail(msg);
                //LogHelper.WriteErrorLog(msg);


                foreach (string entryKey in dirEntriesWithoutXML)
                {
                    S_CHINA_COURTCASE_PROCESS entityObject = new S_CHINA_COURTCASE_PROCESS();


                





                    entityObject.ID = System.Guid.NewGuid().ToString();

                    var pn = CompressUtil.getEntryShortName(entryKey);

                    var childPdfEntry = CompressUtil.getChildEntryWhithSuffix(archive, entryKey, ".PDF");

                    entityObject.EXIST_XML = "0";
                    //是否存在PDF文件
                    if(null == childPdfEntry)
                    {
                        entityObject.EXIST_PDF = "0";
                    }
                    else
                    {
                        entityObject.EXIST_PDF = "1";
                        entityObject.PATH_PDF = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + entryKey;
                    }

                    
                                        
                    //importSession.HAS_ERROR = "Y";
                    //IMPORT_ERROR importError = new IMPORT_ERROR() { ID = System.Guid.NewGuid().ToString(), SESSION_ID = importSession.SESSION_ID, IGNORED = "N", ISZIP = "Y", POINTOR = handledCount, ZIP_OR_DIR_PATH = filePath, REIMPORTED = "N", ZIP_PATH = entryKey, OCURREDTIME = System.DateTime.Now, ERROR_MESSAGE = "文件夹中不存在XML" };
                    //importSession.FAILED_COUNT++;
                    //entiesContext.IMPORT_ERROR.Add(importError);
                    //entiesContext.SaveChanges();
                }
            }
            #endregion


            MessageUtil.DoAppendTBDetail("开始寻找'中国专利标准化全文文本数据'XML文件：");

            var allXMLEntires = from entry in archive.Entries.AsParallel()
                                where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
                                select entry;

            totalCount = allXMLEntires.Count();

            MessageUtil.DoAppendTBDetail("在压缩包中发现" + totalCount + "个待导入XML条目");

            //已处理计数清零
            handledCount = 0;
            if (0 == allXMLEntires.Count())
            {
                MessageUtil.DoAppendTBDetail("没有找到XML");
                importSession.NOTE = "没有找到XML";
                //添加错误信息
                entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", filePath, "", ""));
                entiesContext.SaveChanges();
            }
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

                S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
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


                entityObject.STA_APP_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager); ;
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

                entityObject.IMPORT_TIME = System.DateTime.Now;

                entiesContext.SaveChanges();


                //输出插入记录
                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);

                MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");

                #endregion

                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }
            #endregion 循环入库
        }

        private static void parseZip06(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = "S_CHINA_PATENT_BIBLIOGRAPHIC".ToUpper();
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
                                     where entry.IsDirectory && CompressUtil.getEntryDepth(entry.Key) == 2
                                     select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();


            //排除压缩包中无关XML
            var xmlEntryParentDirEntries = (from entry in archive.Entries.AsParallel()
                                            where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
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
                                where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
                                select entry;

            totalCount = allXMLEntires.Count();

            MessageUtil.DoAppendTBDetail("在压缩包中发现" + totalCount + "个待导入XML条目");

            //已处理计数清零
            handledCount = 0;
            if (0 == allXMLEntires.Count())
            {
                MessageUtil.DoAppendTBDetail("没有找到XML");
                importSession.NOTE = "没有找到XML";
                //添加错误信息
                entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", filePath, "", ""));
                entiesContext.SaveChanges();
            }
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

                S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
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


                entityObject.STA_APP_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager); ;
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

                entityObject.IMPORT_TIME = System.DateTime.Now;

                entiesContext.SaveChanges();


                //输出插入记录
                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);

                MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");

                #endregion

                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }
            #endregion 循环入库
        }

        private static void parseTRS05(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
        {
            MessageUtil.DoAppendTBDetail($"正在解析TRS文件");

            List<Dictionary<string, string>> result = TRSUtil.paraseTrsRecord(filePath);

            MessageUtil.DoAppendTBDetail($"发现{result.Count}条记录");

            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;
            totalCount = result.Count();
            importSession.TOTAL_ITEM = totalCount;
            importSession.TABLENAME = "S_CHINA_PATENT_GAZETTE".ToUpper();
            importSession.IS_ZIP = "N";
            entiesContext.SaveChanges();

            var parsedEntites = from rec in result
                                select new S_CHINA_PATENT_GAZETTE()
                                {
                                    APPL_TYPE = MiscUtil.getDictValueOrDefaultByKey(rec, "类型"),
                                    APP_NUMBER = MiscUtil.getDictValueOrDefaultByKey(rec, "申请号"),
                                    PATH_TIF = MiscUtil.getDictValueOrDefaultByKey(rec, "图形路径"),
                                    PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getDictValueOrDefaultByKey(rec, "公开（公告）日")),
                                    THE_PAGE = MiscUtil.getDictValueOrDefaultByKey(rec, "专利所在页"),
                                    TURN_PAGE_INFORMATION = MiscUtil.getDictValueOrDefaultByKey(rec, "翻页信息"),
                                    FILE_PATH = filePath,
                                    ID = System.Guid.NewGuid().ToString(),
                                    IMPORT_SESSION_ID = importSession.SESSION_ID,
                                    IMPORT_TIME = System.DateTime.Now,
                                };

            foreach (var entityObject in parsedEntites)
            {
                handledCount++;
                entiesContext.S_CHINA_PATENT_GAZETTE.Add(entityObject);
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);

                //每500条, 提交下
                if (handledCount % 500 == 0)
                {
                    entiesContext.SaveChanges();
                }
            }
            entiesContext.SaveChanges();
        }

        private static void parseZip04(string zipFilePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = "S_CHINA_PATENT_STAND_TEXTIMAGE".ToUpper();
            entiesContext.SaveChanges();

            //获取索引信息
            FileInfo zipFileInfo = new FileInfo(zipFilePath);
            //需找指定的XML
            var indexXMLList = zipFileInfo.Directory.GetFiles("*INDEX*.XML");


            List<Dictionary<string, string>> docList = new List<Dictionary<string, string>>();
            //XMLFile
            foreach (var xmlFile in indexXMLList)
            {
                XDocument indexTemp = XDocument.Load(xmlFile.FullName);
                var docListParition = indexTemp.Root.XPathSelectElements("//DocList").Select(currentNode =>
                {
                    Dictionary<string, string> indexInfo = new Dictionary<string, string>();
                    indexInfo.Add("ApplicationNum", currentNode.Attribute("ApplicationNum").Value);
                    indexInfo.Add("ApplicationDate", currentNode.Attribute("ApplicationDate").Value);
                    indexInfo.Add("PublicationNum", currentNode.Attribute("PublicationNum").Value);
                    indexInfo.Add("PublicationDate", currentNode.Attribute("PublicationDate").Value);
                    return indexInfo;
                }).ToList();
                docList.AddRange(docListParition);
            }


            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(zipFilePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = totalCount;
            entiesContext.SaveChanges();

            #region 检查目录内无XML的情况
            var dirNameSetEntires = (from entry in archive.Entries.AsParallel()
                                     where entry.IsDirectory && CompressUtil.getEntryDepth(entry.Key) == 2
                                     select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();


            //排除压缩包中无关XML
            var xmlEntryParentDirEntries = (from entry in archive.Entries.AsParallel()
                                            where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
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
                    IMPORT_ERROR importError = new IMPORT_ERROR() { ID = System.Guid.NewGuid().ToString(), SESSION_ID = importSession.SESSION_ID, IGNORED = "N", ISZIP = "Y", POINTOR = handledCount, ZIP_OR_DIR_PATH = zipFilePath, REIMPORTED = "N", ZIP_PATH = entryKey, OCURREDTIME = System.DateTime.Now, ERROR_MESSAGE = "文件夹中不存在XML" };
                    importSession.FAILED_COUNT++;
                    entiesContext.IMPORT_ERROR.Add(importError);
                    entiesContext.SaveChanges();
                }
            }
            #endregion


            MessageUtil.DoAppendTBDetail("开始寻找'中国专利标准化全文文本数据'XML文件：");

            var allXMLEntires = from entry in archive.Entries.AsParallel()
                                where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
                                select entry;

            totalCount = allXMLEntires.Count();

            MessageUtil.DoAppendTBDetail("在压缩包中发现" + totalCount + "个待导入XML条目");

            //已处理计数清零
            handledCount = 0;
            if (0 == allXMLEntires.Count())
            {
                MessageUtil.DoAppendTBDetail("没有找到XML");
                importSession.NOTE = "没有找到XML";
                //添加错误信息
                entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", zipFilePath, "", ""));
                entiesContext.SaveChanges();
            }








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

                #region 解压当前的XML文件
                string entryFullPath = CompressUtil.writeEntryToTemp(entry);

                if ("" == entryFullPath.Trim())
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{zipFilePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", zipFilePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    entiesContext.SaveChanges();
                    continue;
                }
                #endregion 


                //初始化Entity, 添加控制信息
                S_CHINA_PATENT_STAND_TEXTIMAGE entityObject = new S_CHINA_PATENT_STAND_TEXTIMAGE() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = zipFilePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                entiesContext.S_CHINA_PATENT_STAND_TEXTIMAGE.Add(entityObject);
                //entiesContext.SaveChanges();





                XDocument doc = XDocument.Load(entryFullPath);

                #region 具体的入库操作,EF
                //获取所有字段名， 获取字段的配置信息， 对字段值进行复制， 

                //定义命名空间
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.CreateReader().NameTable);
                //namespaceManager.AddNamespace("base", "http://www.sipo.gov.cn/XMLSchema/base");
                namespaceManager.AddNamespace("business", "http://www.sipo.gov.cn/XMLSchema/business");
                //namespaceManager.AddNamespace("m", "http://www.w3.org/1998/Math/MathML");
                //namespaceManager.AddNamespace("tbl", "http://oasis-open.org/specs/soextblx");

                var rootElement = doc.Root;

                //entityObject.STA_PUB_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/business:PublicationReference", "appl-type", namespaceManager);

                entityObject.STA_PUB_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "/business:PatentDocumentAndRelated", "country", namespaceManager);
                entityObject.STA_PUB_NUMBER = MiscUtil.getXElementValueByXPath(rootElement, "/business:PatentDocumentAndRelated", "docNumber", namespaceManager);
                //公告类型
                entityObject.STA_PUB_KIND = string.IsNullOrEmpty(entityObject.STA_PUB_NUMBER) ? "" : entityObject.STA_PUB_NUMBER.Last().ToString();

                var pubDateStr = MiscUtil.getXElementValueByXPath(rootElement, "/business:PatentDocumentAndRelated", "datePublication", namespaceManager);

                entityObject.STA_PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(pubDateStr);


                entityObject.STA_NUMBEROFFIGURES = MiscUtil.getXElementValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:FullDocImage", "numberOfFigures", namespaceManager);
                entityObject.STA_TYPE = MiscUtil.getXElementValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:FullDocImage", "type", namespaceManager);


                var correspondDocInfo = (from docInfo in docList
                                         where !string.IsNullOrEmpty(entityObject.STA_PUB_NUMBER) && MiscUtil.getDictValueOrDefaultByKey(docInfo, "PublicationNum") == entityObject.STA_PUB_NUMBER && MiscUtil.getDictValueOrDefaultByKey(docInfo, "PublicationDate") == pubDateStr
                                         select docInfo).FirstOrDefault();

                if (null != correspondDocInfo)
                {
                    //entityObject.STA_APP_COUNTRY = MiscUtil.getDictValueOrDefaultByKey(correspondDocInfo, "") ;
                    entityObject.STA_APP_NUMBER = MiscUtil.getDictValueOrDefaultByKey(correspondDocInfo, "ApplicationNum");
                    if (!string.IsNullOrEmpty(entityObject.STA_APP_NUMBER))
                    {
                        try
                        {
                            entityObject.STA_APP_COUNTRY = entityObject.STA_APP_NUMBER.Substring(0, 2);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    entityObject.STA_APP_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getDictValueOrDefaultByKey(correspondDocInfo, "ApplicationDate"));
                }

                entityObject.EXIST_XML = "1";

                entityObject.PATH_XML = MiscUtil.getRelativeFilePathInclude(zipFilePath, 2) + Path.DirectorySeparatorChar + CompressUtil.removeDirEntrySlash(entry.Key);

                entityObject.IMPORT_TIME = System.DateTime.Now;

                entiesContext.SaveChanges();

                //输出插入记录
                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);

                MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");

                #endregion


                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, zipFilePath);
            }
            #endregion 循环入库
        }

        private static void parseZip03(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
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
                                     where entry.IsDirectory && CompressUtil.getEntryDepth(entry.Key) == 2
                                     select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();


            //排除压缩包中无关XML
            var xmlEntryParentDirEntries = (from entry in archive.Entries.AsParallel()
                                            where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
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
                                where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
                                select entry;

            totalCount = allXMLEntires.Count();

            MessageUtil.DoAppendTBDetail("在压缩包中发现" + totalCount + "个待导入XML条目");

            //已处理计数清零
            handledCount = 0;
            if (0 == allXMLEntires.Count())
            {
                MessageUtil.DoAppendTBDetail("没有找到XML");
                importSession.NOTE = "没有找到XML";
                //添加错误信息
                entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", filePath, "", ""));
                entiesContext.SaveChanges();
            }
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


                entityObject.STA_APP_COUNTRY = MiscUtil.getXElementValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager); ;
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

                entityObject.IMPORT_TIME = System.DateTime.Now;

                entiesContext.SaveChanges();


                //输出插入记录
                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);

                MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");

                #endregion

                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }
            #endregion 循环入库
        }

        private static void parseZip02(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
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
                                 where entry.IsDirectory && CompressUtil.getEntryDepth(entry.Key) == 1
                                 select CompressUtil.removeDirEntrySlash(entry.Key)).FirstOrDefault();

            DateTime? PUB_DATE = System.DateTime.Now;
            if (null != pub_dateEntry)
            {
                PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(pub_dateEntry);
            }



            //所有的待导入条目
            var dirNameSetEntires = (from entry in archive.Entries.AsParallel()
                                     where entry.IsDirectory && CompressUtil.getEntryDepth(entry.Key) == 2
                                     select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();


            //所有包含Tif的条目
            var tifEntryParentDirEntries = (from entry in archive.Entries.AsParallel()
                                            where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".TIF")
                                            select CompressUtil.getFileEntryParentPath(entry.Key)).Distinct();



            //不包含tif的目录
            var dirEntiresWithoutTif = dirNameSetEntires.Except(tifEntryParentDirEntries);

            totalCount = dirEntiresWithoutTif.Count() + tifEntryParentDirEntries.Count();

            handledCount = 0;

            //包含tif
            Parallel.ForEach<string>(tifEntryParentDirEntries, key =>
            {
                lock (typeof(ImportManger))
                {
                    handledCount++;
                    string importedMsg = ImportLogicUtil.importS_China_Patent_TextImage(entiesContext, filePath, importSession.SESSION_ID, APPL_TYPE, PUB_DATE, key, "1");
                    MessageUtil.DoAppendTBDetail($"记录:{importedMsg}插入成功");
                    MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                }
            });

            //不包含tif
            Parallel.ForEach<string>(dirEntiresWithoutTif, key =>
            {
                lock (typeof(ImportManger))
                {
                    handledCount++;
                    string importedMsg = ImportLogicUtil.importS_China_Patent_TextImage(entiesContext, filePath, importSession.SESSION_ID, APPL_TYPE, PUB_DATE, key, "0");
                    MessageUtil.DoAppendTBDetail($"记录:{importedMsg}插入成功");
                }
            });

            MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
        }

        private static void parseZip01(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
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
                                     where entry.IsDirectory && CompressUtil.getEntryDepth(entry.Key) == 2
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
        #endregion 入库逻辑
    }
}
