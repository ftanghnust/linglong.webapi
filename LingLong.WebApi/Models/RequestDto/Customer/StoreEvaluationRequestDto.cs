using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Customer
{
    /// <summary>
    /// 门店评价 请求对象
    /// </summary>
    public class StoreEvaluationRequestDto
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
        /// 门店评价得分（1-5分）
        /// </summary>
        public int StoreScore { get; set; }


    }
}