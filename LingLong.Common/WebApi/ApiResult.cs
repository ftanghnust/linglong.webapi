using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LingLong.Common.WebApi
{
    /// <summary>
    ///  Api 统一返回结果的助手类
    /// </summary> 
    public class ApiResult
    {
        /// <summary>
        /// 获取成功的返回结果
        /// </summary>
        /// <param name="data">需要返回的数据</param>
        /// <param name="message">提示信息</param>
        /// <returns></returns>
        public static HttpResponseMessage Success(object data,string message="")
        {
            return ApiResult.Get(0, message, data);
        }

        /// <summary>
        /// 获取错误的返回结果
        /// </summary>
        /// <param name="message">错误信息</param>
        /// <param name="code">错误编码</param>
        /// <returns></returns>
        public static HttpResponseMessage Error(string message = "", int code = 1)
        {
            return ApiResult.Get(code, message, "");
        }

        /// <summary>
        /// 获取错误的返回结果
        /// </summary>
        /// <param name="code">结果编码</param>
        /// <param name="message">提示信息</param>
        /// <param name="data">返回数据</param>
        /// <returns></returns>
        public static HttpResponseMessage Get(int code, string message, object data)
        {
            var model = new ApiResultModel() { Code = code, Message = message, Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Data = data };
            String value = JsonConvert.SerializeObject(model);

            HttpResponseMessage result = new HttpResponseMessage { Content = new StringContent(value, Encoding.GetEncoding("UTF-8"), "application/json") };
            return result;
        }

    }

    /// <summary>
    /// Api 统一返回实体类
    /// </summary>
    public class ApiResultModel
    {
        /// <summary>
        /// 错误编码，0表示成功，其他则是失败
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 服务器时间
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// 提示信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 返回的数据，比如对象、结果集等等
        /// </summary>
        public object Data { get; set; }

    }

}
