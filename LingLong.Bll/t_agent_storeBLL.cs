using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LingLong.Model;
using LingLong.Dal;
 
namespace LingLong.Bll {
	public partial class t_agent_storeBLL {
		/// <summary>
        /// 查询单条
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public static t_agent_store GetModel(int id)
        {
			t_agent_storeDAL dal = new t_agent_storeDAL();
            return dal.GetModel(id);
        }

        /// <summary>
        /// 查询所有
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<t_agent_store> GetList()
        {
			t_agent_storeDAL dal = new t_agent_storeDAL();
            return dal.GetList();
        }

		/// <summary>
        /// 查询ByWhere
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<t_agent_store> GetListByWhere(string strWhere)
        {
           	t_agent_storeDAL dal = new t_agent_storeDAL();
            return dal.GetListByWhere(strWhere);
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageCount">每页显示行数</param>
        /// <returns></returns>
        public static IEnumerable<t_agent_store> GetListPager(int pageIndex, int pageCount)
        {
			t_agent_storeDAL dal = new t_agent_storeDAL();
            return dal.GetListPager(pageIndex, pageCount);
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Insert(t_agent_store entity)
        {
			t_agent_storeDAL dal = new t_agent_storeDAL();
            return dal.Insert(entity);
        }

        public static int InsertByTrans(t_agent_store entity, IDbConnection connection, IDbTransaction trans)
        {
            t_agent_storeDAL dal = new t_agent_storeDAL();
            return dal.InsertByTrans(entity, connection, trans);
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Update(t_agent_store entity)
        {
			t_agent_storeDAL dal = new t_agent_storeDAL();
            return dal.Update(entity);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public static int Delete(int id)
        {
			t_agent_storeDAL dal = new t_agent_storeDAL();
            return dal.Delete(id);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Delete(t_agent_store entity)
        {
			t_agent_storeDAL dal = new t_agent_storeDAL();
            return dal.Delete(entity);
        }

        /// <summary>
        /// 删除多行
        /// </summary>
        /// <param name="inIds"></param>
        /// <returns></returns>
        public static int DeleteList(string inIds)
        {
			t_agent_storeDAL dal = new t_agent_storeDAL();
            return dal.DeleteList(inIds);
        }
	} 
}
	