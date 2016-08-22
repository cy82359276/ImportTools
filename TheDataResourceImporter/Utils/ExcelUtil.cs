using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.OleDb;

namespace UpdateDataFromExcel.Utils
{
    public class ExcelUtil
    {
        private string GetConStr(string ExcelPath)
        {
            string path = ExcelPath;
            if (!File.Exists(path))
                return null;
            string str2 = Path.GetExtension(path).ToLower();
            if ((str2 != ".xls") && (str2 != ".xlsx"))
                return null;
            string str3 = "Provider = Microsoft.Jet.OLEDB.4.0; Data Source =" + path + "; Extended Properties=Excel 8.0";
            if (str2 == ".xlsx")
                str3 = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source=" + path + "; Extended Properties=Excel 12.0";
            return str3;
        }

        public DataTable ExcelToDataTable(string ExcelPath,out string msg)
        {
            return ExcelToDataTable(ExcelPath, null,out msg);
        }

        public DataTable ExcelToDataTable(string ExcelPath, string SheetName,out string msg)
        {
            try
            {
                msg = "";
                string conStr = GetConStr(ExcelPath);
                if (string.IsNullOrEmpty(conStr))
                    return null;
                OleDbConnection connection = new OleDbConnection(conStr);
                connection.Open();
                if (string.IsNullOrEmpty(SheetName))
                    SheetName = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null).Rows[0]["TABLE_NAME"].ToString();
                else if (!SheetName.Contains("$"))
                    SheetName = SheetName + "$";
                OleDbDataAdapter adapter = new OleDbDataAdapter("select * from [" + SheetName + "]", conStr);
                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet, "[" + SheetName + "$]");
                connection.Close();

                return dataSet.Tables[0];
            }
            catch (Exception e)
            {
                msg = e.Message;
                return null;
            }
        }

    }
}
