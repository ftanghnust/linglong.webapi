using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web;
using System.Xml;
using System.Data;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Configuration;
using LingLong.Common.WebApi;
using System.Net.Http;

namespace LingLong.Common
{
    public class WxHelper
    {
        /// <summary>
        /// 临时登录凭证code 获取 session_key 和 openid 
        /// </summary>
        /// <param name="JsCode">登录时获取的 code</param>
        /// <param name="AppID">小程序唯一标识</param>
        /// <param name="AppSecret">小程序的 app secret</param>
        /// <returns></returns>
        public static string GetOpenId(string JsCode, string AppID, string AppSecret)
        {
            string serviceAddress = string.Format("https://api.weixin.qq.com/sns/jscode2session?appid={0}&secret={1}&js_code={2}&grant_type=authorization_code",
                AppID, AppSecret, JsCode);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
            request.Method = "GET";
            request.ContentType = "textml;charset=UTF-8";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

        /// <summary>
        /// 通过appID和appsecret获取AccessToken
        /// </summary>
        /// <param name="AppID"></param>
        /// <param name="AppSecret"></param>
        /// <returns></returns>
        public static string GetAccessToken(string AppID, string AppSecret)
        {
            string AccessToken = string.Empty;
            string strUrl = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid=" + AppID + "&secret=" + AppSecret;
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(strUrl);
            req.Method = "GET";
            using (WebResponse wr = req.GetResponse())
            {
                HttpWebResponse myResponse = (HttpWebResponse)req.GetResponse();
                StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
                string content = reader.ReadToEnd();//在这里对AccessToken 赋值  
                AccessToken = GetJsonValue(content, "access_token");
            }
            return AccessToken;
        }

        /// <summary>
        /// 微信支付统一下单接口
        /// </summary>
        /// <param name="openid">微信唯一标识</param>
        /// <param name="totalfee">支付金额（分）</param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static HttpResponseMessage Pay(string openid, int totalfee, Model.t_reward model)
        {
            //WriteLog(string.Format("totalfee:{0}", totalfee));
            string appid = ConfigurationManager.AppSettings["appid"];
            string secret = ConfigurationManager.AppSettings["secret"];
            string key = ConfigurationManager.AppSettings["key"];
            string mch_id = ConfigurationManager.AppSettings["mch_id"];
            string ip = ConfigurationManager.AppSettings["ip"];
            string PayResulturl = ConfigurationManager.AppSettings["PayResulturl"];
            string strcode = "玲珑-打赏";                  //商品描述交易字段格式根据不同的应用场景按照以下格式：APP——需传入应用市场上的APP名字-实际商品名称，天天爱消除-游戏充值。
            byte[] buffer = Encoding.UTF8.GetBytes(strcode);
            string body = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

            string output = "";

            if (!string.IsNullOrEmpty(openid))
            {
                System.Random Random = new System.Random();
                var dic = new Dictionary<string, string>
                {
                     {"appid", appid},
                     {"mch_id", mch_id},
                     {"nonce_str", GetRandomString(20)},
                     {"body",body},
                     {"out_trade_no", DateTime.Now.ToString("yyyyMMddHHmmssfff") + Random.Next(999).ToString()},//商户自己的订单号码
                     {"total_fee",totalfee.ToString()},
                     {"spbill_create_ip",ip},//服务器的IP地址
                     {"notify_url",PayResulturl},//异步通知的地址，不能带参数
                     {"trade_type","JSAPI" },
                     {"openid",openid}
                };

                //加入签名
                dic.Add("sign", GetSignString(dic));
                var sb = new StringBuilder();
                sb.Append("<xml>");
                foreach (var d in dic)
                {
                    sb.Append("<" + d.Key + ">" + d.Value + "</" + d.Key + ">");
                }
                sb.Append("</xml>");

                HttpWebResponse response = CreatePostHttpResponse("https://api.mch.weixin.qq.com/pay/unifiedorder", sb.ToString(), Encoding.GetEncoding("UTF-8"));
                Stream stream = response.GetResponseStream();       //获取响应的字符串流
                StreamReader sr = new StreamReader(stream);         //创建一个stream读取流
                string html = sr.ReadToEnd();                       //从头读到尾，放到字符串html
                //对请求返回值 进行处理
                DataSet ds = new DataSet();
                StringReader stram = new StringReader(html);
                XmlTextReader reader = new XmlTextReader(stram);
                ds.ReadXml(reader);
                string return_code = ds.Tables[0].Rows[0]["return_code"].ToString();
                if (return_code.ToUpper() == "SUCCESS")
                {
                    //通信成功
                    string result_code = ds.Tables[0].Rows[0]["result_code"].ToString();//业务结果
                    if (result_code.ToUpper() == "SUCCESS")
                    {
                        var res = new Dictionary<string, string>
                        {
                            {"appId", appid},
                            {"timeStamp", GetTimeStamp()},
                            {"nonceStr", dic["nonce_str"]},
                            {"package",  "prepay_id="+ds.Tables[0].Rows[0]["prepay_id"].ToString()},
                            {"signType", "MD5"}
                        };
                        //在服务器上签名
                        res.Add("paySign", GetSignString(res));
                        string signapp = JsonConvert.SerializeObject(res);
                        if (!string.IsNullOrEmpty(openid))
                        {
                            //存储订单信息
                            model.openid = openid;
                            model.out_trade_no = dic["out_trade_no"];
                            model.pay_price = totalfee;
                            model.prepay_id = ds.Tables[0].Rows[0]["prepay_id"].ToString();
                            model.order_time = DateTime.Now;
                            Bll.t_rewardBLL.Insert(model);
                        }

                        return ApiResult.Success(signapp);
                    }
                    else
                    {
                        output = ds.Tables[0].Rows[0]["err_code_des"].ToString();
                    }
                }
                else
                {
                    output = ds.Tables[0].Rows[0]["return_msg"].ToString();
                }
            }
            return ApiResult.Error(output);
        }

        #region 支付辅助方法
        /// <summary>
        /// 从字符串里随机得到，规定个数的字符串.
        /// </summary>
        /// <param name="allChar"></param>
        /// <param name="CodeCount"></param>
        /// <returns></returns>
        private static string GetRandomString(int CodeCount)
        {
            string allChar = "1,2,3,4,5,6,7,8,9,A,B,C,D,E,F,G,H,i,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z";
            string[] allCharArray = allChar.Split(',');
            string RandomCode = "";
            int temp = -1;
            Random rand = new Random();
            for (int i = 0; i < CodeCount; i++)
            {
                if (temp != -1)
                {
                    rand = new Random(temp * i * ((int)DateTime.Now.Ticks));
                }
                int t = rand.Next(allCharArray.Length - 1);
                while (temp == t)
                {
                    t = rand.Next(allCharArray.Length - 1);
                }
                temp = t;
                RandomCode += allCharArray[t];
            }
            return RandomCode;
        }
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受   
        }
        private static HttpWebResponse CreatePostHttpResponse(string url, string datas, Encoding charset)
        {
            HttpWebRequest request = null;
            //HTTPSQ请求
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            request = WebRequest.Create(url) as HttpWebRequest;
            request.ProtocolVersion = HttpVersion.Version10;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            StringBuilder buffer = new StringBuilder();
            buffer.AppendFormat(datas);
            byte[] data = charset.GetBytes(buffer.ToString());
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            return request.GetResponse() as HttpWebResponse;
        }
        private static string GetSignString(Dictionary<string, string> dic)
        {
            string key = System.Web.Configuration.WebConfigurationManager.AppSettings["key"].ToString();//商户平台 API安全里面设置的KEY  32位长度                                                           
            dic = dic.OrderBy(d => d.Key).ToDictionary(d => d.Key, d => d.Value);    //排序
            //连接字段
            var sign = dic.Aggregate("", (current, d) => current + (d.Key + "=" + d.Value + "&"));
            sign += "key=" + key;
            //MD5
            sign = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(sign, "MD5").ToUpper();
            //System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            //sign = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(sign))).Replace("-", null);
            return sign;
        }

