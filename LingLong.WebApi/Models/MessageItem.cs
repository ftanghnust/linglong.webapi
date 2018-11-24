using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models
{
    public class MessageItem
    {
        /// <summary>
        /// 门店Id
        /// </summary>
        public int StoreId { get; set; }
        /// <summary>
        /// 消息列表中每条消息中的用户Id
        /// </summary>
        public int UserId { get; set; }
        /// <summary>
        /// 消息列表中每条消息中的用户OpenId
        /// </summary>
        public string UserOpenId { get; set; }
        /// <summary>
        /// 发送消息内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 发送时间
        /// </summary>
        public DateTime SendTime { get; set; }
        /// <summary>
        /// 消息列表中每条消息 没有阅读的消息数目
        /// </summary>
        public int NoReadCount { get; set; }
        /// <summary>
        /// 消息列表中每条消息 用户昵称
        /// </summary>
        public string Nickname { get; set; }
        /// <summary>
        /// 消息列表中每条消息 用户图像
        /// </summary>
        public string AvatarUrl { get; set; }
    }
}