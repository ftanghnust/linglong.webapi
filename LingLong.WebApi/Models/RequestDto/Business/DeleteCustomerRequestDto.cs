using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Business
{
    /// <summary>
    /// 删除客户 请求对象
    /// </summary>
    public class DeleteCustomerRequestDto
    {
        /// <summary>
        /// 门店Id
        /// </summary>
        public int StoreId { get; set; }
        /// <summary>
        /// 服务人员Id
        /// </summary>
        public int BusinessId { get; set; }
        /// <summary>
        /// 客户Id
        /// </summary>
        public int CustomerId { get; set; }
         
    }
}