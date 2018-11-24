using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Customer
{
    /// <summary>
    /// 服务评价 请求对象
    /// </summary>
    public class ServiceEvaluationRequestDto
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
        /// 服务员Id
        /// </summary>
        public int BusinessId { get; set; }

        /// <summary>
        /// 服务员评价得分 1：满意 2：一般 3：不满意
        /// </summary>
        public int BusinessScore { get; set; }


    }
}