using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Business
{
    /// <summary>
    /// 门店申请 请求对象
    /// </summary>
    public class ApplyStoreRequestDto
    {
        /// <summary>
        /// 真实姓名
        /// </summary>
        public string TrueName { get; set; }
        /// <summary>
        /// 门店名称
        /// </summary>
        public string StoreName { get; set; }
        /// <summary>
        /// 区
        /// </summary>
        public string Area { get; set; }
        /// <summary>
        /// 市
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// 省
        /// </summary>
        public string Province { get; set; }
        /// <summary>
        /// 详细地址
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// 门店电话
        /// </summary>
        public string PhoneNumber { get; set; }
        /// <summary>
        /// 申请人OpenId
        /// </summary>
        public string ApplyOpenId { get; set; }
        /// <summary>
        /// 区域经理Id（代理商）
        /// </summary>
        public int AgentId { get; set; }
       
    }
}