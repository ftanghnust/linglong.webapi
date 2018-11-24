using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LingLong.Model;
using LingLong.Dal;
 
namespace LingLong.Bll {
	public partial class t_businessBLL {
		/// <summary>
        /// 查询单条
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public static t_business GetModel(int id)
        {
			t_businessDAL dal = new t_businessDAL();
            return dal.GetModel(id);
        }

        /// <summary>
        /// 查询所有
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<t_business> GetList()
        {
			t_businessDAL dal = new t_businessDAL();
            return dal.GetList();
        }

		/// <summary>
        /// 查询ByWhere
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<t_business> GetListByWhere(string strWhere)
        {
           	t_businessDAL dal = new t_businessDAL();
            return dal.GetListByWhere(strWhere);
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageCount">每页显示行数</param>
        /// <returns></returns>
        public static IEnumerable<t_business> GetListPager(int pageIndex, int pageCount)
        {
			t_businessDAL dal = new t_businessDAL();
            return dal.GetListPager(pageIndex, pageCount);
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Insert(t_business entity)
        {
			t_businessDAL dal = new t_businessDAL();
            return dal.Insert(entity);
        }

        public static int InsertByTrans(t_business entity, IDbConnection connection, IDbTransaction trans)
        {
            t_businessDAL dal = new t_businessDAL();
            return dal.InsertByTrans(entity, connection, trans);
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Update(t_business entity)
        {
			t_businessDAL dal = new t_businessDAL();
            return dal.Update(entity);
        }

        public static int UpdateByTrans(t_business entity, IDbConnection connection, IDbTransaction trans)
        {
            t_businessDAL dal = new t_businessDAL();
            return dal.UpdateByTrans(entity, connection, trans);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public static int Delete(int id)
        {
			t_businessDAL dal = new t_businessDAL();
            return dal.Delete(id);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Delete(t_business entity)
        {
			t_businessDAL dal = new t_businessDAL();
            return dal.Delete(entity);
        }

        /// <summary>
        /// 删除多行
        /// </summary>
        /// <param name="inIds"></param>
        /// <returns></returns>
        public static int DeleteList(string inIds)
        {
			t_businessDAL dal = new t_businessDAL();
            return dal.DeleteList(inIds);
        }
	} 
}
	