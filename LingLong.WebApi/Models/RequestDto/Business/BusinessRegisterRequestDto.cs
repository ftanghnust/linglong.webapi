using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Business
{
    /// <summary>
    /// 商户(管理员、门店经理、服务人员)注册 请求对象
    /// </summary>
    public class BusinessRegisterRequestDto
    {
        /// <summary>
        /// 角色Id（1：管理员 2：门店经理 3：服务人员）
        /// </summary>
        public int RoleId { get; set; }
        /// <summary>
        /// 门店Id
        /// </summary>
        public int StoreId { get; set; }
        /// <summary>
        /// 微信唯一标识(来自微信接口)
        /// </summary>
        public string OpenId { get; set; }
 
        /// <summary>
        /// 昵称(来自微信接口)
        /// </summary>
        public string Nickname { get; set; }
        /// <summary>
        /// 性别(来自微信接口)
        /// </summary>
        public int Gender { get; set; }
        /// <summary>
        /// 头像链接(来自微信接口)
        /// </summary>
        public string AvatarUrl { get; set; }
        /// <summary>
        /// 微信开放平台唯一标识(来自微信接口)
        /// </summary>
        public string UnionId { get; set; }
        /// <summary>
        /// 小程序的appid(来自微信接口)
        /// </summary>
        public string AppId { get; set; }
        /// <summary>
        /// 所在市(来自微信接口)
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// 所在省(来自微信接口)
        /// </summary>
        public string Province { get; set; }
        /// <summary>
        /// 所在国家(来自微信接口)
        /// </summary>
        public string Country { get; set; }
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