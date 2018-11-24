using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Business
{
    /// <summary>
    /// 修改个人信息（选择上级领导） 请求对象
    /// </summary>
    public class SelectLeaderRequestDto
    {
        /// <summary>
        /// 门店Id
        /// </summary>
        public int StoreId { get; set; }
        /// <summary>
        /// 商户OpenId
        /// </summary>
        public string OpenId { get; set; }
        /// <summary>
        /// 上级领导Id
        /// </summary>
        public int ParentId { get; set; }
    }
}