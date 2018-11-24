using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Agent
{
    /// <summary>
    /// 创建代理商微信小程序二维码（B接口） 请求对象
    /// </summary>
    public class AgentCreateWxCodeRequestDto
    {
        /// <summary>
        ///  
        /// </summary>
        public string scene { get; set; }
        /// <summary>
        ///  
        /// </summary>
        public string page { get; set; } = "";

    }
}