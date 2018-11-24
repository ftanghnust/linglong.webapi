using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Business
{
    /// <summary>
    /// 创建商户微信小程序二维码（B接口） 请求对象
    /// </summary>
    public class BusinessCreateWxCodeRequestDto
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