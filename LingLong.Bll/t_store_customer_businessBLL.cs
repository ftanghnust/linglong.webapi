using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LingLong.Model;
using LingLong.Dal;

namespace LingLong.Bll
{
    public partial class t_store_customer_businessBLL
    {
        /// <summary>
        /// 查询单条
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public static t_store_customer_business GetModel(int id)
        {
            t_store_customer_businessDAL dal = new t_store_customer_businessDAL();
            return dal.GetModel(id);
        }

        /// <summary>
        /// 查询所有
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<t_store_customer_business> GetList()
        {
            t_store_customer_businessDAL dal = new t_store_customer_businessDAL();
            return dal.GetList();
        }

        /// <summary>
        /// 查询ByWhere
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<t_store_customer_business> GetListByWhere(string strWhere)
        {
            t_store_customer_businessDAL dal = new t_store_customer_businessDAL();
            return dal.GetListByWhere(strWhere);
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageCount">每页显示行数</param>
        /// <returns></returns>
        public static IEnumerable<t_store_customer_business> GetListPager(int pageIndex, int pageCount)
        {
            t_store_customer_businessDAL dal = new t_store_customer_businessDAL();
            return dal.GetListPager(pageIndex, pageCount);
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Insert(t_store_customer_business entity)
        {
            t_store_customer_businessDAL dal = new t_store_customer_businessDAL();
            return dal.Insert(entity);
        }

        public static int InsertByTrans(t_store_customer_business entity, IDbConnection connection, IDbTransaction trans)
        {
            t_store_customer_businessDAL dal = new t_store_customer_businessDAL();
            return dal.InsertByTrans(entity, connection, trans);
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Update(t_store_customer_business entity)
        {
            t_store_customer_businessDAL dal = new t_store_customer_businessDAL();
            return dal.Update(entity);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public static int Delete(int id)
        {
            t_store_customer_businessDAL dal = new t_store_customer_businessDAL();
            return dal.Delete(id);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Delete(t_store_customer_business entity)
        {
            t_store_customer_businessDAL dal = new t_store_customer_businessDAL();
            return dal.Delete(entity);
        }

        public static int DeleteByTrans(t_store_customer_business entity, IDbConnection connection, IDbTransaction trans)
        {
            t_store_customer_businessDAL dal = new t_store_customer_businessDAL();
            return dal.DeleteByTrans(entity, connection, trans);
        }

        /// <summary>
        /// 删除多行
        /// </summary>
        /// <param name="inIds"></param>
        /// <returns></returns>
        public static int DeleteList(string inIds)
        {
            t_store_customer_businessDAL dal = new t_store_customer_businessDAL();
            return dal.DeleteList(inIds);
        }
    }
}
