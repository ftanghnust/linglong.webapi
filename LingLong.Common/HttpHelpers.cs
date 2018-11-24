using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LingLong.Common
{
    public class HttpHelpers
    {
        private static HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Http Get 请求 （同步）
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetSync(string url)
        {
            try
            {
                Task<HttpResponseMessage> taskResponse = null;
                Uri uri = new Uri(url);
                taskResponse = httpClient.GetAsync(uri);
                string result = taskResponse.Result.Content.ReadAsStringAsync().Result.ToString();
                return result;
            }
            catch (Exception ex)
            {
                return string.Format("ERROR：{0}！", ex.Message);
            }
        }

        /// <summary>
        /// Http Get 请求
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string> GetAsync(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                HttpResponseMessage response = await httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                string result = await response.Content.ReadAsStringAsync();
                return result;
            }
            catch (Exception ex)
            {
                return string.Format("ERROR：{0}！", ex.Message);
            }
        }

        /// <summary>
        /// Http Post 请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="strParams">json格式的参数</param>
        /// <returns></returns>
        public static async Task<string> PostAsync(string url, string strParams)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                Uri uri = new Uri(url);
                HttpContent content = new StringContent(strParams);
                content.Headers.ContentType.CharSet = "UTF-8";
                content.Headers.ContentType.MediaType = "application/json";
                //忽略错误的证书
                if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, error) =>
                    {
                        return true;
                    };
                }
                HttpResponseMessage response = await httpClient.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
                content = response.Content;
                string result = await content.ReadAsStringAsync();
                return result;
            }
            catch (Exception ex)
            {
                return string.Format("ERROR：{0}！", ex.Message);
            }
        }
        public static string PostAsyncWithPFX(string url, string strParams)
        {
            try
            {
           
                Uri uri = new Uri(url);
                HttpContent content = new StringContent(strParams);
                content.Headers.ContentType.CharSet = "UTF-8";

                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateServerCertificate);

                HttpWebRequest httpRequest = (HttpWebRequest)HttpWebRequest.Create(url);

                X509Certificate cerCaiShang = new X509Certificate(string.Format("{0}/App_Data/pfx.pfx", System.AppDomain.CurrentDomain.BaseDirectory), "123456");
                byte[] byteArray = Encoding.UTF8.GetBytes("json=" + strParams);
                httpRequest.ClientCertificates.Add(cerCaiShang);
                //2． 初始化HttpWebRequest对象
                httpRequest.Method = "POST";
                httpRequest.ContentType = "application/json";
                httpRequest.Accept = "";
                httpRequest.ContentLength = byteArray.Length;
                //3． 附加要POST给服务器的数据到HttpWebRequest对象(附加POST数据的过程比较特殊，它并没有提供一个属性给用户存取，需要写入HttpWebRequest对象提供的一个stream里面。)
                Stream newStream = httpRequest.GetRequestStream();//创建一个Stream,赋值是写入HttpWebRequest对象提供的一个stream里面
                newStream.Write(byteArray, 0, byteArray.Length);
                newStream.Close();
                //4． 读取服务器的返回信息
                HttpWebResponse response = (HttpWebResponse)httpRequest.GetResponse();
                StreamReader php = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string phpend = php.ReadToEnd();
                return phpend;

            }
            catch (Exception ex)
            {
                return string.Format("ERROR：{0}！", ex.Message);
            }
        }
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)

        {

            if (sslPolicyErrors == SslPolicyErrors.None)

                return true;

            return false;

        }
    }
}
