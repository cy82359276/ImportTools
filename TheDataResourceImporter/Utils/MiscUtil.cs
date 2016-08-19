using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpCompress;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Xml;

namespace TheDataResourceImporter.Utils
{
    class MiscUtil
    {
        public static IMPORT_ERROR getImpErrorInstance(string sessionId, string isZip, string zipOrDirPath, string zipPath="", string errorMessage = "", string errorDetail = "")
        {
            var innerImpError = new IMPORT_ERROR() {ID=System.Guid.NewGuid().ToString(), IGNORED="N", OCURREDTIME=DateTime.Now, REIMPORTED="N", ISZIP = isZip, POINTOR = 0, SESSION_ID= sessionId , ZIP_OR_DIR_PATH = zipOrDirPath, ZIP_PATH = zipPath, ERROR_MESSAGE = errorMessage, ERROR_DETAIL = errorDetail};
            return innerImpError;
        }

        public static IMPORT_SESSION getNewImportSession(string fileType, string filePath, string IS_ZIP = "Y")
        {
            return new IMPORT_SESSION() { SESSION_ID = System.Guid.NewGuid().ToString(), ROLLED_BACK = "N", DATA_RES_TYPE = fileType, START_TIME = System.DateTime.Now, ZIP_OR_DIR_PATH = filePath, HAS_ERROR = "N", FAILED_COUNT = 0, COMPLETED = "N", LAST_TIME = 0, ZIP_ENTRIES_COUNT = 0, ZIP_ENTRY_POINTOR = 0, IS_ZIP = IS_ZIP};
        }


        public static Type getTypeByFullName(string typeFullName)
        {
            return  Type.GetType(typeFullName, true);
        }



        public static void setProperityByName(Type type,string propName, Object obj, Object value)
        {
            try
            {
                type.GetProperty(propName).SetValue(obj, value);
            }
            catch(Exception ex)
            {

            }
        }

        /***
         *获取指定XPath的值 
         ***/
        public static string getXElementValueByXPath(XElement currentNode, string xPath, string attribute = "", IXmlNamespaceResolver resolver = null)
        {
            string value = "";

            XElement target = null;
            if (null != resolver)
            {
                target = currentNode.XPathSelectElement(xPath, resolver);
            }
            else
            {
                target = currentNode.XPathSelectElement(xPath);
            }
            
            if(null != target)
            {
                if(String.IsNullOrEmpty(attribute))
                {
                    value =  target.Value;
                }
                else
                {
                    var targetAttr = target.Attribute(attribute);
                    if(null != targetAttr)
                    {
                        value = targetAttr.Value;
                    }
                }
            }
            return value;
        }

        /***
         *在currentNode中搜索tagName的元素, 可以指定tagName的直接子元素标签, 子元素标签可选
         ***/
        public static string getXElementValueByTagNameaAndChildTabName(XElement currentNode,string tagName, params string[] subTags)
        {
            string value = "";

            var targetNode = currentNode.Descendants(tagName).FirstOrDefault();

            if(subTags.Length > 0)
            {
                foreach(string tag in subTags)
                {
                    if(null != targetNode)
                    {
                        targetNode = targetNode.Element(tag);
                    }
                }
            }
            if(null != targetNode)
            {
                value = targetNode.Value;
            }

            return value;
        }


        public static string jsonSerilizeObject(Object source)
        {
            var jSetting = new JsonSerializerSettings();
            jSetting.NullValueHandling = NullValueHandling.Ignore;
            IsoDateTimeConverter dtConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd" };
            jSetting.Converters.Add(dtConverter);
            string json = JsonConvert.SerializeObject(source, jSetting);
            return json;
        }



        public  static  DateTime? pareseDateTimeExactUseCurrentCultureInfo(string dataStr, string format = "yyyyMMdd")
        {
            DateTime? resultDate = null;
            try
            {
                resultDate = DateTime.ParseExact(dataStr, format, System.Globalization.CultureInfo.CurrentCulture);
            }
            catch (Exception)
            {

            }

            return resultDate;
        }

        public static  string getRelativeFilePathInclude(string path, int depth)
        {
            string relativeFilePath = "";
            FileInfo fileInfoTmp = new FileInfo(path);
            if(fileInfoTmp.Exists)
            {
                var parentDir = fileInfoTmp.Directory;
                for (int index = 0; index < depth - 1; index++)
                {
                    parentDir = parentDir.Parent;
                }
                relativeFilePath =parentDir.Name +  fileInfoTmp.FullName.Substring(parentDir.FullName.Length);
            }
            return relativeFilePath;
        }


        public static string  getDictValueOrDefaultByKey(Dictionary<string, string> dict, string key)
        {
            string value = "";//默认值

            dict.TryGetValue(key, out value);

            return value;
        }
    }
}
