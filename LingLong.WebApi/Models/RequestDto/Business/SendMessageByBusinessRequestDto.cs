using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Business
{
    /// <summary>
    /// 商户发送消息 请求对象
    /// </summary>
    public class SendMessageByBusinessRequestDto
    {
        /// <summary>
        /// 门店Id
        /// </summary>
        public int StoreId { get; set; }
        /// <summary>
        /// 发送人Id
        /// </summary>
        public int SendUserId { get; set; }
        /// <summary>
        /// 接受人Id
        /// </summary>
        public int AcceptUserId { get; set; }
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 消息类型 0：系统消息；1：聊天消息
        /// </summary>
        public int MessageType { get; set; }
        
    }
}