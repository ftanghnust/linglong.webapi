using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models.RequestDto.Business
{
    /// <summary>
    /// 新增消费记录 请求对象
    /// </summary>
    public class AddConsumptionRequestDto
    {
        /// <summary>
        /// 门店Id
        /// </summary>
        public int StoreId { get; set; }
        /// <summary>
        /// 服务人员Id
        /// </summary>
        public int BusinessId { get; set; }
        /// <summary>
        /// 客户Id
        /// </summary>
        public int CustomerId { get; set; }
        /// <summary>
        /// 包厢号
        /// </summary>
        public string BoxNumber { get; set; }
        /// <summary>
        /// 消费时间
        /// </summary>
        public DateTime ConsumeTime { get; set; }

    }
}