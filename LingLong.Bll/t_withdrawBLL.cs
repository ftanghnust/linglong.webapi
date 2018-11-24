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
    public partial class t_withdrawBLL
    {
        /// <summary>
        /// 查询单条
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public static t_withdraw GetModel(int id)
        {
            t_withdrawDAL dal = new t_withdrawDAL();
            return dal.GetModel(id);
        }

        /// <summary>
        /// 查询所有
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<t_withdraw> GetList()
        {
            t_withdrawDAL dal = new t_withdrawDAL();
            return dal.GetList();
        }

        /// <summary>
        /// 查询ByWhere
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<t_withdraw> GetListByWhere(string strWhere)
        {
            t_withdrawDAL dal = new t_withdrawDAL();
            return dal.GetListByWhere(strWhere);
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageCount">每页显示行数</param>
        /// <returns></returns>
        public static IEnumerable<t_withdraw> GetListPager(int pageIndex, int pageCount)
        {
            t_withdrawDAL dal = new t_withdrawDAL();
            return dal.GetListPager(pageIndex, pageCount);
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Insert(t_withdraw entity)
        {
            t_withdrawDAL dal = new t_withdrawDAL();
            return dal.Insert(entity);
        }

        public static int InsertByTrans(t_withdraw entity, IDbConnection connection, IDbTransaction trans)
        {
            t_withdrawDAL dal = new t_withdrawDAL();
            return dal.InsertByTrans(entity, connection, trans);
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Update(t_withdraw entity)
        {
            t_withdrawDAL dal = new t_withdrawDAL();
            return dal.Update(entity);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public static int Delete(int id)
        {
            t_withdrawDAL dal = new t_withdrawDAL();
            return dal.Delete(id);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Delete(t_withdraw entity)
        {
            t_withdrawDAL dal = new t_withdrawDAL();
            return dal.Delete(entity);
        }

        /// <summary>
        /// 删除多行
        /// </summary>
        /// <param name="inIds"></param>
        /// <returns></returns>
        public static int DeleteList(string inIds)
        {
            t_withdrawDAL dal = new t_withdrawDAL();
            return dal.DeleteList(inIds);
        }
    }
}
