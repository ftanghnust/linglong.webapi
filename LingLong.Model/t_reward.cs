//-----------------------------------------------------------------------
// <copyright file=" t_reward.cs" company="LingLong Enterprises">
// * Copyright (C) 2018 LingLong Enterprises All Rights Reserved
// * version : 4.0.30319.42000
// * author  : auto generated by T4
// * FileName: t_reward.cs
// * history : Created by T4 2018-07-06 23:37:34 
// </copyright>
//-----------------------------------------------------------------------
using System;

namespace LingLong.Model
{
    /// <summary>
    /// t_reward Entity Model
    /// </summary>    
    [Serializable]
    public class t_reward
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID { get; set; }
    
        /// <summary>
        /// 门店Id
        /// </summary>
        public int StoreId { get; set; }
    
        /// <summary>
        /// 支付订单号
        /// </summary>
        public string PaymentNo { get; set; }
    
        /// <summary>
        /// 打赏人Id
        /// </summary>
        public int CustomerId { get; set; }
    
        /// <summary>
        /// 被打赏人Id
        /// </summary>
        public int BusinessId { get; set; }

        /// <summary>
        /// 打赏商品Id
        /// </summary>
        public int GoodsId { get; set; }

        /// <summary>
        /// 打赏时间
        /// </summary>
        public DateTime RewardTime { get; set; }
    
        /// <summary>
        /// 打赏金额
        /// </summary>
        public decimal Money { get; set; }
    
        /// <summary>
        /// 删除标志（0：未删除 1：已删除）
        /// </summary>
        public int IsDeleted { get; set; }
    
        /// <summary>
        /// 删除用户Id
        /// </summary>
        public long DeleterUserId { get; set; }
    
        /// <summary>
        /// 最后编辑时间
        /// </summary>
        public DateTime LastModificationTime { get; set; }
    
        /// <summary>
        /// 最后编辑用户Id
        /// </summary>
        public long LastModifierUserId { get; set; }
    
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }
    
        /// <summary>
        /// 创建用户Id
        /// </summary>
        public long CreatorUserId { get; set; }

        /// <summary>
        /// openid
        /// </summary>
        public string openid { get; set; }

        /// <summary>
        /// prepay_id
        /// </summary>
        public string prepay_id { get; set; }

        /// <summary>
        /// out_trade_no
        /// </summary>
        public string out_trade_no { get; set; }

        /// <summary>
        /// pay_price
        /// </summary>
        public int pay_price { get; set; }

        /// <summary>
        /// result_code
        /// </summary>
        public string result_code { get; set; }

        /// <summary>
        /// order_time
        /// </summary>
        public DateTime order_time { get; set; }
    }
}
