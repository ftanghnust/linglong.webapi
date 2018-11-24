using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Customer
{
    /// <summary>
    /// 写入关联门店、服务员信息（服务评价页面加载、门店服务人发送门店推荐页面加载时调用） 请求对象
    /// </summary>
    public class JoinStoreAndBusinessRequestDto
    {
        /// <summary>
        /// 门店Id
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// 客户Id
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// 关联类型 1：扫描服务人员二维码；2：服务人员发送门店推荐；3：微信朋友分享门店入口进入
        /// </summary>
        public int JoinType { get; set; }

        /// <summary>
        /// 服务员Id
        /// </summary>
        public int BusinessId { get; set; }

    }
}