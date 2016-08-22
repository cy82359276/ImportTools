using System;
using System.Collections.Generic;
using System.Text;
using System.Data.OleDb;
using System.Data;
using TheDataResourceImporter.Utils;

namespace TheDataResourceImporter
{
    public class AccessUtil
    {

        /// <summary>   
        /// AccessDb 的摘要说明，以下信息请完整保留   
        /// 请在数据传递完毕后调用Close()方法，关闭数据链接。   
        /// </summary>   

        #region 变量声明处
        public OleDbConnection Conn;
        public string ConnString;//连接字符串  
        #endregion


        #region 构造函数与连接关闭数据库
        /**/
        /// <summary>   
        /// 构造函数   
        /// </summary>   
        /// <param name="Dbpath">ACCESS数据库路径</param>   
        public AccessUtil(string Dbpath)
        {
            ConnString = "Provider=Microsoft.Jet.OleDb.4.0;Data Source=";
            ConnString += Dbpath;
            Conn = new OleDbConnection(ConnString);
            Conn.Open();
        }

        /**/
        /// <summary>   
        /// 打开数据源链接   
        /// </summary>   
        /// <returns></returns>   
        public OleDbConnection DbConn()
        {
            Conn.Open();
            return Conn;
        }

        /**/
        /// <summary>   
        /// 请在数据传递完毕后调用该函数，关闭数据链接。   
        /// </summary>   
        public void Close()
        {
            Conn.Close();
        }
        #endregion


        #region 数据库基本操作
        /**/
        /// <summary>   
        /// 根据SQL命令返回数据DataTable数据表,   
        /// 可直接作为dataGridView的数据源   
        /// </summary>   
        /// <param name="SQL"></param>   
        /// <returns></returns>   
        public DataTable SelectToDataTable(string SQL)
        {
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            OleDbCommand command = new OleDbCommand(SQL, Conn);
            adapter.SelectCommand = command;
            DataTable Dt = new DataTable();
            adapter.Fill(Dt);
            return Dt;
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="tableName">表明</param>
        /// <param name="pk">分页依据的字段名</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页的记录数</param>
        /// <param name="tar">要查询的字段</param>
        /// <returns></returns>
        public DataTable SelectByPage(string tableName, string pk, int pageIndex, int pageSize, string tar)
        {
            string SQL;
            if (pageIndex == 1)
            {
                SQL = string.Format("select top {0} {3} from {1} order by {2}", pageSize, tableName, pk, tar);
            }
            else
            {
                string inSideSql = string.Format("select top {0} {1} from {2} order by {1}", (pageIndex - 1) * pageSize, pk, tableName);
                SQL = string.Format("select top {0} {4} from {1} where {2} > (select max({2}) from ({3})) order by {2}", pageSize, tableName, pk, inSideSql, tar);
            }
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            OleDbCommand command = new OleDbCommand(SQL, Conn);
            adapter.SelectCommand = command;
            DataTable Dt = new DataTable();
            adapter.Fill(Dt);
            return Dt;
        }

        /**/
        /// <summary>   
        /// 根据SQL命令返回数据DataSet数据集，其中的表可直接作为dataGridView的数据源。   
        /// </summary>   
        /// <param name="SQL"></param>   
        /// <param name="subtableName">在返回的数据集中所添加的表的名称</param>   
        /// <returns></returns>   
        public DataSet SelectToDataSet(string SQL, string subtableName)
        {
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            OleDbCommand command = new OleDbCommand(SQL, Conn);
            adapter.SelectCommand = command;
            DataSet Ds = new DataSet();
            Ds.Tables.Add(subtableName);
            adapter.Fill(Ds, subtableName);
            return Ds;
        }

        /**/
        /// <summary>   
        /// 在指定的数据集中添加带有指定名称的表，由于存在覆盖已有名称表的危险，返回操作之前的数据集。   
        /// </summary>   
        /// <param name="SQL"></param>   
        /// <param name="subtableName">添加的表名</param>   
        /// <param name="DataSetName">被添加的数据集名</param>   
        /// <returns></returns>   
        public DataSet SelectToDataSet(string SQL, string subtableName, DataSet DataSetName)
        {
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            OleDbCommand command = new OleDbCommand(SQL, Conn);
            adapter.SelectCommand = command;
            DataTable Dt = new DataTable();
            DataSet Ds = new DataSet();
            Ds = DataSetName;
            adapter.Fill(DataSetName, subtableName);
            return Ds;
        }

        /**/
        /// <summary>   
        /// 根据SQL命令返回OleDbDataAdapter，   
        /// 使用前请在主程序中添加命名空间System.Data.OleDb   
        /// </summary>   
        /// <param name="SQL"></param>   
        /// <returns></returns>   
        public OleDbDataAdapter SelectToOleDbDataAdapter(string SQL)
        {
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            OleDbCommand command = new OleDbCommand(SQL, Conn);
            adapter.SelectCommand = command;
            return adapter;
        }

        /**/
        /// <summary>   
        /// 执行SQL命令，不需要返回数据的修改，删除可以使用本函数   
        /// </summary>   
        /// <param name="SQL"></param>   
        /// <returns></returns>   
        public bool ExecuteSQLNonquery(string SQL)
        {
            OleDbCommand cmd = new OleDbCommand(SQL, Conn);
            try
            {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }


        public OleDbDataReader ExecuteReader(string SQL)
        {
            OleDbCommand cmd = new OleDbCommand(SQL, Conn);

            try
            {
                return cmd.ExecuteReader();
            }
            catch (Exception e)
            {
                LogHelper.WriteLog("", "", SQL + "\r\n" + e.Message);
                return null;
            }

        }

        #endregion
    }
}
