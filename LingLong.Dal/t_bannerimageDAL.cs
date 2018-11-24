using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LingLong.Model;
using Dapper;
 
namespace LingLong.Dal {
	public partial class t_bannerimageDAL
    { 
		/// <summary>
        /// 查询单条
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public t_bannerimage GetModel(int id)
        {
            using (var connection = ConnectionFactory.GetOpenMySqlConnection())
            {
                return connection.Get<t_bannerimage>(id);
            }
        }

	    /// <summary>
        /// 查询ByWhere
        /// </summary>
        /// <returns></returns>
        public IEnumerable<t_bannerimage> GetListByWhere(string strWhere)
        {
            using (var connection = ConnectionFactory.GetOpenMySqlConnection())
            {
                return connection.GetList<t_bannerimage>(strWhere);
            }
        }

        /// <summary>
        /// 查询所有
        /// </summary>
        /// <returns></returns>
        public IEnumerable<t_bannerimage> GetList()
        {
            using (var connection = ConnectionFactory.GetOpenMySqlConnection())
            {
                return connection.GetList<t_bannerimage>();
            }
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageCount">每页显示行数</param>
        /// <returns></returns>
        public IEnumerable<t_bannerimage> GetListPager(int pageIndex, int pageCount)
        {
            using (var connection = ConnectionFactory.GetOpenMySqlConnection())
            {
                return connection.GetListPaged<t_bannerimage>(pageIndex, pageCount, "WHERE 1=1", "Id ASC");
            }
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public int Insert(t_bannerimage entity)
        {
            using (var connection = ConnectionFactory.GetOpenMySqlConnection())
            {
                return connection.Insert(entity) ?? 0;
            }
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public int Update(t_bannerimage entity)
        {
            using (var connection = ConnectionFactory.GetOpenMySqlConnection())
            {
                return connection.Update(entity);
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public int Delete(int id)
        {
            using (var connection = ConnectionFactory.GetOpenMySqlConnection())
            {
                return connection.Delete<t_bannerimage>(id);
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public int Delete(t_bannerimage entity)
        {
            using (var connection = ConnectionFactory.GetOpenMySqlConnection())
            {
                return connection.Delete(entity);
            }
        }

        /// <summary>
        /// 删除多行
        /// </summary>
        /// <param name="inIds"></param>
        /// <returns></returns>
        public int DeleteList(string inIds)
        {
            using (var connection = ConnectionFactory.GetOpenMySqlConnection())
            {
                string strWhere = string.Format("WHERE id IN({0})", inIds);
                return connection.DeleteList<t_bannerimage>(strWhere);
            }
        }
	} 
}
	