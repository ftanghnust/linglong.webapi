using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Customer
{
    /// <summary>
    /// 修改客户信息 请求对象
    /// </summary>
    public class UpdateCustomerInfoRequestDto
    {
        /// <summary>
        /// 客户微信唯一标识OpenId
        /// </summary>
        public string OpenId { get; set; }

        /// <summary>
        /// 客户手机号码
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// 头像链接 
        /// </summary>
        public string AvatarUrl { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        public string Nickname { get; set; }

    }
}