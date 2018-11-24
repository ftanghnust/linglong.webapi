using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Business
{
    /// <summary>
    /// 手动添加客户信息 请求对象
    /// </summary>
    public class ManualAddCustomerRequestDto
    {
        /// <summary>
        /// 门店Id
        /// </summary>
        public int StoreId { get; set; }
        /// <summary>
        /// 添加人Id
        /// </summary>
        public int BusinessId { get; set; }
        /// <summary>
        /// 客户姓名
        /// </summary>
        public string TrueName { get; set; }
        /// <summary>
        ///手机号码
        /// </summary>
        public string PhoneNumber { get; set; }
        /// <summary>
        /// 头像链接 
        /// </summary>
        public string AvatarUrl { get; set; }
        /// <summary>
        /// 微信号
        /// </summary>
        public string Wechat { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
      
    }
}