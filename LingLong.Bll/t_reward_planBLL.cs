using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LingLong.Model;
using LingLong.Dal;
 
namespace LingLong.Bll {
	public partial class t_reward_planBLL {
		/// <summary>
        /// 查询单条
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public static t_reward_plan GetModel(int id)
        {
			t_reward_planDAL dal = new t_reward_planDAL();
            return dal.GetModel(id);
        }

        /// <summary>
        /// 查询所有
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<t_reward_plan> GetList()
        {
			t_reward_planDAL dal = new t_reward_planDAL();
            return dal.GetList();
        }

		/// <summary>
        /// 查询ByWhere
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<t_reward_plan> GetListByWhere(string strWhere)
        {
           	t_reward_planDAL dal = new t_reward_planDAL();
            return dal.GetListByWhere(strWhere);
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageCount">每页显示行数</param>
        /// <returns></returns>
        public static IEnumerable<t_reward_plan> GetListPager(int pageIndex, int pageCount)
        {
			t_reward_planDAL dal = new t_reward_planDAL();
            return dal.GetListPager(pageIndex, pageCount);
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Insert(t_reward_plan entity)
        {
			t_reward_planDAL dal = new t_reward_planDAL();
            return dal.Insert(entity);
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Update(t_reward_plan entity)
        {
			t_reward_planDAL dal = new t_reward_planDAL();
            return dal.Update(entity);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        public static int Delete(int id)
        {
			t_reward_planDAL dal = new t_reward_planDAL();
            return dal.Delete(id);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static int Delete(t_reward_plan entity)
        {
			t_reward_planDAL dal = new t_reward_planDAL();
            return dal.Delete(entity);
        }

        /// <summary>
        /// 删除多行
        /// </summary>
        /// <param name="inIds"></param>
        /// <returns></returns>
        public static int DeleteList(string inIds)
        {
			t_reward_planDAL dal = new t_reward_planDAL();
            return dal.DeleteList(inIds);
        }
	} 
}
	