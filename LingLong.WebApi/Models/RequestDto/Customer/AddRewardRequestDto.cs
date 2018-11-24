using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Customer
{
    /// <summary>
    /// 新增打赏记录（单击打赏支付按钮时调用） 请求对象
    /// </summary>
    public class AddRewardRequestDto
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
        /// 打赏商品Id
        /// </summary>
        public int GoodsId { get; set; }

        /// <summary>
        /// 打赏人 OpenId
        /// </summary>
        public string OpenId { get; set; }

        /// <summary>
        /// 商品金额
        /// </summary>
        public decimal Money { get; set; }
    }
}