using LingLong.Bll;
using LingLong.Common;
using LingLong.Common.Enum;
using LingLong.Common.WebApi;
using LingLong.WebApi.Models;
using LingLong.WebApi.Models.RequestDto.Agent;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;

namespace LingLong.WebApi.Controllers
{
    /// <summary>
    /// 代理商相关接口 控制器
    /// 服务人员被禁用了，在门店详情统计的时候是否要算在里面？  不算
    /// 服务人员，门店被被禁用了， 打赏统计的时候是否要算在里面？ 算
    /// </summary>
    public class AgentController : ApiController
    {
        /// <summary>
        /// 指定手机号码发送验证码(发送间隔不能小于60秒)
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Agent/SendRegisterCodeByAgent")]
        public HttpResponseMessage SendRegisterCodeByAgent(SendRegisterCodeByAgentRequestDto RequestDto)
        {
            try
            {
                if (string.IsNullOrEmpty(RequestDto.PhoneNumber))
                {
                    return ApiResult.Error("手机号码不能为空");
                }
                if (RequestDto.PhoneNumber.Length != 11)
                {
                    return ApiResult.Error("手机号码格式错误");
                }
                var checkcode = t_checkcodeBLL.GetListByWhere(string.Format("Where PhoneNumber='{0}' and Type=1", RequestDto.PhoneNumber)).OrderByDescending(o => o.CreateTime).FirstOrDefault();
                if (checkcode != null)
                {
                    DateTime createTime = checkcode.CreateTime;
                    if ((DateTime.Now - createTime).TotalSeconds < 60)
                    {
                        return ApiResult.Error("发送验证码的时间间隔不能小于60秒");
                    }
                }
                string RandCode = this.RandCode();
                //发送验证码
                string result = SMSHelper.SendRegisterCodeByAgent(RequestDto.PhoneNumber, RandCode);
                if (result == "OK")
                {
                    t_checkcodeBLL.Insert(new Model.t_checkcode
                    {
                        PhoneNumber = RequestDto.PhoneNumber,
                        Code = RandCode,
                        Type = 1,
                        CreateTime = DateTime.Now
                    });
                    return ApiResult.Success("发送成功");
                }
                else
                {
                    return ApiResult.Success("发送失败");
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取 session_key 和 openid 
        /// </summary>
        /// <param name="JsCode">登录时获取的 code</param>
        /// <returns></returns>
        [HttpGet, Route("Agent/GetAgentOpenId")]
        public HttpResponseMessage GetAgentOpenId(string JsCode)
        {
            try
            {
                if (string.IsNullOrEmpty(JsCode))
                {
                    return ApiResult.Error("参数JsCode不能为空");
                }

                string AgentAppID = ConfigurationManager.AppSettings["AgentAppID"].ToString();
                string AgentAppSecret = ConfigurationManager.AppSettings["AgentAppSecret"].ToString();

                string result = WxHelper.GetOpenId(JsCode, AgentAppID, AgentAppSecret);

                return ApiResult.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取代理商信息
        /// </summary>
        /// <param name="OpenId">代理商微信唯一标识OpenId</param>
        /// <returns></returns>
        [HttpGet, Route("Agent/GetAgentInfo")]
        public HttpResponseMessage GetAgentInfo(string OpenId)
        {
            try
            {
                //1.参数验证
                if (string.IsNullOrEmpty(OpenId))
                {
                    return ApiResult.Error("参数OpenId不能为空");
                }

                //2.获取代理信息
                var agent = t_agentBLL.GetListByWhere(string.Format("where OpenId='{0}' and IsDeleted=0", OpenId)).FirstOrDefault();
                if (agent == null)
                {
                    return ApiResult.Error("没有该代理商信息(或已删除)");
                }

                //3.返回结果
                return ApiResult.Success(new
                {
                    agent.ID,
                    agent.IsManage,
                    agent.AvatarUrl,
                    agent.TrueName,
                    agent.AgentCode,
                    agent.PhoneNumber,
                    agent.NativePlace,
                    agent.Height,
                    Birthday = agent.Birthday?.ToString("yyyy-MM-dd"),
                    agent.State
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 代理商注册
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Agent/AgentRegister")]
        public HttpResponseMessage AgentRegister(AgentRegisterRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (string.IsNullOrEmpty(RequestDto.OpenId))
                {
                    return ApiResult.Error("参数OpenId不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.AgentName))
                {
                    return ApiResult.Error("代理商名称不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.PhoneNumber))
                {
                    return ApiResult.Error("代理商手机号码不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.RegisterCode))
                {
                    return ApiResult.Error("RegisterCode不能为空");
                }
                if (RequestDto.PhoneNumber.Length != 11)
                {
                    return ApiResult.Error("代理商手机号码格式错误");
                }
                if (!(new List<int> { 0, 1 }).Contains(RequestDto.IsManage))
                {
                    return ApiResult.Error("参数IsManage错误");
                }

                var checkcode = t_checkcodeBLL.GetListByWhere(string.Format("Where PhoneNumber='{0}' and Type=1", RequestDto.PhoneNumber)).OrderByDescending(o => o.CreateTime).FirstOrDefault();
                if (checkcode == null)
                {
                    return ApiResult.Error("该手机号码没有发送验证码");
                }
                else
                {
                    DateTime createTime = checkcode.CreateTime;
                    if ((DateTime.Now - createTime).TotalMinutes > 30)
                    {
                        return ApiResult.Error("该验证码超过30分钟有效期");
                    }
                    if (checkcode.Code != RequestDto.RegisterCode)
                    {
                        return ApiResult.Error("验证码不正确");
                    }
                }

                //2.数据模型验证
                var agent = t_agentBLL.GetListByWhere(string.Format("where OpenId='{0}'", RequestDto.OpenId)).FirstOrDefault();
                if (agent != null)
                {
                    return ApiResult.Error("该代理商信息已经注册");
                }

                var agentInDB = t_agentBLL.GetListByWhere(string.Format("where PhoneNumber='{0}'", RequestDto.PhoneNumber)).FirstOrDefault();
                if (agentInDB != null)
                {
                    return ApiResult.Error("该手机号码已经被注册了");
                }
                #endregion

                int agentCount = t_agentBLL.GetList().Count();

                //获取代理钱包信息
                var wallet = t_walletBLL.GetListByWhere(string.Format("where OpenId='{0}'", RequestDto.OpenId)).FirstOrDefault();

                //开启事务
                using (var connection = CommonBll.GetOpenMySqlConnection())
                {
                    IDbTransaction transaction = connection.BeginTransaction();

                    //3.注册
                    var AgentInsert = t_agentBLL.InsertByTrans(new Model.t_agent
                    {
                        TrueName = RequestDto.AgentName,
                        IsManage = RequestDto.IsManage,
                        AgentCode = string.Format("DLS{0:0000}", agentCount + 1),
                        Nickname = RequestDto.Nickname,
                        Gender = RequestDto.Gender,
                        AvatarUrl = RequestDto.AvatarUrl,
                        PhoneNumber = RequestDto.PhoneNumber,
                        OpenId = RequestDto.OpenId,
                        UnionId = RequestDto.UnionId,
                        AppId = RequestDto.AppId,
                        City = RequestDto.City,
                        Province = RequestDto.Province,
                        Country = RequestDto.Country,
                        NativePlace = RequestDto.NativePlace,
                        Height = RequestDto.Height,
                        Birthday = RequestDto.Birthday,
                        RegisterTime = DateTime.Now,
                        State = 0,
                        IsDeleted = 0,
                        CreationTime = DateTime.Now
                    }, connection, transaction);
                    if (AgentInsert <= 0)
                    {
                        transaction.Rollback();
                        return ApiResult.Error("写入代理商信息失败");
                    }

                    if (wallet == null)
                    {
                        //新增钱包信息
                        var WalletInsert = t_walletBLL.InsertByTrans(new Model.t_wallet
                        {
                            OpenId = RequestDto.OpenId,
                            Balance = 0,
                            IsDeleted = 0,
                            CreationTime = DateTime.Now,
                            Withdraw = 0,
                            StoreId = 0,
                        }, connection, transaction);

                        if (WalletInsert <= 0)
                        {
                            transaction.Rollback();
                            return ApiResult.Error("新增钱包信息失败");
                        }
                    }
                    transaction.Commit();
                }

                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取代理商门店列表
        /// </summary>
        /// <param name="OpenId">代理商微信唯一标识OpenId</param>
        /// <returns></returns>
        [HttpGet, Route("Agent/GetAgentStoreList")]
        public HttpResponseMessage GetAgentStoreList(string OpenId)
        {
            try
            {
                //1.参数验证
                if (string.IsNullOrEmpty(OpenId))
                {
                    return ApiResult.Error("参数OpenId不能为空");
                }

                //2.获取代理信息
                var agent = t_agentBLL.GetListByWhere(string.Format("where OpenId='{0}' and IsDeleted=0", OpenId)).FirstOrDefault();
                if (agent == null)
                {
                    return ApiResult.Error("没有该代理商信息(或已删除)");
                }

                //3.获取代理所属门店信息
                var AgentStoreList = t_agent_storeBLL.GetListByWhere(string.Format("where AgentId={0}", agent.ID));

                if (AgentStoreList.Any())
                {
                    var result = t_storeBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0", string.Join(",", AgentStoreList.Select(o => o.StoreId))));
                    return ApiResult.Success(result.Select(o => new
                    {
                        StoreID = o.ID,
                        o.StoreImgUrl,
                        o.StoreName,
                        o.State,
                        o.Address
                    }));
                }
                else
                {
                    return ApiResult.Success(new List<Model.t_store>());
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取代理商门店详情 （不算禁用的）
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <returns></returns>
        [HttpGet, Route("Agent/GetAgentStoreDetail")]
        public HttpResponseMessage GetAgentStoreDetail(int StoreId)
        {
            try
            {
                //1.参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }

                //2.获取门店信息
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息(或已删除)");
                }

                //3.获取门店人员信息
                var StoreBusinessList = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0}", StoreId));
                int AdministratorsCount = StoreBusinessList.Where(o => o.RoleId == (int)RoleEnum.Administrators && o.State == 0).Count();
                int ManagerCount = StoreBusinessList.Where(o => o.RoleId == (int)RoleEnum.Manager && o.State == 0).Count();
                int BusinessCount = StoreBusinessList.Where(o => o.RoleId == (int)RoleEnum.Business && o.State == 0).Count();

                //4.获取门店申请人信息
                string ApplyUserName = string.Empty;
                string ApplyPhoneNum = string.Empty;
                if (!string.IsNullOrEmpty(store.ApplyOpenId))
                {
                    var business = t_businessBLL.GetListByWhere(string.Format("where OpenId='{0}'", store.ApplyOpenId)).FirstOrDefault();
                    if (business != null)
                    {
                        ApplyUserName = string.IsNullOrEmpty(business.Nickname) ? business.TrueName : business.Nickname;
                        ApplyPhoneNum = business.PhoneNumber;
                    }
                }

                return ApiResult.Success(new
                {
                    store.StoreName,
                    store.StoreImgUrl,
                    AdministratorsCount,
                    ManagerCount,
                    BusinessCount,
                    store.Address,
                    store.State,
                    ApplyUserName,
                    ApplyPhoneNum
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 修改门店状态
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Agent/ChangeStoreState")]
        public HttpResponseMessage ChangeStoreState(ChangeStoreStateRequestDto RequestDto)
        {
            try
            {
                //1.参数验证
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (RequestDto.State == (int)StoreStateEnum.Audited)
                {
                    return ApiResult.Error("修改门店状态错误");
                }

                //2.获取门店信息
                var store = t_storeBLL.GetModel(RequestDto.StoreId);
                if (store == null || store.IsDeleted == 1)
                {
                    return ApiResult.Error("没有该门店信息(或已删除)");
                }

                store.State = RequestDto.State;
                store.LastModificationTime = DateTime.Now;

                var result = t_storeBLL.Update(store);
                if (result <= 0)
                {
                    return ApiResult.Error("更新门店状态失败");
                }

                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取打赏统计信息
        /// </summary>
        /// <param name="OpenId">代理商微信唯一标识OpenId</param>
        /// <param name="StatisticalType">统计类型：1：日报 2：周报 3：月报</param>
        /// <returns></returns>
        [HttpGet, Route("Agent/GetAgentRewardStatistical")]
        public HttpResponseMessage GetAgentRewardStatistical(string OpenId, int StatisticalType)
        {
            try
            {
                //1.参数验证
                if (string.IsNullOrEmpty(OpenId))
                {
                    return ApiResult.Error("参数OpenId不能为空");
                }
                if (!(new List<int> { 1, 2, 3 }).Contains(StatisticalType))
                {
                    return ApiResult.Error("统计类型值错误");
                }
                //2.获取代理信息
                var agent = t_agentBLL.GetListByWhere(string.Format("where OpenId='{0}' and IsDeleted=0", OpenId)).FirstOrDefault();
                if (agent == null)
                {
                    return ApiResult.Error("没有该代理商信息(或已删除)");
                }

                //2.获取查询时间
                DateTime? StartTime = null;
                DateTime? EndTime = null;
                DateTime NowTime = DateTime.Now;
                int DayOfWeek = (int)NowTime.DayOfWeek;
                int Day = (int)NowTime.Day;
                switch (StatisticalType)
                {
                    case 1:
                        StartTime = NowTime;
                        break;
                    case 2:
                        StartTime = NowTime.AddDays(-DayOfWeek + 1);
                        EndTime = NowTime.AddDays(7 - DayOfWeek);
                        break;
                    case 3:
                        StartTime = NowTime.AddDays(-Day + 1);
                        EndTime = NowTime.AddMonths(1).AddDays(-NowTime.AddMonths(1).Day + 1).AddDays(-1);
                        break;
                    default:
                        break;
                }

                //3.获取打赏信息
                List<Model.t_reward> rewards = GetAgentRewardList(OpenId, StartTime, EndTime);
                int RewardCount = 0;
                decimal ReWardMoney = 0;
                int RewardStoreCount = 0;

                if (rewards.Any())
                {
                    ReWardMoney = rewards.Sum(o => o.Money);
                    RewardCount = rewards.Count();
                    RewardStoreCount = rewards.GroupBy(o => o.StoreId).Count();
                }

                return ApiResult.Success(new
                {
                    StartTime = StartTime.Value.ToString("yyyy-MM-dd"),
                    EndTime = EndTime.HasValue ? EndTime.Value.ToString("yyyy-MM-dd") : "",
                    RewardCount,
                    ReWardMoney,
                    RewardStoreCount
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取打赏统计信息明细
        /// </summary>
        /// <param name="OpenId">代理商微信唯一标识OpenId</param>
        /// <param name="StatisticalType">统计类型：1：日报 2：周报 3：月报</param>
        /// <returns></returns>
        [HttpGet, Route("Agent/GetAgentRewardDetail")]
        public HttpResponseMessage GetAgentRewardDetail(string OpenId, int StatisticalType)
        {
            try
            {
                //1.参数验证
                if (string.IsNullOrEmpty(OpenId))
                {
                    return ApiResult.Error("参数OpenId不能为空");
                }
                if (!(new List<int> { 1, 2, 3 }).Contains(StatisticalType))
                {
                    return ApiResult.Error("统计类型值错误");
                }
                //2.获取代理信息
                var agent = t_agentBLL.GetListByWhere(string.Format("where OpenId='{0}' and IsDeleted=0", OpenId)).FirstOrDefault();
                if (agent == null)
                {
                    return ApiResult.Error("没有该代理商信息(或已删除)");
                }

                //2.获取查询时间
                DateTime? StartTime = null;
                DateTime? EndTime = null;
                DateTime NowTime = DateTime.Now;
                int DayOfWeek = (int)NowTime.DayOfWeek;
                int Day = (int)NowTime.Day;
                switch (StatisticalType)
                {
                    case 1:
                        StartTime = NowTime;
                        break;
                    case 2:
                        StartTime = NowTime.AddDays(-DayOfWeek + 1);
                        EndTime = NowTime.AddDays(7 - DayOfWeek);
                        break;
                    case 3:
                        StartTime = NowTime.AddDays(-Day + 1);
                        EndTime = NowTime.AddMonths(1).AddDays(-NowTime.AddMonths(1).Day + 1).AddDays(-1);
                        break;
                    default:
                        break;
                }

                //3.获取打赏信息
                List<Model.t_reward> rewards = GetAgentRewardList(OpenId, StartTime, EndTime);
                if (rewards.Any())
                {
                    //4.获取门店信息
                    var stores = t_storeBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0",
                        string.Join(",", rewards.GroupBy(o => o.StoreId).Select(o => o.Key))));

                    var leftJoin = from r in rewards
                                   join s in stores on r.StoreId equals s.ID into temp
                                   from t in temp.DefaultIfEmpty()
                                   select new
                                   {
                                       r.Money,
                                       RewardTime = r.RewardTime.ToString("yyyy-MM-dd"),
                                       StoreName = (t == null ? "" : t.StoreName)
                                   };
                    return ApiResult.Success(leftJoin);
                }
                else
                {
                    return ApiResult.Success(new List<Model.t_store>());
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取指定代理商钱包信息
        /// </summary>
        /// <param name="OpenId">代理商微信唯一标识OpenId</param>
        /// <returns></returns>
        [HttpGet, Route("Agent/GetAgentWallet")]
        public HttpResponseMessage GetAgentWallet(string OpenId)
        {
            try
            {
                //1.参数验证
                if (string.IsNullOrEmpty(OpenId))
                {
                    return ApiResult.Error("参数OpenId不能为空");
                }

                //2.获取代理钱包信息
                var wallet = t_walletBLL.GetListByWhere(string.Format("where OpenId='{0}' and IsDeleted=0", OpenId)).FirstOrDefault();
                if (wallet == null)
                {
                    return ApiResult.Error("没有该代理商钱包信息(或已删除)");
                }

                //3.返回结果
                return ApiResult.Success(new
                {
                    wallet.Withdraw,
                    wallet.Balance
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取指定代理商提现记录
        /// </summary>
        /// <param name="OpenId">代理商微信唯一标识OpenId</param>
        /// <returns></returns>
        [HttpGet, Route("Agent/GetAgentWithdrawRecord")]
        public HttpResponseMessage GetAgentWithdrawRecord(string OpenId)
        {
            try
            {
                //1.参数验证
                if (string.IsNullOrEmpty(OpenId))
                {
                    return ApiResult.Error("参数OpenId不能为空");
                }

                //2.获取代理钱包信息
                var withdraws = t_withdrawBLL.GetListByWhere(string.Format("where OpenId='{0}' and IsDeleted=0", OpenId));
                //if (!withdraws.Any())
                //{
                //    return ApiResult.Error("没有该代理商提现信息(或已删除)");
                //}

                //3.返回结果
                return ApiResult.Success(withdraws.OrderByDescending(o => o.WithdrawTime).Select(o => new
                {
                    WithdrawTime = o.WithdrawTime.ToString("yyyy-MM-dd"),
                    o.Withdraw,
                    o.WithdrawName
                }));
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 代理商提现
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Agent/AgentWithdraw")]
        public HttpResponseMessage AgentWithdraw(AgentWithdrawRequestDto RequestDto)
        {
            try
            {
                //1.参数验证
                if (string.IsNullOrEmpty(RequestDto.OpenId))
                {
                    return ApiResult.Error("参数OpenId不能为空");
                }
                if (RequestDto.WithdrawMoney < 1 || RequestDto.WithdrawMoney > 2000)
                {
                    return ApiResult.Error("单笔提现最少¥1,最多¥2000");
                }

                //获取代理信息
                var agent = t_agentBLL.GetListByWhere(string.Format("where OpenId='{0}' and IsDeleted=0", RequestDto.OpenId)).FirstOrDefault();
                if (agent == null || agent.State != 1)
                {
                    return ApiResult.Error("没有该代理商信息(或被禁用)");
                }

                //2.获取代理钱包信息
                var wallet = t_walletBLL.GetListByWhere(string.Format("where OpenId='{0}' and IsDeleted=0", RequestDto.OpenId)).FirstOrDefault();
                if (wallet == null)
                {
                    return ApiResult.Error("没有该代理商钱包信息(或已删除)");
                }
                if (wallet.Balance - RequestDto.WithdrawMoney < 0)
                {
                    return ApiResult.Error("余额不够");
                }

                //开启事务
                using (var connection = CommonBll.GetOpenMySqlConnection())
                {
                    IDbTransaction transaction = connection.BeginTransaction();
                    var state = 0;
                    var billno = "";
                    //调用企业付款给个人接口

                    //3.写入提现记录
                    var InsertWithdraw = t_withdrawBLL.InsertByTrans(new Model.t_withdraw
                    {
                        WithdrawName = "打赏提现",
                        Withdraw = RequestDto.WithdrawMoney,
                        OpenId = RequestDto.OpenId,
                        WithdrawTime = DateTime.Now,
                        CreationTime = DateTime.Now,
                        IsDeleted = 0,
                        State = state,
                        BillNo = billno
                    }, connection, transaction);

                    if (InsertWithdraw < 0)
                    {
                        transaction.Rollback();
                        return ApiResult.Error("写入提现记录失败");
                    }

                    //4.更新钱包余额
                    wallet.Balance = wallet.Balance - RequestDto.WithdrawMoney;
                    wallet.Withdraw = wallet.Withdraw + RequestDto.WithdrawMoney;
                    wallet.LastModificationTime = DateTime.Now;
                    var UpdateWallet = t_walletBLL.UpdateByTrans(wallet, connection, transaction);
                    if (UpdateWallet < 0)
                    {
                        transaction.Rollback();
                        return ApiResult.Error("更新钱包信息失败");
                    }

                    transaction.Commit();
                }
                //5.返回结果
                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 代理商管理员（获取代理商列表）
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("AgentManage/GetAgentList")]
        public HttpResponseMessage GetAgentList()
        {
            try
            {
                var AgentList = t_agentBLL.GetListByWhere("where IsDeleted=0");

                var AgentStore = t_agent_storeBLL.GetList().GroupBy(o => o.AgentId).Select(o => new { AgentId = o.Key, Count = o.Count() });

                var leftJoin = from a in AgentList
                               join s in AgentStore on a.ID equals s.AgentId into temp
                               from t in temp.DefaultIfEmpty()
                               select new
                               {
                                   AgentId = a.ID,
                                   a.AvatarUrl,
                                   Nickname = string.IsNullOrEmpty(a.Nickname) ? a.TrueName : a.Nickname,
                                   a.State,
                                   StoreCount = (t == null ? 0 : t.Count)
                               };

                return ApiResult.Success(leftJoin);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 代理商管理员（获取代理商详情）
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("AgentManage/GetAgentDetail")]
        public HttpResponseMessage GetAgentDetail(int AgentId)
        {
            try
            {
                if (AgentId <= 0)
                {
                    return ApiResult.Error("参数AgentId错误");
                }
                //获取代理信息
                var agent = t_agentBLL.GetListByWhere(string.Format("where ID={0} and IsDeleted=0", AgentId)).FirstOrDefault();
                if (agent == null)
                {
                    return ApiResult.Error("没有该代理商信息");
                }

                //获取代理所属门店信息
                var AgentStoreList = t_agent_storeBLL.GetListByWhere(string.Format("where AgentId={0}", agent.ID));
                if (AgentStoreList.Any())
                {
                    var result = t_storeBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0", string.Join(",", AgentStoreList.Select(o => o.StoreId))));
                    return ApiResult.Success(new
                    {
                        agent.AvatarUrl,
                        Nickname = string.IsNullOrEmpty(agent.Nickname) ? agent.TrueName : agent.Nickname,
                        agent.PhoneNumber,
                        SubStores = result.OrderBy(o => o.State).Select(o => new
                        {
                            StoreId = o.ID,
                            o.StoreName,
                            o.State
                        })
                    });
                }
                else
                {
                    return ApiResult.Success(new
                    {
                        agent.AvatarUrl,
                        Nickname = string.IsNullOrEmpty(agent.Nickname) ? agent.TrueName : agent.Nickname,
                        agent.PhoneNumber,
                        SubStores = new List<Model.t_store>()
                    });
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 修改代理商个人信息
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Agent/UpdateAgentInfo")]
        public HttpResponseMessage UpdateAgentInfo(UpdateAgentInfoRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (string.IsNullOrEmpty(RequestDto.OpenId))
                {
                    return ApiResult.Error("OpenId不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.AgentName))
                {
                    return ApiResult.Error("姓名不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.PhoneNumber))
                {
                    return ApiResult.Error("手机号码不能为空");
                }
                if (RequestDto.PhoneNumber.Length != 11)
                {
                    return ApiResult.Error("代理商手机号码格式错误");
                }
                //2.数据模型验证
                var agent = t_agentBLL.GetListByWhere(string.Format("where OpenId='{0}'", RequestDto.OpenId)).FirstOrDefault();
                if (agent == null)
                {
                    return ApiResult.Error("该代理商信息不存在");
                }

                var agentInDB = t_agentBLL.GetListByWhere(string.Format("where PhoneNumber='{0}' and OpenId !='{1}'", RequestDto.PhoneNumber, RequestDto.OpenId)).FirstOrDefault();
                if (agentInDB != null)
                {
                    return ApiResult.Error("该手机号码已经被注册了");
                }
                #endregion

                //更新代理商信息
                agent.TrueName = RequestDto.AgentName;
                agent.PhoneNumber = RequestDto.PhoneNumber;
                agent.AvatarUrl = RequestDto.AvatarUrl;
                agent.NativePlace = RequestDto.NativePlace;
                agent.Height = RequestDto.Height;
                agent.Birthday = RequestDto.Birthday;

                var UpdateAgent = t_agentBLL.Update(agent);
                if (UpdateAgent < 0)
                {
                    return ApiResult.Error("更新代理商信息失败");
                }

                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 图片上传 用form-data 提交
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("Agent/UploadAgentImg")]
        public HttpResponseMessage UploadImg()
        {
            try
            {
                var uploadfiles = HttpContext.Current.Request.Files;
                if (!Request.Content.IsMimeMultipartContent("form-data") || uploadfiles.Count == 0)
                {
                    return ApiResult.Error("未上传有效的文件");
                }
                HttpPostedFile file = HttpContext.Current.Request.Files.Get(0);
                string rootpath = HttpContext.Current.Server.MapPath("~");

                //获取文件后缀  
                string extensionName = Path.GetExtension(file.FileName).ToLower();
                string[] filestype = { ".jpg", ".png", ".gif", ".bmp", ".jpeg" };    //允许的文件类型
                if (!filestype.Contains(extensionName))
                {
                    return ApiResult.Error("文件类型不允许");
                }

                //文件名 
                string FileName = Guid.NewGuid().ToString("N") + extensionName;

                //保存文件路径
                string FilePathName = rootpath + "Imges/" + DateTime.Now.ToString("yyyy-MM-dd");
                if (!Directory.Exists(FilePathName))
                {
                    Directory.CreateDirectory(FilePathName);
                }
                file.SaveAs(FilePathName + "/" + FileName);

                string ApiWebUrl = ConfigurationManager.AppSettings["ApiWebUrl"].ToString();
                string RelativePath = "Imges/" + DateTime.Now.ToString("yyyy-MM-dd") + "/" + FileName;

                return ApiResult.Success(string.Format("{0}{1}", ApiWebUrl, RelativePath));
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 创建代理商微信小程序二维码（B接口）
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Agent/CreateAgentWxCode")]
        public HttpResponseMessage CreateAgentWxCode(AgentCreateWxCodeRequestDto RequestDto)
        {
            try
            {
                if (string.IsNullOrEmpty(RequestDto.scene))
                {
                    return ApiResult.Error("参数scene错误");
                }
                string AgentAppID = ConfigurationManager.AppSettings["AgentAppID"].ToString();
                string AgentAppSecret = ConfigurationManager.AppSettings["AgentAppSecret"].ToString();
                //1.获取AccessToken
                string AccessToken = WxHelper.GetAccessToken(AgentAppID, AgentAppSecret);
                if (string.IsNullOrEmpty(AccessToken))
                {
                    return ApiResult.Error("获取AccessToken失败");
                }
                //2.生成二维码图片地址
                string ImgUrl = WxHelper.CreateWxCode(AccessToken, RequestDto.scene, RequestDto.page);

                string ApiWebUrl = ConfigurationManager.AppSettings["ApiWebUrl"].ToString();
                return ApiResult.Success(string.Format("{0}{1}", ApiWebUrl, ImgUrl));
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        #region 辅助方法
        /// <summary>
        /// 获取指定代理商打赏记录
        /// </summary>
        /// <param name="OpenId"></param>
        /// <param name="StartTime"></param>
        /// <param name="EndTime"></param>
        /// <returns></returns>
        private List<Model.t_reward> GetAgentRewardList(string OpenId, DateTime? StartTime = null, DateTime? EndTime = null)
        {
            List<Model.t_reward> result = new List<Model.t_reward>();

            //获取代理信息
            var agent = t_agentBLL.GetListByWhere(string.Format("where OpenId='{0}' and IsDeleted=0", OpenId)).FirstOrDefault();
            if (agent == null)
            {
                return result;
            }

            //获取代理所属门店信息
            var AgentStoreList = t_agent_storeBLL.GetListByWhere(string.Format("where AgentId={0}", agent.ID));
            if (AgentStoreList.Any())
            {
                if (EndTime.HasValue)
                {
                    result = t_rewardBLL.GetListByWhere(string.Format("where StoreId IN ({0}) and IsDeleted=0 and RewardTime>='{1}' and RewardTime<'{2}'",
                        string.Join(",", AgentStoreList.Select(o => o.StoreId)),
                        StartTime.Value.ToString("yyyy-MM-dd"),
                        EndTime.Value.AddDays(1).ToString("yyyy-MM-dd"))).ToList();
                }
                else
                {
                    result = t_rewardBLL.GetListByWhere(string.Format("where StoreId IN ({0}) and IsDeleted=0 and  date_format(RewardTime, '%Y-%m-%d')='{1}'",
                        string.Join(",", AgentStoreList.Select(o => o.StoreId)),
                        StartTime.Value.ToString("yyyy-MM-dd"))).ToList();
                }
            }
            return result;
        }

        /// <summary>
        /// 随机生成4位验证码
        /// </summary>
        /// <returns></returns>
        private string RandCode()
        {
            char[] chars = "1234567890".ToCharArray();
            System.Random random = new Random();

            string code = string.Empty;
            for (int i = 0; i < 4; i++) code += chars[random.Next(0, chars.Length)].ToString();  //随机生成验证码字符串(4个字符)
            return code;
        }
        #endregion

    }
}
