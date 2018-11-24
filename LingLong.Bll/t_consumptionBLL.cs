using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LingLong.Model;
using LingLong.Dal;
 
namespace LingLong.Bll {
	public partial class t_consumptionBLL {
		/// <summary>
        /// 查询单条
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public static t_consumption GetModel(int id)
        {
			t_consumptionDAL dal = new t_consumptionDAL();
            return dal.GetModel(id);
        }

        /// <summary>
        /// 查询所有
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<t_consumption> GetList()
        {
			t_consumptionDAL dal = new t_consumptionDAL();
            return dal.GetList();
        }

		/// <summary>
        /// 查询ByWhere
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<t_consumption> GetListByWhere(string strWhere)
        {
           	t_consumptionDAL dal = new t_consumptionDAL();
            return dal.GetListByWhere(strWhere);
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageCount">每页显示行数</param>
        /// <returns></returns>
        public static IEnumerable<t_consumption> GetListPager(int pageIndex, int pageCount)
        {
			t_consumptionDAL dal = new t_consumptionDAL();
            return dal.GetListPager(pageIndex, pageCount);
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Insert(t_consumption entity)
        {
			t_consumptionDAL dal = new t_consumptionDAL();
            return dal.Insert(entity);
        }

        public static int InsertByTrans(t_consumption entity, IDbConnection connection, IDbTransaction trans)
        {
            t_consumptionDAL dal = new t_consumptionDAL();
            return dal.InsertByTrans(entity, connection, trans);
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Update(t_consumption entity)
        {
			t_consumptionDAL dal = new t_consumptionDAL();
            return dal.Update(entity);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public static int Delete(int id)
        {
			t_consumptionDAL dal = new t_consumptionDAL();
            return dal.Delete(id);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Delete(t_consumption entity)
        {
			t_consumptionDAL dal = new t_consumptionDAL();
            return dal.Delete(entity);
        }

        /// <summary>
        /// 删除多行
        /// </summary>
        /// <param name="inIds"></param>
        /// <returns></returns>
        public static int DeleteList(string inIds)
        {
			t_consumptionDAL dal = new t_consumptionDAL();
            return dal.DeleteList(inIds);
        }
	} 
}
	