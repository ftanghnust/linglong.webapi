using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Agent
{
    /// <summary>
    /// 修改门店状态 请求对象
    /// </summary>
    public class ChangeStoreStateRequestDto
    {
        /// <summary>
        /// 门店Id
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// 0：待审核 1：启用 2：禁用
        /// </summary>
        public int State { get; set; }
 
    }
}