        /// <summary>  
        /// 获取时间戳  
        /// </summary>  
        /// <returns></returns>  
        private static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }
        #endregion

        /// <summary>
        /// B接口-微信小程序带参数二维码的生成
        /// </summary>
        /// <param name="access_token"></param>
        /// <param name="scene"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public static string CreateWxCode(string access_token, string scene, string page)
        {
            string ret = string.Empty;
            try
            {
                string DataJson = string.Empty;
                string url = string.Format("https://api.weixin.qq.com/wxa/getwxacodeunlimit?access_token={0}", access_token);
                DataJson = "{";
                DataJson += string.Format("\"scene\":\"{0}\",", scene);//所要传的参数用,分看
                //DataJson += string.Format("\"width\":\"{0}\",", 124);
                if (!string.IsNullOrEmpty(page))
                {
                    DataJson += string.Format("\"page\":\"{0}\",", page);//扫码所要跳转的地址，根路径前不要填加'/',不能携带参数（参数请放在scene字段里），如果不填写这个字段，默认跳主页面
                }
                DataJson += "\"line_color\":{";
                DataJson += string.Format("\"r\":\"{0}\",", "0");
                DataJson += string.Format("\"g\":\"{0}\",", "0");
                DataJson += string.Format("\"b\":\"{0}\"", "0");
                DataJson += "}";
                DataJson += "}";
                //DataJson的配置见小程序开发文档，B接口：https://mp.weixin.qq.com/debug/wxadoc/dev/api/qrcode.html
                ret = PostMoths(url, DataJson);
                if (ret.Length > 0)
                {
                    //对图片进行存储操作，下次可直接调用你存储的图片，不用再调用接口
                }
            }
            catch (Exception e)
            {
                ret = e.Message;
            }
            return ret;//返回图片地址
        }

