using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Agent
{
    /// <summary>
    /// 指定手机号码发送验证码(发送间隔不能小于60秒) 请求对象
    /// </summary>
    public class SendRegisterCodeByAgentRequestDto
    {
        /// <summary>
        /// 手机号码
        /// </summary>
        public string PhoneNumber { get; set; }
    }
}