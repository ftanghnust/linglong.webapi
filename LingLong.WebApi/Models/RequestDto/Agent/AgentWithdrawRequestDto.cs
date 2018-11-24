using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Agent
{
    /// <summary>
    /// 代理商提现 请求对象
    /// </summary>
    public class AgentWithdrawRequestDto
    {
        /// <summary>
        /// 代理商微信唯一标识OpenId
        /// </summary>
        public string OpenId { get; set; }

        /// <summary>
        /// 提现金额
        /// </summary>
        public decimal WithdrawMoney { get; set; }

   
    }
}