        /// <summary>
        /// 请求处理，返回二维码图片
        /// </summary>
        /// <param name="url"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string PostMoths(string url, string param)
        {
            string strURL = url;
            System.Net.HttpWebRequest request;
            request = (System.Net.HttpWebRequest)WebRequest.Create(strURL);
            request.Method = "POST";
            request.ContentType = "application/json;charset=UTF-8";
            string paraUrlCoded = param;
            byte[] payload;
            payload = System.Text.Encoding.UTF8.GetBytes(paraUrlCoded);
            request.ContentLength = payload.Length;
            Stream writer = request.GetRequestStream();
            writer.Write(payload, 0, payload.Length);
            writer.Close();
            System.Net.HttpWebResponse response;
            response = (System.Net.HttpWebResponse)request.GetResponse();
            System.IO.Stream s;
            s = response.GetResponseStream();//返回图片数据流
            byte[] tt = StreamToBytes(s);//将数据流转为byte[]

            //在文件名前面加上时间，以防重名
            string imgName = Guid.NewGuid().ToString("N") + ".jpg";
            //文件存储相对于当前应用目录的虚拟目录
            string path = "/WxCodeImage/" + DateTime.Now.ToString("yyyy-MM-dd") + "/";
            //获取相对于应用的基目录,创建目录
            string imgPath = System.AppDomain.CurrentDomain.BaseDirectory + path;     //通过此对象获取文件名
            if (!Directory.Exists(imgPath))
            {
                Directory.CreateDirectory(imgPath);
            }
            System.IO.File.WriteAllBytes(System.Web.HttpContext.Current.Server.MapPath(path + imgName), tt);//讲byte[]存储为图片
            return "WxCodeImage/" + DateTime.Now.ToString("yyyy-MM-dd") + "/" + imgName;
        }

        ///将数据流转为byte[]
        public static byte[] StreamToBytes(Stream stream)
        {
            List<byte> bytes = new List<byte>();
            int temp = stream.ReadByte();
            while (temp != -1)
            {
                bytes.Add((byte)temp);
                temp = stream.ReadByte();
            }
            return bytes.ToArray();
        }

