using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Customer
{
    /// <summary>
    /// 打赏支付回调方法 请求对象
    /// </summary>
    public class RewardPayCallbackRequestDto
    {
        /// <summary>
        /// 打赏Id
        /// </summary>
        public int RewardId { get; set; }

        /// <summary>
        /// 支付单号
        /// </summary>
        public string PaymentNo { get; set; }

        /// <summary>
        /// 支付状态 0：失败 1：成功
        /// </summary>
        public int State { get; set; }
    }
}