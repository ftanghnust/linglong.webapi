using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Business
{
    /// <summary>
    /// 商户提现 请求对象
    /// </summary>
    public class BusinessWithdrawRequestDto
    {
        /// <summary>
        /// 门店Id
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// 商户微信唯一标识OpenId
        /// </summary>
        public string OpenId { get; set; }

        /// <summary>
        /// 提现金额
        /// </summary>
        public decimal WithdrawMoney { get; set; }

   
    }
}