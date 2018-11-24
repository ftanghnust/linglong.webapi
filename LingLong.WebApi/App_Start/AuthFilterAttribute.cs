using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace LingLong.WebApi.App_Start
{
    public class AuthFilterAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            //如果用户方位的Action带有AllowAnonymousAttribute，则不进行授权验证
            if (actionContext.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Any())
            {
                return;
            }
            //var verifyResult = actionContext.Request.Headers.Authorization != null &&  //要求请求中需要带有Authorization头
            //                   actionContext.Request.Headers.Authorization.Parameter == "123456"; //并且Authorization参数为123456则验证通过

            DateTime EndTime = Convert.ToDateTime("2018-11-30");
            if (DateTime.Now > EndTime)
            {
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, new HttpError(""));
            }
        }
    }
}