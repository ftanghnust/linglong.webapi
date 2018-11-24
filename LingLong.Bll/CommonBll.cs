using LingLong.Dal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LingLong.Bll
{
    public partial class CommonBll
    {
        public static IDbConnection GetOpenMySqlConnection()
        {
            return ConnectionFactory.GetOpenMySqlConnection();
        }  
    }
}
