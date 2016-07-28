using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Windows.Forms;
using System.Configuration;
using System.Data.OleDb;

namespace TheDataResourceImporter.Utils
{
    public class OracleDB
    {
        public static OleDbConnection conSource;
        public static OleDbConnection conBd;

        public OracleDB()
        {
            CreateCon();
        }
        public static void CreateCon()
        {
            if (conBd == null || conBd.State != ConnectionState.Open)
            {
                string bdConnectionString = ConfigurationManager.ConnectionStrings["conStr"].ConnectionString.ToString();
                conBd = new OleDbConnection(bdConnectionString);
                try
                {
                    conBd.Open();
                }
                catch (Exception e)
                {
                    MessageBox.Show("数据库没有正确连接，请检查!\r\n" + bdConnectionString + e.Message);
                }
            }
        }
        public static int ExecuteSql(string SQLString)
        {
            CreateCon();
            try
            {
                OleDbCommand cmd = new OleDbCommand(SQLString, conBd);
                int rows = cmd.ExecuteNonQuery();
                return rows;
            }
            catch (Exception e)
            {
                MessageBox.Show(SQLString + e.Message);
            }
            return 0;
        }
        
        public static DataTable GetDT(string sqlString)
        {
            CreateCon();
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            try
            {
                OleDbDataAdapter oda = new OleDbDataAdapter(sqlString, conBd);
                oda.Fill(dt);
                return dt;
            }
            catch (Exception e)
            {
                MessageBox.Show(sqlString + e.Message);
            }
            return dt;
        }


        public static bool ExecuteSqlByTrans(List<string> sqlString)
        {
            CreateCon();
            OleDbCommand oraCmd = conBd.CreateCommand();
            OleDbTransaction oraTrans = conBd.BeginTransaction();
            oraCmd.Connection = conBd;
            oraCmd.Transaction = oraTrans;
            string errSql = "";
            try
            {
                foreach (string s in sqlString)
                {
                    if (s != "")
                    {
                        errSql = s;
                        oraCmd.CommandText = s;
                        int a = oraCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        continue;
                    }
                }
                oraTrans.Commit();
                conBd.Close();
            }
            catch (Exception ee)
            {
                oraTrans.Rollback();
                MessageBox.Show(ee.Message);
                return false;
            }
            return true;
        }

        public static OleDbDataReader ExecuteReader(string sql)
        {
            CreateCon();
            OleDbCommand oraCmd = conBd.CreateCommand();
            oraCmd.Connection = conBd;
            oraCmd.CommandText = sql;

            try
            {
                OleDbDataReader dataReader = oraCmd.ExecuteReader();
                oraCmd.Dispose();
                return dataReader;
            }
            catch (Exception)
            {
                throw;
            }

        }
    }
}
