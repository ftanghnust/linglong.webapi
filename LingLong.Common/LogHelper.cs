using log4net;
using System;
using System.IO;

namespace LingLong.Common
{
    public sealed class LogHelper
    {
        //记录Web的日志
        private static readonly ILog loggerWeb = LogManager.GetLogger("WebLogger");
        private static readonly ILog loggerDal = LogManager.GetLogger("DALLogger");

        public static void SetConfig()
        {
            log4net.Config.XmlConfigurator.Configure();
        }


        /// <summary>
        /// 记录Web层的消息日志
        /// </summary>
        /// <param name="message"></param>
        public static void WriteWebInfoLog(string message)
        {
            loggerWeb.Info(message);
        }

        /// <summary>
        /// 记录Web层的错误日志
        /// </summary>
        /// <param name="message"></param>
        public static void WriteWebErrorLog(string message)
        {
            loggerWeb.Error(message);
        }

        /// <summary>
        /// 记录Web层的错误日志
        /// </summary>
        /// <param name="message"></param>
        public static void WriteDalErrorLog(string message)
        {
            loggerWeb.Error(message);
        }

        public static void WriteLog(string msg)
        {
            string filePath = AppDomain.CurrentDomain.BaseDirectory + "Log";
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "Log\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            try
            {
                using (StreamWriter sw = File.AppendText(logPath))
                {
                    sw.WriteLine("消息：" + msg);
                    sw.WriteLine("时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sw.WriteLine("**************************************************");
                    sw.WriteLine();
                    sw.Flush();
                    sw.Close();
                    sw.Dispose();
                }
            }
            catch (IOException e)
            {
                using (StreamWriter sw = File.AppendText(logPath))
                {
                    sw.WriteLine("异常：" + e.Message);
                    sw.WriteLine("时间：" + DateTime.Now.ToString("yyy-MM-dd HH:mm:ss"));
                    sw.WriteLine("**************************************************");
                    sw.WriteLine();
                    sw.Flush();
                    sw.Close();
                    sw.Dispose();
                }
            }
        }

    }
}
