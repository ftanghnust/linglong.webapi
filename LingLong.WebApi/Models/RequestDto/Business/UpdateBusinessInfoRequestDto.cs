using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Business
{
    /// <summary>
    /// 修改员工信息 请求对象
    /// </summary>
    public class UpdateBusinessInfoRequestDto
    {
        /// <summary>
        /// 门店Id
        /// </summary>
        public int StoreId { get; set; }
        /// <summary>
        /// 商户Id
        /// </summary>
        public int BusinessId { get; set; }
        /// <summary>
        /// 角色Id
        /// </summary>
        public int RoleId { get; set; }
        /// <summary>
        /// 上级商户Id
        /// </summary>
        public int ParentId { get; set; }
        /// <summary>
        /// 员工状态
        /// </summary>
        public int State { get; set; }
    }
}