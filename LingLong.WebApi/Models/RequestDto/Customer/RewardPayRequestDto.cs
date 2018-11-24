using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Customer
{
    /// <summary>
    /// 打赏支付 请求对象
    /// </summary>
    public class RewardPayRequestDto
    {
        /// <summary>
        /// 打赏Id
        /// </summary>
        public string OpenId { get; set; }

        /// <summary>
        /// 支付金额（分）
        /// </summary>
        public int TotalFee { get; set; }

    }
}