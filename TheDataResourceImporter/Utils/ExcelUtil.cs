using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.OleDb;
using OfficeOpenXml;
using System.Reflection;

namespace UpdateDataFromExcel.Utils
{
    public class ExcelUtil
    {
        public static DirectoryInfo outputDir = new DirectoryInfo(@"d:\temp\SampleApp");


        public static List<T> GetClassFromExcel<T>(string path, int fromRow, int fromColumn, int toColumn = 0)
        {
            List<T> retList = new List<T>();
            using (var pck = new ExcelPackage())
            {
                using (var stream = File.OpenRead(path))
                {
                    pck.Load(stream);
                }
                //Retrieve first Worksheet
                var ws = pck.Workbook.Worksheets.First();
                //If the to column is empty or 0, then make the tocolumn to the count of the properties
                //Of the class object inserted
                toColumn = toColumn == 0 ? typeof(T).GetProperties().Count() : toColumn;

                //Read the first Row for the column names and place into a list so that
                //it can be used as reference to properties
                List<string> columnNames = new List<string>();
                // wsRow = ws.Row(0);
                foreach (var cell in ws.Cells[1, 1, 1, ws.Cells.Count()])
                {
                    columnNames.Add(cell.Value.ToString());
                }
                //Loop through the rows of the excel sheet
                for (var rowNum = fromRow; rowNum <= ws.Dimension.End.Row; rowNum++)
                {
                    //create a instance of T
                    T objT = Activator.CreateInstance<T>();
                    //Retrieve the type of T
                    Type myType = typeof(T);
                    //Get all the properties associated with T
                    PropertyInfo[] myProp = myType.GetProperties();

                    var wsRow = ws.Cells[rowNum, fromColumn, rowNum, ws.Cells.Count()];

                    foreach (var propertyInfo in myProp)
                    {
                        if (columnNames.Contains(propertyInfo.Name))
                        {
                            int position = columnNames.IndexOf(propertyInfo.Name);
                            //To prevent an exception cast the value to the type of the property.
                            propertyInfo.SetValue(objT, Convert.ChangeType(wsRow[rowNum, position + 1].Value, propertyInfo.PropertyType));
                        }
                    }

                    retList.Add(objT);
                }

            }
            return retList;
        }

        public static List<Dictionary<string, object>> parseExcel<T>(string path, int fromRow, int fromColumn, int toColumn = 0)
        {
            List<Dictionary<string, object>> resultList = new List<Dictionary<string, object>>();









            return resultList;
        }









        public static void RunLinqSample(DirectoryInfo outputDir, string filePath)
        {
            FileInfo existingFile = new FileInfo(outputDir.FullName + filePath);
            using (ExcelPackage package = new ExcelPackage(existingFile))
            {
                ExcelWorksheet sheet = package.Workbook.Worksheets[1];

                //Select all cells in column d between 9990 and 10000
                var query1 = (from cell in sheet.Cells["d:d"] where cell.Value is double && (double)cell.Value >= 9990 && (double)cell.Value <= 10000 select cell);

                Console.WriteLine("Print all cells with value between 9990 and 10000 in column D ...");
                Console.WriteLine();

                int count = 0;
                foreach (var cell in query1)
                {
                    Console.WriteLine("Cell {0} has value {1:N0}", cell.Address, cell.Value);
                    count++;
                }

                Console.WriteLine("{0} cells found ...", count);
                Console.WriteLine();



                
                //Select all bold cells
                Console.WriteLine("Now get all bold cells from the entire sheet...");
                var query2 = (from cell in sheet.Cells[sheet.Dimension.Address] where cell.Style.Font.Bold select cell);
                //If you have a clue where the data is, specify a smaller range in the cells indexer to get better performance (for example "1:1,65536:65536" here)
                count = 0;
                foreach (var cell in query2)
                {
                    if (!string.IsNullOrEmpty(cell.Formula))
                    {
                        Console.WriteLine("Cell {0} is bold and has a formula of {1:N0}", cell.Address, cell.Formula);
                    }
                    else
                    {
                        Console.WriteLine("Cell {0} is bold and has a value of {1:N0}", cell.Address, cell.Value);
                    }
                    count++;
                }

                //Here we use more than one column in the where clause. We start by searching column D, then use the Offset method to check the value of column C.
                var query3 = (from cell in sheet.Cells["d:d"]
                              where cell.Value is double &&
                                    (double)cell.Value >= 9500 && (double)cell.Value <= 10000 &&
                                    cell.Offset(0, -1).GetValue<DateTime>().Year == DateTime.Today.Year + 1
                              select cell);

                Console.WriteLine();
                Console.WriteLine("Print all cells with a value between 9500 and 10000 in column D and the year of Column C is {0} ...", DateTime.Today.Year + 1);
                Console.WriteLine();

                count = 0;
                foreach (var cell in query3)    //The cells returned here will all be in column D, since that is the address in the indexer. Use the Offset method to print any other cells from the same row.
                {
                    Console.WriteLine("Cell {0} has value {1:N0} Date is {2:d}", cell.Address, cell.Value, cell.Offset(0, -1).GetValue<DateTime>());
                    count++;
                }
            }
        }

















        //private string GetConStr(string ExcelPath)
        //{
        //    string path = ExcelPath;
        //    if (!File.Exists(path))
        //        return null;
        //    string str2 = Path.GetExtension(path).ToLower();
        //    if ((str2 != ".xls") && (str2 != ".xlsx"))
        //        return null;
        //    string str3 = "Provider = Microsoft.Jet.OLEDB.4.0; Data Source =" + path + "; Extended Properties=Excel 8.0";
        //    if (str2 == ".xlsx")
        //        str3 = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source=" + path + "; Extended Properties=Excel 12.0";
        //    return str3;
        //}

        //public DataTable ExcelToDataTable(string ExcelPath,out string msg)
        //{
        //    return ExcelToDataTable(ExcelPath, null,out msg);
        //}

        //public DataTable ExcelToDataTable(string ExcelPath, string SheetName,out string msg)
        //{
        //    try
        //    {
        //        msg = "";
        //        string conStr = GetConStr(ExcelPath);
        //        if (string.IsNullOrEmpty(conStr))
        //            return null;
        //        OleDbConnection connection = new OleDbConnection(conStr);
        //        connection.Open();
        //        if (string.IsNullOrEmpty(SheetName))
        //            SheetName = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null).Rows[0]["TABLE_NAME"].ToString();
        //        else if (!SheetName.Contains("$"))
        //            SheetName = SheetName + "$";
        //        OleDbDataAdapter adapter = new OleDbDataAdapter("select * from [" + SheetName + "]", conStr);
        //        DataSet dataSet = new DataSet();
        //        adapter.Fill(dataSet, "[" + SheetName + "$]");
        //        connection.Close();

        //        return dataSet.Tables[0];
        //    }
        //    catch (Exception e)
        //    {
        //        msg = e.Message;
        //        return null;
        //    }
        //}

    }
}
