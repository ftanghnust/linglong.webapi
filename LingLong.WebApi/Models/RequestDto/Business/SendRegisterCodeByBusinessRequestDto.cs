using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Business
{
    /// <summary>
    /// 指定手机号码发送验证码(发送间隔不能小于60秒) 请求对象
    /// </summary>
    public class SendRegisterCodeByBusinessRequestDto
    {
        /// <summary>
        /// 手机号码
        /// </summary>
        public string PhoneNumber { get; set; }
    }
}