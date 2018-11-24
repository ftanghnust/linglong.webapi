﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Exceptions;
using Aliyun.Acs.Core.Profile;

using Aliyun.Acs.Dysmsapi.Model.V20170525;

namespace LingLong.Common
{
    public class SMSHelper
    {
        /// <summary>
        ///  发送注册验证码
        /// </summary>
        /// <param name="PhoneNumbers"></param>
        /// <param name="Code"></param>
        /// <returns></returns>
        public static string SendRegisterCodeByAgent(string PhoneNumbers, string Code)
        {
            String product = "Dysmsapi";//短信API产品名称
            String domain = "dysmsapi.aliyuncs.com";//短信API产品域名
            String accessKeyId = ConfigurationManager.AppSettings["AccessKeyId"].ToString();
            String accessKeySecret = ConfigurationManager.AppSettings["AccessKeySecret"].ToString();
            IClientProfile profile = DefaultProfile.GetProfile("cn-hangzhou", accessKeyId, accessKeySecret);
            DefaultProfile.AddEndpoint("cn-hangzhou", "cn-hangzhou", product, domain);
            IAcsClient acsClient = new DefaultAcsClient(profile);
            SendSmsRequest request = new SendSmsRequest();
            try
            {
                //必填:待发送手机号。支持以逗号分隔的形式进行批量调用，批量上限为20个手机号码,批量调用相对于单条调用及时性稍有延迟,验证码类型的短信推荐使用单条调用的方式
                request.PhoneNumbers = PhoneNumbers;
                //必填:短信签名-可在短信控制台中找到
                request.SignName = "阿里云短信测试专用";
                //必填:短信模板-可在短信控制台中找到
                request.TemplateCode = "SMS_137690069";
                //可选:模板中的变量替换JSON串,如模板内容为"亲爱的${name},您的验证码为${code}"时,此处的值为
                request.TemplateParam = string.Format("{{\"code\":\"{0}\"}}", Code);
                //可选:outId为提供给业务方扩展字段,最终在短信回执消息中将此值带回给调用者
                request.OutId = "21212121211";
                //请求失败这里会抛ClientException异常
                SendSmsResponse sendSmsResponse = acsClient.GetAcsResponse(request);

                return sendSmsResponse.Message;
            }
            catch (ServerException ex)
            {
                return ex.Message;
            }
            catch (ClientException ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        ///  发送注册验证码
        /// </summary>
        /// <param name="PhoneNumbers"></param>
        /// <param name="Code"></param>
        /// <returns></returns>
        public static string SendRegisterCodeByBusiness(string PhoneNumbers, string Code)
        {
            String product = "Dysmsapi";//短信API产品名称
            String domain = "dysmsapi.aliyuncs.com";//短信API产品域名
            String accessKeyId = ConfigurationManager.AppSettings["AccessKeyId"].ToString();
            String accessKeySecret = ConfigurationManager.AppSettings["AccessKeySecret"].ToString();
            IClientProfile profile = DefaultProfile.GetProfile("cn-hangzhou", accessKeyId, accessKeySecret);
            DefaultProfile.AddEndpoint("cn-hangzhou", "cn-hangzhou", product, domain);
            IAcsClient acsClient = new DefaultAcsClient(profile);
            SendSmsRequest request = new SendSmsRequest();
            try
            {
                //必填:待发送手机号。支持以逗号分隔的形式进行批量调用，批量上限为20个手机号码,批量调用相对于单条调用及时性稍有延迟,验证码类型的短信推荐使用单条调用的方式
                request.PhoneNumbers = PhoneNumbers;
                //必填:短信签名-可在短信控制台中找到
                request.SignName = "阿里云短信测试专用";
                //必填:短信模板-可在短信控制台中找到
                request.TemplateCode = "SMS_137690069";
                //可选:模板中的变量替换JSON串,如模板内容为"亲爱的${name},您的验证码为${code}"时,此处的值为
                request.TemplateParam = string.Format("{{\"code\":\"{0}\"}}", Code);
                //可选:outId为提供给业务方扩展字段,最终在短信回执消息中将此值带回给调用者
                request.OutId = "21212121211";
                //请求失败这里会抛ClientException异常
                SendSmsResponse sendSmsResponse = acsClient.GetAcsResponse(request);

                return sendSmsResponse.Message;
            }
            catch (ServerException ex)
            {
                return ex.Message;
            }
            catch (ClientException ex)
            {
                return ex.Message;
            }
        }
    }
}

