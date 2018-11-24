using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Customer
{
    /// <summary>
    /// 修改客户密码 请求对象
    /// </summary>
    public class UpdateCustomerPassWordRequestDto
    {
        /// <summary>
        /// 客户微信唯一标识OpenId
        /// </summary>
        public string OpenId { get; set; }

        /// <summary>
        /// 客户密码
        /// </summary>
        public string PassWord { get; set; }
 
    }
}