        /// <summary>
        /// 获取Json字符串某节点的值
        /// </summary>
        private static string GetJsonValue(string jsonStr, string key)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(jsonStr))
            {
                key = "\"" + key.Trim('"') + "\"";
                int index = jsonStr.IndexOf(key) + key.Length + 1;
                if (index > key.Length + 1)
                {
                    //先截逗号，若是最后一个，截“｝”号，取最小值
                    int end = jsonStr.IndexOf(',', index);
                    if (end == -1)
                    {
                        end = jsonStr.IndexOf('}', index);
                    }

                    result = jsonStr.Substring(index, end - index);
                    result = result.Trim(new char[] { '"', ' ', '\'' }); //过滤引号或空格
                }
            }
            return result;
        }


        /// <summary>
        /// 企业付款给个人
        /// </summary>       
        /// <returns></returns>
        public static string EnterprisePay(string Bill_No, string toOpenid, decimal Charge_Amt, string title)
        {
            string APPID = ConfigurationManager.AppSettings["BusinessAppID"];
            string PARTNER = ConfigurationManager.AppSettings["mch_id"];
            string IPAddress = ConfigurationManager.AppSettings["ip"];

            //公众账号appid mch_appid 是 wx8888888888888888 String 微信分配的公众账号ID（企业号corpid即为此appId） 
            //商户号 mchid 是 1900000109 String(32) 微信支付分配的商户号 
            //设备号 device_info 否 013467007045764 String(32) 微信支付分配的终端设备号 
            //随机字符串 nonce_str 是 5K8264ILTKCH16CQ2502SI8ZNMTM67VS String(32) 随机字符串，不长于32位 
            //签名 sign 是 C380BEC2BFD727A4B6845133519F3AD6 String(32) 签名，详见签名算法 
            //商户订单号 partner_trade_no 是 10000098201411111234567890 String 商户订单号，需保持唯一性 
            //用户openid openid 是 oxTWIuGaIt6gTKsQRLau2M0yL16E String 商户appid下，某用户的openid 
            //校验用户姓名选项 check_name 是 OPTION_CHECK String NO_CHECK：不校验真实姓名 
            //FORCE_CHECK：强校验真实姓名（未实名认证的用户会校验失败，无法转账） 
            //OPTION_CHECK：针对已实名认证的用户才校验真实姓名（未实名认证用户不校验，可以转账成功） 
            //收款用户姓名 re_user_name 可选 马花花 String 收款用户真实姓名。 
            // 如果check_name设置为FORCE_CHECK或OPTION_CHECK，则必填用户真实姓名 
            //金额 amount 是 10099 int 企业付款金额，单位为分 
            //企业付款描述信息 desc 是 理赔 String 企业付款操作说明信息。必填。 
            //Ip地址 spbill_create_ip 是 192.168.0.1 String(32) 调用接口的机器Ip地址 

            Bill_No = PARTNER + GetTimeStamp() + Bill_No;  //订单号组成 商户号 + 随机时间串 + 记录ID

            //设置package订单参数
            Dictionary<string, string> dic = new Dictionary<string, string>();

            string total_fee = (Charge_Amt * 100).ToString("f0");
            string wx_nonceStr = Guid.NewGuid().ToString().Replace("-", "");    //Interface_WxPay.getNoncestr();

            dic.Add("mch_appid", APPID);
            dic.Add("mchid", PARTNER);//财付通帐号商家
            //dic.Add("device_info", "013467007045711");//可为空
            dic.Add("nonce_str", wx_nonceStr);
            dic.Add("partner_trade_no", Bill_No);
            dic.Add("openid", toOpenid);
            dic.Add("check_name", "NO_CHECK");
            dic.Add("amount", total_fee);
            dic.Add("desc", title);                   //商品描述
            dic.Add("spbill_create_ip", IPAddress);   //用户的公网ip，不是商户服务器IP
            //生成签名
            string get_sign = GetSignString(dic);

            LogHelper.WriteLog("第一步 get_sign：" + get_sign);

            string _req_data = "<xml>";
            _req_data += "<mch_appid>" + APPID + "</mch_appid>";
            _req_data += "<mchid>" + PARTNER + "</mchid>";
            _req_data += "<nonce_str>" + wx_nonceStr + "</nonce_str>";
            _req_data += "<partner_trade_no>" + Bill_No + "</partner_trade_no>";
            _req_data += "<openid>" + toOpenid + "</openid>";
            _req_data += "<check_name>NO_CHECK</check_name>";
            _req_data += "<amount>" + total_fee + "</amount>";
            _req_data += "<desc>" + title + "</desc>";
            _req_data += "<spbill_create_ip>" + IPAddress +"</spbill_create_ip>";
            _req_data += "<sign>" + get_sign + "</sign>";
            _req_data += "</xml>";

            LogHelper.WriteLog("企业付款生成的xml：" + _req_data.Trim());

            var result = HttpPost("https://api.mch.weixin.qq.com/mmpaymkttransfers/promotion/transfers", _req_data.Trim(), true, 300);
            //var result = HttpPost(URL, _req_data, Encoding.UTF8);

            LogHelper.WriteLog("返回结果：" + result);

            return result;
        }

        /// <summary>
        /// post提交支付
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="url"></param>
        /// <param name="isUseCert">是否使用证书</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static string HttpPost(string url, string xml, bool isUseCert, int timeout)
        {
            //=======【证书路径设置】===================================== 
            /* 证书路径,注意应该填写绝对路径（仅退款、撤销订单时需要）
            */
            string SSLCERT_PATH = ConfigurationManager.AppSettings["SSLCERT_PATH"]; //"E:\\cert\\apiclient_cert.p12";
            string SSLCERT_PASSWORD = ConfigurationManager.AppSettings["mch_id"]; 

            System.GC.Collect();//垃圾回收，回收没有正常关闭的http连接

            string result = "";//返回结果

            HttpWebRequest request = null;
            HttpWebResponse response = null;
            Stream reqStream = null;

            try
            {
                //设置最大连接数
                ServicePointManager.DefaultConnectionLimit = 200;
                //设置https验证方式
                if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    ServicePointManager.ServerCertificateValidationCallback =
                            new RemoteCertificateValidationCallback(CheckValidationResult);
                }

                /***************************************************************
                * 下面设置HttpWebRequest的相关属性
                * ************************************************************/
                request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = "POST";
                request.Timeout = timeout * 1000;

                //设置代理服务器
                //WebProxy proxy = new WebProxy();                          //定义一个网关对象
                //proxy.Address = new Uri(PROXY_URL);              //网关服务器端口:端口
                //request.Proxy = proxy;

                //设置POST的数据类型和长度
                request.ContentType = "text/xml";
                byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
                request.ContentLength = data.Length;

                //是否使用证书
                if (isUseCert)
                {
                    //string path = HttpContext.Current.Request.PhysicalApplicationPath;
                    //X509Certificate2 cert = new X509Certificate2(path + SSLCERT_PATH, SSLCERT_PASSWORD);

                    //将上面的改成
                    X509Certificate2 cert = new X509Certificate2( SSLCERT_PATH, SSLCERT_PASSWORD, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);//线上发布需要添加

                    request.ClientCertificates.Add(cert);

                    LogHelper.WriteLog("证书路径：" + ( SSLCERT_PATH));

                    //Vincent._Log.SaveMessage("WxPayApi:PostXml used cert");
                }

                //往服务器写入数据
                reqStream = request.GetRequestStream();
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();

                //获取服务端返回
                response = (HttpWebResponse)request.GetResponse();

                //获取服务端返回数据
                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                result = sr.ReadToEnd().Trim();
                sr.Close();
            }
            catch (System.Threading.ThreadAbortException e)
            {
                LogHelper.WriteLog("HttpService:Thread - caught ThreadAbortException - resetting.");
                LogHelper.WriteLog("Exception message:" + e.Message);
                System.Threading.Thread.ResetAbort();
            }
            catch (WebException e)
            {
                LogHelper.WriteLog("HttpService" + e.ToString());
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    LogHelper.WriteLog("HttpService:StatusCode : " + ((HttpWebResponse)e.Response).StatusCode);
                    LogHelper.WriteLog("HttpService:StatusDescription : " + ((HttpWebResponse)e.Response).StatusDescription);
                }
                throw new Exception(e.ToString());
            }
            catch (Exception e)
            {
                LogHelper.WriteLog("HttpService" + e.ToString());
                throw new Exception(e.ToString());
            }
            finally
            {
                //关闭连接和流
                if (response != null)
                {
                    response.Close();
                }
                if (request != null)
                {
                    request.Abort();
                }
            }
            return result;
        }
    }
}
