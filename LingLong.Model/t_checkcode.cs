//-----------------------------------------------------------------------
// <copyright file=" t_checkcode.cs" company="LingLong Enterprises">
// * Copyright (C) 2018 LingLong Enterprises All Rights Reserved
// * version : 4.0.30319.42000
// * author  : auto generated by T4
// * FileName: t_checkcode.cs
// * history : Created by T4 2018-07-20 13:25:36 
// </copyright>
//-----------------------------------------------------------------------
using System;

namespace LingLong.Model
{
    /// <summary>
    /// t_checkcode Entity Model
    /// </summary>    
    [Serializable]
    public class t_checkcode
    {
        /// <summary>
        /// 
        /// </summary>
        public int ID { get; set; }
    
        /// <summary>
        /// 手机号码
        /// </summary>
        public string PhoneNumber { get; set; }
    
        /// <summary>
        /// 验证码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 类型： 1：代理商注册 2： 商户超级管理员注册
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
