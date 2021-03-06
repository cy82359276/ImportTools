﻿using System;
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
using System.Threading.Tasks.Dataflow;

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
            try
            {
                bool specialDirctoryMode = false;
                //importStartTime = System.DateTime.Now;
                fileCount = AllFilePaths.Length;

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
                    else if ("国外专利生物序列加工成品数据".Equals(fileType)) //186 生物序列 压缩包是.tar.gz格式
                    {
                        //tar.gz
                        suffixFilter = "*.tar.gz";
                    }
                    else //默认是zip包
                    {
                        //Zip XML
                        suffixFilter = "*.zip";
                    }


                    if ("中国专利复审（无效）数据".Equals(fileType) || "中国专利的判决书数据".Equals(fileType)) //194, 196 xml格式 不遍历文件夹路径
                    {
                        //什么也不做, 直接传递文件夹路径
                        specialDirctoryMode = true; //特殊的文件夹模式: 文件夹内文件即需入库文件，不需要在解压等处理
                    }
                    else
                    {
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
                }

                #endregion

                bath.FILECOUNT = AllFilePaths.Count();

                #region 对指定的或发现的路径进行处理

                foreach (string path in AllFilePaths)//遍历处理需要处理的路径
                {
                    using (DataSourceEntities dataSourceEntites = new DataSourceEntities())
                    {
                        dataSourceEntites.Configuration.AutoDetectChangesEnabled = false;
                        dataSourceEntites.Configuration.ProxyCreationEnabled = false;

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

                            }
                            //特殊文件夹模式
                            else if (specialDirctoryMode && Directory.Exists(path) && !Main.showFileDialog)
                            {

                                ImportByPath(path, fileType, dataSourceEntites, bath);
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

                            LogHelper.WriteErrorLog(ex.Message + "\r\n" + ex.StackTrace);
                            if (ex.InnerException != null)
                            {
                                LogHelper.WriteErrorLog(ex.InnerException.Message + "\r\n" + ex.InnerException.StackTrace);
                            }

                            //LogHelper.WriteLog("", "error", currentFile + ":" + ex.Message);
                            LogHelper.WriteErrorLog($"导入文件{currentFile}时发生错误,错误消息:{ex.Message}详细信息{ex.StackTrace}");
                            continue;
                        }

                    }

                    System.GC.Collect();
                    GC.WaitForPendingFinalizers();

                }
                #endregion

                var lastTime = DateTime.Now.Subtract(bath.START_TIME.Value).TotalSeconds;
                bath.LAST_TIME = new decimal(lastTime);
                bath.ISCOMPLETED = "Y";

                try
                {
                    using (DataSourceEntities dataSourceEntites = new DataSourceEntities())
                    {
                        dataSourceEntites.S_IMPORT_BATH.Add(bath);
                        dataSourceEntites.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }


                MessageUtil.DoAppendTBDetail($"当前批次运行完毕，处理了{bath.FILECOUNT}个文件，入库了{bath.HANDLED_ITEM_COUNT}条目，总耗时{bath.LAST_TIME}秒");

            }
            catch (Exception ex)
            {
                LogHelper.WriteErrorLog(ex.Message + "\r\n" + ex.StackTrace);
                //MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
                if (ex.InnerException != null)
                {
                    LogHelper.WriteErrorLog(ex.InnerException.Message + "\r\n" + ex.InnerException.StackTrace);
                    //MessageBox.Show(ex.InnerException.Message + "\r\n" + ex.InnerException.StackTrace);
                }
            }

            return true;
        }

        public static bool ImportByPath(string filePath, string fileType, DataSourceEntities dataSourceEntites, S_IMPORT_BATH bath)
        {
            //fileType = fileType.Trim();

            #region 导入前准备 新建session对象
            currentFile = filePath;
            MessageUtil.DoAppendTBDetail("您选择的资源类型为：" + fileType);
            MessageUtil.DoAppendTBDetail("当前文件：" + filePath);


            //导入操作信息
            IMPORT_SESSION importSession = MiscUtil.getNewImportSession(fileType, filePath, bath);
            dataSourceEntites.IMPORT_SESSION.Add(importSession);
            importSession.START_TIME = DateTime.Now;
            //dataSourceEntites.SaveChanges();
            #endregion



            //判断是否是
            #region 分文件类型进行处理
            #region 01 中国专利全文代码化数据
            //压缩包内解析XML
            //目前监测了XML文件缺失的情况
            if (fileType == "中国专利全文代码化数据")
            {
                parseZip01(filePath, dataSourceEntites, importSession);

            }
            #endregion

            #region 02 中国专利全文图像数据
            else if (fileType == "中国专利全文图像数据")
            {
                parseZip02(filePath, dataSourceEntites, importSession);

            }
            #endregion

            #region 03 中国专利标准化全文文本数据 通用字段
            //有疑问: XML结构不同, 文件路径不确定
            else if (fileType == "中国专利标准化全文文本数据")
            {
                //parseZip03(filePath, dataSourceEntites, importSession);
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_CHINA_PATENT_STANDARDFULLTXT, typeof(S_CHINA_PATENT_STANDARDFULLTXT));
            }

            #endregion

            #region 04 中国专利标准化全文图像数据 XML

            else if (fileType == "中国专利标准化全文图像数据")
            {
                //未根据Index文件进行完整性校验
                parseZip04(filePath, dataSourceEntites, importSession);
            }

            #endregion
            #region 05 中国专利公报数据 TRS
            else if (fileType == "中国专利公报数据")
            {
                parseTRS05(filePath, dataSourceEntites, importSession);
            }

            #endregion
            #region 06 中国专利著录项目与文摘数据 通用字段 
            else if (fileType == "中国专利著录项目与文摘数据")
            {
                //parse06(filePath, entiesContext, importSession);
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_CHINA_PATENT_BIBLIOGRAPHIC, typeof(S_CHINA_PATENT_BIBLIOGRAPHIC));
            }


            #endregion
            #region 10 中国专利数据法律状态数据 TRS
            else if (fileType == "中国专利法律状态数据")
            {
                parseTRS10(filePath, dataSourceEntites, importSession);
            }
            #endregion
            #region  11 中国专利法律状态变更翻译数据 mdb文件
            else if (fileType == "中国专利法律状态变更翻译数据")
            {
                parseMDB11(filePath, dataSourceEntites, importSession);
            }
            #endregion
            #region 13 中国标准化简单引文数据 通用字段
            else if (fileType == "中国标准化简单引文数据")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_CHINA_STANDARD_SIMPCITATION, typeof(S_CHINA_STANDARD_SIMPCITATION));
            }
            #endregion
            #region 14 专利缴费数据 TRS
            else if (fileType == "专利缴费数据")
            {
                parseTRS14(filePath, dataSourceEntites, importSession);

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
            #region 50 美国专利全文文本数据（标准化） 通用  未建库
            else if (fileType == "美国专利全文文本数据（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_AMERICAN_PATENT_FULLTEXT, typeof(S_AMERICAN_PATENT_FULLTEXT));
            }
            #endregion
            #region 51 欧专局专利全文文本数据（标准化） 通用  未建库
            else if (fileType == "欧专局专利全文文本数据（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_EUROPEAN_PATENT_FULLTEXT, typeof(S_EUROPEAN_PATENT_FULLTEXT));
            }
            #endregion
            #region  52 韩国专利全文代码化数据（标准化） 通用  未建库 
            else if (fileType == "韩国专利全文代码化数据（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_KOREAN_PATENT_FULLTEXTCODE, typeof(S_KOREAN_PATENT_FULLTEXTCODE));
            }
            #endregion
            #region  53 瑞士专利全文代码化数据（标准化）通用  未建库
            else if (fileType == "瑞士专利全文代码化数据（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_SWISS_PATENT_FULLTEXTCODE, typeof(S_SWISS_PATENT_FULLTEXTCODE));
            }
            #endregion
            #region 54 英国专利全文代码化数据（标准化）通用  未建库
            else if (fileType == "英国专利全文代码化数据（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_BRITISH_PATENT_FULLTEXTCODE, typeof(S_BRITISH_PATENT_FULLTEXTCODE));
            }
            #endregion
            #region 55 日本专利全文代码化数据（标准化）通用  未建库
            else if (fileType == "日本专利全文代码化数据（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_JAPAN_PATENT_FULLTEXTCODE, typeof(S_JAPAN_PATENT_FULLTEXTCODE));
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
            #region 103 比利时专利全文数据（BE）（标准化） 通用字段 
            else if (fileType == "比利时专利全文数据（BE）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_BELGIAN_PATENT_FULLTEXT, typeof(S_BELGIAN_PATENT_FULLTEXT));
            }
            #endregion 
            #region 104 奥地利专利全文数据（AT）（标准化） 通用字段 
            else if (fileType == "奥地利专利全文数据（AT）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_AUSTRIA_PATENT_FULLTEXT, typeof(S_AUSTRIA_PATENT_FULLTEXT));
            }
            #endregion
            #region 105 西班牙专利全文数据（ES）（标准化） 通用字段 
            else if (fileType == "西班牙专利全文数据（ES）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_SPANISH_PATENT_FULLTEXT, typeof(S_SPANISH_PATENT_FULLTEXT));
            }
            #endregion
            #region 106 波兰专利著录项及全文数据（PL）（标准化） 通用字段 
            else if (fileType == "波兰专利著录项及全文数据（PL）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_POLAND_PATENT_DESCRIPTION, typeof(S_POLAND_PATENT_DESCRIPTION));
            }
            #endregion
            #region 107 以色列专利著录项及全文数据（IL）（标准化） 通用字段 
            else if (fileType == "以色列专利著录项及全文数据（IL）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_ISRAEL_PATENT_DESCRIPTION, typeof(S_ISRAEL_PATENT_DESCRIPTION));
            }
            #endregion
            #region 108 新加坡专利著录项及全文数据（SG）（标准化） 通用字段 
            else if (fileType == "新加坡专利著录项及全文数据（SG）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_SINGAPORE_PATENT_DESCRIPTION, typeof(S_SINGAPORE_PATENT_DESCRIPTION));
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
            #region 113 美国外观设计专利数据（DI）通用 
            else if (fileType == "美国外观设计专利数据（DI）")
            {

            }
            #endregion
            #region 日本外观设计专利数据（DI）通用 
            else if (fileType == "日本外观设计专利数据（DI）")
            {

            }
            #endregion
            #region 115 韩国外观设计专利数据（DI）通用 
            else if (fileType == "韩国外观设计专利数据（DI）")
            {

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
            #region 121 中国专利全文数据PDF（DI）
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
            #region 127 俄罗斯专利文摘英文翻译数据（DI） 通用字段 
            else if (fileType == "俄罗斯专利文摘英文翻译数据（DI）")
            {



            }
            #endregion
            #region 132 中国商标 XML
            else if (fileType == "中国商标")
            {
                parseZip132(filePath, dataSourceEntites, importSession, "S_CHINA_BRAND");
            }
            #endregion
            #region 133 中国商标许可数据 XML
            else if (fileType == "中国商标许可数据")
            {
                parseZip133(filePath, dataSourceEntites, importSession, "S_CHINA_BRAND_LICENSE", fileType);
            }
            #endregion
            #region 134 中国商标转让数据 XML
            else if (fileType == "中国商标转让数据")
            {
                parseZip134(filePath, dataSourceEntites, importSession, "S_CHINA_BRAND_TRANSFER", fileType);
            }
            #endregion
            #region 136 马德里商标进入中国 XML
            else if (fileType == "马德里商标进入中国")
            {
                parseZip136(filePath, dataSourceEntites, importSession, "S_MADRID_BRAND_ENTER_CHINA", fileType);
            }
            #endregion
            #region 137  中国驰名商标数据 XML
            else if (fileType == "中国驰名商标数据")
            {
                parseZip137(filePath, dataSourceEntites, importSession, "S_CHINA_WELLKNOWN_BRAND", fileType);
            }
            #endregion
            #region 138 美国申请商标 XML
            else if (fileType == "美国申请商标")
            {
                parseZip138(filePath, dataSourceEntites, importSession, "S_AMERICA_APPLY_BRAND", fileType);
            }
            #endregion
            #region 139 美国转让商标 XML
            else if (fileType == "美国转让商标")
            {
                parseZip139(filePath, dataSourceEntites, importSession, "S_AMERICA_TRANSFER_BRAND", fileType);
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
                parseZip153(filePath, dataSourceEntites, importSession, "S_JOURNAL_PROJECT_ABSTRACT", fileType);
            }
            #endregion
            #region 162 中国法院判例初加工数据 XML
            else if (fileType == "中国法院判例初加工数据")
            {
                parseZip162(filePath, dataSourceEntites, importSession);
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
                dataSourceEntites.SaveChanges();

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
                    dataSourceEntites.S_CHINA_BRAND_CLASSIFICATION.Add(entityObject);

                    if (handledCount % 100 == 0)
                    {
                        MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                        //每500条, 提交下
                        if (handledCount % 500 == 0)
                        {
                            dataSourceEntites.SaveChanges();
                        }
                    }
                }
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                dataSourceEntites.SaveChanges();

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
                dataSourceEntites.SaveChanges();

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
                    dataSourceEntites.S_AMERICAN_BRAND_GRAPHCLASSIFY.Add(entityObject);

                    if (handledCount % 100 == 0)
                    {
                        MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                        //每500条, 提交下
                        if (handledCount % 500 == 0)
                        {
                            dataSourceEntites.SaveChanges();
                        }
                    }
                }
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                dataSourceEntites.SaveChanges();





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
                dataSourceEntites.SaveChanges();

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
                    dataSourceEntites.S_AMERICAN_BRAND_USCLASSIFY.Add(entityObject);

                    if (handledCount % 100 == 0)
                    {
                        MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                        //每500条, 提交下
                        if (handledCount % 500 == 0)
                        {
                            dataSourceEntites.SaveChanges();
                        }
                    }
                }
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
                dataSourceEntites.SaveChanges();

            }
            #endregion
            #region 172 马德里商标购买数据 XML
            else if (fileType == "马德里商标购买数据")
            {
                parseZip172(filePath, dataSourceEntites, importSession, "S_MADRID_BRAND_PURCHASE", fileType);
            }
            #endregion
            #region 180 中国专利代理知识产权法律法规加工数据 XML
            else if (fileType == "中国专利代理知识产权法律法规加工数据")
            {
                parseZip180(filePath, dataSourceEntites, importSession, "S_CHINA_PATENT_LAWSPROCESS", fileType);

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
            #region 186 国外专利生物序列加工成品数据 XML
            else if (fileType == "国外专利生物序列加工成品数据")
            {
                parseXML186Special(filePath, dataSourceEntites, importSession, "S_FOREIGN_PATENT_SEQUENCE", fileType);
            }


            #endregion
            #region 194 中国专利复审（无效）数据 XML
            else if (fileType == "中国专利复审（无效）数据")
            {
                parseXML194(filePath, dataSourceEntites, importSession, "S_CHINA_PATENT_REVIEW", fileType);
            }

            #endregion
            #region 196 中国专利的判决书数据 XML

            else if (fileType == "中国专利的判决书数据")
            {
                parseXML196(filePath, dataSourceEntites, importSession, "S_CHINA_PATENT_JUDGMENT", fileType);
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
                parseMDB210(filePath, dataSourceEntites, importSession);
            }

            #endregion
            #region 211 中国化学药物专利深加工数据 mdb

            else if (fileType == "中国化学药物专利深加工数据")
            {
                parseMDB211(filePath, dataSourceEntites, importSession);

            }

            #endregion
            #region 212 中国中药专利深加工数据 mdb

            else if (fileType == "中国中药专利深加工数据")
            {
                parseMDB212(filePath, dataSourceEntites, importSession);

            }
            #endregion
            #region 213 中国专利摘要英文翻译数据（标准化） XML 通用字段 
            else if (fileType == "中国专利摘要英文翻译数据（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_CHINA_PATENT_ABSTRACTS, typeof(S_CHINA_PATENT_ABSTRACTS));
            }
            #endregion
            #region 214 DOCDB数据（标准化） XML
            else if (fileType == "DOCDB数据（标准化）")
            {
                handledCount = 0;
                importStartTime = importSession.START_TIME.Value;

                importSession.TABLENAME = "S_China_Patent_StandardFullTxt".ToUpper();
                dataSourceEntites.SaveChanges();

                SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
                IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

                //总条目数
                importSession.IS_ZIP = "Y";
                totalCount = archive.Entries.Count();
                importSession.ZIP_ENTRIES_COUNT = totalCount;
                dataSourceEntites.SaveChanges();

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
                        dataSourceEntites.IMPORT_ERROR.Add(importError);
                        dataSourceEntites.SaveChanges();
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
                    dataSourceEntites.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", filePath, "", ""));
                    dataSourceEntites.SaveChanges();
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
                        dataSourceEntites.SaveChanges();
                        break;
                    }

                    var keyTemp = entry.Key;

                    //解压当前的XML文件
                    MemoryStream memStream = new MemoryStream(); entry.WriteTo(memStream);

                    if (0 == memStream.Length)
                    {
                        MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                        LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                        importSession.FAILED_COUNT++;
                        IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                        dataSourceEntites.IMPORT_ERROR.Add(errorTemp);
                        dataSourceEntites.SaveChanges();
                        continue;
                    }

                    S_CHINA_PATENT_STANDARDFULLTXT entityObject = new S_CHINA_PATENT_STANDARDFULLTXT() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                    entityObject.ARCHIVE_INNER_PATH = entry.Key;
                    entityObject.FILE_PATH = filePath;
                    //sCNPatentTextCode.SESSION_INDEX = handledCount;
                    dataSourceEntites.S_CHINA_PATENT_STANDARDFULLTXT.Add(entityObject);
                    ////entiesContext.SaveChanges();

                    if (memStream.Position > 0) { memStream.Position = 0; }
                    XDocument doc = XDocument.Load(memStream);

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
                    entityObject.STA_PUB_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                    entityObject.STA_PUB_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:DocNumber", "", namespaceManager);
                    entityObject.STA_PUB_KIND = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:Kind", "", namespaceManager);
                    entityObject.STA_PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:Date", "", namespaceManager));


                    entityObject.ORI_PUB_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                    entityObject.ORI_PUB_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:DocNumber", "", namespaceManager);
                    entityObject.ORI_PUB_KIND = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:Kind", "", namespaceManager);
                    entityObject.ORI_PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:Date", "", namespaceManager));


                    entityObject.STA_APP_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager); ;
                    entityObject.STA_APP_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:DocNumber", "", namespaceManager);
                    entityObject.STA_APP_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:Date", "", namespaceManager));


                    entityObject.ORI_APP_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                    entityObject.ORI_APP_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:DocNumber", "", namespaceManager);
                    entityObject.ORI_APP_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:Date", "", namespaceManager));


                    entityObject.DESIGN_PATENTNUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBibliographicData/business:PatentNumber", "", namespaceManager);

                    entityObject.CLASSIFICATIONIPCR = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ClassificationIPCRDetails/business:ClassificationIPCR[@sequence='1']/base:Text", "", namespaceManager);

                    entityObject.CLASSIFICATIONLOCARNO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBibliographicData/business:ClassificationLocarno", "", namespaceManager);

                    entityObject.INVENTIONTITLE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:BibliographicData/business:InventionTitle", "", namespaceManager);

                    entityObject.ABSTRACT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:Abstract/base:Paragraphs", "", namespaceManager);

                    entityObject.DESIGNBRIEFEXPLANATION = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBriefExplanation", "", namespaceManager);
                    entityObject.FULLDOCIMAGE_NUMBEROFFIGURES = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:FullDocImagenumberOfFigures", "", namespaceManager);
                    entityObject.FULLDOCIMAGE_TYPE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:FullDocImage/type", "", namespaceManager);


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

                    dataSourceEntites.SaveChanges();


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
            #region 215 国际知识产权组织专利著录项及全文数据（WIPO)（标准化） XML  通用字段 
            else if (fileType == "国际知识产权组织专利著录项及全文数据（WIPO)（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_WIPO_PATENT_DESCRIPTION, typeof(S_WIPO_PATENT_DESCRIPTION));
            }
            #endregion
            #region 216 加拿大专利著录项及全文数据（CA）（标准化） XML  通用字段 
            else if (fileType == "加拿大专利著录项及全文数据（CA）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_CANADIAN_PATENT_DESCRIPTION, typeof(S_CANADIAN_PATENT_DESCRIPTION));
            }
            #endregion
            #region 217 俄罗斯专利著录项及全文数据（RU）（标准化） XML  通用字段 
            else if (fileType == "俄罗斯专利著录项及全文数据（RU）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_RUSSIAN_PATENT_DESCRIPTION, typeof(S_RUSSIAN_PATENT_DESCRIPTION));
            }
            #endregion
            #region 218 澳大利亚专利全文文本数据（AU）（标准化） XML  通用字段 
            else if (fileType == "澳大利亚专利全文文本数据（AU）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_AUSTRALIAN_PATENT_FULLTEXT, typeof(S_AUSTRALIAN_PATENT_FULLTEXT));
            }
            #endregion
            #region 219 德国专利著录项及全文数据（DE）（标准化） XML  通用字段 
            else if (fileType == "德国专利著录项及全文数据（DE）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_GERMAN_PATENT_DESCRIPTION, typeof(S_GERMAN_PATENT_DESCRIPTION));
            }
            #endregion
            #region 220 法国专利著录项及全文数据（FR）（标准化） XML  通用字段 
            else if (fileType == "法国专利著录项及全文数据（FR）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_FRENCH_PATENT_DESCRIPTION, typeof(S_FRENCH_PATENT_DESCRIPTION));
            }
            #endregion
            #region 221 台湾专利著录项及全文数据（TW）（标准化） XML  通用字段 
            else if (fileType == "台湾专利著录项及全文数据（TW）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_TAIWAN_PATENT_DESCRIPTION, typeof(S_TAIWAN_PATENT_DESCRIPTION));
            }
            #endregion
            #region 222 香港专利著录项数据（HK）（标准化） XML  通用字段 
            else if (fileType == "香港专利著录项数据（HK）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_HONGKONG_PATENT_DESCRIPTION, typeof(S_HONGKONG_PATENT_DESCRIPTION));
            }
            #endregion
            #region 223 澳门专利著录项数据（MO）（标准化） XML  通用字段 
            else if (fileType == "澳门专利著录项数据（MO）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_MACAO_PATENT_DESCRIPTION, typeof(S_MACAO_PATENT_DESCRIPTION));
            }
            #endregion
            #region 224 欧亚组织专利著录项及全文数据（EA）（标准化） XML  通用字段 
            else if (fileType == "欧亚组织专利著录项及全文数据（EA）（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_EURASIAN_PATENT_DESCRIPTION, typeof(S_EURASIAN_PATENT_DESCRIPTION));
            }
            #endregion
            #region 225 日本外观设计专利数据（标准化） XML  通用字段 
            else if (fileType == "日本外观设计专利数据（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_JAPAN_DESIGN_PATENT, typeof(S_JAPAN_DESIGN_PATENT));
            }
            #endregion
            #region 226 德国外观设计专利数据（标准化） XML  通用字段 
            else if (fileType == "德国外观设计专利数据（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_GERMAN_DESIGN_PATENT, typeof(S_GERMAN_DESIGN_PATENT));
            }
            #endregion
            #region 227 法国外观设计专利数据（标准化） XML  通用字段 
            else if (fileType == "法国外观设计专利数据（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_FRENCH_DESIGN_PATENT, typeof(S_FRENCH_DESIGN_PATENT));
            }
            #endregion
            #region 228 俄罗斯外观设计专利数据（标准化） XML  通用字段 
            else if (fileType == "俄罗斯外观设计专利数据（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_RUSSIAN_DESIGN_PATENT, typeof(S_RUSSIAN_DESIGN_PATENT));
            }
            #endregion
            #region 229 日本专利文摘英文翻译数据（PAJ)（标准化） XML  通用字段 
            else if (fileType == "日本专利文摘英文翻译数据（PAJ)（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_JAPAN_PATENT_ABSTRACTS, typeof(S_JAPAN_PATENT_ABSTRACTS));
            }
            #endregion
            #region 230 韩国专利文摘英文翻译数据(KPA)（标准化） XML  通用字段 
            else if (fileType == "韩国专利文摘英文翻译数据(KPA)（标准化）")
            {
                parseZipUniversalSTA(filePath, dataSourceEntites, importSession, dataSourceEntites.S_KOREA_PATENT_ABSTRACTS, typeof(S_KOREA_PATENT_ABSTRACTS));
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
            dataSourceEntites.SaveChanges();
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
            //entiesContext.SaveChanges();

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
                        //entiesContext.SaveChanges();
                    }
                }

            }

            //entiesContext.SaveChanges();
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
            //entiesContext.SaveChanges();

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
                        //entiesContext.SaveChanges();
                    }
                }

            }

            //entiesContext.SaveChanges();
            MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);

            accUtil.Close();//关闭数据库                
        }

        private static void parseXML186Special(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession, string tableName, string dataResChineseName)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = tableName;//设置表名
            //entiesContext.SaveChanges();


            MessageUtil.DoAppendTBDetail($"开始寻找'{dataResChineseName}'XML文件：");


            List<string> xmlFiles = new List<string>();
            if (Main.showFileDialog)//文件模式
            {
                if (File.Exists(filePath) && filePath.ToUpper().EndsWith("XML"))//指定的文件是必须是XML文件
                {
                    xmlFiles.Add(filePath);
                }
                else
                {
                    importSession.HAS_ERROR = "Y";
                    IMPORT_ERROR importError = new IMPORT_ERROR() { ID = System.Guid.NewGuid().ToString(), SESSION_ID = importSession.SESSION_ID, IGNORED = "N", ISZIP = "Y", POINTOR = handledCount, ZIP_OR_DIR_PATH = filePath, REIMPORTED = "N", ZIP_PATH = filePath, OCURREDTIME = System.DateTime.Now, ERROR_MESSAGE = "没找到指定的XML" };
                    importSession.FAILED_COUNT++;
                    entiesContext.IMPORT_ERROR.Add(importError);
                    //entiesContext.SaveChanges();
                }
            }
            else//文件夹模式
            {
                string suffix = "*.xml";
                List<FileInfo> fileInfos = MiscUtil.getFileInfosByDirPathRecuriouslyWithSingleSearchPattern(filePath, suffix);
                var allXMLFilesFullPaths = (from xmlFileInfo in fileInfos
                                            select xmlFileInfo.FullName).ToList();
                if (allXMLFilesFullPaths.Count > 0)
                {
                    xmlFiles.AddRange(allXMLFilesFullPaths);
                }
                else
                {
                    importSession.HAS_ERROR = "Y";
                    IMPORT_ERROR importError = new IMPORT_ERROR() { ID = System.Guid.NewGuid().ToString(), SESSION_ID = importSession.SESSION_ID, IGNORED = "N", ISZIP = "Y", POINTOR = handledCount, ZIP_OR_DIR_PATH = filePath, REIMPORTED = "N", ZIP_PATH = filePath, OCURREDTIME = System.DateTime.Now, ERROR_MESSAGE = "没找到指定的XML" };
                    importSession.FAILED_COUNT++;
                    entiesContext.IMPORT_ERROR.Add(importError);
                    //entiesContext.SaveChanges();
                }
            }

            totalCount = xmlFiles.Count;
            #region 循环入库
            foreach (var xmlFilePath in xmlFiles)
            {
                //计数变量
                handledCount++;

                if (forcedStop)
                {
                    MessageUtil.DoAppendTBDetail("强制终止了插入");
                    importSession.NOTE = "用户强制终止了本次插入";
                    //entiesContext.SaveChanges();
                    break;
                }

                //解压当前的XML文件
                string entryFullPath = xmlFilePath;

                //S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                //dynamic entityObject = Activator.CreateInstance(entityObjectType);
                var entityObject = new S_FOREIGN_PATENT_SEQUENCE();
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                //entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                entiesContext.S_FOREIGN_PATENT_SEQUENCE.Add(entityObject);
                ////entiesContext.SaveChanges();

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


                entityObject.FILENAME = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content", "file");
                entityObject.DATEEXCHANGE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/content", "dateExchange"));
                entityObject.DATEPRODUCED = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/content", "dateProduced"));
                entityObject.PATCNT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content", "patcnt");
                entityObject.FILECNT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content", "filecnt");
                entityObject.DATASIZE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content", "size");
                entityObject.MD5 = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content", "md5");
                entityObject.STATUS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content", "status");
                entityObject.DOCLIST_TOPIC = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content/doclist", "topic");
                entityObject.DOCLIST_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content/doclist", "country");
                entityObject.DOCLIST_DOCNUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content/doclist", "docNumber");
                entityObject.DOCLIST_KIND = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content/doclist", "kind");
                entityObject.DOCLIST_PNO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content/doclist", "PNO");
                entityObject.DOCLIST_PNS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content/doclist", "PNS");
                entityObject.DOCLIST_DATEPUBLICATION = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/content/doclist", "datePublication"));
                entityObject.DOCLIST_FORMAT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content/doclist", "Format");
                entityObject.DOCLIST_PATH = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content/doclist", "path");
                entityObject.DOCLIST_STATUS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content/doclist", "status");


                entityObject.DOCLIST_FILENAME = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content/doclist/file", "filename");
                entityObject.DOCLIST_FILETYPE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content/doclist/file", "filetype");
                entityObject.DOCLIST_SECTION = MiscUtil.getXElementSingleValueByXPath(rootElement, "/content/doclist/file", "section");
                entityObject.DOCLIST_SEQLIST_AMOUNT = MiscUtil.tryParseIntNullable(MiscUtil.getXElementSingleValueByXPath(rootElement, "/content/doclist/file/seqlist", "amount"));

                var seqEleList = rootElement.XPathSelectElements("/content/doclist/file/seqlist/seq");

                MessageUtil.DoAppendTBDetail($"发现{seqEleList.Count()}个序列信息，正在进行解析……");

                foreach (var seqEle in seqEleList)
                {
                    string id = "";
                    try
                    {
                        id = seqEle.Attribute("id").Value;
                    }
                    catch (Exception)
                    {
                        continue;//继续解析下个seq
                    }
                    var length = MiscUtil.getXElementValueByTagNameaAndChildTabName(seqEle, "length");
                    var type = MiscUtil.getXElementValueByTagNameaAndChildTabName(seqEle, "type");
                    var gn = MiscUtil.getXElementValueByTagNameaAndChildTabName(seqEle, "gn");
                    var organism = MiscUtil.getXElementValueByTagNameaAndChildTabName(seqEle, "organism");
                    string features = "";
                    try
                    {
                        features = seqEle.Descendants("features").FirstOrDefault().ToString();
                    }
                    catch (Exception)
                    {

                    }
                    S_BIOLOGICAL_SEQ bioSQ = new S_BIOLOGICAL_SEQ() { ID = id, LENGTH = length, TYPE = type, GN = gn, ORGANISM = organism, FEATURES = features, S_FOREIGN_PATENT_SEQ_ID = entityObject.ID };
                    entiesContext.S_BIOLOGICAL_SEQ.Add(bioSQ);
                    MessageUtil.DoAppendTBDetail($"当前序列{MiscUtil.jsonSerilizeObject(bioSQ)}");
                }
                MessageUtil.DoAppendTBDetail($"序列信息解析完成,正在写库,请稍等……");

                entityObject.IMPORT_TIME = System.DateTime.Now;
                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);
                try
                {
                    //entiesContext.SaveChanges();
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

        private static void parseXML196(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession, string tableName, string dataResChineseName)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = tableName;//设置表名
            //entiesContext.SaveChanges();


            MessageUtil.DoAppendTBDetail($"开始寻找'{dataResChineseName}'XML文件：");


            List<string> xmlFiles = new List<string>();
            if (Main.showFileDialog)//文件模式
            {
                if (File.Exists(filePath) && filePath.ToUpper().EndsWith("XML"))//指定的文件是必须是XML文件
                {
                    xmlFiles.Add(filePath);
                }
                else
                {
                    importSession.HAS_ERROR = "Y";
                    IMPORT_ERROR importError = new IMPORT_ERROR() { ID = System.Guid.NewGuid().ToString(), SESSION_ID = importSession.SESSION_ID, IGNORED = "N", ISZIP = "Y", POINTOR = handledCount, ZIP_OR_DIR_PATH = filePath, REIMPORTED = "N", ZIP_PATH = filePath, OCURREDTIME = System.DateTime.Now, ERROR_MESSAGE = "没找到指定的XML" };
                    importSession.FAILED_COUNT++;
                    entiesContext.IMPORT_ERROR.Add(importError);
                    //entiesContext.SaveChanges();
                }
            }
            else//文件夹模式
            {
                string suffix = "*.xml";
                List<FileInfo> fileInfos = MiscUtil.getFileInfosByDirPathRecuriouslyWithSingleSearchPattern(filePath, suffix);
                var allXMLFilesFullPaths = (from xmlFileInfo in fileInfos
                                            select xmlFileInfo.FullName).ToList();
                if (allXMLFilesFullPaths.Count > 0)
                {
                    xmlFiles.AddRange(allXMLFilesFullPaths);
                }
                else
                {
                    importSession.HAS_ERROR = "Y";
                    IMPORT_ERROR importError = new IMPORT_ERROR() { ID = System.Guid.NewGuid().ToString(), SESSION_ID = importSession.SESSION_ID, IGNORED = "N", ISZIP = "Y", POINTOR = handledCount, ZIP_OR_DIR_PATH = filePath, REIMPORTED = "N", ZIP_PATH = filePath, OCURREDTIME = System.DateTime.Now, ERROR_MESSAGE = "没找到指定的XML" };
                    importSession.FAILED_COUNT++;
                    entiesContext.IMPORT_ERROR.Add(importError);
                    //entiesContext.SaveChanges();
                }
            }

            totalCount = xmlFiles.Count;
            #region 循环入库
            foreach (var xmlFilePath in xmlFiles)
            {
                //计数变量
                handledCount++;

                if (forcedStop)
                {
                    MessageUtil.DoAppendTBDetail("强制终止了插入");
                    importSession.NOTE = "用户强制终止了本次插入";
                    //entiesContext.SaveChanges();
                    break;
                }

                //解压当前的XML文件
                string entryFullPath = xmlFilePath;

                //S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                //dynamic entityObject = Activator.CreateInstance(entityObjectType);
                var entityObject = new S_CHINA_PATENT_JUDGMENT();
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                //entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                entiesContext.S_CHINA_PATENT_JUDGMENT.Add(entityObject);
                ////entiesContext.SaveChanges();

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

                entityObject.CN_COURT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-court");
                entityObject.CN_DECISION_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-decision-number");
                entityObject.ASSIGNEES = MiscUtil.getXElementInnerXMLByXPath(rootElement, "/cn-patent-verdict/cn-patent-info/assignees");
                entityObject.PATENT_APPLICATION_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-patent-info/application-reference/document-id/country");
                entityObject.PATENT_APPLICATION_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-patent-info/application-reference/document-id/doc-number");
                entityObject.PATENT_APPLICATION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-patent-info/application-reference/document-id/date"));
                entityObject.PATENT_PUBLICATION_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-patent-info/publication-reference/document-id/country");
                entityObject.PATENT_PUBLICATION_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-patent-info/publication-reference/document-id/doc-number");
                entityObject.PATENT_PUBLICATION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-patent-info/publication-reference/document-id/date"));
                entityObject.INVENTION_TITLE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-patent-info/invention-title");
                entityObject.CLASSIFICATION_IPC = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-patent-info/classification-ipc");
                entityObject.CN_LEGAL_FILE_NUMBERS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-legal-file-numbers");
                entityObject.CN_LEGAL_PLAINTIFFS = MiscUtil.getXElementInnerXMLByXPath(rootElement, "/cn-patent-verdict/cn-legal-case-parties/cn-legal-plaintiffs");
                entityObject.CN_LEGAL_DEFENDANTS = MiscUtil.getXElementInnerXMLByXPath(rootElement, "/cn-patent-verdict/cn-legal-case-parties/cn-legal-defendants");
                entityObject.CN_JUDGES = MiscUtil.getXElementInnerXMLByXPath(rootElement, "/cn-patent-verdict/cn-judges");
                entityObject.CN_COURT_REPORTERS = MiscUtil.getXElementInnerXMLByXPath(rootElement, "/cn-patent-verdict/cn-court-reporters");
                entityObject.CN_VERDICT_DETAIL = MiscUtil.getXElementInnerXMLByXPath(rootElement, "/cn-patent-verdict/cn-verdict-detail");
                entityObject.CN_VERDICT_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-verdict-date/date"));
                entityObject.PUBLICATION_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-publication-info/document-id/country");
                entityObject.PUBLICATION_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-publication-info/document-id/doc-number");
                entityObject.PUBLICATION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-publication-info/document-id/date"));
                entityObject.CN_PUB_VOL = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-verdict/cn-publication-info/cn-pub-vol");
                entityObject.PATH_XML = MiscUtil.getRelativeFilePathInclude(xmlFilePath, 4);

                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);
                try
                {
                    //entiesContext.SaveChanges();
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
        private static void parseXML194(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession, string tableName, string dataResChineseName)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = tableName;//设置表名
            //entiesContext.SaveChanges();


            MessageUtil.DoAppendTBDetail($"开始寻找'{dataResChineseName}'XML文件：");


            List<string> xmlFiles = new List<string>();
            if (Main.showFileDialog)//文件模式
            {
                if (File.Exists(filePath) && filePath.ToUpper().EndsWith("XML"))//指定的文件是必须是XML文件
                {
                    xmlFiles.Add(filePath);
                }
                else
                {
                    importSession.HAS_ERROR = "Y";
                    IMPORT_ERROR importError = new IMPORT_ERROR() { ID = System.Guid.NewGuid().ToString(), SESSION_ID = importSession.SESSION_ID, IGNORED = "N", ISZIP = "Y", POINTOR = handledCount, ZIP_OR_DIR_PATH = filePath, REIMPORTED = "N", ZIP_PATH = filePath, OCURREDTIME = System.DateTime.Now, ERROR_MESSAGE = "没找到指定的XML" };
                    importSession.FAILED_COUNT++;
                    entiesContext.IMPORT_ERROR.Add(importError);
                    //entiesContext.SaveChanges();
                }
            }
            else//文件夹模式
            {
                string suffix = "*.xml";
                List<FileInfo> fileInfos = MiscUtil.getFileInfosByDirPathRecuriouslyWithSingleSearchPattern(filePath, suffix);
                var allXMLFilesFullPaths = (from xmlFileInfo in fileInfos
                                            select xmlFileInfo.FullName).ToList();
                if (allXMLFilesFullPaths.Count > 0)
                {
                    xmlFiles.AddRange(allXMLFilesFullPaths);
                }
                else
                {
                    importSession.HAS_ERROR = "Y";
                    IMPORT_ERROR importError = new IMPORT_ERROR() { ID = System.Guid.NewGuid().ToString(), SESSION_ID = importSession.SESSION_ID, IGNORED = "N", ISZIP = "Y", POINTOR = handledCount, ZIP_OR_DIR_PATH = filePath, REIMPORTED = "N", ZIP_PATH = filePath, OCURREDTIME = System.DateTime.Now, ERROR_MESSAGE = "没找到指定的XML" };
                    importSession.FAILED_COUNT++;
                    entiesContext.IMPORT_ERROR.Add(importError);
                    //entiesContext.SaveChanges();
                }
            }

            totalCount = xmlFiles.Count;
            #region 循环入库
            foreach (var xmlFilePath in xmlFiles)
            {
                //计数变量
                handledCount++;

                if (forcedStop)
                {
                    MessageUtil.DoAppendTBDetail("强制终止了插入");
                    importSession.NOTE = "用户强制终止了本次插入";
                    //entiesContext.SaveChanges();
                    break;
                }

                //解压当前的XML文件
                string entryFullPath = xmlFilePath;

                //S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                //dynamic entityObject = Activator.CreateInstance(entityObjectType);
                var entityObject = new S_CHINA_PATENT_REVIEW();
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                //entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                entiesContext.S_CHINA_PATENT_REVIEW.Add(entityObject);
                ////entiesContext.SaveChanges();

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



                entityObject.CN_DECISION_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-decision-number");
                entityObject.CN_DECISION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-decision-date/date"));
                entityObject.APPLICATION_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-patent-info/application-reference/document-id/country");
                entityObject.APPLICATION_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-patent-info/application-reference/document-id/doc-number");
                entityObject.APPLICATION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-patent-info/application-reference/document-id/date"));
                entityObject.APPLICANTS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-patent-info/applicants");
                entityObject.PUBLICATION_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-patent-info/publication-reference/document-id/country");
                entityObject.PUBLICATION_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-patent-info/publication-reference/document-id/doc-number");
                entityObject.PUBLICATION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-patent-info/publication-reference/document-id/date"));
                entityObject.INVENTION_TITLE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-patent-info/invention-title");
                entityObject.CLASSIFICATION_IPC = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-patent-info/classification-ipc");
                entityObject.CN_COMPLAINANT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-parties/cn-complainant");
                entityObject.CN_EXAMINERS = MiscUtil.getXElementInnerXMLByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-examiners");
                entityObject.PUBLICATION_INFO_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-publication-info/document-id/country");
                entityObject.PUBLICATION_INFO_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-publication-info/document-id/doc-number");
                entityObject.PUBLICATION_INFO_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-publication-info/document-id/date"));
                entityObject.CN_PUB_VOL = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-publication-info/cn-pub-vol");
                entityObject.CN_ACCESSORY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-accessories/cn-accessory/file-name");
                entityObject.CN_LAW_REFERENCE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-decision-abstract/cn-legal-basis/cn-law-reference");
                entityObject.CN_MAIN_POINT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/cn-decision-abstract/cn-main-points/cn-main-point");
                entityObject.ABSTRACT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/abstract");
                entityObject.KEYWORD = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-case-info/keyword");
                entityObject.CN_BRIEF_HISTORY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-decision-detail/cn-brief-history");
                entityObject.CN_REASONING = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-decision-detail/cn-reasoning");
                entityObject.CN_HOLDING = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision/cn-decision-detail/cn-holding");
                var appealType = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-appeal-decision", "appeal-type");
                entityObject.REEXAMINE_INVALID = "invalidation".Equals(appealType) ? "无效" : "复审";
                entityObject.PATH_XML = MiscUtil.getRelativeFilePathInclude(xmlFilePath, 4);



                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);
                try
                {
                    //entiesContext.SaveChanges();
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
            //entiesContext.SaveChanges();

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
                        //entiesContext.SaveChanges();
                    }
                }

            }

            //entiesContext.SaveChanges();
            MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);

            accUtil.Close();//关闭数据库                
        }

        private static void parseZip180(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession, string tableName, string dataResChineseName)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = tableName;//设置表名
            //entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = totalCount;
            //entiesContext.SaveChanges();

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
                    //entiesContext.SaveChanges();
                }
            }
            #endregion


            MessageUtil.DoAppendTBDetail($"开始寻找'{dataResChineseName}'XML文件：");

            var allXMLEntires = from entry in archive.Entries.AsParallel()
                                where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 4
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
                //entiesContext.SaveChanges();
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
                    //entiesContext.SaveChanges();
                    break;
                }

                var keyTemp = entry.Key;

                //解压当前的XML文件
                MemoryStream memStream = new MemoryStream(); entry.WriteTo(memStream);
                if (0 == memStream.Length)
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    //entiesContext.SaveChanges();
                    continue;
                }

                //S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                //dynamic entityObject = Activator.CreateInstance(entityObjectType);
                var entityObject = new S_CHINA_PATENT_LAWSPROCESS();
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                entiesContext.S_CHINA_PATENT_LAWSPROCESS.Add(entityObject);
                ////entiesContext.SaveChanges();

                if (memStream.Position > 0) { memStream.Position = 0; }
                XDocument doc = XDocument.Load(memStream);

                #region 具体的入库操作,EF
                //获取所有字段名， 获取字段的配置信息， 对字段值进行复制， 

                //定义命名空间
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.CreateReader().NameTable);
                namespaceManager.AddNamespace("base", "http://www.sipo.gov.cn/XMLSchema/base");
                namespaceManager.AddNamespace("business", "http://www.sipo.gov.cn/XMLSchema/business");
                //namespaceManager.AddNamespace("m", "http://www.w3.org/1998/Math/MathML");
                //namespaceManager.AddNamespace("tbl", "http://oasis-open.org/specs/soextblx");

                var rootElement = doc.Root;
                entityObject.LAW_NO = MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/LAW_NO");
                entityObject.LAW_TITLE_CN = MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/LAW_TITLE_CN");
                entityObject.LAW_TITLE_EN = MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/LAW_TITLE_EN");
                entityObject.LAW_DOC_NUM = MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/LAW_DOC_NUM");
                entityObject.AREA_OF_LAW = MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/AREA_OF_LAW");
                entityObject.APPROVAL_DEPARTMENT = MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/APPROVAL_DEPARTMENT");
                entityObject.APPROVAL_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/APPROVAL_DATE"));
                entityObject.ISSUING_AUTHORITY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/ISSUING_AUTHORITY");
                entityObject.DATE_ISSUED = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/DATE_ISSUED"));
                entityObject.IMPLEMENTATION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/IMPLEMENTATION_DATE"));
                entityObject.SIGN_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/SIGN_DATE"));
                entityObject.EFFECTIVE_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/EFFECTIVE_DATE"));
                entityObject.STATUS = MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/STATUS");
                entityObject.LEVEL_OF_AUTHORITY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/LEVEL_OF_AUTHORITY");
                entityObject.LAW_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//LAW_INFOR/LAW_COUNTRY");

                entityObject.EXIST_XML = "1";
                entityObject.PATH_XML = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + entry.Key;
                entityObject.IMPORT_TIME = System.DateTime.Now;

                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);
                try
                {
                    //entiesContext.SaveChanges();
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");
                }
                catch (Exception ex)
                {
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入失败!!!");
                    throw ex;
                }

                //输出插入记录
                #endregion

                memStream.Dispose();
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
            //entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            //totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = archive.Entries.Count(); ;
            //entiesContext.SaveChanges();


            #region 检查目录内无XML的情况 并入库
            var dirNameSetEntires = (from entry in archive.Entries.AsParallel()
                                     where entry.IsDirectory && CompressUtil.getEntryDepth(entry.Key) == 2
                                     select CompressUtil.removeDirEntrySlash(entry.Key)).Distinct();

            //总文件数实际为包内文件夹数
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

            MessageUtil.DoAppendTBDetail("开始解析'中国法院判例初加工数据：");

            //发现存在XML不存在的情况
            if (dirEntriesWithoutXML.Count() > 0)
            {
                //string msg = "如下压缩包中的文件夹内未发现XML文件：";
                //msg += String.Join(Environment.NewLine, dirEntriesWithoutXML.ToArray());
                //MessageUtil.DoAppendTBDetail(msg);
                //LogHelper.WriteErrorLog(msg);


                foreach (string entryKey in dirEntriesWithoutXML)
                {
                    handledCount++;
                    S_CHINA_COURTCASE_PROCESS entityObject = new S_CHINA_COURTCASE_PROCESS();

                    entityObject.ID = System.Guid.NewGuid().ToString();

                    var pn = CompressUtil.getEntryShortName(entryKey);

                    var childPdfEntry = CompressUtil.getChildEntryWhithSuffix(archive, entryKey, ".PDF");

                    entityObject.EXIST_XML = "0";


                    //是否存在PDF文件
                    if (null == childPdfEntry)
                    {
                        entityObject.EXIST_PDF = "0";
                    }
                    else
                    {
                        entityObject.EXIST_PDF = "1";
                        entityObject.PATH_PDF = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + childPdfEntry.Key;
                    }


                    entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                    entityObject.IMPORT_TIME = System.DateTime.Now;
                    entityObject.ARCHIVE_INNER_PATH = entryKey;
                    entityObject.FILE_PATH = filePath;
                    entiesContext.S_CHINA_COURTCASE_PROCESS.Add(entityObject);
                    //entiesContext.SaveChanges();

                    //importSession.HAS_ERROR = "Y";
                    //IMPORT_ERROR importError = new IMPORT_ERROR() { ID = System.Guid.NewGuid().ToString(), SESSION_ID = importSession.SESSION_ID, IGNORED = "N", ISZIP = "Y", POINTOR = handledCount, ZIP_OR_DIR_PATH = filePath, REIMPORTED = "N", ZIP_PATH = entryKey, OCURREDTIME = System.DateTime.Now, ERROR_MESSAGE = "文件夹中不存在XML" };
                    //importSession.FAILED_COUNT++;
                    //entiesContext.IMPORT_ERROR.Add(importError);
                    ////entiesContext.SaveChanges();
                }
            }
            #endregion


            //MessageUtil.DoAppendTBDetail("开始寻找'中国法院判例初加工数据：");

            var allXMLEntires = from entry in archive.Entries.AsParallel()
                                where !entry.IsDirectory && entry.Key.ToUpper().EndsWith(".XML") && CompressUtil.getEntryDepth(entry.Key) == 3
                                select entry;

            totalCount = allXMLEntires.Count();

            MessageUtil.DoAppendTBDetail("在压缩包中发现" + totalCount + "个待导入XML条目");

            //已处理计数清零
            //handledCount = 0;
            //if (0 == allXMLEntires.Count())
            //{
            //    MessageUtil.DoAppendTBDetail("没有找到XML");
            //    importSession.NOTE = "没有找到XML";
            //    //添加错误信息
            //    entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", filePath, "", ""));
            //    //entiesContext.SaveChanges();
            //}
            #region 循环入库
            foreach (IArchiveEntry entry in allXMLEntires)
            {
                //计数变量
                handledCount++;

                if (forcedStop)
                {
                    MessageUtil.DoAppendTBDetail("强制终止了插入");
                    importSession.NOTE = "用户强制终止了本次插入";
                    //entiesContext.SaveChanges();
                    break;
                }

                var keyTemp = entry.Key;

                //解压当前的XML文件
                MemoryStream memStream = new MemoryStream(); entry.WriteTo(memStream);

                if (0 == memStream.Length)
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    //entiesContext.SaveChanges();
                    continue;
                }

                S_CHINA_COURTCASE_PROCESS entityObject = new S_CHINA_COURTCASE_PROCESS() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                entiesContext.S_CHINA_COURTCASE_PROCESS.Add(entityObject);
                ////entiesContext.SaveChanges();
                try
                {
                    XmlReaderSettings xmlReaderSettings = new XmlReaderSettings { CheckCharacters = false };
                    if (memStream.Position > 0)
                    {
                        memStream.Position = 0;
                    }
                    XmlReader xmlReader = XmlReader.Create(memStream, xmlReaderSettings);
                    xmlReader.MoveToContent();
                    XDocument doc = XDocument.Load(xmlReader);

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
                    entityObject.PN = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/PN");

                    entityObject.C_ID = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_ID");

                    entityObject.C_LEGAL_NUM = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_LEGAL_NUM");
                    entityObject.C_YEAR = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_YEAR");
                    entityObject.C_VERDICTTYPE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_VERDICTTYPE");
                    entityObject.C_TITLE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_TITLE");
                    entityObject.C_NAME = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_NAME");
                    entityObject.C_ORIGIN_PASS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_ORIGIN_PASS");


                    entityObject.C_IP_TYPES = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/CASE/C_IP_TYPES/C_IP_TYPE");


                    entityObject.C_COURT_NAME = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_COURT_NAME");
                    entityObject.C_COURT_PROVINCE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_COURT_PROVINCE");
                    entityObject.C_COURT_CITY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_COURT_CITY");
                    entityObject.C_COURT_CODE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_COURT_CODE");
                    entityObject.C_COURT_LEVEL = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_COURT_LEVEL");
                    entityObject.C_COURT_NUM = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_COURT_NUM");
                    entityObject.C_CASETYPE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_CASETYPE");
                    entityObject.C_SUBBRANCH = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_SUBBRANCH");
                    entityObject.C_VERDICTRESULT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_VERDICTRESULT");
                    entityObject.C_APPELLENTINFOS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_APPELLENTINFOS");
                    entityObject.C_PLAINTIFF_PRES = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_PLAINTIFF_PRES");
                    entityObject.C_PLAINTIFF_AGENT_NAMES = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_PLAINTIFF_AGENT_NAMES");
                    entityObject.C_PLAINTIFF_AGENT_ORGS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_PLAINTIFF_AGENT_ORGS");
                    entityObject.C_APPELLEEINFOS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_APPELLEEINFOS");
                    entityObject.C_DEFENDENT_PRES = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_DEFENDENT_PRES");
                    entityObject.C_DEFENDENT_AGENT_NAMES = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_DEFENDENT_AGENT_NAMES");
                    entityObject.C_DEFENDENT_AGENT_ORGS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_DEFENDENT_AGENT_ORGS");
                    entityObject.C_THIRD_PERSONINFOS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_THIRD_PERSONINFOS");
                    entityObject.C_THIRD_PERSON_PRES = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_THIRD_PERSON_PRES");
                    entityObject.C_THIRD_PERSON_AGENT_NAMES = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_THIRD_PERSON_AGENT_NAMES");
                    entityObject.C_THIRD_PERSON_AGENT_ORGS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_THIRD_PERSON_AGENT_ORGS");
                    entityObject.C_LEGALNUMBER_SUPERIORS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_LEGALNUMBER_SUPERIORS");


                    entityObject.C_STARTDATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_STARTDATE"));


                    entityObject.C_APPLYCOST = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_APPLYCOST");
                    entityObject.C_JUDGECOST = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_JUDGECOST");


                    entityObject.C_PT_NS = MiscUtil.getXElementInnerXMLByXPath(rootElement, "/CASE/C_PT_NS");
                    entityObject.C_PL_NS = MiscUtil.getXElementInnerXMLByXPath(rootElement, "/CASE/C_PL_NS");
                    entityObject.C_TR_NS = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/CASE/C_TR_NS/C_TR_N");


                    entityObject.C_VD_IVFO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_VD_IVFO");
                    entityObject.C_PRE_JUDGE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_PRE_JUDGE");
                    entityObject.C_PROXY_PRE_JUDGE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_PROXY_PRE_JUDGE");
                    entityObject.C_JUDGE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_JUDGE");
                    entityObject.C_PROXY_JUDGE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_PROXY_JUDGE");
                    entityObject.C_JURY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_JURY");
                    entityObject.C_CLERK = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_CLERK");


                    entityObject.C_VERDICTDATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_VERDICTDATE"));


                    entityObject.S_YEAR = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/S_YEAR");
                    entityObject.HOLDINGTYPE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/HOLDINGTYPE");
                    entityObject.LAWREFERENCES = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/LAWREFERENCES");
                    entityObject.EVIDENCES = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/EVIDENCES");
                    entityObject.C_PL_N_KEY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_PL_N_KEY");
                    entityObject.C_PL_N_KEY_STANDARD = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/C_PL_N_KEY_STANDARD");
                    entityObject.ALLTEXT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/ALLTEXT");
                    entityObject.CASEFAMILY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/CASE/CASEFAMILYS");

                    entityObject.EXIST_XML = "1";

                    var childPdfEntry = CompressUtil.getChildEntryWhithSuffix(archive, CompressUtil.getFileEntryParentPath(entry.Key), ".PDF");

                    //是否存在PDF文件
                    if (null == childPdfEntry)
                    {
                        entityObject.EXIST_PDF = "0";
                    }
                    else
                    {
                        entityObject.EXIST_PDF = "1";
                        entityObject.PATH_PDF = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + childPdfEntry.Key;
                    }

                    entityObject.IMPORT_TIME = System.DateTime.Now;




                    //entiesContext.SaveChanges();




                    //输出插入记录
                    var currentValue = MiscUtil.jsonSerilizeObject(entityObject);

                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");




                    #endregion

                    xmlReader.Dispose();
                    memStream.Dispose();
                }
                catch (Exception ex)
                {
                    var message = $"发生错误{ex.Message}，错误详情{ex.StackTrace}";
                    MessageUtil.DoAppendTBDetail(message);
                    LogHelper.WriteErrorLog(message);

                    var importError = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, ex.Message, ex.StackTrace);
                    importSession.HAS_ERROR = "Y";
                    entiesContext.IMPORT_ERROR.Add(importError);
                    //entiesContext.SaveChanges();
                }

                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }
            #endregion 循环入库
        }

        private static void parseZip153(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession, string tableName, string dataResChineseName)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = tableName;//设置表名
            //entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = totalCount;
            //entiesContext.SaveChanges();

            /**
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
                    //entiesContext.SaveChanges();
                }
            }
            #endregion

            **/


            MessageUtil.DoAppendTBDetail($"开始寻找'{dataResChineseName}'XML文件：");

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
                //entiesContext.SaveChanges();
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
                    //entiesContext.SaveChanges();
                    break;
                }

                var keyTemp = entry.Key;

                //解压当前的XML文件
                MemoryStream memStream = new MemoryStream(); entry.WriteTo(memStream);

                if (0 == memStream.Length)
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    //entiesContext.SaveChanges();
                    continue;
                }

                //S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                //dynamic entityObject = Activator.CreateInstance(entityObjectType);
                var entityObject = new S_JOURNAL_PROJECT_ABSTRACT();
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                entiesContext.S_JOURNAL_PROJECT_ABSTRACT.Add(entityObject);
                ////entiesContext.SaveChanges();

                if (memStream.Position > 0) { memStream.Position = 0; }
                XDocument doc = XDocument.Load(memStream);

                #region 具体的入库操作,EF
                //获取所有字段名， 获取字段的配置信息， 对字段值进行复制， 

                //定义命名空间
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.CreateReader().NameTable);
                namespaceManager.AddNamespace("base", "http://www.sipo.gov.cn/XMLSchema/base");
                namespaceManager.AddNamespace("business", "http://www.sipo.gov.cn/XMLSchema/business");
                //namespaceManager.AddNamespace("m", "http://www.w3.org/1998/Math/MathML");
                //namespaceManager.AddNamespace("tbl", "http://oasis-open.org/specs/soextblx");

                var rootElement = doc.Root;

                entityObject.SYSTEM_FIELD_RID = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/system_field/rid");
                entityObject.CLASS_INFO_CLASS_CODE = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/class_info/class_code");
                entityObject.CLASS_INFO_SUBJECT_ALL = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/class_info/subject_all");
                entityObject.CLASS_INFO_ASJC = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/class_info/asjc");
                entityObject.CLASS_INFO_CJCR = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/class_info/cjcr");
                entityObject.CLASS_INFO_CSSCI = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/class_info/cssci");
                entityObject.CLASS_INFO_JCR = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/class_info/jcr");
                entityObject.CLASS_INFO_PKU = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/class_info/pku");
                entityObject.CLASS_INFO_SFX = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/class_info/sfx");
                entityObject.CLASS_INFO_INDEXBY = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/class_info/indexby");
                entityObject.ARTICLE_INFO_TITLE_SOURCE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/article_info/title_source");
                entityObject.ARTICLE_INFO_TITLE_CN = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/article_info/title_cn");
                entityObject.ARTICLE_INFO_TITLE_EN = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/article_info/title_en");
                entityObject.ARTICLE_INFO_VENDOR = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/article_info/vendor");
                entityObject.ARTICLE_INFO_COUNTRY = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/article_info/country");
                entityObject.ARTICLE_INFO_LANGUAGE = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/article_info/language");
                entityObject.ARTICLE_INFO_KEYWORDS_ALL = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/article_info/keywords_all");
                entityObject.ARTICLE_INFO_KEYWORDS_SOURCE = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/article_info/keywords_source");
                entityObject.ARTICLE_INFO_KEYWORDS_CN = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/article_info/keywords_cn");
                entityObject.ARTICLE_INFO_KEYWORDS_EN = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/article_info/keywords_en");
                entityObject.ARTICLE_INFO_ABSTRACT_ALL = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/article_info/abstract_all");
                entityObject.ARTICLE_INFO_ABSTRACT_SOURCE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/article_info/abstract_source");
                entityObject.ARTICLE_INFO_ABSTRACT_CN = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/article_info/abstract_cn");
                entityObject.ARTICLE_INFO_ABSTRACT_EN = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/article_info/abstract_en");
                entityObject.ARTICLE_INFO_FUND = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/article_info/fund");
                entityObject.ARTICLE_INFO_SYS_SCORE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/article_info/sys_score");
                entityObject.ARTICLE_INFO_PAGE_COUNT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/article_info/page_count");
                entityObject.ARTICLE_INFO_PAGES = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/article_info/pages");
                entityObject.RESOURCE_INFO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/resource_info");
                entityObject.FIRST_AUTHOR_SOURCE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/first_author/first_author_source");
                entityObject.FIRST_AUTHOR_CN = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/first_author/first_author_cn");
                entityObject.FIRST_AUTHOR_EN = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/first_author/first_author_en");
                entityObject.AUTHOR_SOURCE = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/author/author_source");
                entityObject.AUTHOR_CN = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/author/author_cn");
                entityObject.AUTHOR_EN = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/cauthor/author_en");
                entityObject.ORG_SOURCE = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/org/org_source");
                entityObject.ORG_CN = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/org/org_cn");
                entityObject.ORG_EN = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/org/org_en");
                entityObject.JOURNAL_INFO_JID = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/journal_information/jid");
                entityObject.JOURNAL_INFO_LITERATURE_ALL = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/journal_information/literature_all");
                entityObject.JOURNAL_INFO_LITERATURE_SOURCE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/journal_information/literature_sources");
                entityObject.JOURNAL_INFO_LITERATURE_CN = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/journal_information/literature_cn");
                entityObject.JOURNAL_INFO_LITERATURE_EN = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/journal_information/literature_en");
                entityObject.JOURNAL_INFO_DOI = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/journal_information/doi");
                entityObject.JOURNAL_INFO_ISSN = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/journal_information/issn");
                entityObject.JOURNAL_INFO_CN = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/journal_information/cn");
                entityObject.JOURNAL_INFO_EISSN = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/root/journals/journal_information/eissn");
                entityObject.JOURNAL_INFO_VOLUME = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/journal_information/volume");
                entityObject.JOURNAL_INFO_ISSUE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/journal_information/issue");
                entityObject.JOURNAL_INFO_MONTH = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/journal_information/month");
                entityObject.JOURNAL_INFO_YEAR = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/journal_information/year");
                entityObject.JOURNAL_INFO_PUB_DATE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/journal_information/pub_date");
                entityObject.DOCUMENT_TYPE_SCHOLARLY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/document_type_info/scholarly");
                entityObject.DOCUMENT_TYPE_CORE_JOURNAL = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/document_type_info/core_journal");
                entityObject.DOCUMENT_TYPE_OPEN_ACCESS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/document_type_info/open_access");
                entityObject.DOCUMENT_TYPE_PEER_REVIEW = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/document_type_info/peer_review");
                entityObject.DOCUMENT_TYPE_FREE_CONTENT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/document_type_info/free_content");
                entityObject.DOCUMENT_TYPE_FUND_SUPPORT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/document_type_info/fund_support");
                entityObject.DOCUMENT_TYPE_DOC_TYPE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/root/journals/document_type_info/doc_type");

                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);
                try
                {
                    //entiesContext.SaveChanges();
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");
                }
                catch (Exception ex)
                {
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入失败!!!");
                    throw ex;
                }

                //输出插入记录
                #endregion

                memStream.Dispose();
                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }
            #endregion 循环入库
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
            //entiesContext.SaveChanges();

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
                        //entiesContext.SaveChanges();
                    }
                }
            }
            MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            //entiesContext.SaveChanges();
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
            //entiesContext.SaveChanges();

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
                        //entiesContext.SaveChanges();
                    }

                }

            }

            //entiesContext.SaveChanges();
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
            //entiesContext.SaveChanges();

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
                        //entiesContext.SaveChanges();
                    }
                }
            }
            MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            //entiesContext.SaveChanges();
        }

        #region 商标相关
        private static void parseZip172(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession, string tableName, string dataResChineseName)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = tableName;//设置表名
            //entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = totalCount;
            //entiesContext.SaveChanges();

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
                    //entiesContext.SaveChanges();
                }
            }
            #endregion


            MessageUtil.DoAppendTBDetail($"开始寻找'{dataResChineseName}'XML文件：");

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
                //entiesContext.SaveChanges();
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
                    //entiesContext.SaveChanges();
                    break;
                }

                var keyTemp = entry.Key;

                //解压当前的XML文件
                MemoryStream memStream = new MemoryStream(); entry.WriteTo(memStream);

                if (0 == memStream.Length)
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    //entiesContext.SaveChanges();
                    continue;
                }

                //S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                //dynamic entityObject = Activator.CreateInstance(entityObjectType);
                var entityObject = new S_MADRID_BRAND_PURCHASE();
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                entiesContext.S_MADRID_BRAND_PURCHASE.Add(entityObject);
                ////entiesContext.SaveChanges();

                if (memStream.Position > 0) { memStream.Position = 0; }
                XDocument doc = XDocument.Load(memStream);

                #region 具体的入库操作,EF
                //获取所有字段名， 获取字段的配置信息， 对字段值进行复制， 

                //定义命名空间
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.CreateReader().NameTable);
                namespaceManager.AddNamespace("base", "http://www.sipo.gov.cn/XMLSchema/base");
                namespaceManager.AddNamespace("business", "http://www.sipo.gov.cn/XMLSchema/business");
                //namespaceManager.AddNamespace("m", "http://www.w3.org/1998/Math/MathML");
                //namespaceManager.AddNamespace("tbl", "http://oasis-open.org/specs/soextblx");

                var rootElement = doc.Root;

                entityObject.INTREGN = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR", "INTREGN");
                entityObject.OOCD = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR", "OOCD");
                entityObject.INTREGD = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR", "INTREGD"));
                entityObject.EXPDATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR", "EXPDATE"));
                entityObject.ORIGLAN = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR", "ORIGLAN");
                entityObject.HOLGR = MiscUtil.getXElementInnerXMLByXPath(rootElement, "//MARKGR/CURRENT/HOLGR");
                entityObject.HOLGR_NAME_NAMEL = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "//MARKGR/CURRENT/HOLGR/NAME/NAMEL");
                entityObject.REPGR = MiscUtil.getXElementInnerXMLByXPath(rootElement, "//MARKGR/CURRENT/REPGR");
                entityObject.PHOLGR = MiscUtil.getXElementInnerXMLByXPath(rootElement, "//MARKGR/CURRENT/PHOLGR");
                entityObject.IMAGE_COLOUR = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR/CURRENT/IMAGE", "COLOUR");
                entityObject.IMAGE_TEXT = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR/CURRENT/IMAGE", "TEXT");
                entityObject.VIENNAGR_VIECLAI = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "//MARKGR/CURRENT/VIENNAGR", "VIECLAI");
                entityObject.VIENNAGR_VIECLA3 = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "//MARKGR/CURRENT/VIENNAGR", "VIECLA3");
                entityObject.THRDMAR = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR/CURRENT/THRDMAR");
                entityObject.SOUMARI = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR/CURRENT/SOUMARI");
                entityObject.TYPMARI = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR/CURRENT/TYPMARI");
                entityObject.COLMARI = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR/CURRENT/COLMARI");
                entityObject.PREREGG = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR/CURRENT/PREREGG");
                entityObject.COLCLAGR = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR/CURRENT/COLCLAGR");
                entityObject.MARDESGR = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR/CURRENT/MARDESGR");
                entityObject.MARTRGR = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR/CURRENT/MARTRGR");
                entityObject.DISCLAIMGR = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR/CURRENT/DISCLAIMGR");


                entityObject.BASICGS = MiscUtil.getXElementInnerXMLByXPath(rootElement, "//MARKGR/CURRENT/BASICGS");


                entityObject.BASICGS_GSGR_NICCLAI = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "//MARKGR/CURRENT/BASICGS/GSGR", "NICCLAI");


                entityObject.BASREGGR = MiscUtil.getXElementInnerXMLByXPath(rootElement, "//MARKGR/CURRENT/BASGR/BASREGGR");
                entityObject.BASAPPGR = MiscUtil.getXElementInnerXMLByXPath(rootElement, "//MARKGR/CURRENT/BASGR/BASAPPGR");
                entityObject.PRIGR = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "//MARKGR/CURRENT/PRIGR");
                entityObject.SENGRP = MiscUtil.getXElementSingleValueByXPath(rootElement, "//MARKGR/CURRENT/SENGRP");
                entityObject.DESAG_DCPCD = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "//MARKGR/CURRENT/DESAG/DCPCD");
                entityObject.DESPG_DCPCD = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "//MARKGR/CURRENT/DESPG/DCPCD");
                entityObject.DESPG2_DCPCD = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "//MARKGR/CURRENT/DESPG2/DCPCD");


                entityObject.EXIST_XML = "1";
                entityObject.PATH_XML = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + entry.Key;
                entityObject.IMPORT_TIME = System.DateTime.Now;

                var markId = CompressUtil.getEntryShortName(CompressUtil.getFileEntryParentPath(entry.Key));



                //var childPdfEntry = CompressUtil.getChildEntryWhithSuffix(archive, CompressUtil.getFileEntryParentPath(entry.Key), ".PDF");
                var picEntry = CompressUtil.getSpecifiedSiblingEntry(archive, entry.Key, markId + ".gif");
                if (null != picEntry)
                {
                    entityObject.EXIST_PIC = "1";
                    entityObject.PATH_PIC = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + CompressUtil.removeDirEntrySlash(picEntry.Key);
                }
                else
                {
                    entityObject.EXIST_PIC = "0";
                }

                var sfPICEntry = CompressUtil.getSpecifiedSiblingEntry(archive, entry.Key, markId + "sf.gif");

                if (null != sfPICEntry)
                {
                    entityObject.EXIST_PIC_SF = "1";
                    entityObject.PATH_PIC_SF = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + CompressUtil.removeDirEntrySlash(sfPICEntry.Key);
                }
                else
                {
                    entityObject.EXIST_PIC_SF = "0";
                }

                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);
                try
                {
                    //entiesContext.SaveChanges();
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

        private static void parseZip139(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession, string tableName, string dataResChineseName)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = tableName;//设置表名
            //entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = totalCount;
            //entiesContext.SaveChanges();

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
                    //entiesContext.SaveChanges();
                }
            }
            #endregion


            MessageUtil.DoAppendTBDetail($"开始寻找'{dataResChineseName}'XML文件：");

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
                //entiesContext.SaveChanges();
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
                    //entiesContext.SaveChanges();
                    break;
                }

                var keyTemp = entry.Key;

                //解压当前的XML文件
                MemoryStream memStream = new MemoryStream(); entry.WriteTo(memStream);

                if (0 == memStream.Length)
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    //entiesContext.SaveChanges();
                    continue;
                }

                //S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                //dynamic entityObject = Activator.CreateInstance(entityObjectType);
                var entityObject = new S_AMERICA_TRANSFER_BRAND();
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                entiesContext.S_AMERICA_TRANSFER_BRAND.Add(entityObject);
                ////entiesContext.SaveChanges();

                if (memStream.Position > 0) { memStream.Position = 0; }
                XDocument doc = XDocument.Load(memStream);

                #region 具体的入库操作,EF
                //获取所有字段名， 获取字段的配置信息， 对字段值进行复制， 

                //定义命名空间
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.CreateReader().NameTable);
                namespaceManager.AddNamespace("base", "http://www.sipo.gov.cn/XMLSchema/base");
                namespaceManager.AddNamespace("business", "http://www.sipo.gov.cn/XMLSchema/business");
                //namespaceManager.AddNamespace("m", "http://www.w3.org/1998/Math/MathML");
                //namespaceManager.AddNamespace("tbl", "http://oasis-open.org/specs/soextblx");

                var rootElement = doc.Root;

                entityObject.VERSION_NO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/trademark-assignments/version/version-no");
                entityObject.VERSION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/trademark-assignments/version/version-date"));
                entityObject.TRANSACTION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/trademark-assignments/transaction-date"));
                entityObject.ASSIGNMENT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/trademark-assignments/assignment-information/assignment-entry/assignment");
                entityObject.ASSIGNMENT_REEL_NO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/trademark-assignments/assignment-information/assignment-entry/assignment/reel-no");
                entityObject.ASSIGNMENT_FRAME_NO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/trademark-assignments/assignment-information/assignment-entry/assignment/frame-no");
                entityObject.ASSIGNMENT_DATE_RECORDED = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/trademark-assignments/assignment-information/assignment-entry/assignment/date-recorded"));
                entityObject.ASSIGNMENT_CONVEYANCE_TEXT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/trademark-assignments/assignment-information/assignment-entry/assignment/conveyance-text");
                entityObject.ASSIGNORS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/trademark-assignments/assignment-information/assignment-entry/assignors");

                entityObject.ASSIGNOR_PERSON_NAME = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/trademark-assignments/assignment-information/assignment-entry/assignors/assignor/person-or-organization-name");

                entityObject.ASSIGNEES = MiscUtil.getXElementInnerXMLByXPath(rootElement, "/trademark-assignments/assignment-information/assignment-entry/assignees");

                entityObject.ASSIGNOR_PERSON_NAME = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/trademark-assignments/assignment-information/assignment-entry/assignees/assignee/person-or-organization-name");

                entityObject.SERIAL_NO = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/trademark-assignments/assignment-information/assignment-entry/properties/propertyregistration-no");

                entityObject.EXIST_XML = "1";
                entityObject.PATH_XML = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + entry.Key;
                entityObject.IMPORT_TIME = System.DateTime.Now;

                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);
                try
                {
                    //entiesContext.SaveChanges();
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

        private static void parseZip138(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession, string tableName, string dataResChineseName)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = tableName;//设置表名
            //entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = totalCount;
            //entiesContext.SaveChanges();

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
                    //entiesContext.SaveChanges();
                }
            }
            #endregion


            MessageUtil.DoAppendTBDetail($"开始寻找'{dataResChineseName}'XML文件：");

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
                //entiesContext.SaveChanges();
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
                    //entiesContext.SaveChanges();
                    break;
                }

                var keyTemp = entry.Key;

                //解压当前的XML文件
                MemoryStream memStream = new MemoryStream(); entry.WriteTo(memStream);

                if (0 == memStream.Length)
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    //entiesContext.SaveChanges();
                    continue;
                }

                //S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                //dynamic entityObject = Activator.CreateInstance(entityObjectType);
                var entityObject = new S_AMERICA_APPLY_BRAND();
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                entiesContext.S_AMERICA_APPLY_BRAND.Add(entityObject);
                ////entiesContext.SaveChanges();

                if (memStream.Position > 0) { memStream.Position = 0; }
                XDocument doc = XDocument.Load(memStream);

                #region 具体的入库操作,EF
                //获取所有字段名， 获取字段的配置信息， 对字段值进行复制， 

                //定义命名空间
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.CreateReader().NameTable);
                namespaceManager.AddNamespace("base", "http://www.sipo.gov.cn/XMLSchema/base");
                namespaceManager.AddNamespace("business", "http://www.sipo.gov.cn/XMLSchema/business");
                //namespaceManager.AddNamespace("m", "http://www.w3.org/1998/Math/MathML");
                //namespaceManager.AddNamespace("tbl", "http://oasis-open.org/specs/soextblx");

                var rootElement = doc.Root;
                entityObject.VERSION_NO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/trademark-applications-daily/version/version-no");
                entityObject.VERSION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/trademark-applications-daily/version/version-date"));
                entityObject.ACTION_KEY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/action-key");
                entityObject.SERIAL_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/serial-number");
                entityObject.REGISTRATION_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/registration-number");
                entityObject.TRANSACTION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/transaction-date"));
                entityObject.HEADER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/case-file-header");
                entityObject.HEADER_FILING_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/case-file-header/filing-date"));
                entityObject.HEADER_REGISTRATION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/case-file-header/registration-date"));
                entityObject.HEADER_STATUS_CODE = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/case-file-header/status-code");
                entityObject.HEADER_STATUS_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/case-file-header/status-date"));
                entityObject.HEADER_MARK_IDENTIFICATION = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/case-file-header/mark-identification");
                entityObject.HEADER_MARK_DRAWING_CODE = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/case-file-header/mark-drawing-code");
                entityObject.HEADER_ABANDONMENT_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/case-file-header/abandonment-date"));
                entityObject.HEADER_CANCELLATION_CODE = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/case-file-header/cancellation-code");
                entityObject.HEADER_CANCELLATION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/case-file-header/cancellation-date"));
                entityObject.STATEMENTS = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/case-file-statements");
                entityObject.EVENT_STATEMENTS = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/case-file-event-statements");
                entityObject.PRIOR_REGISTRATION_APPLICATION = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/prior-registration-applications");
                entityObject.FOREIGN_APPLICATIONS = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/foreign-applications");
                entityObject.CLASSIFICATIONS = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/classifications");
                entityObject.INTERNATIONAL_CODE = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "//action-keys/case-file/classifications/international-code");
                entityObject.CORRESPONDENT = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/correspondent");
                entityObject.CASE_FILE_OWNERS = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/case-file-owners");
                entityObject.DESIGN_SEARCHES = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/design-searches");
                entityObject.DESIGN_SEARCH_CODE = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "//action-keys/case-file/design-searches/code");
                entityObject.INTERNATIONAL_REGISTRATION = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/international-registration");
                entityObject.MADRID_INTER_FILING_REQUESTS = MiscUtil.getXElementSingleValueByXPath(rootElement, "//action-keys/case-file/madrid-international-filing-requests");
                entityObject.EXIST_XML = "1";
                entityObject.PATH_XML = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + entry.Key;
                entityObject.IMPORT_TIME = System.DateTime.Now;

                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);
                try
                {
                    //entiesContext.SaveChanges();
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");
                }
                catch (Exception ex)
                {
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入失败!!!");
                    throw ex;
                }

                //输出插入记录
                #endregion

                if (0 == handledCount % 10000)//每500条写库一次
                {
                    entiesContext.SaveChanges();
                    GC.Collect();
                }

                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }
            #endregion 循环入库
        }

        private static void parseZip137(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession, string tableName, string dataResChineseName)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = tableName;//设置表名
            //entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = totalCount;
            //entiesContext.SaveChanges();

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
                    //entiesContext.SaveChanges();
                }
            }
            #endregion


            MessageUtil.DoAppendTBDetail($"开始寻找'{dataResChineseName}'XML文件：");

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
                //entiesContext.SaveChanges();
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
                    //entiesContext.SaveChanges();
                    break;
                }

                var keyTemp = entry.Key;

                //解压当前的XML文件
                MemoryStream memStream = new MemoryStream(); entry.WriteTo(memStream);

                if (0 == memStream.Length)
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    //entiesContext.SaveChanges();
                    continue;
                }

                //S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                //dynamic entityObject = Activator.CreateInstance(entityObjectType);
                var entityObject = new S_CHINA_WELLKNOWN_BRAND();
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                entiesContext.S_CHINA_WELLKNOWN_BRAND.Add(entityObject);
                ////entiesContext.SaveChanges();

                if (memStream.Position > 0) { memStream.Position = 0; }
                XDocument doc = XDocument.Load(memStream);

                #region 具体的入库操作,EF
                //获取所有字段名， 获取字段的配置信息， 对字段值进行复制， 

                //定义命名空间
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.CreateReader().NameTable);
                namespaceManager.AddNamespace("base", "http://www.sipo.gov.cn/XMLSchema/base");
                namespaceManager.AddNamespace("business", "http://www.sipo.gov.cn/XMLSchema/business");
                //namespaceManager.AddNamespace("m", "http://www.w3.org/1998/Math/MathML");
                //namespaceManager.AddNamespace("tbl", "http://oasis-open.org/specs/soextblx");

                var rootElement = doc.Root;

                entityObject.MARK_CN_ID = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/MARK_CN_ID");
                entityObject.NAMEINFO_NAME = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/NAMEINFO/NAME");
                entityObject.BASEINFO_REGISTRATION_NO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/REGISTRATION-NO");
                entityObject.TRADEMARK_CERTIFICATION = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/WELL-KNOWN-TRADEMARK/CERTIFICATION");
                entityObject.TRADEMARK_AREA = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/WELL-KNOWN-TRADEMARK/AREA");
                entityObject.TRADEMARK_DETERMINATION_ORGAN = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/WELL-KNOWN-TRADEMARK/DETERMINATION_ORGAN");
                entityObject.TRADEMARK_DETERMINATION_METHOD = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/WELL-KNOWN-TRADEMARK/DETERMINATION_METHOD");
                entityObject.TRADEMARK_DETERMINATION_BATCH = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/WELL-KNOWN-TRADEMARK/DETERMINATION_BATCH");
                entityObject.TRADEMARK_DETERMINATION_TIME = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/WELL-KNOWN-TRADEMARK/DETERMINATION_TIME"));
                entityObject.TRADEMARK_WEBSITE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/WELL-KNOWN-TRADEMARK/WEBSITE");
                entityObject.IMPORT_TIME = System.DateTime.Now;

                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);
                try
                {
                    //entiesContext.SaveChanges();
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");
                }
                catch (Exception ex)
                {
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入失败!!!");
                    throw ex;
                }

                //输出插入记录
                #endregion

                if (0 == handledCount % 10000)//每500条写库一次
                {
                    entiesContext.SaveChanges();
                    GC.Collect();
                }

                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }
            #endregion 循环入库
        }

        private static void parseZip136(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession, string tableName, string dataResChineseName)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = tableName;//设置表名
            //entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = totalCount;
            //entiesContext.SaveChanges();

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
                    //entiesContext.SaveChanges();
                }
            }
            #endregion


            MessageUtil.DoAppendTBDetail($"开始寻找'{dataResChineseName}'XML文件：");

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
                //entiesContext.SaveChanges();
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
                    //entiesContext.SaveChanges();
                    break;
                }

                var keyTemp = entry.Key;

                //解压当前的XML文件
                MemoryStream memStream = new MemoryStream(); entry.WriteTo(memStream);

                if (0 == memStream.Length)
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    //entiesContext.SaveChanges();
                    continue;
                }

                //S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                //dynamic entityObject = Activator.CreateInstance(entityObjectType);
                var entityObject = new S_MADRID_BRAND_ENTER_CHINA();
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                entiesContext.S_MADRID_BRAND_ENTER_CHINA.Add(entityObject);
                ////entiesContext.SaveChanges();

                if (memStream.Position > 0) { memStream.Position = 0; }
                XDocument doc = XDocument.Load(memStream);

                #region 具体的入库操作,EF
                //获取所有字段名， 获取字段的配置信息， 对字段值进行复制， 

                //定义命名空间
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.CreateReader().NameTable);
                namespaceManager.AddNamespace("base", "http://www.sipo.gov.cn/XMLSchema/base");
                namespaceManager.AddNamespace("business", "http://www.sipo.gov.cn/XMLSchema/business");
                //namespaceManager.AddNamespace("m", "http://www.w3.org/1998/Math/MathML");
                //namespaceManager.AddNamespace("tbl", "http://oasis-open.org/specs/soextblx");

                var rootElement = doc.Root;


                entityObject.MARK_CN_ID = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/MARK_CN_ID");
                entityObject.NAMEINFO_NAME = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/NAMEINFO/NAME");
                entityObject.BASEINFO_ORIGINAL_LANGUAGE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/ORIGINAL-LANGUAGE");
                entityObject.BASEINFO_REGISTRATION_NO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/REGISTRATION-NO");
                //entityObject.BASEINFO_APPLICATION_NO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/APPLICATION-NO");

                entityObject.BASEINFO_ICN = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/ICN");

                entityObject.BASEINFO_TRADEMARK_TYPE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/TRADEMARK-TYPE");
                entityObject.BASEINFO_CURRENT_STATE_RIGHT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/CURRENT-STATE-RIGHT");
                entityObject.BASEINFO_SPECIFIED_COLOR = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/SPECIFIED-COLOR");
                //entityObject.DATEINFO_APPLICATION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/DATEINFO/APPLICATION-DATE"));
                entityObject.DATEINFO_REG_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/DATEINFO/REG-DATE"));

                entityObject.SPECIAL_RIGHT_STARTDATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/DATEINFO/SPECIAL-RIGHT-STARTDATE"));
                entityObject.SPECIAL_RIGHT_ENDDATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/DATEINFO/SPECIAL-RIGHT-ENDDATE"));
                entityObject.DATEINFO_PRIORITY_RIGHT_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/DATEINFO/PRIORITY-RIGHT-DATE"));
                entityObject.APPLICATION_PERSION_INFOS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/APPLICATION-PERSION-INFOS");
                entityObject.SERVICE_LIST = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/SERVICE-LIST");
                entityObject.OTHERINFO = MiscUtil.getXElementInnerXMLByXPath(rootElement, "/TRADEMARK/OTHERINFO");
                //entityObject.TRADEMARK_NOTICE_STATUS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/OTHERINFO/TRADEMARK-NOTICE-STATUS");


                entityObject.PATH_FILE = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + CompressUtil.removeDirEntrySlash(entry.Key);
                entityObject.EXIST_FILE = "1";

                var markId = CompressUtil.getEntryShortName(CompressUtil.getFileEntryParentPath(entry.Key));

                //var childPdfEntry = CompressUtil.getChildEntryWhithSuffix(archive, CompressUtil.getFileEntryParentPath(entry.Key), ".PDF");
                var jpgEntry = CompressUtil.getChildEntryWhithSuffix(archive, CompressUtil.getFileEntryParentPath(entry.Key), markId + ".jpg");
                var temp = CompressUtil.getSpecifiedSiblingEntry(archive, entry.Key, markId + ".jpg");
                if (null != jpgEntry)
                {
                    entityObject.EXIST_JPG = "1";
                    entityObject.PATH_JPG = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + CompressUtil.removeDirEntrySlash(jpgEntry.Key);
                }
                else
                {
                    entityObject.EXIST_JPG = "0";
                }

                var sfJPGEntry = CompressUtil.getSpecifiedSiblingEntry(archive, entry.Key, markId + "sf.jpg");

                if (null != sfJPGEntry)
                {
                    entityObject.EXIST_JPG_SF = "1";
                    entityObject.PATH_JPG_SF = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + CompressUtil.removeDirEntrySlash(sfJPGEntry.Key);
                }
                else
                {
                    entityObject.EXIST_JPG_SF = "0";
                }

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
                    //entiesContext.SaveChanges();
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");
                }
                catch (Exception ex)
                {
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入失败!!!");
                    throw ex;
                }





                //输出插入记录


                #endregion

                if (0 == handledCount % 10000)//每500条写库一次
                {
                    entiesContext.SaveChanges();
                    GC.Collect();
                }

                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }
            #endregion 循环入库
        }

        private static void parseZip134(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession, string tableName, string dataResChineseName)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = tableName;//设置表名
            //entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = totalCount;
            //entiesContext.SaveChanges();

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
                    //entiesContext.SaveChanges();
                }
            }
            #endregion


            MessageUtil.DoAppendTBDetail($"开始寻找'{dataResChineseName}'XML文件：");

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
                //entiesContext.SaveChanges();
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
                    //entiesContext.SaveChanges();
                    break;
                }

                var keyTemp = entry.Key;

                //解压当前的XML文件
                MemoryStream memStream = new MemoryStream(); entry.WriteTo(memStream);

                if (0 == memStream.Length)
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    //entiesContext.SaveChanges();
                    continue;
                }

                //S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                //dynamic entityObject = Activator.CreateInstance(entityObjectType);
                var entityObject = new S_CHINA_BRAND_TRANSFER();
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                entiesContext.S_CHINA_BRAND_TRANSFER.Add(entityObject);
                ////entiesContext.SaveChanges();

                if (memStream.Position > 0) { memStream.Position = 0; }
                XDocument doc = XDocument.Load(memStream);

                #region 具体的入库操作,EF
                //获取所有字段名， 获取字段的配置信息， 对字段值进行复制， 

                //定义命名空间
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.CreateReader().NameTable);
                namespaceManager.AddNamespace("base", "http://www.sipo.gov.cn/XMLSchema/base");
                namespaceManager.AddNamespace("business", "http://www.sipo.gov.cn/XMLSchema/business");
                //namespaceManager.AddNamespace("m", "http://www.w3.org/1998/Math/MathML");
                //namespaceManager.AddNamespace("tbl", "http://oasis-open.org/specs/soextblx");

                var rootElement = doc.Root;


                entityObject.MARK_CN_ID = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/MARK_CN_ID");
                entityObject.NAMEINFO_NAME = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/NAMEINFO/NAME");
                entityObject.BASEINFO_REGISTRATION_NO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/REGISTRATION-NO");
                entityObject.TRANSFERINFO_SEQUENCE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/TRANSFERINFOSTRANSFERINFO/sequence");
                entityObject.TRANSFERINFO_ZHRGGQH = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/TRANSFERINFOSTRANSFERINFO/ZHRGGQH");
                entityObject.TRANSFERINFO_YM = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/TRANSFERINFOSTRANSFERINFO/YM");
                entityObject.TRANSFERINFO_ZHRR = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/TRANSFERINFOSTRANSFERINFO/ZHRR");
                entityObject.TRANSFERINFO_SHRR = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/TRANSFERINFOSTRANSFERINFO/SHRR");
                entityObject.TRANSFERINFO_ZHGGRQ = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/TRANSFERINFOSTRANSFERINFO/ZHGGRQ"));
                entityObject.TRANSFERINFO_ZHRR_NAME = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/TRADEMARK/TRANSFERINFOS/TRANSFERINFO/ZHRR/NAME-ZH") + ";;" + MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/TRADEMARK/TRANSFERINFOS/TRANSFERINFO/ZHRR/NAME-EN");
                entityObject.TRANSFERINFO_SHRR_NAME = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/TRADEMARK/TRANSFERINFOS/TRANSFERINFO/SHRR/NAME-ZH") + ";;" + MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/TRADEMARK/TRANSFERINFOS/TRANSFERINFO/SHRR/NAME-EN");

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
                    //entiesContext.SaveChanges();
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");
                }
                catch (Exception ex)
                {
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入失败!!!");
                    throw ex;
                }





                //输出插入记录


                #endregion

                if (0 == handledCount % 10000)//每500条写库一次
                {
                    entiesContext.SaveChanges();
                    GC.Collect();
                }

                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }
            #endregion 循环入库
        }

        private static void parseZip133(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession, string tableName, string dataResChineseName)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = tableName;//设置表名
            //entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = totalCount;
            //entiesContext.SaveChanges();

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
                    //entiesContext.SaveChanges();
                }
            }
            #endregion


            MessageUtil.DoAppendTBDetail($"开始寻找'{dataResChineseName}'XML文件：");

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
                //entiesContext.SaveChanges();
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
                    //entiesContext.SaveChanges();
                    break;
                }

                var keyTemp = entry.Key;

                //解压当前的XML文件
                MemoryStream memStream = new MemoryStream(); entry.WriteTo(memStream);

                if (0 == memStream.Length)
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    //entiesContext.SaveChanges();
                    continue;
                }

                //S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                //dynamic entityObject = Activator.CreateInstance(entityObjectType);
                var entityObject = new S_CHINA_BRAND_LICENSE();
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                entiesContext.S_CHINA_BRAND_LICENSE.Add(entityObject);
                ////entiesContext.SaveChanges();

                if (memStream.Position > 0) { memStream.Position = 0; }
                XDocument doc = XDocument.Load(memStream);

                #region 具体的入库操作,EF
                //获取所有字段名， 获取字段的配置信息， 对字段值进行复制， 

                //定义命名空间
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.CreateReader().NameTable);
                namespaceManager.AddNamespace("base", "http://www.sipo.gov.cn/XMLSchema/base");
                namespaceManager.AddNamespace("business", "http://www.sipo.gov.cn/XMLSchema/business");
                //namespaceManager.AddNamespace("m", "http://www.w3.org/1998/Math/MathML");
                //namespaceManager.AddNamespace("tbl", "http://oasis-open.org/specs/soextblx");

                var rootElement = doc.Root;


                entityObject.MARK_CN_ID = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/MARK_CN_ID");
                entityObject.NAMEINFO_NAME = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/NAMEINFO/NAME");
                entityObject.BASEINFO_REGISTRATION_NO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/REGISTRATION-NO");
                entityObject.LICENSEPROCESSINFO_SEQUENCE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/LICENSEPROCESSINFOS/LICENSEPROCESSINFO/sequence");
                entityObject.LICENSEPROCESSINFO_XKBAGGQH = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/LICENSEPROCESSINFOS/LICENSEPROCESSINFO/XKBAGGQH");
                entityObject.LICENSEPROCESSINFO_PAGENUM = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/LICENSEPROCESSINFOS/LICENSEPROCESSINFO/PAGENUM");
                entityObject.LICENSEPROCESSINFO_GGTYPE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/LICENSEPROCESSINFOS/LICENSEPROCESSINFO/GGTYPE");
                entityObject.LICENSEPROCESSINFO_BAH = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/LICENSEPROCESSINFOS/LICENSEPROCESSINFO/BAH");
                entityObject.LICENSEPROCESSINFO_XKQX = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/LICENSEPROCESSINFOS/LICENSEPROCESSINFO/XKQX");
                entityObject.LICENSEPROCESSINFO_XKR = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/LICENSEPROCESSINFOS/LICENSEPROCESSINFO/XKR");
                entityObject.LICENSEPROCESSINFO_BXKR = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/LICENSEPROCESSINFOS/LICENSEPROCESSINFO/BXKR");
                entityObject.LICENSEPROCESSINFO_XKGS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/LICENSEPROCESSINFOS/LICENSEPROCESSINFO/XKGS");
                entityObject.LICENSEPROCESSINFO_XKBAGGRQ = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/LICENSEPROCESSINFOS/LICENSEPROCESSINFO/XKBAGGRQ"));
                entityObject.LICENSEPROCESSINFO_XKR_NAME = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/TRADEMARK/LICENSEPROCESSINFOS/LICENSEPROCESSINFO/XKR/NAME-ZH") + ";;" + MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/TRADEMARK/LICENSEPROCESSINFOS/LICENSEPROCESSINFO/XKR/NAME-EN");
                entityObject.LICENSEPROCESSINFO_BXKR_NAME = MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "/TRADEMARK/LICENSEPROCESSINFOS/LICENSEPROCESSINFO/BXKR/NAME-ZH") + ";;" + MiscUtil.getXElementMultiValueByXPathSepratedByDoubleColon(rootElement, "TRADEMARK/LICENSEPROCESSINFOS/LICENSEPROCESSINFO/BXKR/NAME-EN");

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
                    //entiesContext.SaveChanges();
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");
                }
                catch (Exception ex)
                {
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入失败!!!");
                    throw ex;
                }





                //输出插入记录


                #endregion

                if (0 == handledCount % 10000)//每500条写库一次
                {
                    entiesContext.SaveChanges();
                    GC.Collect();
                }

                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }
            #endregion 循环入库
        }

        private static void parseZip132(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession, string tableName)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = tableName;//设置表名
            //entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = totalCount;
            //entiesContext.SaveChanges();

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
                    //entiesContext.SaveChanges();
                }
            }
            #endregion


            MessageUtil.DoAppendTBDetail("开始寻找'中国商标'XML文件：");

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
                //entiesContext.SaveChanges();
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
                    //entiesContext.SaveChanges();
                    break;
                }

                var keyTemp = entry.Key;

                //解压当前的XML文件
                MemoryStream memStream = new MemoryStream(); entry.WriteTo(memStream);

                if (0 == memStream.Length)
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    //entiesContext.SaveChanges();
                    continue;
                }

                //S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                //dynamic entityObject = Activator.CreateInstance(entityObjectType);
                var entityObject = new S_CHINA_BRAND();
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                entiesContext.S_CHINA_BRAND.Add(entityObject);
                ////entiesContext.SaveChanges();

                if (memStream.Position > 0) { memStream.Position = 0; }
                XDocument doc = XDocument.Load(memStream);

                #region 具体的入库操作,EF
                //获取所有字段名， 获取字段的配置信息， 对字段值进行复制， 

                //定义命名空间
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.CreateReader().NameTable);
                namespaceManager.AddNamespace("base", "http://www.sipo.gov.cn/XMLSchema/base");
                namespaceManager.AddNamespace("business", "http://www.sipo.gov.cn/XMLSchema/business");
                //namespaceManager.AddNamespace("m", "http://www.w3.org/1998/Math/MathML");
                //namespaceManager.AddNamespace("tbl", "http://oasis-open.org/specs/soextblx");

                var rootElement = doc.Root;


                entityObject.MARK_CN_ID = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/MARK_CN_ID");
                entityObject.NAMEINFO_NAME = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/NAMEINFO/NAME");
                entityObject.BASEINFO_ORIGINAL_LANGUAGE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/ORIGINAL-LANGUAGE");
                entityObject.BASEINFO_REGISTRATION_NO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/REGISTRATION-NO");
                entityObject.BASEINFO_APPLICATION_NO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/APPLICATION-NO");

                var icn = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/ICN");
                if (!string.IsNullOrEmpty(icn.Trim()))
                {
                    try
                    {
                        entityObject.BASEINFO_ICN = new decimal?(new decimal(int.Parse(icn)));
                    }
                    catch (Exception)
                    {
                    }
                }

                entityObject.BASEINFO_TRADEMARK_TYPE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/TRADEMARK-TYPE");
                entityObject.BASEINFO_CURRENT_STATE_RIGHT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/CURRENT-STATE-RIGHT");
                entityObject.BASEINFO_SPECIFIED_COLOR = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/BASEINFO/SPECIFIED-COLOR");
                entityObject.DATEINFO_APPLICATION_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/DATEINFO/APPLICATION-DATE"));
                entityObject.DATEINFO_REG_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/DATEINFO/REG-DATE"));

                entityObject.SPECIAL_RIGHT_STARTDATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/DATEINFO/SPECIAL-RIGHT-STARTDATE"));
                entityObject.SPECIAL_RIGHT_ENDDATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/DATEINFO/SPECIAL-RIGHT-ENDDATE"));
                entityObject.DATEINFO_PRIORITY_RIGHT_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/DATEINFO/PRIORITY-RIGHT-DATE"));
                entityObject.APPLICATION_PERSION_INFOS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/APPLICATION-PERSION-INFOS");
                entityObject.SERVICE_LIST = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/SERVICE-LIST");
                entityObject.OTHERINFO = MiscUtil.getXElementInnerXMLByXPath(rootElement, "/TRADEMARK/OTHERINFO");
                entityObject.TRADEMARK_NOTICE_STATUS = MiscUtil.getXElementSingleValueByXPath(rootElement, "/TRADEMARK/OTHERINFO/TRADEMARK-NOTICE-STATUS");


                entityObject.PATH_FILE = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + CompressUtil.removeDirEntrySlash(entry.Key);
                entityObject.EXIST_FILE = "1";

                var markId = CompressUtil.getEntryShortName(CompressUtil.getFileEntryParentPath(entry.Key));

                //var childPdfEntry = CompressUtil.getChildEntryWhithSuffix(archive, CompressUtil.getFileEntryParentPath(entry.Key), ".PDF");
                var jpgEntry = CompressUtil.getChildEntryWhithSuffix(archive, CompressUtil.getFileEntryParentPath(entry.Key), markId + ".jpg");
                //var temp = CompressUtil.getSpecifiedSiblingEntry(archive, entry.Key, markId + ".jpg");
                if (null != jpgEntry)
                {
                    entityObject.EXIST_JPG = "1";
                    entityObject.PATH_JPG = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + CompressUtil.removeDirEntrySlash(jpgEntry.Key);
                }
                else
                {
                    entityObject.EXIST_JPG = "0";
                }

                var sfJPGEntry = CompressUtil.getSpecifiedSiblingEntry(archive, entry.Key, markId + "sf.jpg");

                if (null != sfJPGEntry)
                {
                    entityObject.EXIST_JPG_SF = "1";
                    entityObject.PATH_JPG_SF = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + CompressUtil.removeDirEntrySlash(sfJPGEntry.Key);
                }
                else
                {
                    entityObject.EXIST_JPG_SF = "0";
                }

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
                    //entiesContext.SaveChanges();
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");
                }
                catch (Exception ex)
                {
                    MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入失败!!!");
                    throw ex;
                }





                //输出插入记录


                #endregion

                if (0 == handledCount % 10000)//每500条写库一次
                {
                    entiesContext.SaveChanges();
                    GC.Collect();
                }

                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }
            #endregion 循环入库
        }
        #endregion

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
            //entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            using (IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath))
            {
                //总条目数
                importSession.IS_ZIP = "Y";
                totalCount = archive.Entries.Count();
                importSession.ZIP_ENTRIES_COUNT = totalCount;
                //entiesContext.SaveChanges();

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
                        //entiesContext.SaveChanges();
                    }
                }
                #endregion


                MessageUtil.DoAppendTBDetail($"开始寻找'{importSession.TABLENAME}'XML文件：");

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
                    //entiesContext.SaveChanges();
                }

                #region 循环入库
                var buffer = new BufferBlock<Tuple<IArchiveEntry, Stream>>();
                var consumer = parseZipUniversalSTA_ConsumeAsync(buffer, importSession, filePath, entiesContext, dbSet, entityObjectType);
                Work_Producer(buffer, allXMLEntires);

                consumer.Wait();

                #endregion 循环入库

            }
        }

        static async Task<int> parseZipUniversalSTA_ConsumeAsync(ISourceBlock<Tuple<IArchiveEntry, Stream>> source, IMPORT_SESSION importSession, string filePath, DataSourceEntities entiesContext, dynamic dbSet, Type entityObjectType)
        {
            int handledCount = 0;

            while (await source.OutputAvailableAsync())
            {
                Tuple<IArchiveEntry, Stream> tis = source.Receive();
                IArchiveEntry iae = tis.Item1;
                Stream ms = tis.Item2;

                //计数变量
                handledCount++;

                var keyTemp = iae.Key;


                dynamic entityObject = Activator.CreateInstance(entityObjectType);
                entityObject.ID = System.Guid.NewGuid().ToString();

                entityObject.IMPORT_SESSION_ID = importSession.SESSION_ID;
                entityObject.ARCHIVE_INNER_PATH = iae.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                //entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                dbSet.Add(entityObject);
                ////entiesContext.SaveChanges();


                XDocument doc = XDocument.Load(ms);

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
                entityObject.STA_PUB_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                entityObject.STA_PUB_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:DocNumber", "", namespaceManager);
                entityObject.STA_PUB_KIND = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:Kind", "", namespaceManager);
                entityObject.STA_PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:Date", "", namespaceManager));


                entityObject.ORI_PUB_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                entityObject.ORI_PUB_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:DocNumber", "", namespaceManager);
                entityObject.ORI_PUB_KIND = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:Kind", "", namespaceManager);
                entityObject.ORI_PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:Date", "", namespaceManager));


                entityObject.STA_APP_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager); ;
                entityObject.STA_APP_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:DocNumber", "", namespaceManager);
                entityObject.STA_APP_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:Date", "", namespaceManager));


                entityObject.ORI_APP_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                entityObject.ORI_APP_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:DocNumber", "", namespaceManager);
                entityObject.ORI_APP_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:Date", "", namespaceManager));


                entityObject.DESIGN_PATENTNUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBibliographicData/business:PatentNumber", "", namespaceManager);

                entityObject.CLASSIFICATIONIPCR = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ClassificationIPCRDetails/business:ClassificationIPCR[@sequence='1']/base:Text", "", namespaceManager);

                entityObject.CLASSIFICATIONLOCARNO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBibliographicData/business:ClassificationLocarno/business:MainClassification[1]", "", namespaceManager);
                if (!string.IsNullOrEmpty(entityObject.CLASSIFICATIONLOCARNO))
                {
                    //entityObject.EDITIONSTATEMENT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBibliographicData/business:ClassificationLocarno/base:EditionStatement", "", namespaceManager);
                    //entityObject.MAINCLASSIFICATION = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBibliographicData/business:ClassificationLocarno/business:MainClassification[1]", "", namespaceManager);
                }

                entityObject.INVENTIONTITLE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:BibliographicData/business:InventionTitle", "", namespaceManager);
                if (string.IsNullOrEmpty(entityObject.INVENTIONTITLE))
                    entityObject.INVENTIONTITLE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBibliographicData/business:DesignTitle", "", namespaceManager);

                entityObject.ABSTRACT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:Abstract/base:Paragraphs", "", namespaceManager);

                entityObject.DESIGNBRIEFEXPLANATION = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBriefExplanation", "", namespaceManager);
                entityObject.FULLDOCIMAGE_NUMBEROFFIGURES = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:FullDocImagenumberOfFigures", "", namespaceManager);
                entityObject.FULLDOCIMAGE_TYPE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:FullDocImage/type", "", namespaceManager);


                entityObject.PATH_STA_FULLTEXT = MiscUtil.getRelativeFilePathInclude(filePath, 2) + Path.DirectorySeparatorChar + CompressUtil.getFileEntryParentPath(iae.Key);

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



                //输出插入记录
                #endregion

                ms.Close();

                if (0 == handledCount % 30000)//每500条写库一次
                {
                    entiesContext.SaveChanges();
                    GC.Collect();
                }

                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }

            return handledCount;
        }

        private static void parseZip06(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = "S_CHINA_PATENT_BIBLIOGRAPHIC".ToUpper();
            //entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = totalCount;
            //entiesContext.SaveChanges();

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
                //entiesContext.SaveChanges();
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
                    //entiesContext.SaveChanges();
                    break;
                }

                var keyTemp = entry.Key;

                //解压当前的XML文件
                MemoryStream memStream = new MemoryStream(); entry.WriteTo(memStream);

                if (0 == memStream.Length)
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    //entiesContext.SaveChanges();
                    continue;
                }

                S_CHINA_PATENT_BIBLIOGRAPHIC entityObject = new S_CHINA_PATENT_BIBLIOGRAPHIC() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                entiesContext.S_CHINA_PATENT_BIBLIOGRAPHIC.Add(entityObject);
                ////entiesContext.SaveChanges();

                if (memStream.Position > 0) { memStream.Position = 0; }
                XDocument doc = XDocument.Load(memStream);

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
                entityObject.STA_PUB_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                entityObject.STA_PUB_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:DocNumber", "", namespaceManager);
                entityObject.STA_PUB_KIND = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:Kind", "", namespaceManager);
                entityObject.STA_PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:Date", "", namespaceManager));


                entityObject.ORI_PUB_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                entityObject.ORI_PUB_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:DocNumber", "", namespaceManager);
                entityObject.ORI_PUB_KIND = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:Kind", "", namespaceManager);
                entityObject.ORI_PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:Date", "", namespaceManager));


                entityObject.STA_APP_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager); ;
                entityObject.STA_APP_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:DocNumber", "", namespaceManager);
                entityObject.STA_APP_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:Date", "", namespaceManager));


                entityObject.ORI_APP_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                entityObject.ORI_APP_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:DocNumber", "", namespaceManager);
                entityObject.ORI_APP_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:Date", "", namespaceManager));


                entityObject.DESIGN_PATENTNUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBibliographicData/business:PatentNumber", "", namespaceManager);

                entityObject.CLASSIFICATIONIPCR = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ClassificationIPCRDetails/business:ClassificationIPCR[@sequence='1']/base:Text", "", namespaceManager);

                entityObject.CLASSIFICATIONLOCARNO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBibliographicData/business:ClassificationLocarno", "", namespaceManager);

                entityObject.INVENTIONTITLE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:BibliographicData/business:InventionTitle", "", namespaceManager);

                entityObject.ABSTRACT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:Abstract/base:Paragraphs", "", namespaceManager);

                entityObject.DESIGNBRIEFEXPLANATION = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBriefExplanation", "", namespaceManager);
                entityObject.FULLDOCIMAGE_NUMBEROFFIGURES = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:FullDocImagenumberOfFigures", "", namespaceManager);
                entityObject.FULLDOCIMAGE_TYPE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:FullDocImage/type", "", namespaceManager);


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

                //entiesContext.SaveChanges();


                //输出插入记录
                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);
                MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");
                #endregion

                memStream.Dispose();

                if (0 == handledCount % 10000)//每500条写库一次
                {
                    entiesContext.SaveChanges();
                    GC.Collect();
                }


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
            //entiesContext.SaveChanges();

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
                    //entiesContext.SaveChanges();
                }
            }
            //entiesContext.SaveChanges();
        }

        private static void parseZip04(string zipFilePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = "S_CHINA_PATENT_STAND_TEXTIMAGE".ToUpper();
            //entiesContext.SaveChanges();

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
            //entiesContext.SaveChanges();

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
                entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", zipFilePath, "", ""));
                //entiesContext.SaveChanges();
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
                    //entiesContext.SaveChanges();
                    break;
                }

                var keyTemp = entry.Key;

                #region 解压当前的XML文件
                MemoryStream memStream = new MemoryStream(); entry.WriteTo(memStream);

                if (0 == memStream.Length)
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{zipFilePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", zipFilePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    //entiesContext.SaveChanges();
                    continue;
                }
                #endregion 


                //初始化Entity, 添加控制信息
                S_CHINA_PATENT_STAND_TEXTIMAGE entityObject = new S_CHINA_PATENT_STAND_TEXTIMAGE() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = zipFilePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                entiesContext.S_CHINA_PATENT_STAND_TEXTIMAGE.Add(entityObject);
                ////entiesContext.SaveChanges();





                if (memStream.Position > 0) { memStream.Position = 0; }
                XDocument doc = XDocument.Load(memStream);

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

                entityObject.STA_PUB_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated", "country", namespaceManager);
                entityObject.STA_PUB_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated", "docNumber", namespaceManager);
                //公告类型
                //entityObject.STA_PUB_KIND = string.IsNullOrEmpty(entityObject.STA_PUB_NUMBER) ? "" : entityObject.STA_PUB_NUMBER.Last().ToString();
                entityObject.STA_PUB_KIND = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated", "kind", namespaceManager);
                var pubDateStr = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated", "datePublication", namespaceManager);

                entityObject.STA_PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(pubDateStr);


                entityObject.STA_NUMBEROFFIGURES = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:FullDocImage", "numberOfFigures", namespaceManager);
                entityObject.STA_TYPE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:FullDocImage", "type", namespaceManager);


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

                //entiesContext.SaveChanges();

                //输出插入记录
                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);

                MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");




                #endregion
                memStream.Dispose();


                if (0 == handledCount % 10000)//每500条写库一次
                {
                    entiesContext.SaveChanges();
                    GC.Collect();
                }

                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, zipFilePath);
            }
            #endregion 循环入库
        }

        /*
        private static void parseZip03(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
        {
            handledCount = 0;
            importStartTime = importSession.START_TIME.Value;

            importSession.TABLENAME = "S_China_Patent_StandardFullTxt".ToUpper();
            //entiesContext.SaveChanges();

            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;
            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            //总条目数
            importSession.IS_ZIP = "Y";
            totalCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = totalCount;
            //entiesContext.SaveChanges();

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
                //entiesContext.SaveChanges();
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
                    //entiesContext.SaveChanges();
                    break;
                }

                var keyTemp = entry.Key;

                //解压当前的XML文件
                MemoryStream memStream = new MemoryStream();entry.WriteTo(memStream);

                if (0 == memStream.Length)
                {
                    MessageUtil.DoAppendTBDetail("----------当前条目：" + entry.Key + "解压失败!!!,跳过本条目");
                    LogHelper.WriteErrorLog($"----------当前条目:{filePath}{Path.DirectorySeparatorChar}{entry.Key}解压失败!!!");
                    importSession.FAILED_COUNT++;
                    IMPORT_ERROR errorTemp = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, entry.Key, "解压失败!");
                    entiesContext.IMPORT_ERROR.Add(errorTemp);
                    //entiesContext.SaveChanges();
                    continue;
                }

                S_CHINA_PATENT_STANDARDFULLTXT entityObject = new S_CHINA_PATENT_STANDARDFULLTXT() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                entityObject.ARCHIVE_INNER_PATH = entry.Key;
                entityObject.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;
                entiesContext.S_CHINA_PATENT_STANDARDFULLTXT.Add(entityObject);
                ////entiesContext.SaveChanges();

                if (memStream.Position > 0){ memStream.Position = 0;}                XDocument doc = XDocument.Load(memStream);

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
                entityObject.STA_PUB_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                entityObject.STA_PUB_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:DocNumber", "", namespaceManager);
                entityObject.STA_PUB_KIND = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:Kind[@]", "", namespaceManager);
                entityObject.STA_PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='standard']/base:DocumentID/base:Date", "", namespaceManager));


                entityObject.ORI_PUB_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                entityObject.ORI_PUB_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:DocNumber", "", namespaceManager);
                entityObject.ORI_PUB_KIND = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:Kind", "", namespaceManager);
                entityObject.ORI_PUB_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:PublicationReference[@dataFormat='original']/base:DocumentID/base:Date", "", namespaceManager));


                entityObject.STA_APP_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:WIPOST3Code", "", namespaceManager); ;
                entityObject.STA_APP_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:DocNumber", "", namespaceManager);
                entityObject.STA_APP_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='standard']/base:DocumentID/base:Date", "", namespaceManager));


                entityObject.ORI_APP_COUNTRY = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:WIPOST3Code", "", namespaceManager);
                entityObject.ORI_APP_NUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:DocNumber", "", namespaceManager);
                entityObject.ORI_APP_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ApplicationReference[@dataFormat='original']/base:DocumentID/base:Date", "", namespaceManager));


                entityObject.DESIGN_PATENTNUMBER = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBibliographicData/business:PatentNumber", "", namespaceManager);

                entityObject.CLASSIFICATIONIPCR = MiscUtil.getXElementSingleValueByXPath(rootElement, "//business:ClassificationIPCRDetails/business:ClassificationIPCR[@sequence='1']/base:Text", "", namespaceManager);

                entityObject.CLASSIFICATIONLOCARNO = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBibliographicData/business:ClassificationLocarno", "", namespaceManager);

                entityObject.INVENTIONTITLE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:BibliographicData/business:InventionTitle", "", namespaceManager);

                entityObject.ABSTRACT = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:Abstract/base:Paragraphs", "", namespaceManager);

                entityObject.DESIGNBRIEFEXPLANATION = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBriefExplanation", "", namespaceManager);
                entityObject.FULLDOCIMAGE_NUMBEROFFIGURES = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:FullDocImagenumberOfFigures", "", namespaceManager);
                entityObject.FULLDOCIMAGE_TYPE = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:FullDocImage/type", "", namespaceManager);


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

                //entiesContext.SaveChanges();


                //输出插入记录
                var currentValue = MiscUtil.jsonSerilizeObject(entityObject);

                MessageUtil.DoAppendTBDetail("记录：" + currentValue + "插入成功!!!");

                #endregion

                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }
            #endregion 循环入库
        }
        */

        private static void parseZip02(string filePath, DataSourceEntities entiesContext, IMPORT_SESSION importSession)
        {
            importSession.TABLENAME = "s_China_Patent_Textimage".ToUpper();
            //entiesContext.SaveChanges();


            importStartTime = System.DateTime.Now;

            FileInfo selectedFileInfo = new FileInfo(filePath);


            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;

            IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath);

            importSession.IS_ZIP = "Y";
            int zipEntriesCount = archive.Entries.Count();
            importSession.ZIP_ENTRIES_COUNT = zipEntriesCount;
            //entiesContext.SaveChanges();

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
            //entiesContext.SaveChanges();

            importStartTime = System.DateTime.Now;

            //清零
            handledCount = 0;


            SharpCompress.Common.ArchiveEncoding.Default = System.Text.Encoding.Default;

            using (IArchive archive = SharpCompress.Archive.ArchiveFactory.Open(@filePath))
            {

                importSession.IS_ZIP = "Y";
                //entiesContext.SaveChanges();

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
                        //entiesContext.SaveChanges();
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

                var buffer = new BufferBlock<Tuple<IArchiveEntry, Stream>>();
                var consumer = parseZip01_ConsumeAsync(buffer, importSession, filePath, entiesContext);
                Work_Producer(buffer, allXMLEntires);

                consumer.Wait();

                #endregion 循环入库


                if (0 == allXMLEntires.Count())
                {
                    MessageUtil.DoAppendTBDetail("没有找到XML");
                    importSession.NOTE = "没有找到XML";
                    //添加错误信息
                    entiesContext.IMPORT_ERROR.Add(MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "N", filePath, "", ""));
                    //entiesContext.SaveChanges();
                }
            }
        }

        static void Work_Producer(ITargetBlock<Tuple<IArchiveEntry, Stream>> target, ParallelQuery<IArchiveEntry> entries)
        {
 
            foreach(var entry in entries)
            {
                MemoryStream ms = new MemoryStream();
                entry.WriteTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                Tuple<IArchiveEntry, Stream> tis = new Tuple<IArchiveEntry, Stream>(entry, ms);
                target.Post(tis);
            }

            target.Complete();
        }


        static async Task<int> parseZip01_ConsumeAsync(ISourceBlock<Tuple<IArchiveEntry, Stream>> source, IMPORT_SESSION importSession, string filePath, DataSourceEntities entiesContext)
        {
            int handledCount = 0;

            while (await source.OutputAvailableAsync())
            {
                Tuple<IArchiveEntry, Stream> tis = source.Receive();
                IArchiveEntry iae = tis.Item1;
                Stream ms = tis.Item2;

                //计数变量
                handledCount++;

                var keyTemp = iae.Key;


                S_CHINA_PATENT_TEXTCODE sCNPatentTextCode = new S_CHINA_PATENT_TEXTCODE() { ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = importSession.SESSION_ID };
                sCNPatentTextCode.ARCHIVE_INNER_PATH = iae.Key;
                sCNPatentTextCode.FILE_PATH = filePath;
                //sCNPatentTextCode.SESSION_INDEX = handledCount;

                entiesContext.S_CHINA_PATENT_TEXTCODE.Add(sCNPatentTextCode);
                //ccbag.Add(sCNPatentTextCode);

                //XDocument doc = XDocument.Load(entryFullPath);
                XDocument doc = XDocument.Load(ms);

                #region 具体的入库操作,EF
                //获取所有字段名， 获取字段的配置信息， 对字段值进行复制， 
                //appl-type
                var rootElement = doc.Root;

                var appl_type = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/application-reference", "appl-type");
                sCNPatentTextCode.APPL_TYPE = appl_type;



                var pub_country = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/cn-publication-reference/document-id/country");
                sCNPatentTextCode.PUB_COUNTRY = pub_country;

                var pub_number = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/cn-publication-reference/document-id/doc-number");
                sCNPatentTextCode.PUB_NUMBER = pub_number;

                var pub_date = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/cn-publication-reference/document-id/date");

                try
                {
                    sCNPatentTextCode.PUB_DATE = DateTime.ParseExact(pub_date, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
                }
                catch (Exception)
                {
                }


                var pub_kind = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/cn-publication-reference/document-id/kind");
                sCNPatentTextCode.PUB_KIND = pub_kind;

                var gazette_num = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/cn-publication-reference/gazette-reference/gazette-num");
                sCNPatentTextCode.GAZETTE_NUM = gazette_num;

                var gazette_date = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/cn-publication-reference/gazette-reference/date");

                try
                {
                    sCNPatentTextCode.GAZETTE_DATE = MiscUtil.pareseDateTimeExactUseCurrentCultureInfo(gazette_date);
                }
                catch (Exception)
                {
                }

                var app_country = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/application-reference/document-id/country");
                sCNPatentTextCode.APP_COUNTRY = app_country;

                var app_number = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/application-reference/document-id/doc-number");
                sCNPatentTextCode.APP_NUMBER = app_number;


                var app_date = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/application-reference/document-id/date");
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
                    classification_ipcr = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/classifications-ipcr/classification-ipcr/text");
                }

                sCNPatentTextCode.CLASSIFICATION_IPCR = classification_ipcr;

                var invention_title = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/invention-title");
                if(string.IsNullOrEmpty(invention_title))
                {
                    invention_title = MiscUtil.getXElementSingleValueByXPath(rootElement, "/business:PatentDocumentAndRelated/business:DesignBibliographicData/business:DesignTitle");
                }

                sCNPatentTextCode.INVENTION_TITLE = invention_title;

                var abstractEle = MiscUtil.getXElementSingleValueByXPath(rootElement, "/cn-patent-document/cn-bibliographic-data/abstract");



                sCNPatentTextCode.ABSTRACT = abstractEle;
                sCNPatentTextCode.PATH_XML = iae.Key;
                sCNPatentTextCode.EXIST_XML = "1";
                sCNPatentTextCode.IMPORT_TIME = System.DateTime.Now;


                #endregion

                ms.Close();
                ms.Dispose();

                try
                {
                    if (0 == handledCount % 30000)//每500条写库一次
                    {
                        entiesContext.SaveChanges();
                        GC.Collect();
                    }
                }
                catch (Exception ex)
                {
                    var importError = MiscUtil.getImpErrorInstance(importSession.SESSION_ID, "Y", filePath, iae.Key, ex.Message, ex.StackTrace);
                }


                //更新进度信息
                MessageUtil.DoupdateProgressIndicator(totalCount, handledCount, 0, 0, filePath);
            }

            return handledCount;
        }

        #endregion 入库逻辑
    }
}
