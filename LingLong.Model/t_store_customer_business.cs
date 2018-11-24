//-----------------------------------------------------------------------
// <copyright file=" t_store_customer_business.cs" company="LingLong Enterprises">
// * Copyright (C) 2018 LingLong Enterprises All Rights Reserved
// * version : 4.0.30319.42000
// * author  : auto generated by T4
// * FileName: t_store_customer_business.cs
// * history : Created by T4 2018-07-06 23:37:34 
// </copyright>
//-----------------------------------------------------------------------
using System;

namespace LingLong.Model
{
    /// <summary>
    /// t_store_customer_business Entity Model
    /// </summary>    
    [Serializable]
    public class t_store_customer_business
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
        /// 客户Id
        /// </summary>
        public int CustomerId { get; set; }
    
        /// <summary>
        /// 商户Id
        /// </summary>
        public int BusinessId { get; set; }

        /// <summary>
        /// 客户备注
        /// </summary>
        public string Remark { get; set; }
        
    }
}
