using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using Dapper;

namespace LingLong.Dal
{
    public class ConnectionFactory
    {
       
        private static readonly string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ToString();

        /// <summary>
        /// 打开mssql数据库连接
        /// </summary>
        /// <returns></returns>
        public static IDbConnection GetOpenConnection()
        {
            IDbConnection connection = null;
            connection = new SqlConnection(connString);

            SimpleCRUD.SetDialect(SimpleCRUD.Dialect.SQLServer);

            connection.Open();
            return connection;
        }

        /// <summary>
        /// 打开mysql数据库连接
        /// </summary>
        /// <returns></returns>
        public static IDbConnection GetOpenMySqlConnection()
        {
            IDbConnection connection = null;
            connection = new MySql.Data.MySqlClient.MySqlConnection(connString);

            SimpleCRUD.SetDialect(SimpleCRUD.Dialect.MySQL);

            connection.Open();
            return connection;
        }

    }
}
