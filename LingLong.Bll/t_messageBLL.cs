using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LingLong.Model;
using LingLong.Dal;
 
namespace LingLong.Bll {
	public partial class t_messageBLL {
		/// <summary>
        /// 查询单条
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public static t_message GetModel(int id)
        {
			t_messageDAL dal = new t_messageDAL();
            return dal.GetModel(id);
        }

        /// <summary>
        /// 查询所有
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<t_message> GetList()
        {
			t_messageDAL dal = new t_messageDAL();
            return dal.GetList();
        }

		/// <summary>
        /// 查询ByWhere
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<t_message> GetListByWhere(string strWhere)
        {
           	t_messageDAL dal = new t_messageDAL();
            return dal.GetListByWhere(strWhere);
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageCount">每页显示行数</param>
        /// <returns></returns>
        public static IEnumerable<t_message> GetListPager(int pageIndex, int pageCount)
        {
			t_messageDAL dal = new t_messageDAL();
            return dal.GetListPager(pageIndex, pageCount);
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Insert(t_message entity)
        {
			t_messageDAL dal = new t_messageDAL();
            return dal.Insert(entity);
        }

        public static int InsertByTrans(t_message entity, IDbConnection connection, IDbTransaction trans)
        {
            t_messageDAL dal = new t_messageDAL();
            return dal.InsertByTrans(entity, connection, trans);
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Update(t_message entity)
        {
			t_messageDAL dal = new t_messageDAL();
            return dal.Update(entity);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public static int Delete(int id)
        {
			t_messageDAL dal = new t_messageDAL();
            return dal.Delete(id);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Delete(t_message entity)
        {
			t_messageDAL dal = new t_messageDAL();
            return dal.Delete(entity);
        }

        /// <summary>
        /// 删除多行
        /// </summary>
        /// <param name="inIds"></param>
        /// <returns></returns>
        public static int DeleteList(string inIds)
        {
			t_messageDAL dal = new t_messageDAL();
            return dal.DeleteList(inIds);
        }
	} 
}
	