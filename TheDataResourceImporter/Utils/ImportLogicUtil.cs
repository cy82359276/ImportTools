using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDataResourceImporter.Utils
{
    class ImportLogicUtil
    {


        public  static string importS_China_Patent_TextImage(DataSourceEntities entitiesContext,string filePath,string session_id, string app_type, DateTime pub_date, string entryKey, string hasTif)
        {
            S_CHINA_PATENT_TEXTIMAGE sCNPatTxtImg = new S_CHINA_PATENT_TEXTIMAGE() { APPL_TYPE = app_type, APP_NUMBER = CompressUtil.getEntryShortName(entryKey), ARCHIVE_INNER_PATH = entryKey, EXIST_TIF = hasTif, FILE_PATH = filePath, ID = System.Guid.NewGuid().ToString(), IMPORT_SESSION_ID = session_id, IMPORT_TIME = System.DateTime.Now, PATH_TIF = entryKey, PUB_DATE = pub_date };

            try
            {
                entitiesContext.S_CHINA_PATENT_TEXTIMAGE.Add(sCNPatTxtImg);
                entitiesContext.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {

                //ex.;
            }

            return MiscUtil.jsonSerilizeObject(sCNPatTxtImg);
            //sCNPatTxtImg.APPL_TYPE = app_type;
            //sCNPatTxtImg.APP_NUMBER = CompressUtil.getEntryShortName(entryKey);
            //sCNPatTxtImg.ARCHIVE_INNER_PATH = entryKey;
            //sCNPatTxtImg.FILE_PATH = filePath;
            //sCNPatTxtImg.ID = System.Guid.NewGuid().ToString();
            //sCNPatTxtImg.IMPORT_SESSION_ID = session_id;
            //sCNPatTxtImg.EXIST_TIF = hasTif;
            //sCNPatTxtImg.IMPORT_TIME
        }
    }
}
