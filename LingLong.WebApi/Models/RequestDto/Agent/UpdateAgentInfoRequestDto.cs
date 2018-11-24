using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Agent
{
    /// <summary>
    /// 修改代理商信息 请求对象
    /// </summary>
    public class UpdateAgentInfoRequestDto
    {
        /// <summary>
        /// 微信唯一标识 
        /// </summary>
        public string OpenId { get; set; }
        /// <summary>
        /// 头像链接 
        /// </summary>
        public string AvatarUrl { get; set; }
        /// <summary>
        /// 真实姓名
        /// </summary>
        public string AgentName { get; set; }
        /// <summary>
        /// 手机号码
        /// </summary>
        public string PhoneNumber { get; set; }
        /// <summary>
        /// 籍贯
        /// </summary>
        public string NativePlace { get; set; }
        /// <summary>
        /// 身高
        /// </summary>
        public float Height { get; set; }
        /// <summary>
        /// 生日
        /// </summary>
        public DateTime? Birthday { get; set; }
    }
}