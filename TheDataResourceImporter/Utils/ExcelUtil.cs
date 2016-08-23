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
        /// <summary>
        /// 解析Excel, 使用EEPlus 仅支持String类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="fromRow">数据开头</param>
        /// <param name="fromColumn"></param>
        /// <param name="headerRow">表头的行号</param>
        /// <param name="headers"><表头名, 位置> 1 based</param>
        /// <param name="workSheetIndex">1 based</param>
        /// <returns></returns>
        public static List<Dictionary<string, string>> parseExcelWithEEPlus(string path, int fromRow, int fromColumn, Dictionary<string, int> headers, int workSheetIndex = 1)
        {
            List<Dictionary<string, string>> resultList = new List<Dictionary<string, string>>();

            using (var pck = new ExcelPackage())
            {
                using (var stream = File.OpenRead(path))
                {
                    pck.Load(stream);
                }
                var ws = pck.Workbook.Worksheets[1]; // 默认加载第一个sheet
                var toColumn = headers.Count();
                for (var rowNum = fromRow; rowNum <= ws.Dimension.End.Row; rowNum++)
                {
                    Dictionary<string, string> recDict = new Dictionary<string, string>();
                    var wsRow = ws.Cells[rowNum, fromColumn, rowNum, ws.Cells.Count()];//加载数据一行数据
                    foreach (var headerTemp in headers)
                    {
                        string headerName = headerTemp.Key; //字段名
                        int position = headerTemp.Value; //位置
                        var value = wsRow[rowNum, position].Text;
                        recDict.Add(headerName, value);
                    }
                    resultList.Add(recDict);
                }
            }
            return resultList;
        }
    }
}
