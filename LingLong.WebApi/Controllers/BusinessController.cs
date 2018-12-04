using LingLong.Bll;
using LingLong.Common;
using LingLong.Common.Enum;
using LingLong.Common.WebApi;
using LingLong.WebApi.Models;
using LingLong.WebApi.Models.RequestDto.Business;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace LingLong.WebApi.Controllers
{
    /// <summary>
    /// 商户相关接口 控制器
    /// 代理商禁用了，指定区域经理Id的时候，还要写入代理商与门店的关系吗？    不用写
    /// 如果客户与服务人员产生了关联   服务人员将这个客户删除了，那对于客户还能看到这个服务人员吗？ 不能看到这个服务人员
    /// 下属新增客户 手动添加的客户算不算？ 算
    /// 商户禁用  针对所有的门店而言，该商户是禁用的还是 当前门店该商户是禁用的？？？   当前门店是禁用的
    /// </summary>
    public class BusinessController : ApiController
    {
        /// <summary>
        /// 指定手机号码发送验证码(发送间隔不能小于60秒)
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/SendRegisterCodeByBusiness")]
        public HttpResponseMessage SendRegisterCodeByBusiness(SendRegisterCodeByBusinessRequestDto RequestDto)
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
                var checkcode = t_checkcodeBLL.GetListByWhere(string.Format("Where PhoneNumber='{0}' and Type=2", RequestDto.PhoneNumber)).OrderByDescending(o => o.CreateTime).FirstOrDefault();
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
                string result = SMSHelper.SendRegisterCodeByBusiness(RequestDto.PhoneNumber, RandCode);
                if (result == "OK")
                {
                    t_checkcodeBLL.Insert(new Model.t_checkcode
                    {
                        PhoneNumber = RequestDto.PhoneNumber,
                        Code = RandCode,
                        Type = 2,
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
        [HttpGet, Route("Business/GetBusinessOpenId")]
        public HttpResponseMessage GetBusinessOpenId(string JsCode)
        {
            try
            {
                if (string.IsNullOrEmpty(JsCode))
                {
                    return ApiResult.Error("参数JsCode不能为空");
                }

                string BusinessAppID = ConfigurationManager.AppSettings["BusinessAppID"].ToString();
                string BusinessAppSecret = ConfigurationManager.AppSettings["BusinessAppSecret"].ToString();

                string result = WxHelper.GetOpenId(JsCode, BusinessAppID, BusinessAppSecret);

                return ApiResult.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取商户详情(进入小程序时调用)
        /// </summary>
        /// <param name="OpenId">商户OpenId</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetBusinessDetailByOpenId")]
        public HttpResponseMessage GetBusinessDetailByOpenId(string OpenId)
        {
            try
            {
                //1.参数验证
                if (string.IsNullOrEmpty(OpenId))
                {
                    return ApiResult.Error("OpenId不能为空");
                }

                var business = t_businessBLL.GetListByWhere(string.Format("where OpenId='{0}'", OpenId)).FirstOrDefault();
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该商户信息(或已删除)");
                }

                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where BusinessId={0}", business.ID));
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该商户没有门店信息");
                }
                List<Model.t_store> stores = new List<Model.t_store>();
                if (StoreBusiness.Any())
                {
                    stores = t_storeBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0",
                   string.Join(",", StoreBusiness.Select(o => o.StoreId)))).ToList();
                }
                //3.返回结果
                return ApiResult.Success(new
                {
                    BusinessId = business.ID,
                    StoreList = stores.Where(o => o.State == 1).Select(o => new
                    {
                        StoreId = o.ID,
                        o.StoreName,
                        o.Address
                    })
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 商户(超级管理员)注册
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/SuperAdminRegister")]
        public HttpResponseMessage SuperAdminRegister(SuperAdminRegisterRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (string.IsNullOrEmpty(RequestDto.OpenId))
                {
                    return ApiResult.Error("参数OpenId错误");
                }
                if (string.IsNullOrEmpty(RequestDto.PhoneNumber))
                {
                    return ApiResult.Error("手机号码不能为空");
                }
                if (RequestDto.PhoneNumber.Length != 11)
                {
                    return ApiResult.Error("手机号码格式错误");
                }
                if (string.IsNullOrEmpty(RequestDto.RegisterCode))
                {
                    return ApiResult.Error("RegisterCode不能为空");
                }
                var checkcode = t_checkcodeBLL.GetListByWhere(string.Format("Where PhoneNumber='{0}' and Type=2", RequestDto.PhoneNumber)).OrderByDescending(o => o.CreateTime).FirstOrDefault();
                if (checkcode == null)
                {
                    return ApiResult.Error("该手机号码没有发送注册码");
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

                var businessInDB = t_businessBLL.GetListByWhere(string.Format("where PhoneNumber='{0}'", RequestDto.PhoneNumber)).FirstOrDefault();
                if (businessInDB != null)
                {
                    return ApiResult.Error("该手机号码已经被注册了");
                }
                #endregion

                int BusinessInsert = 0;
                //数据模型验证
                var business = t_businessBLL.GetListByWhere(string.Format("where OpenId='{0}'", RequestDto.OpenId)).FirstOrDefault();
                if (business == null)
                {
                    //3.注册
                    BusinessInsert = t_businessBLL.Insert(new Model.t_business
                    {
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
                        IsDeleted = 0,
                        CreationTime = DateTime.Now,
                    });
                    if (BusinessInsert <= 0)
                    {
                        return ApiResult.Error("写入商户信息失败");
                    }
                }

                return ApiResult.Success(BusinessInsert);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 门店申请
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/ApplyStore")]
        public HttpResponseMessage ApplyStore(ApplyStoreRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (string.IsNullOrEmpty(RequestDto.TrueName))
                {
                    return ApiResult.Error("真实姓名不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.StoreName))
                {
                    return ApiResult.Error("门店名称不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.Area))
                {
                    return ApiResult.Error("门店所属区不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.City))
                {
                    return ApiResult.Error("门店所属市不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.Province))
                {
                    return ApiResult.Error("门店所属省不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.Address))
                {
                    return ApiResult.Error("门店详细地址不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.PhoneNumber))
                {
                    return ApiResult.Error("手机号码不能为空");
                }
                if (RequestDto.PhoneNumber.Length != 11)
                {
                    return ApiResult.Error("手机号码格式错误");
                }
                var business = t_businessBLL.GetListByWhere(string.Format("where OpenId='{0}'", RequestDto.ApplyOpenId)).FirstOrDefault();
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有商户(申请人)信息(或已删除)");
                }
                Model.t_agent agent = new Model.t_agent();
                if (RequestDto.AgentId > 0)
                {
                    //获取代理信息
                    agent = t_agentBLL.GetListByWhere(string.Format("where ID={0} and IsDeleted=0", RequestDto.AgentId)).FirstOrDefault();
                    if (agent == null)
                    {
                        return ApiResult.Error("没有该代理商信息");
                    }
                }
                var storeInDB = t_storeBLL.GetListByWhere(string.Format("where ApplyOpenId='{0}' and StoreName='{1}'", RequestDto.ApplyOpenId, RequestDto.StoreName)).FirstOrDefault();
                if (storeInDB != null)
                {
                    return ApiResult.Error("该商户已申请过门店，请勿重复申请");
                }
                var storeInDB02 = t_storeBLL.GetListByWhere(string.Format("where PhoneNumber='{0}'", RequestDto.PhoneNumber)).FirstOrDefault();
                if (storeInDB02 != null)
                {
                    return ApiResult.Error("该手机号码已经被注册了");
                }
                #endregion

                //开启事务
                using (var connection = CommonBll.GetOpenMySqlConnection())
                {
                    IDbTransaction transaction = connection.BeginTransaction();

                    #region 更新商户信息
                    business.TrueName = RequestDto.TrueName;
                    business.LastModificationTime = DateTime.Now;
                    var UpdateBusiness = t_businessBLL.UpdateByTrans(business, connection, transaction);
                    if (UpdateBusiness < 0)
                    {
                        transaction.Rollback();
                        return ApiResult.Error("更新商户信息失败");
                    }
                    #endregion

                    #region 写入门店信息
                    int StoreId = t_storeBLL.InsertByTrans(new Model.t_store
                    {
                        StoreName = RequestDto.StoreName,
                        PhoneNumber = RequestDto.PhoneNumber,
                        Area = RequestDto.Area,
                        City = RequestDto.City,
                        Province = RequestDto.Province,
                        Address = RequestDto.Address,
                        ApplyOpenId = RequestDto.ApplyOpenId,
                        Score = 5,
                        IsDeleted = 0,
                        State = 0,
                        CreationTime = DateTime.Now,
                    }, connection, transaction);
                    if (StoreId < 0)
                    {
                        transaction.Rollback();
                        return ApiResult.Error("写入门店信息失败");
                    }
                    #endregion

                    #region 写入门店服务人员信息
                    var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", StoreId, business.ID)).FirstOrDefault();
                    if (StoreBusiness == null)
                    {
                        var InsertStoreBusiness = t_store_businessBLL.InsertByTrans(new Model.t_store_business
                        {
                            BusinessId = business.ID,
                            StoreId = StoreId,
                            ParentId = 0,
                            RoleId = (int)RoleEnum.SuperAdministrators,
                            State = 0
                        }, connection, transaction);

                        if (InsertStoreBusiness < 0)
                        {
                            transaction.Rollback();
                            return ApiResult.Error("写入门店服务人员信息失败");
                        }
                    }
                    #endregion

                    #region 写入门店代理商关联记录
                    if (RequestDto.AgentId > 0 && agent.State == 1) //代理商状态为启用才需要写入关联关系
                    {
                        var agentStore = t_agent_storeBLL.GetListByWhere(string.Format("where AgentId={0} and StoreId={1}", RequestDto.AgentId, StoreId)).FirstOrDefault();
                        if (agentStore == null)
                        {
                            var InsertAgentStore = t_agent_storeBLL.InsertByTrans(new Model.t_agent_store
                            {
                                AgentId = RequestDto.AgentId,
                                StoreId = StoreId
                            }, connection, transaction);

                            if (InsertAgentStore < 0)
                            {
                                transaction.Rollback();
                                return ApiResult.Error("写入门店代理商关联记录失败");
                            }
                        }
                    }
                    #endregion

                    #region 写入钱包信息

                    //获取钱包信息
                    var wallet = t_walletBLL.GetListByWhere(string.Format("where OpenId='{0}' and StoreId={1}", RequestDto.ApplyOpenId, StoreId)).FirstOrDefault();
                    if (wallet == null)
                    {
                        //新增钱包信息
                        var WalletInsert = t_walletBLL.InsertByTrans(new Model.t_wallet
                        {
                            OpenId = RequestDto.ApplyOpenId,
                            Balance = 0,
                            IsDeleted = 0,
                            CreationTime = DateTime.Now,
                            Withdraw = 0,
                            StoreId = StoreId,
                        }, connection, transaction);

                        if (WalletInsert <= 0)
                        {
                            transaction.Rollback();
                            return ApiResult.Error("新增钱包信息失败");
                        }
                    }
                    #endregion

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
        /// 商户(管理员、门店经理、服务人员)注册
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/BusinessRegister")]
        public HttpResponseMessage BusinessRegister(BusinessRegisterRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (string.IsNullOrEmpty(RequestDto.OpenId))
                {
                    return ApiResult.Error("参数OpenId错误");
                }
                if (!(new List<int> { 1, 2, 3 }).Contains(RequestDto.RoleId))
                {
                    return ApiResult.Error("参数RoleId错误");
                }
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                #endregion

                var business = t_businessBLL.GetListByWhere(string.Format("where OpenId='{0}'", RequestDto.OpenId)).FirstOrDefault();

                //获取钱包信息
                var wallet = t_walletBLL.GetListByWhere(string.Format("where OpenId='{0}' and StoreId={1}", RequestDto.OpenId, RequestDto.StoreId)).FirstOrDefault();
                int BusinessId = 0;
                //开启事务
                using (var connection = CommonBll.GetOpenMySqlConnection())
                {
                    IDbTransaction transaction = connection.BeginTransaction();

                    if (business == null)
                    {
                        #region 注册
                        BusinessId = t_businessBLL.InsertByTrans(new Model.t_business
                        {
                            Nickname = RequestDto.Nickname,
                            Gender = RequestDto.Gender,
                            AvatarUrl = RequestDto.AvatarUrl,
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
                            IsDeleted = 0,
                            CreationTime = DateTime.Now,
                        }, connection, transaction);
                        if (BusinessId <= 0)
                        {
                            transaction.Rollback();
                            return ApiResult.Error("写入商户信息失败");
                        }
                        #endregion
                    }
                    else
                    {
                        BusinessId = business.ID;
                    }

                    #region 写入门店服务人员信息
                    var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", RequestDto.StoreId, BusinessId)).FirstOrDefault();
                    if (StoreBusiness == null)
                    {
                        var InsertStoreBusiness = t_store_businessBLL.InsertByTrans(new Model.t_store_business
                        {
                            BusinessId = BusinessId,
                            StoreId = RequestDto.StoreId,
                            State = 0,
                            ParentId = 0,
                            RoleId = RequestDto.RoleId
                        }, connection, transaction);
                        if (InsertStoreBusiness <= 0)
                        {
                            transaction.Rollback();
                            return ApiResult.Error("写入门店服务人员信息失败");
                        }
                    }
                    #endregion

                    #region 写入钱包信息
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
                            StoreId = RequestDto.StoreId,
                        }, connection, transaction);

                        if (WalletInsert <= 0)
                        {
                            transaction.Rollback();
                            return ApiResult.Error("新增钱包信息失败");
                        }
                    }
                    #endregion

                    transaction.Commit();
                }

                return ApiResult.Success(BusinessId);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 填写个人资料信息
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/FillSelfInformation")]
        public HttpResponseMessage FillSelfInformation(FillSelfInformationRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (string.IsNullOrEmpty(RequestDto.OpenId))
                {
                    return ApiResult.Error("OpenId不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.TrueName))
                {
                    return ApiResult.Error("真实姓名不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.PhoneNumber))
                {
                    return ApiResult.Error("手机号码不能为空");
                }
                if (RequestDto.PhoneNumber.Length != 11)
                {
                    return ApiResult.Error("手机号码格式错误");
                }
                var business = t_businessBLL.GetListByWhere(string.Format("where OpenId='{0}'", RequestDto.OpenId)).FirstOrDefault();
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有商户信息(或已删除)");
                }
                var businessInDB = t_businessBLL.GetListByWhere(string.Format("where PhoneNumber='{0}' and ID !={1}", RequestDto.PhoneNumber, business.ID)).FirstOrDefault();
                if (businessInDB != null)
                {
                    return ApiResult.Error("该手机号码已经被注册了");
                }
                #endregion

                #region 更新商户信息
                business.TrueName = RequestDto.TrueName;
                business.PhoneNumber = RequestDto.PhoneNumber;
                if (!string.IsNullOrEmpty(RequestDto.AvatarUrl))
                {
                    business.AvatarUrl = RequestDto.AvatarUrl;
                }
                if (!string.IsNullOrEmpty(RequestDto.NativePlace))
                {
                    business.NativePlace = RequestDto.NativePlace;
                }
                if (RequestDto.Height > 0)
                {
                    business.Height = RequestDto.Height;
                }
                if (RequestDto.Birthday.HasValue)
                {
                    business.Birthday = RequestDto.Birthday;
                }
                business.LastModificationTime = DateTime.Now;
                var BusinessUpdate = t_businessBLL.Update(business);
                if (BusinessUpdate <= 0)
                {
                    return ApiResult.Error("更新商户信息失败");
                }
                #endregion

                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取指定商户上级领导信息
        /// </summary>
        /// <param name="OpenId">商户OpenId</param>
        /// <param name="StoreId">门店Id</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetSuperiorLeaderByOpenId")]
        public HttpResponseMessage GetSuperiorLeaderByOpenId(string OpenId, int StoreId)
        {
            try
            {
                #region 参数验证
                if (string.IsNullOrEmpty(OpenId))
                {
                    return ApiResult.Error("OpenId不能为空");
                }
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                var business = t_businessBLL.GetListByWhere(string.Format("where OpenId='{0}'", OpenId)).FirstOrDefault();
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该商户信息(或已删除)");
                }
                #endregion

                //获取该门店下的所有服务人员
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0}", StoreId));
                var CurrentBusiness = StoreBusiness.Where(o => o.BusinessId == business.ID).FirstOrDefault();
                if (CurrentBusiness == null)
                {
                    return ApiResult.Error("该商户没有门店信息");
                }
                //获取当前上级领导角色
                int RoleId = CurrentBusiness.RoleId - 1;
                //获取当前上级领导Id集合（没有被禁用的）
                var SuperiorLeaderBusiness = StoreBusiness.Where(o => o.RoleId == RoleId && o.State == 0);
                if (SuperiorLeaderBusiness.Any())
                {
                    var result = t_businessBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0",
                   string.Join(",", SuperiorLeaderBusiness.Select(o => o.BusinessId))));

                    return ApiResult.Success(result.Select(o => new
                    {
                        o.ID,
                        TrueName = string.IsNullOrEmpty(o.TrueName) ? o.Nickname : o.TrueName
                    }));
                }
                else
                {
                    return ApiResult.Success(new List<Model.t_business>());
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 修改个人信息（选择上级领导）
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/SelectLeader")]
        public HttpResponseMessage SelectLeader(SelectLeaderRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (string.IsNullOrEmpty(RequestDto.OpenId))
                {
                    return ApiResult.Error("OpenId不能为空");
                }
                if (RequestDto.ParentId <= 0)
                {
                    return ApiResult.Error("参数ParentId错误");
                }
                var business = t_businessBLL.GetListByWhere(string.Format("where OpenId='{0}'", RequestDto.OpenId)).FirstOrDefault();
                if (business == null)
                {
                    return ApiResult.Error("没有该商户信息");
                }
                #endregion

                var StoreBusinessList = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", RequestDto.StoreId, business.ID)).FirstOrDefault();
                if (StoreBusinessList == null)
                {
                    return ApiResult.Error("该门店没有该商户信息");
                }
                StoreBusinessList.ParentId = RequestDto.ParentId;
                t_store_businessBLL.Update(StoreBusinessList);

                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 手动添加客户信息
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/ManualAddCustomer")]
        public HttpResponseMessage ManualAddCustomer(ManualAddCustomerRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (RequestDto.BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                if (string.IsNullOrEmpty(RequestDto.TrueName))
                {
                    return ApiResult.Error("真实姓名不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.PhoneNumber))
                {
                    return ApiResult.Error("手机号码不能为空");
                }
                if (RequestDto.PhoneNumber.Length != 11)
                {
                    return ApiResult.Error("手机号码格式错误");
                }
                var store = t_storeBLL.GetModel(RequestDto.StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息（或已删除）");
                }
                var business = t_businessBLL.GetModel(RequestDto.BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息（或已删除）");
                }
                #endregion

                //开启事务
                using (var connection = CommonBll.GetOpenMySqlConnection())
                {
                    IDbTransaction transaction = connection.BeginTransaction();

                    #region 写入客户信息
                    var CustomerId = t_customerBLL.InsertByTrans(new Model.t_customer
                    {
                        CustomerType = 1,  //客户类型（0:自动；1：服务员手动添加）
                        AvatarUrl = RequestDto.AvatarUrl,
                        PhoneNumber = RequestDto.PhoneNumber,
                        TrueName = RequestDto.TrueName,
                        OpenId = "",
                        Wechat = RequestDto.Wechat,
                        Remark = RequestDto.Remark,
                        RegisterTime = DateTime.Now,
                        IsDeleted = 0,
                        CreationTime = DateTime.Now,
                    }, connection, transaction);
                    if (CustomerId <= 0)
                    {
                        transaction.Rollback();
                        return ApiResult.Error("手动添加客户信息失败");
                    }
                    #endregion

                    #region 写入关联门店商户记录失败
                    var StoreCustomerBusiness = t_store_customer_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1} and CustomerId={2}", RequestDto.StoreId, RequestDto.BusinessId, CustomerId)).FirstOrDefault();
                    if (StoreCustomerBusiness == null)
                    {
                        var InsertStoreCustomerBusiness = t_store_customer_businessBLL.InsertByTrans(new Model.t_store_customer_business
                        {
                            StoreId = RequestDto.StoreId,
                            BusinessId = RequestDto.BusinessId,
                            CustomerId = CustomerId
                        }, connection, transaction);
                        if (InsertStoreCustomerBusiness < 0)
                        {
                            transaction.Rollback();
                            return ApiResult.Error("写入关联门店商户记录失败");
                        }
                    }
                    #endregion

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
        /// 获取客户列表
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">服务人员Id</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetCustomerList")]
        public HttpResponseMessage GetCustomerList(int StoreId, int BusinessId)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息（或已删除）");
                }
                var business = t_businessBLL.GetModel(BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息（或已删除）");
                }
                //获取门店服务人员信息
                var CurrentBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", StoreId, BusinessId)).FirstOrDefault();
                if (CurrentBusiness == null)
                {
                    return ApiResult.Error("该门店没有该服务人员信息");
                }
                if (CurrentBusiness.State == 1)
                {
                    return ApiResult.Error("该商户已被禁用");
                }
                #endregion

                var StoreBusinessList = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0}", StoreId));

                List<int> subBusiness = new List<int>();
                List<int> subMangerBusiness = new List<int>();
                List<int> subAdminBusiness = new List<int>();
                if (CurrentBusiness.RoleId == 2)
                {
                    //获取所有的下属员工
                    subBusiness = StoreBusinessList.Where(o => o.RoleId == 3).Select(o => o.BusinessId).ToList();
                }
                else if (CurrentBusiness.RoleId == 1)
                {
                    //获取所有的下属门店经理
                    subMangerBusiness = StoreBusinessList.Where(o => o.RoleId == 2).Select(o => o.BusinessId).ToList();
                    //获取所有的下属员工
                    subBusiness = StoreBusinessList.Where(o => o.RoleId == 3).Select(o => o.BusinessId).ToList();
                }
                else if (CurrentBusiness.RoleId == 0)
                {
                    //获取所有的下属管理员
                    subAdminBusiness = StoreBusinessList.Where(o => o.RoleId == 1).Select(o => o.BusinessId).ToList();
                    //获取所有的下属门店经理
                    subMangerBusiness = StoreBusinessList.Where(o => o.RoleId == 2).Select(o => o.BusinessId).ToList();
                    //获取所有的下属员工
                    subBusiness = StoreBusinessList.Where(o => o.RoleId == 3).Select(o => o.BusinessId).ToList();
                }

                var tempBusinessIdList = subAdminBusiness;
                tempBusinessIdList.AddRange(subMangerBusiness);
                tempBusinessIdList.AddRange(subBusiness);
                tempBusinessIdList.Add(BusinessId);

                var StoreCustomer = t_store_customer_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId IN ({1})", StoreId, string.Join(",", tempBusinessIdList)));
                if (StoreCustomer.Any())
                {
                    var customers = t_customerBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0 ",
                         string.Join(",", StoreCustomer.Select(o => o.CustomerId))));

                    return ApiResult.Success(customers.Select(o => new
                    {
                        o.ID,
                        Nickname = (o.CustomerType != 1 ? o.Nickname : o.TrueName),
                        o.AvatarUrl
                    }));
                }
                else
                {
                    return ApiResult.Success(StoreCustomer);
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 客户筛选
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">服务员Id</param>
        /// <param name="ConsumerTimeType">消费时间类型 0：所有 1：最近一周 2：最近一个月 3：最近三个月</param>
        /// <param name="ConsumerCount">消费次数 0：所有 其他： 1-3次</param>
        /// <returns></returns>
        [HttpGet, Route("Business/QueryCustomer")]
        public HttpResponseMessage QueryCustomer(int StoreId, int BusinessId, int ConsumerTimeType, int ConsumerCount)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息（或已删除）");
                }
                var business = t_businessBLL.GetModel(BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息（或已删除）");
                }
                if (!(new List<int> { 0, 1, 2, 3 }).Contains(ConsumerTimeType))
                {
                    return ApiResult.Error("参数ConsumerTimeType错误");
                }
                if (!(new List<int> { 0, 1, 2, 3 }).Contains(ConsumerCount))
                {
                    return ApiResult.Error("参数ConsumerCount错误");
                }
                #endregion

                //获取该服务人员的客户列表
                var StoreCustomer = t_store_customer_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", StoreId, BusinessId));
                if (!StoreCustomer.Any())
                {
                    return ApiResult.Success(new List<Model.t_customer>());
                }
                var customers = t_customerBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0 ",
                         string.Join(",", StoreCustomer.Select(o => o.CustomerId))));

                //获取客户的消费列表
                var consumption = t_consumptionBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1} and CustomerId IN ({2}) and IsDeleted=0 ",
                            StoreId, BusinessId, string.Join(",", customers.Select(o => o.ID))));

                var result = consumption.GroupBy(o => o.CustomerId).Select(o => new
                {
                    CustomerId = o.Key,
                    ConsumeTime = o.Max(p => p.ConsumeTime),
                    Count = o.Count()
                });

                //消费时间
                if (ConsumerTimeType > 0)
                {
                    int day = 0;
                    switch (ConsumerTimeType)
                    {
                        case 1:
                            day = 7;
                            break;
                        case 2:
                            day = 30;
                            break;
                        case 3:
                            day = 90;
                            break;
                        default:
                            break;
                    }
                    DateTime compareTime = Convert.ToDateTime(DateTime.Now.AddDays(-day).ToString("yyyy-MM-dd"));
                    result = result.Where(o => o.ConsumeTime >= compareTime);
                }
                //消费次数
                if (ConsumerCount > 0)
                {
                    result = result.Where(o => o.Count == ConsumerCount);
                }

                if (result.Any())
                {
                    var leftJoin = from r in result
                                   join c in customers on r.CustomerId equals c.ID into temp
                                   from t in temp.DefaultIfEmpty()
                                   select new
                                   {
                                       r.CustomerId,
                                       ConsumeTime = r.ConsumeTime.ToString("yyyy-MM-dd"),
                                       r.Count,
                                       Nickname = (t == null ? "" : (t.CustomerType != 1 ? t.Nickname : t.TrueName)),
                                       AvatarUrl = (t == null ? "" : t.AvatarUrl)
                                   };

                    return ApiResult.Success(leftJoin);
                }
                else
                {
                    return ApiResult.Success(result);
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取客户详情
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">服务人员Id</param>
        /// <param name="CustomerId">客户Id</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetCustomerDetail")]
        public HttpResponseMessage GetCustomerDetail(int StoreId, int BusinessId, int CustomerId)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                if (CustomerId <= 0)
                {
                    return ApiResult.Error("参数CustomerId错误");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息（或已删除）");
                }
                var business = t_businessBLL.GetModel(BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息（或已删除）");
                }
                var customer = t_customerBLL.GetModel(CustomerId);
                if (customer == null || customer.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该客户信息（或已删除）");
                }
                #endregion

                var storecustomerbusiness = t_store_customer_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1} and CustomerId ={2}",
                            StoreId, BusinessId, CustomerId)).FirstOrDefault();

                //获取客户的消费列表
                var consumption = t_consumptionBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1} and CustomerId ={2} and IsDeleted=0 ",
                            StoreId, BusinessId, CustomerId));

                return ApiResult.Success(new

                {
                    customer.ID,
                    Nickname = (customer.CustomerType != 1 ? customer.Nickname : customer.TrueName),
                    customer.AvatarUrl,
                    customer.PhoneNumber,
                    Remark = (storecustomerbusiness == null ? "" : storecustomerbusiness.Remark),
                    ConsumptionList = consumption.OrderByDescending(o => o.ConsumeTime).Select(o => new
                    {
                        o.ID,
                        o.Money,
                        ConsumeTime = o.ConsumeTime.ToString("yyyy-MM-dd")
                    })
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 添加客户备注信息
        /// </summary>
        /// <param name="RequestDto"></param>
        /// <returns></returns>
        [HttpPost, Route("Business/AddCustomerRemark")]
        public HttpResponseMessage AddCustomerRemark(AddCustomerRemarkRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (RequestDto.BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                if (RequestDto.CustomerId <= 0)
                {
                    return ApiResult.Error("参数CustomerId错误");
                }
                var store = t_storeBLL.GetModel(RequestDto.StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息（或已删除）");
                }
                var business = t_businessBLL.GetModel(RequestDto.BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息（或已删除）");
                }
                var customer = t_customerBLL.GetModel(RequestDto.CustomerId);
                if (customer == null || customer.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该客户信息（或已删除）");
                }
                if (string.IsNullOrEmpty(RequestDto.Remark))
                {
                    return ApiResult.Error("备注信息不能为空");
                }

                #endregion

                var storecustomerbusiness = t_store_customer_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1} and CustomerId ={2}",
                            RequestDto.StoreId, RequestDto.BusinessId, RequestDto.CustomerId)).FirstOrDefault();

                if (storecustomerbusiness == null)
                {
                    return ApiResult.Error("该门店服务人员没有该客户信息");
                }
                storecustomerbusiness.Remark = RequestDto.Remark;
                var result = t_store_customer_businessBLL.Update(storecustomerbusiness);

                if (result < 0)
                {
                    return ApiResult.Error("更新客户备注信息失败");
                }

                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 删除客户
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/DeleteCustomer")]
        public HttpResponseMessage DeleteCustomer(DeleteCustomerRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (RequestDto.BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                if (RequestDto.CustomerId <= 0)
                {
                    return ApiResult.Error("参数CustomerId错误");
                }
                var store = t_storeBLL.GetModel(RequestDto.StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息（或已删除）");
                }
                var business = t_businessBLL.GetModel(RequestDto.BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息（或已删除）");
                }
                var customer = t_customerBLL.GetModel(RequestDto.CustomerId);
                if (customer == null || customer.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该客户信息（或已删除）");
                }
                //if (customer.CustomerType == 0)
                //{
                //    return ApiResult.Error("只能删除手动添加客户");
                //}
                #endregion

                var StoreCustomerBusiness = t_store_customer_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1} and CustomerId={2}", RequestDto.StoreId, RequestDto.BusinessId, RequestDto.CustomerId)).FirstOrDefault();
                //开启事务
                using (var connection = CommonBll.GetOpenMySqlConnection())
                {
                    IDbTransaction transaction = connection.BeginTransaction();
                    var DeleteCustomer = t_customerBLL.DeleteByTrans(customer, connection, transaction);
                    if (DeleteCustomer <= 0)
                    {
                        transaction.Rollback();
                        return ApiResult.Error("删除客户信息失败");
                    }

                    if (StoreCustomerBusiness != null)
                    {
                        var DeleteStoreCustomerBusiness = t_store_customer_businessBLL.DeleteByTrans(StoreCustomerBusiness, connection, transaction);
                        if (DeleteStoreCustomerBusiness < 0)
                        {
                            transaction.Rollback();
                            return ApiResult.Error("删除关联门店商户记录失败");
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
        /// 新增消费记录
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/AddConsumption")]
        public HttpResponseMessage AddConsumption(AddConsumptionRequestDto RequestDto)
        {
            try
            {
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (RequestDto.BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                if (RequestDto.CustomerId <= 0)
                {
                    return ApiResult.Error("参数CustomerId错误");
                }
                var store = t_storeBLL.GetModel(RequestDto.StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息");
                }
                var business = t_businessBLL.GetModel(RequestDto.BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息");
                }
                var customer = t_customerBLL.GetModel(RequestDto.CustomerId);
                if (customer == null || customer.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该客户信息");
                }
                if (string.IsNullOrEmpty(RequestDto.BoxNumber))
                {
                    return ApiResult.Error("包厢号不能为空");
                }
                if (RequestDto.ConsumeTime > DateTime.Now)
                {
                    return ApiResult.Error("参数ConsumeTime错误");
                }

                var InsertConsumption = t_consumptionBLL.Insert(new Model.t_consumption
                {
                    BoxNumber = RequestDto.BoxNumber,
                    BusinessId = RequestDto.BusinessId,
                    CustomerId = RequestDto.CustomerId,
                    StoreId = RequestDto.StoreId,
                    RecordType = 1,
                    ConsumeTime = RequestDto.ConsumeTime,
                    IsDeleted = 0,
                    CreationTime = DateTime.Now
                });
                if (InsertConsumption < 0)
                {
                    return ApiResult.Error("写入消费记录失败");
                }
                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        ///  获取消费记录详情
        /// </summary>
        /// <param name="ConsumerId">消费记录Id</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetConsumptionDetail")]
        public HttpResponseMessage GetConsumptionDetail(int ConsumerId)
        {
            try
            {
                if (ConsumerId <= 0)
                {
                    return ApiResult.Error("参数ConsumerId错误");
                }

                var Consumption = t_consumptionBLL.GetModel(ConsumerId);
                if (Consumption == null || Consumption.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该消费记录信息(或已删除)");
                }
                //获取消费记录所含服务人员信息
                var business = t_businessBLL.GetModel(Consumption.BusinessId);
                return ApiResult.Success(new
                {
                    ConsumerId = Consumption.ID,
                    Consumption.BoxNumber,
                    Consumption.RecordType,
                    ConsumeTime = Consumption.ConsumeTime.ToString("yyyy-MM-dd"),
                    Consumption.BusinessId,
                    ComeSourceTime = Consumption.CreationTime.ToString("yyyy-MM-dd"),
                    BusinessName = business != null ? (string.IsNullOrEmpty(business.Nickname) ? business.TrueName : business.Nickname) : ""
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 服务评价排行榜
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">服务人员Id</param>
        /// <param name="RoleId">角色Id</param>
        /// <param name="StatisticalType">统计类型：0：总榜 1：周榜 2：月榜</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetServiceEvaluation")]
        public HttpResponseMessage GetServiceEvaluation(int StoreId, int BusinessId, int RoleId, int StatisticalType)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (!(new List<int> { 0, 1, 2, 3 }).Contains(RoleId))
                {
                    return ApiResult.Error("参数RoleId错误");
                }
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息(或已删除)");
                }
                var business = t_businessBLL.GetModel(BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息(或已删除)");
                }
                if (!(new List<int> { 0, 1, 2 }).Contains(StatisticalType))
                {
                    return ApiResult.Error("参数StatisticalType错误");
                }
                #endregion

                //获取该门店的所有服务评价记录
                var ServiceEvaluation = t_service_evaluationBLL.GetListByWhere(string.Format("where StoreId={0} and IsDeleted=0", StoreId));

                //统计类型过滤
                if (StatisticalType > 0)
                {
                    DateTime? StartTime = null;
                    DateTime? EndTime = null;
                    DateTime NowTime = DateTime.Now;
                    int DayOfWeek = (int)NowTime.DayOfWeek;
                    DayOfWeek = (DayOfWeek == 0 ? 7 : DayOfWeek);
                    int Day = (int)NowTime.Day;
                    switch (StatisticalType)
                    {
                        case 1:
                            StartTime = NowTime.AddDays(-DayOfWeek + 1);
                            EndTime = NowTime.AddDays(7 - DayOfWeek);
                            break;
                        case 2:
                            StartTime = NowTime.AddDays(-Day + 1);
                            EndTime = NowTime.AddMonths(1).AddDays(-NowTime.AddMonths(1).Day + 1).AddDays(-1);
                            break;
                        default:
                            break;
                    }
                    DateTime StartTimeStr = Convert.ToDateTime(StartTime.Value.ToString("yyyy-MM-dd"));
                    DateTime EndTimeStr = Convert.ToDateTime(EndTime.Value.AddDays(1).ToString("yyyy-MM-dd"));
                    ServiceEvaluation = ServiceEvaluation.Where(o => o.EvaluateTime >= StartTimeStr && o.EvaluateTime < EndTimeStr);
                }

                if (ServiceEvaluation.Any())
                {
                    //服务员评价得分 1：满意 2：一般 3：不满意
                    //总计数为0不显示，只显示榜单前十名
                    var GroupServiceEvaluation = ServiceEvaluation.Where(o => o.Score == 1).GroupBy(o => o.BusinessId).
                        Select(o => new
                        {
                            BusinessId = o.Key,
                            Count = o.Count()
                        }).OrderByDescending(o => o.Count).Take(10);

                    if (GroupServiceEvaluation.Any())
                    {
                        //获取服务人员信息
                        var businessList = t_businessBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0 ",
                        string.Join(",", GroupServiceEvaluation.Select(o => o.BusinessId))));

                        var leftJoin = from s in GroupServiceEvaluation
                                       join b in businessList on s.BusinessId equals b.ID into temp
                                       from t in temp.DefaultIfEmpty()
                                       select new
                                       {
                                           s.BusinessId,
                                           s.Count,
                                           Nickname = (t == null ? "" : string.IsNullOrEmpty(t.Nickname) ? t.TrueName : t.Nickname),
                                           AvatarUrl = (t == null ? "" : t.AvatarUrl),
                                       };
                        if (RoleId == 3) //对于服务人员还需要显示个人服务评价统计信息
                        {
                            var ServiceEvaluationTemp = ServiceEvaluation.Where(o => o.BusinessId == BusinessId);

                            return ApiResult.Success(new
                            {
                                Nickname = string.IsNullOrEmpty(business.Nickname) ? business.TrueName : business.Nickname,
                                business.AvatarUrl,
                                SelfSatisfy = ServiceEvaluationTemp.Where(o => o.Score == 1).Count(),  //满意
                                SelfCommon = ServiceEvaluationTemp.Where(o => o.Score == 2).Count(),   //一般
                                SelfNoSatisfy = ServiceEvaluationTemp.Where(o => o.Score == 3).Count(), //不满意
                                RankList = leftJoin
                            });
                        }
                        else
                        {
                            return ApiResult.Success(leftJoin);
                        }
                    }
                    else
                    {
                        return ApiResult.Success(GroupServiceEvaluation);
                    }
                }
                else
                {
                    return ApiResult.Success(ServiceEvaluation);
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 打赏排行榜（支付成功）
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">服务人员Id</param>
        /// <param name="RoleId">角色Id</param>
        /// <param name="StatisticalType">统计类型：0：总榜 1：周榜 2：月榜</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetBusinessReward")]
        public HttpResponseMessage GetBusinessReward(int StoreId, int BusinessId, int RoleId, int StatisticalType)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (!(new List<int> { 0, 1, 2, 3 }).Contains(RoleId))
                {
                    return ApiResult.Error("参数RoleId错误");
                }
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息(或已删除)");
                }
                var business = t_businessBLL.GetModel(BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息(或已删除)");
                }
                if (!(new List<int> { 0, 1, 2 }).Contains(StatisticalType))
                {
                    return ApiResult.Error("参数StatisticalType错误");
                }
                #endregion

                //获取该门店的所有打赏记录（支付成功）
                var StoreReward = t_rewardBLL.GetListByWhere(string.Format("where StoreId={0} and IsDeleted=0 and PaymentNo<>''", StoreId));
                if (StatisticalType > 0)
                {
                    DateTime? StartTime = null;
                    DateTime? EndTime = null;
                    DateTime NowTime = DateTime.Now;
                    int DayOfWeek = (int)NowTime.DayOfWeek;
                    DayOfWeek = (DayOfWeek == 0 ? 7 : DayOfWeek);
                    int Day = (int)NowTime.Day;
                    switch (StatisticalType)
                    {
                        case 1:
                            StartTime = NowTime.AddDays(-DayOfWeek + 1);
                            EndTime = NowTime.AddDays(7 - DayOfWeek);
                            break;
                        case 2:
                            StartTime = NowTime.AddDays(-Day + 1);
                            EndTime = NowTime.AddMonths(1).AddDays(-NowTime.AddMonths(1).Day + 1).AddDays(-1);
                            break;
                        default:
                            break;
                    }
                    DateTime StartTimeStr = Convert.ToDateTime(StartTime.Value.ToString("yyyy-MM-dd"));
                    DateTime EndTimeStr = Convert.ToDateTime(EndTime.Value.AddDays(1).ToString("yyyy-MM-dd"));
                    StoreReward = StoreReward.Where(o => o.RewardTime >= StartTimeStr && o.RewardTime < EndTimeStr);
                }
                if (StoreReward.Any())
                {
                    //总计数为0不显示，只显示榜单前十名
                    var GroupStoreReward = StoreReward.GroupBy(o => o.BusinessId).
                        Select(o => new
                        {
                            BusinessId = o.Key,
                            Money = o.Sum(p => p.Money)
                        }).OrderByDescending(o => o.Money).Take(10);

                    if (GroupStoreReward.Any())
                    {
                        //获取服务人员信息
                        var businessList = t_businessBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0 ",
                        string.Join(",", GroupStoreReward.Select(o => o.BusinessId))));

                        var leftJoin = from s in GroupStoreReward
                                       join b in businessList on s.BusinessId equals b.ID into temp
                                       from t in temp.DefaultIfEmpty()
                                       select new
                                       {
                                           s.BusinessId,
                                           s.Money,
                                           Nickname = (t == null ? "" : string.IsNullOrEmpty(t.Nickname) ? t.TrueName : t.Nickname),
                                           AvatarUrl = (t == null ? "" : t.AvatarUrl),
                                       };
                        if (RoleId == 3)
                        {
                            var StoreRewardTemp = StoreReward.Where(o => o.BusinessId == BusinessId);

                            return ApiResult.Success(new
                            {
                                Nickname = string.IsNullOrEmpty(business.Nickname) ? business.TrueName : business.Nickname,
                                business.AvatarUrl,
                                SelfRewardCount = StoreRewardTemp.Count(),
                                SelfRewardMoney = StoreRewardTemp.Sum(o => o.Money),
                                RankList = leftJoin
                            });
                        }
                        else
                        {
                            return ApiResult.Success(leftJoin);
                        }
                    }
                    else
                    {
                        return ApiResult.Success(GroupStoreReward);
                    }
                }
                else
                {
                    return ApiResult.Success(StoreReward);
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 个人打赏统计（服务人员）
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">服务人员Id</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetPersonalReward")]
        public HttpResponseMessage GetPersonalReward(int StoreId, int BusinessId)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息");
                }
                var business = t_businessBLL.GetModel(BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息");
                }
                //获取门店服务人员信息
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", StoreId, BusinessId)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店没有该服务人员信息");
                }
                if (StoreBusiness.State == 1)
                {
                    return ApiResult.Error("该商户已被禁用");
                }
                //if (StoreBusiness.RoleId != 3)
                //{
                //    return ApiResult.Error("该角色商户没有个人打赏统计信息");
                //}
                #endregion

                //获取该门店所指定的服务人的所有打赏记录（支付成功）
                var StoreReward = t_rewardBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1} and IsDeleted=0 and PaymentNo<>''",
                                                         StoreId, BusinessId));
                if (StoreReward.Any())
                {
                    //获取打赏明细数据
                    var RewardDetail = t_reward_detailBLL.GetListByWhere(string.Format("where RewardId IN({0}) and OpenrId='{1}' and UserType=3 ",
                           string.Join(",", StoreReward.Select(o => o.ID)), business.OpenId));

                    //获取打赏人信息
                    var customerList = t_customerBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0 ",
                   string.Join(",", StoreReward.GroupBy(o => o.CustomerId).Select(o => o.Key))));

                    var leftJoin = from s in StoreReward
                                   join c in customerList on s.CustomerId equals c.ID into temp
                                   from t in temp.DefaultIfEmpty()
                                   join r in RewardDetail on s.ID equals r.RewardId into c_join
                                   from v in c_join.DefaultIfEmpty()
                                   select new
                                   {
                                       Nickname = (t == null ? "" : (t.CustomerType != 1 ? t.Nickname : t.TrueName)),
                                       AvatarUrl = (t == null ? "" : t.AvatarUrl),
                                       BenefitMoney = (v == null ? 0 : v.BenefitMoney),
                                       RewardMoney = (v == null ? 0 : v.RewardMoney),
                                       RewardTime = s.RewardTime.ToString("yyyy-MM-dd HH:mm:ss")
                                   };

                    return ApiResult.Success(new
                    {
                        business.AvatarUrl,
                        Nickname = string.IsNullOrEmpty(business.Nickname) ? business.TrueName : business.Nickname,
                        RewardCount = StoreReward.Count(),
                        RewardMoney = StoreReward.Sum(o => o.Money),
                        BenefitMoney = RewardDetail.Sum(o => o.BenefitMoney),
                        RewardDetail = leftJoin
                    });
                }
                else
                {
                    return ApiResult.Success(new
                    {
                        business.AvatarUrl,
                        Nickname = string.IsNullOrEmpty(business.Nickname) ? business.TrueName : business.Nickname,
                        RewardCount = 0,
                        RewardMoney = 0,
                        BenefitMoney = 0,
                        RewardDetail = StoreReward
                    });
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 打赏统计（超级管理员）
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">服务人员Id</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetRewardStatistical")]
        public HttpResponseMessage GetRewardStatistical(int StoreId, int BusinessId)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息");
                }
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                var business = t_businessBLL.GetModel(BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息");
                }
                //获取门店服务人员信息
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", StoreId, BusinessId)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店没有该服务人员信息");
                }
                if (StoreBusiness.State == 1)
                {
                    return ApiResult.Error("该商户已被禁用");
                }
                if (StoreBusiness.RoleId != 0)
                {
                    return ApiResult.Error("该角色商户没有个人打赏统计信息");
                }
                #endregion

                //获取该门店所指定的服务人的所有打赏记录（支付成功）
                var StoreReward = t_rewardBLL.GetListByWhere(string.Format("where StoreId={0} and IsDeleted=0 and PaymentNo<>''",
                                                         StoreId));
                if (StoreReward.Any())
                {
                    //获取打赏明细数据
                    var RewardDetail = t_reward_detailBLL.GetListByWhere(string.Format("where RewardId IN({0}) and UserType=3 ",
                           string.Join(",", StoreReward.Select(o => o.ID))));

                    //获取打赏人信息
                    var customerList = t_customerBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0 ",
                   string.Join(",", StoreReward.GroupBy(o => o.CustomerId).Select(o => o.Key))));

                    var leftJoin = from s in StoreReward
                                   join c in customerList on s.CustomerId equals c.ID into temp
                                   from t in temp.DefaultIfEmpty()
                                   join r in RewardDetail on s.ID equals r.RewardId into c_join
                                   from v in c_join.DefaultIfEmpty()
                                   select new
                                   {
                                       Nickname = (t == null ? "" : (t.CustomerType != 1 ? t.Nickname : t.TrueName)),
                                       AvatarUrl = (t == null ? "" : t.AvatarUrl),
                                       BenefitMoney = (v == null ? 0 : v.BenefitMoney),
                                       RewardMoney = (v == null ? 0 : v.RewardMoney),
                                       RewardTime = s.RewardTime.ToString("yyyy-MM-dd HH:mm:ss")
                                   };

                    return ApiResult.Success(new
                    {
                        store.StoreImgUrl,
                        store.StoreName,
                        RewardCount = StoreReward.Count(),
                        RewardMoney = StoreReward.Sum(o => o.Money),
                        BenefitMoney = RewardDetail.Sum(o => o.BenefitMoney),
                        RewardDetail = leftJoin
                    });
                }
                else
                {
                    return ApiResult.Success(new
                    {
                        store.StoreImgUrl,
                        store.StoreName,
                        RewardCount = 0,
                        RewardMoney = 0,
                        BenefitMoney = 0,
                        RewardDetail = StoreReward
                    });
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取员工管理列表
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">商户Id</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetBusinessManagerList")]
        public HttpResponseMessage GetBusinessManagerList(int StoreId, int BusinessId)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                //if (!(new List<int> { 0, 1 }).Contains(RoleId))
                //{
                //    return ApiResult.Error("该商户没有员工管理列表信息");
                //}
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息");
                }
                var business = t_businessBLL.GetModel(BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息");
                }
                var StoreBusinessList = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0}", StoreId));
                var CurrentBusiness = StoreBusinessList.Where(o => o.BusinessId == BusinessId).FirstOrDefault();
                if (CurrentBusiness == null)
                {
                    return ApiResult.Error("该门店没有该服务人员");
                }
                if (CurrentBusiness.RoleId != 0 && CurrentBusiness.RoleId != 1)
                {
                    return ApiResult.Error("该商户没有员工管理列表信息");
                }
                #endregion

                // 获取门店人员信息
                List<Model.t_business> StoreBusiness = new List<Model.t_business>();
                if (StoreBusinessList.Any())
                {
                    StoreBusiness = t_businessBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0",
                        string.Join(",", StoreBusinessList.Select(o => o.BusinessId)))).ToList();
                }

                if (CurrentBusiness.RoleId == 1)
                {
                    #region 管理员
                    List<Model.t_business> subMangerBusiness = new List<Model.t_business>();
                    List<Model.t_business> subBusiness = new List<Model.t_business>();
                    ////获取所有的下属门店经理
                    //var StoreBusinessManger = StoreBusinessList.Where(o => o.ParentId == BusinessId && o.RoleId == 2);
                    //if (StoreBusinessManger.Any())
                    //{
                    //    var MangerBusinessId = StoreBusinessManger.Select(o => o.BusinessId);
                    //    subMangerBusiness = StoreBusiness.Where(o => MangerBusinessId.Any(p => p == o.ID) && o.IsDeleted == 0).ToList();

                    //    //获取所有的下属员工
                    //    var StoreBusinessService = StoreBusinessList.Where(o => MangerBusinessId.Any(p => p == o.ParentId) && o.RoleId == 3);
                    //    if (StoreBusinessService.Any())
                    //    {
                    //        var ServiceBusinessId = StoreBusinessService.Select(o => o.BusinessId);
                    //        subBusiness = StoreBusiness.Where(o => ServiceBusinessId.Any(p => p == o.ID) && o.IsDeleted == 0).ToList();
                    //    }
                    //}
                    //获取所有的下属门店经理
                    var StoreBusinessManger = StoreBusinessList.Where(o => o.RoleId == 2);
                    if (StoreBusinessManger.Any())
                    {
                        var MangerBusinessId = StoreBusinessManger.Select(o => o.BusinessId);
                        subMangerBusiness = StoreBusiness.Where(o => MangerBusinessId.Any(p => p == o.ID) && o.IsDeleted == 0).ToList();


                    }
                    //获取所有的下属员工
                    var StoreBusinessService = StoreBusinessList.Where(o => o.RoleId == 3);
                    if (StoreBusinessService.Any())
                    {
                        var ServiceBusinessId = StoreBusinessService.Select(o => o.BusinessId);
                        subBusiness = StoreBusiness.Where(o => ServiceBusinessId.Any(p => p == o.ID) && o.IsDeleted == 0).ToList();
                    }
                    return ApiResult.Success(new
                    {
                        Manager = new
                        {
                            RoleId = 2,
                            Count = subMangerBusiness.Count()
                        },
                        Business = new
                        {
                            RoleId = 3,
                            Count = subBusiness.Count()
                        }
                    });
                    #endregion
                }
                else if (CurrentBusiness.RoleId == 0)
                {
                    #region 超级管理员
                    List<Model.t_business> subBusiness = new List<Model.t_business>();
                    List<Model.t_business> subMangerBusiness = new List<Model.t_business>();
                    List<Model.t_business> subAdminBusiness = new List<Model.t_business>();

                    ////获取所有的下属管理员
                    //var subAdminBusinessAdmin = StoreBusinessList.Where(o => o.ParentId == BusinessId && o.RoleId == 1);
                    //if (subAdminBusinessAdmin.Any())
                    //{
                    //    var AdminBusinessId = subAdminBusinessAdmin.Select(o => o.BusinessId);
                    //    subAdminBusiness = StoreBusiness.Where(o => AdminBusinessId.Any(p => p == o.ID) && o.IsDeleted == 0).ToList();

                    //    //获取所有的下属门店经理
                    //    var StoreBusinessManger = StoreBusinessList.Where(o => AdminBusinessId.Any(p => p == o.ParentId) && o.RoleId == 2);
                    //    if (StoreBusinessManger.Any())
                    //    {
                    //        var MangerBusinessId = StoreBusinessManger.Select(o => o.BusinessId);
                    //        subMangerBusiness = StoreBusiness.Where(o => MangerBusinessId.Any(p => p == o.ID) && o.IsDeleted == 0).ToList();

                    //        //获取所有的下属员工
                    //        var StoreBusinessService = StoreBusinessList.Where(o => MangerBusinessId.Any(p => p == o.ParentId) && o.RoleId == 3);
                    //        if (StoreBusinessService.Any())
                    //        {
                    //            var ServiceBusinessId = StoreBusinessService.Select(o => o.BusinessId);
                    //            subBusiness = StoreBusiness.Where(o => ServiceBusinessId.Any(p => p == o.ID) && o.IsDeleted == 0).ToList();
                    //        }
                    //    }
                    //}
                    //获取所有的下属管理员
                    var subAdminBusinessAdmin = StoreBusinessList.Where(o => o.RoleId == 1);
                    if (subAdminBusinessAdmin.Any())
                    {
                        var AdminBusinessId = subAdminBusinessAdmin.Select(o => o.BusinessId);
                        subAdminBusiness = StoreBusiness.Where(o => AdminBusinessId.Any(p => p == o.ID) && o.IsDeleted == 0).ToList();
                    }

                    //获取所有的下属门店经理
                    var StoreBusinessManger = StoreBusinessList.Where(o => o.RoleId == 2);
                    if (StoreBusinessManger.Any())
                    {
                        var MangerBusinessId = StoreBusinessManger.Select(o => o.BusinessId);
                        subMangerBusiness = StoreBusiness.Where(o => MangerBusinessId.Any(p => p == o.ID) && o.IsDeleted == 0).ToList();
                    }

                    //获取所有的下属员工
                    var StoreBusinessService = StoreBusinessList.Where(o => o.RoleId == 3);
                    if (StoreBusinessService.Any())
                    {
                        var ServiceBusinessId = StoreBusinessService.Select(o => o.BusinessId);
                        subBusiness = StoreBusiness.Where(o => ServiceBusinessId.Any(p => p == o.ID) && o.IsDeleted == 0).ToList();
                    }
                    return ApiResult.Success(new
                    {
                        Admin = new
                        {
                            RoleId = 1,
                            Count = subAdminBusiness.Count()
                        },
                        Manager = new
                        {
                            RoleId = 2,
                            Count = subMangerBusiness.Count()
                        },
                        Business = new
                        {
                            RoleId = 3,
                            Count = subBusiness.Count()
                        }
                    });
                    #endregion
                }

                return ApiResult.Error("该商户没有员工管理列表信息");
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取指定角色的员工列表
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="RoleId">角色Id（1：管理员 2：门店经理 3：服务人员）</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetRoleBusinessList")]
        public HttpResponseMessage GetRoleBusinessList(int StoreId, int RoleId)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息");
                }
                if (!(new List<int> { 1, 2, 3 }).Contains(RoleId))
                {
                    return ApiResult.Error("参数RoleId错误");
                }
                #endregion

                // 获取门店指定角色人员信息
                List<Model.t_business> StoreBusiness = new List<Model.t_business>();
                var StoreBusinessList = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and RoleId={1}", StoreId, RoleId));
                if (StoreBusinessList.Any())
                {
                    StoreBusiness = t_businessBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0",
                        string.Join(",", StoreBusinessList.Select(o => o.BusinessId)))).ToList();
                }

                var leftJoin = from s in StoreBusinessList
                               join b in StoreBusiness on s.BusinessId equals b.ID into temp
                               from t in temp.DefaultIfEmpty()
                               select new
                               {
                                   s.BusinessId,
                                   s.State,
                                   Nickname = (t == null ? "" : (string.IsNullOrEmpty(t.TrueName) ? t.Nickname : t.TrueName)),
                                   AvatarUrl = (t == null ? "" : t.AvatarUrl),
                               };

                return ApiResult.Success(leftJoin);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取商户详情
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">商户Id</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetBusinessDetail")]
        public HttpResponseMessage GetBusinessDetail(int StoreId, int BusinessId)
        {
            try
            {
                //1.参数验证
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }

                //2.获取商户信息
                var business = t_businessBLL.GetListByWhere(string.Format("where ID={0} and IsDeleted=0", BusinessId)).FirstOrDefault();
                if (business == null)
                {
                    return ApiResult.Error("没有该商户信息(或已删除)");
                }

                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", StoreId, BusinessId)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店没有该商户信息");
                }

                //3.返回结果
                return ApiResult.Success(new
                {
                    BusinessId = business.ID,
                    business.AvatarUrl,
                    Nickname = string.IsNullOrEmpty(business.Nickname) ? business.TrueName : business.Nickname,
                    business.PhoneNumber,
                    business.OpenId,
                    StoreBusiness.RoleId,
                    StoreBusiness.ParentId,
                    StoreBusiness.State
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 修改员工信息
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/UpdateBusinessInfo")]
        public HttpResponseMessage UpdateBusinessInfo(UpdateBusinessInfoRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (RequestDto.BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                if (!(new List<int> { 1, 2, 3 }).Contains(RequestDto.RoleId))
                {
                    return ApiResult.Error("参数RoleId错误");
                }
                if (RequestDto.ParentId < 0)
                {
                    return ApiResult.Error("参数ParentId错误");
                }
                if (!(new List<int> { 0, 1 }).Contains(RequestDto.State))
                {
                    return ApiResult.Error("参数State错误");
                }
                #endregion

                //获取商户信息
                var business = t_businessBLL.GetListByWhere(string.Format("where ID={0} and IsDeleted=0", RequestDto.BusinessId)).FirstOrDefault();
                if (business == null)
                {
                    return ApiResult.Error("没有该商户信息（或已删除）");
                }
                if (RequestDto.ParentId > 0)
                {
                    var businessParent = t_businessBLL.GetListByWhere(string.Format("where ID={0} and IsDeleted=0", RequestDto.ParentId)).FirstOrDefault();
                    if (businessParent == null)
                    {
                        return ApiResult.Error("没有该上级商户信息（或已删除）");
                    }
                }

                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", RequestDto.StoreId, RequestDto.BusinessId)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店没有该商户信息");
                }

                StoreBusiness.RoleId = RequestDto.RoleId;
                StoreBusiness.ParentId = RequestDto.ParentId;
                StoreBusiness.State = RequestDto.State;

                var UpdateStoreBusiness = t_store_businessBLL.Update(StoreBusiness);
                if (UpdateStoreBusiness < 0)
                {
                    return ApiResult.Error("更新该商户信息失败");
                }

                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 统计中心
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">服务人员Id</param>
        /// <param name="StatisticalType">统计类型：1：日报 2：周报 3：月报</param>
        /// <returns></returns>
        [HttpGet, Route("Business/StatisticalCenter")]
        public HttpResponseMessage StatisticalCenter(int StoreId, int BusinessId, int StatisticalType)
        {
            try
            {
                #region 参数验证
                if (!(new List<int> { 1, 2, 3 }).Contains(StatisticalType))
                {
                    return ApiResult.Error("统计类型值错误");
                }
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息（或已删除）");
                }
                var business = t_businessBLL.GetModel(BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息（或已删除）");
                }
                var CurrentStoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", StoreId, BusinessId)).FirstOrDefault();
                if (CurrentStoreBusiness == null)
                {
                    return ApiResult.Error("该门店没有该商户信息");
                }
                if (CurrentStoreBusiness.RoleId == 3)
                {
                    return ApiResult.Error("该商户没有统计中心权限");
                }
                #endregion

                // 获取门店人员信息
                List<Model.t_business> StoreBusiness = new List<Model.t_business>();
                var StoreBusinessList = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0}", StoreId));
                if (StoreBusinessList.Any())
                {
                    StoreBusiness = t_businessBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0",
                        string.Join(",", StoreBusinessList.Select(o => o.BusinessId)))).ToList();
                }

                #region 获取下属人员信息
                List<Model.t_business> subBusiness = new List<Model.t_business>();
                if (CurrentStoreBusiness.RoleId == 0)
                {
                    subBusiness = StoreBusiness;
                }
                else
                {
                    List<int> Roles = new List<int>();
                    switch (CurrentStoreBusiness.RoleId)
                    {
                        case 1:
                            Roles.Add(2);
                            Roles.Add(3);
                            break;
                        case 2:
                            Roles.Add(3);
                            break;
                        default:
                            break;
                    }
                    var subStoreBusinessList = StoreBusinessList.Where(o => Roles.Any(p => p == o.RoleId)).Select(o => o.BusinessId);
                    subBusiness = subBusiness.Where(o => subStoreBusinessList.Any(p => p == o.ID) && o.IsDeleted == 0).ToList();
                }
                #endregion

                var subBusinessId = subBusiness.Select(o => o.ID);  //下属人员Id集合

                //获取该门店的所有打赏记录（支付成功）
                var StoreReward = t_rewardBLL.GetListByWhere(string.Format("where StoreId={0} and IsDeleted=0 and PaymentNo<>''", StoreId));

                //获取下属打赏记录信息
                var subReward = StoreReward.Where(o => subBusinessId.Any(p => p == o.BusinessId));

                //获取该门店的所有服务评价记录
                var ServiceEvaluation = t_service_evaluationBLL.GetListByWhere(string.Format("where StoreId={0} and IsDeleted=0", StoreId));

                //获取下属服务评价记录
                var subServiceEvaluation = ServiceEvaluation.Where(o => subBusinessId.Any(p => p == o.BusinessId));

                //获取该门店下属客户信息
                var StoreCustomer = t_store_customer_businessBLL.GetListByWhere(string.Format("where StoreId={0}", StoreId)).Where(o => subBusinessId.Any(p => p == o.BusinessId)).Select(o => o.CustomerId);

                //获取下属所有客户信息
                var subCustomer = t_customerBLL.GetList().Where(o => o.IsDeleted == 0 && StoreCustomer.Any(p => p == o.ID));  //手动添加的客户也算

                #region 获取查询时间
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

                DateTime StartTimeStr = Convert.ToDateTime(StartTime.Value.ToString("yyyy-MM-dd"));
                DateTime EndTimeStr = EndTime.HasValue ? Convert.ToDateTime(EndTime.Value.AddDays(1).ToString("yyyy-MM-dd")) :
                    Convert.ToDateTime(StartTime.Value.AddDays(1).ToString("yyyy-MM-dd"));
                #endregion

                subReward = subReward.Where(o => o.RewardTime >= StartTimeStr && o.RewardTime < EndTimeStr);
                var GroupsubServiceEvaluation = subServiceEvaluation.Where(o => o.EvaluateTime >= StartTimeStr && o.EvaluateTime < EndTimeStr).GroupBy(o => o.BusinessId);
                subCustomer = subCustomer.Where(o => o.CreationTime >= StartTimeStr && o.CreationTime < EndTimeStr);

                return ApiResult.Success(new
                {
                    StartTime = StartTime.Value.ToString("yyyy-MM-dd"),
                    EndTime = EndTime.HasValue ? EndTime.Value.ToString("yyyy-MM-dd") : "",
                    SubEvaluationCount = GroupsubServiceEvaluation.Count(),
                    SubRewardMoney = subReward.Sum(o => o.Money),
                    SubNewCustomerCount = subCustomer.Count()
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取门店评价记录
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetStoreEvaluation")]
        public HttpResponseMessage GetStoreEvaluation(int StoreId)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息（或已删除）");
                }
                #endregion

                //获取该门店的所有评价得分
                var storeCommentList = t_store_commentBLL.GetListByWhere(string.Format("where StoreId={0} and IsDeleted=0", StoreId));
                if (storeCommentList.Any())
                {
                    //获取评价记录中关联的客户信息
                    var customers = t_customerBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0 ",
                                string.Join(",", storeCommentList.GroupBy(o => o.CustomerId).Select(o => o.Key))));

                    var leftJoin = from s in storeCommentList
                                   join b in customers on s.CustomerId equals b.ID into temp
                                   from t in temp.DefaultIfEmpty()
                                   select new
                                   {
                                       s.CustomerId,
                                       s.Score,
                                       s.CommentTime,
                                       Nickname = (t == null ? "" : (t.CustomerType != 1 ? t.Nickname : t.TrueName)),
                                       AvatarUrl = (t == null ? "" : t.AvatarUrl),
                                   };

                    return ApiResult.Success(new
                    {

                        store.StoreImgUrl,
                        store.StoreName,
                        store.Score,
                        Detail = leftJoin
                    });
                }
                else
                {
                    return ApiResult.Success(new
                    {

                        store.StoreImgUrl,
                        store.StoreName,
                        store.Score,
                        Detail = storeCommentList
                    });
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取商户个人资料
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">商户Id</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetBusinessInfo")]
        public HttpResponseMessage GetBusinessInfo(int StoreId, int BusinessId)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted == 1)
                {
                    return ApiResult.Error("没有该门店信息(或已删除)");
                }
                var business = t_businessBLL.GetListByWhere(string.Format("where ID={0} and IsDeleted=0", BusinessId)).FirstOrDefault();
                if (business == null)
                {
                    return ApiResult.Error("没有该商户信息(或已删除)");
                }
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", StoreId, business.ID)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店没有该服务人员信息");
                }
                if (StoreBusiness.State == 1)
                {
                    return ApiResult.Error("该商户已被禁用");
                }
                #endregion

                //获取该门店下的所有服务人员
                var StoreBusinessList = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0}", StoreId));

                //获取当前上级领导角色
                int RoleId = StoreBusiness.RoleId - 1;
                //获取当前上级领导Id集合（没有被禁用的）
                var SuperiorLeaderBusiness = StoreBusinessList.Where(o => o.RoleId == RoleId && o.State == 0);
                var SuperiorBusiness = new Model.t_business();
                if (SuperiorLeaderBusiness.Any())
                {
                    SuperiorBusiness = t_businessBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0",
                   string.Join(",", SuperiorLeaderBusiness.Select(o => o.BusinessId)))).FirstOrDefault();
                }

                //返回结果
                return ApiResult.Success(new
                {
                    business.ID,
                    business.AvatarUrl,
                    Nickname = string.IsNullOrEmpty(business.TrueName) ? business.Nickname : business.TrueName,
                    business.PhoneNumber,
                    business.NativePlace,
                    business.Birthday,
                    business.Height,
                    store.StoreName,
                    StoreBusiness.RoleId,
                    SuperiorLeader = (SuperiorBusiness != null ? (!string.IsNullOrEmpty(SuperiorBusiness.TrueName) ? SuperiorBusiness.TrueName : SuperiorBusiness.Nickname) : "")
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取指定商户钱包信息
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="OpenId">商户微信唯一标识OpenId</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetBusinessWallet")]
        public HttpResponseMessage GetBusinessWallet(int StoreId, string OpenId)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (string.IsNullOrEmpty(OpenId))
                {
                    return ApiResult.Error("参数OpenId不能为空");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted == 1)
                {
                    return ApiResult.Error("没有该门店信息(或已删除)");
                }
                var business = t_businessBLL.GetListByWhere(string.Format("where  OpenId='{0}' and IsDeleted=0", OpenId)).FirstOrDefault();
                if (business == null)
                {
                    return ApiResult.Error("没有该商户信息(或已删除)");
                }
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", StoreId, business.ID)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店没有该服务人员信息");
                }
                if (StoreBusiness.State == 1)
                {
                    return ApiResult.Error("该商户已被禁用");
                }
                #endregion

                //2.获取商户钱包信息
                var wallet = t_walletBLL.GetListByWhere(string.Format("where OpenId='{0}' and StoreId={1} and IsDeleted=0", OpenId, StoreId)).FirstOrDefault();
                if (wallet == null)
                {
                    return ApiResult.Error("没有该商户钱包信息");
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
        /// 获取指定商户提现记录
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="OpenId">商户微信唯一标识OpenId</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetBusinessWithdrawRecord")]
        public HttpResponseMessage GetBusinessWithdrawRecord(int StoreId, string OpenId)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (string.IsNullOrEmpty(OpenId))
                {
                    return ApiResult.Error("参数OpenId不能为空");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted == 1)
                {
                    return ApiResult.Error("没有该门店信息(或已删除)");
                }
                var business = t_businessBLL.GetListByWhere(string.Format("where  OpenId='{0}' and IsDeleted=0", OpenId)).FirstOrDefault();
                if (business == null)
                {
                    return ApiResult.Error("没有该商户信息(或已删除)");
                }
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", StoreId, business.ID)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店没有该服务人员信息");
                }
                if (StoreBusiness.State == 1)
                {
                    return ApiResult.Error("该商户已被禁用");
                }
                #endregion


                //2.获取商户钱包信息
                var withdraws = t_withdrawBLL.GetListByWhere(string.Format("where OpenId='{0}' and StoreId={1} and IsDeleted=0", OpenId, StoreId));
                //if (!withdraws.Any())
                //{
                //    return ApiResult.Error("没有该商户提现信息");
                //}

                //3.返回结果
                return ApiResult.Success(withdraws.OrderByDescending(o => o.WithdrawTime).Select(o => new
                {
                    WithdrawTime = o.WithdrawTime.ToString("yyyy-MM-dd HH:mm:ss"),
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
        /// 商户提现
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/BusinessWithdraw")]
        public HttpResponseMessage BusinessWithdraw(BusinessWithdrawRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (string.IsNullOrEmpty(RequestDto.OpenId))
                {
                    return ApiResult.Error("参数OpenId不能为空");
                }
                if (RequestDto.WithdrawMoney < 1 || RequestDto.WithdrawMoney > 2000)
                {
                    return ApiResult.Error("单笔提现最少¥1,最多¥2000");
                }
                var store = t_storeBLL.GetModel(RequestDto.StoreId);
                if (store == null || store.IsDeleted == 1)
                {
                    return ApiResult.Error("没有该门店信息(或已删除)");
                }
                //获取商户信息
                var business = t_businessBLL.GetListByWhere(string.Format("where OpenId='{0}' and IsDeleted=0", RequestDto.OpenId)).FirstOrDefault();
                if (business == null)
                {
                    return ApiResult.Error("没有该商户信息(或被禁用)");
                }
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", RequestDto.StoreId, business.ID)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店没有该服务人员信息");
                }
                if (StoreBusiness.State == 1)
                {
                    return ApiResult.Error("该商户已被禁用");
                }
                #endregion

                //2.获取商户钱包信息
                var wallet = t_walletBLL.GetListByWhere(string.Format("where OpenId='{0}' and StoreId={1} and IsDeleted=0", RequestDto.OpenId, RequestDto.StoreId)).FirstOrDefault();
                if (wallet == null)
                {
                    return ApiResult.Error("没有该商户钱包信息");
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
                        StoreId = RequestDto.StoreId,
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
        /// 商户发送消息 
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/SendMessageByBusiness")]
        public HttpResponseMessage SendMessageByBusiness(SendMessageByBusinessRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (RequestDto.SendUserId <= 0)
                {
                    return ApiResult.Error("参数SendUserId错误");
                }
                if (RequestDto.AcceptUserId <= 0)
                {
                    return ApiResult.Error("参数AcceptUserId错误");
                }
                if (RequestDto.MessageType < 0 || RequestDto.MessageType > 1)
                {
                    return ApiResult.Error("参数MessageType错误");
                }
                if (string.IsNullOrEmpty(RequestDto.Content))
                {
                    return ApiResult.Error("消息内容不能为空");
                }
                var store = t_storeBLL.GetModel(RequestDto.StoreId);
                if (store == null || store.IsDeleted > 0 || store.State != 1)
                {
                    return ApiResult.Error("没有该门店信息(或被禁用)");
                }
                var customer = t_customerBLL.GetModel(RequestDto.AcceptUserId);
                if (customer == null || customer.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该客户信息(或已删除)");
                }
                var business = t_businessBLL.GetModel(RequestDto.SendUserId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息(或已删除)");
                }
                //获取门店服务人员信息
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", RequestDto.StoreId, RequestDto.SendUserId)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店没有该服务人员信息");
                }
                if (StoreBusiness.State == 1)
                {
                    return ApiResult.Error("该商户已被禁用");
                }
                #endregion

                var InsertMessage = t_messageBLL.Insert(new Model.t_message
                {
                    StoreId = RequestDto.StoreId,
                    SendOpenId = business.OpenId,
                    SendUserId = RequestDto.SendUserId,
                    AcceptOpenId = customer.OpenId,
                    AcceptUserId = RequestDto.AcceptUserId,
                    Content = RequestDto.Content,
                    MessageType = RequestDto.MessageType,
                    SendTime = DateTime.Now,
                    State = 0,
                    IsDeleted = 0,
                    CreationTime = DateTime.Now
                });
                if (InsertMessage < 0)
                {
                    return ApiResult.Error("写入消息信息失败");
                }

                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取指定门店商户消息列表
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">商户Id</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetMessageListByBusiness")]
        public HttpResponseMessage GetMessageListByBusiness(int StoreId, int BusinessId)
        {
            try
            {
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                var business = t_businessBLL.GetModel(BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息(或已删除)");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0 || store.State != 1)
                {
                    return ApiResult.Error("没有该门店信息(或被禁用)");
                }

                //获取指定门店，与商户相关的消息
                var MessageList = t_messageBLL.GetListByWhere(string.Format("where  (SendOpenId='{0}' or AcceptOpenId='{0}') and IsDeleted=0 and StoreId={1}", business.OpenId, StoreId));
                List<MessageItem> result = new List<MessageItem>();
                if (MessageList.Any())
                {
                    //获取商户发送消息 门店客户列表
                    var SendCustomerList = MessageList.Where(o => o.SendOpenId == business.OpenId).GroupBy(o => new { o.StoreId, o.AcceptUserId, o.AcceptOpenId }).
                        Select(o => new MessageItem
                        {
                            UserOpenId = o.Key.AcceptOpenId,
                            UserId = o.Key.AcceptUserId,
                            StoreId = o.Key.StoreId
                        });

                    //获取商户接受消息 门店客户列表
                    var AcceptCustomerList = MessageList.Where(o => o.AcceptOpenId == business.OpenId).GroupBy(o => new { o.StoreId, o.SendUserId, o.SendOpenId }).
                        Select(o => new MessageItem
                        {
                            UserOpenId = o.Key.SendOpenId,
                            UserId = o.Key.SendUserId,
                            StoreId = o.Key.StoreId
                        });

                    var UnionCustomerList = SendCustomerList.Union(AcceptCustomerList, new MessageItemEquality());  //消息人员列表
                    foreach (var item in UnionCustomerList)
                    {
                        if (item.UserId != 1)  //系统平台发送的消息
                        {
                            var StoreBusiness = t_store_customer_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1} and CustomerId={2}", item.StoreId, BusinessId, item.UserId)).FirstOrDefault();
                            if (StoreBusiness == null)  //已删除的客户，不出现消息列表中
                            {
                                continue;
                            }
                        }
                        var MessageItemList = MessageList.Where(o => (o.SendOpenId == item.UserOpenId || o.AcceptOpenId == item.UserOpenId)
                                                                && o.StoreId == item.StoreId).OrderByDescending(o => o.SendTime);
                        var MessageItemListFirst = MessageItemList.FirstOrDefault();
                        if (MessageItemListFirst != null)
                        {
                            result.Add(new MessageItem
                            {
                                StoreId = item.StoreId,
                                UserOpenId = item.UserOpenId,
                                UserId = item.UserId,
                                Content = MessageItemListFirst.Content,
                                SendTime = MessageItemListFirst.SendTime,
                                NoReadCount = MessageItemList.Where(o => o.AcceptUserId == BusinessId && o.State == 0).Count()
                            });
                        }
                    }

                    if (result.Any())
                    {
                        //获取客户人员信息
                        var customerList = t_customerBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0 ", string.Join(",", result.Select(o => o.UserId))));
                        var leftJoin = from r in result
                                       join b in customerList on r.UserId equals b.ID into temp
                                       from t in temp.DefaultIfEmpty()
                                       select new MessageItem
                                       {
                                           StoreId = r.StoreId,
                                           UserId = r.UserId,
                                           UserOpenId = r.UserOpenId,
                                           Content = r.Content,
                                           SendTime = r.SendTime,
                                           NoReadCount = r.NoReadCount,
                                           Nickname = (t == null ? "" : (t.CustomerType != 1 ? t.Nickname : t.TrueName)),
                                           AvatarUrl = (t == null ? "" : t.AvatarUrl),
                                       };
                        return ApiResult.Success(leftJoin.OrderByDescending(o => o.SendTime));
                    }
                    else
                    {
                        return ApiResult.Success(result);
                    }
                }
                else
                {
                    return ApiResult.Success(result);
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        ///  获取指定客户消息列表
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="CustomerId">客户Id</param>
        /// <param name="BusinessId">服务员Id</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetMessageListInCustomer")]
        public HttpResponseMessage GetMessageListInCustomer(int StoreId, int CustomerId, int BusinessId)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (CustomerId <= 0)
                {
                    return ApiResult.Error("参数CustomerId错误");
                }
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                var customer = t_customerBLL.GetModel(CustomerId);
                if (customer == null || customer.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该客户信息");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0 || store.State != 1)
                {
                    return ApiResult.Error("没有该门店信息(或被禁用)");
                }
                var business = t_businessBLL.GetModel(BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息(或被禁用)");
                }
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", StoreId, business.ID)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店没有该服务人员信息");
                }
                if (StoreBusiness.State == 1)
                {
                    return ApiResult.Error("该商户已被禁用");
                }
                #endregion

                var MessageList = t_messageBLL.GetListByWhere(string.Format("where ((SendOpenId='{0}' and AcceptOpenId='{1}') or (SendOpenId='{1}' and AcceptOpenId='{0}')) and IsDeleted=0 and StoreId={2}", customer.OpenId, business.OpenId, StoreId));

                if (MessageList.Any())
                {
                    #region 更新消息为已读
                    var NoRead = MessageList.Where(o => o.State == 0 && o.AcceptUserId == BusinessId);
                    foreach (var item in NoRead)
                    {
                        item.State = 1;
                        item.LastModificationTime = DateTime.Now;
                        t_messageBLL.Update(item);
                    }
                    #endregion

                    List<MessageItem> Users = new List<MessageItem>();
                    Users.Add(new MessageItem
                    {
                        UserOpenId = customer.OpenId,
                        UserId = customer.ID,
                        AvatarUrl = customer.AvatarUrl,
                        Nickname = (customer.CustomerType != 1 ? customer.Nickname : customer.TrueName)
                    });
                    Users.Add(new MessageItem
                    {
                        UserOpenId = business.OpenId,
                        UserId = business.ID,
                        AvatarUrl = business.AvatarUrl,
                        Nickname = string.IsNullOrEmpty(business.Nickname) ? business.TrueName : business.Nickname
                    });

                    var leftJoin = from m in MessageList
                                   join s in Users on m.SendOpenId equals s.UserOpenId into temp
                                   from t in temp.DefaultIfEmpty()
                                   join a in Users on m.AcceptOpenId equals a.UserOpenId into temp01
                                   from v in temp01.DefaultIfEmpty()
                                   select new
                                   {
                                       m.SendUserId,
                                       m.SendOpenId,
                                       SendNickname = (t == null ? "" : t.Nickname),
                                       SendAvatarUrl = (t == null ? "" : t.AvatarUrl),
                                       m.AcceptUserId,
                                       m.AcceptOpenId,
                                       AcceptNickname = (v == null ? "" : v.Nickname),
                                       AcceptAvatarUrl = (v == null ? "" : v.AvatarUrl),
                                       m.Content,
                                       m.SendTime
                                   };

                    return ApiResult.Success(leftJoin.OrderBy(o => o.SendTime));
                }
                else
                {
                    return ApiResult.Success(MessageList);
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 创建商户微信小程序二维码（B接口）
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/CreateBusinessWxCode")]
        public HttpResponseMessage CreateBusinessWxCode(BusinessCreateWxCodeRequestDto RequestDto)
        {
            try
            {
                if (string.IsNullOrEmpty(RequestDto.scene))
                {
                    return ApiResult.Error("参数scene错误");
                }
                string AgentAppID = ConfigurationManager.AppSettings["BusinessAppID"].ToString();
                string AgentAppSecret = ConfigurationManager.AppSettings["BusinessAppSecret"].ToString();
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

        /// <summary>
        /// 图片上传 用form-data 提交
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("Business/UploadBusinessImg")]
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
        /// 修改商户个人信息
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/UpdateBusinessPersonalInfo")]
        public HttpResponseMessage UpdateAgentInfo(UpdateBusinessPersonalInfoRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (string.IsNullOrEmpty(RequestDto.OpenId))
                {
                    return ApiResult.Error("OpenId不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.BusinessName))
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
                var business = t_businessBLL.GetListByWhere(string.Format("where OpenId='{0}'", RequestDto.OpenId)).FirstOrDefault();
                if (business == null)
                {
                    return ApiResult.Error("该商户信息不存在");
                }

                var businessInDB = t_businessBLL.GetListByWhere(string.Format("where PhoneNumber='{0}' and OpenId !='{1}'", RequestDto.PhoneNumber, RequestDto.OpenId)).FirstOrDefault();
                if (businessInDB != null)
                {
                    return ApiResult.Error("该手机号码已经被注册了");
                }
                #endregion

                //更新商户个人信息
                business.TrueName = RequestDto.BusinessName;
                business.PhoneNumber = RequestDto.PhoneNumber;
                business.AvatarUrl = RequestDto.AvatarUrl;
                business.NativePlace = RequestDto.NativePlace;
                business.Height = RequestDto.Height;
                business.Birthday = RequestDto.Birthday;

                var UpdateBusiness = t_businessBLL.Update(business);
                if (UpdateBusiness < 0)
                {
                    return ApiResult.Error("更新商户个人信息失败");
                }

                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 员工签到
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Business/SignByDay")]
        public HttpResponseMessage SignByDay(SignByDayRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (RequestDto.BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                if (!(new List<int> { 1, 2, 3 }).Contains(RequestDto.RoleId))
                {
                    return ApiResult.Error("参数RoleId错误");
                }
                #endregion

                //获取商户信息
                var business = t_businessBLL.GetListByWhere(string.Format("where ID={0} and IsDeleted=0", RequestDto.BusinessId)).FirstOrDefault();
                if (business == null)
                {
                    return ApiResult.Error("没有该商户信息（或已删除）");
                }

                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", RequestDto.StoreId, RequestDto.BusinessId)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店没有该商户信息");
                }

                var signList = t_signBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1} and RoleId={2} and IsDeleted=0", RequestDto.StoreId, RequestDto.BusinessId, RequestDto.RoleId));
                if (signList.Count() > 0)
                {
                    DateTime StartTimeStr = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd"));
                    DateTime EndTimeStr = Convert.ToDateTime(DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"));
                    var sign = signList.Where(o => o.SignTime >= StartTimeStr && o.SignTime < EndTimeStr);
                    if (sign.Count() > 0)
                    {
                        return ApiResult.Error("该门店该商户当天已签到");
                    }
                }

                var InsertSign = t_signBLL.Insert(new Model.t_sign
                {
                    StoreId = RequestDto.StoreId,
                    BusinessId = RequestDto.BusinessId,
                    RoleId = RequestDto.RoleId,
                    AvatarUrl = business.AvatarUrl,
                    Nickname = string.IsNullOrEmpty(business.Nickname) ? business.TrueName : business.Nickname,
                    PhoneNumber = business.PhoneNumber,
                    SignTime = DateTime.Now,

                    IsDeleted = 0,
                    CreationTime = DateTime.Now
                });
                if (InsertSign < 0)
                {
                    return ApiResult.Error("写入签到信息失败");
                }

                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 是否已签到
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">员工Id</param>
        /// <returns>true：员工已签到 false：员工没有签到</returns>
        [HttpGet, Route("Business/IsSign")]
        public HttpResponseMessage IsSign(int StoreId, int BusinessId)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息");
                }
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                #endregion

                //获取商户信息
                var business = t_businessBLL.GetListByWhere(string.Format("where ID={0} and IsDeleted=0", BusinessId)).FirstOrDefault();
                if (business == null)
                {
                    return ApiResult.Error("没有该商户信息（或已删除）");
                }

                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", StoreId, BusinessId)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店没有该商户信息");
                }

                var signList = t_signBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1} and IsDeleted=0", StoreId, BusinessId));
                if (signList.Count() > 0)
                {
                    DateTime StartTimeStr = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd"));
                    DateTime EndTimeStr = Convert.ToDateTime(DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"));
                    var sign = signList.Where(o => o.SignTime >= StartTimeStr && o.SignTime < EndTimeStr);
                    if (sign.Count() > 0)
                    {
                        return ApiResult.Success(true);
                    }
                }
                return ApiResult.Success(false);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 签到统计（超级管理员）
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="dateTimeStr">查询时间(yyyy-MM-dd)</param>
        /// <returns></returns>
        [HttpGet, Route("Business/GetSignStatistical")]
        public HttpResponseMessage GetSignStatistical(int StoreId, string dateTimeStr)
        {
            try
            {
                #region 参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息");
                }
                if (string.IsNullOrEmpty(dateTimeStr))
                {
                    return ApiResult.Error("查询时间不能为空");
                }
                #endregion

                //获取门店服务人员信息
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and RoleId !=0", StoreId));
                if (StoreBusiness.Count() == 0)
                {
                    return ApiResult.Error("该门店没有服务人员信息");
                }
                var dt = Convert.ToDateTime(dateTimeStr);
                DateTime StartTimeStr = Convert.ToDateTime(dt.ToString("yyyy-MM-dd"));
                DateTime EndTimeStr = Convert.ToDateTime(dt.AddDays(1).ToString("yyyy-MM-dd"));

                var signList = t_signBLL.GetListByWhere(string.Format("where StoreId={0} and IsDeleted=0", StoreId));
                if (signList.Count() > 0)
                {
                    var sign = signList.Where(o => o.SignTime >= StartTimeStr && o.SignTime < EndTimeStr);
                    //获取该门店没有签名的员工
                    var noSignList = new List<Model.t_store_business>();
                    if (sign.Any())
                    {
                        noSignList = StoreBusiness.Where(o => sign.Any(m => m.BusinessId != o.BusinessId)).ToList();
                    }
                    else
                    {
                        noSignList = StoreBusiness.ToList();
                    }

                    if (noSignList.Any())
                    {
                        var businesss = t_businessBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0 ",
                            string.Join(",", noSignList.Select(o => o.BusinessId))));
                        var leftJoin = from s in noSignList
                                       join b in businesss on s.BusinessId equals b.ID into temp
                                       from t in temp.DefaultIfEmpty()
                                       select new
                                       {
                                           Nickname = (t == null ? "" : (string.IsNullOrEmpty(t.Nickname) ? t.TrueName : t.Nickname)),
                                           AvatarUrl = (t == null ? "" : t.AvatarUrl),
                                           s.RoleId,
                                           PhoneNumber = (t == null ? "" : t.PhoneNumber),
                                       };

                        return ApiResult.Success(new
                        {
                            IsSignList = new
                            {
                                Count = sign.Count(),
                                SignItems = sign.Select(o => new
                                {
                                    o.Nickname,
                                    o.AvatarUrl,
                                    o.RoleId,
                                    o.PhoneNumber,
                                    o.SignTime
                                })
                            },
                            NoSignList = new
                            {
                                Count = StoreBusiness.Count() - sign.Count(),
                                NoSignItems = leftJoin

                            }
                        });
                    }
                    else
                    {
                        return ApiResult.Success(new
                        {
                            IsSignList = new
                            {
                                Count = sign.Count(),
                                SignItems = sign.Select(o => new
                                {
                                    o.Nickname,
                                    o.AvatarUrl,
                                    o.RoleId,
                                    o.PhoneNumber,
                                    o.SignTime
                                })
                            },
                            NoSignList = new
                            {
                                Count = StoreBusiness.Count() - sign.Count(),
                                NoSignItems = noSignList
                            }
                        });
                    }
                }
                else
                {
                    var businesss = t_businessBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0 ",
                          string.Join(",", StoreBusiness.Select(o => o.BusinessId))));
                    var leftJoin = from s in StoreBusiness
                                   join b in businesss on s.BusinessId equals b.ID into temp
                                   from t in temp.DefaultIfEmpty()
                                   select new
                                   {
                                       Nickname = (t == null ? "" : (string.IsNullOrEmpty(t.Nickname) ? t.TrueName : t.Nickname)),
                                       AvatarUrl = (t == null ? "" : t.AvatarUrl),
                                       s.RoleId,
                                       PhoneNumber = (t == null ? "" : t.PhoneNumber),
                                   };

                    return ApiResult.Success(new
                    {
                        IsSignList = new
                        {
                            Count = 0,
                            SignItems = signList
                        },
                        NoSignList = new
                        {
                            Count = StoreBusiness.Count(),
                            NoSignItems = leftJoin

                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
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

        /// <summary>
        /// 获取Banner图地址
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("Business/GetBannerImg")]
        public HttpResponseMessage GetBannerImg()
        {
            try
            {
                //获取商户信息
                var bannerimage = t_bannerimageBLL.GetListByWhere(string.Format("where IsDeleted=0"));

                var tempBannerImageList = bannerimage.Where(o => o.UpOnLineTime <= DateTime.Now && o.DownOnLimeTime > DateTime.Now);

                return ApiResult.Success(tempBannerImageList.Select(o => new
                {
                    o.BannerTitle,
                    o.ClickTrunOnUrl,
                    o.ImgUrl,
                    o.ClickStatus
                }));
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        ///// <summary>
        /////  更新消费记录
        ///// </summary>
        ///// <param name="ConsumerId">消费记录Id</param>
        ///// <param name="BoxNumber">包厢号</param>
        ///// <param name="Money">包厢消费金额</param>
        ///// <returns></returns>
        //[HttpPost, Route("Business/UpdateConsumption")]
        //public HttpResponseMessage UpdateConsumption(int ConsumerId, string BoxNumber, decimal Money)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(BoxNumber))
        //        {
        //            return ApiResult.Error("包厢号不能为空");
        //        }
        //        if (Money <= 0)
        //        {
        //            return ApiResult.Error("参数Money错误");
        //        }
        //        if (ConsumerId <= 0)
        //        {
        //            return ApiResult.Error("参数ConsumerId错误");
        //        }
        //        var Consumption = t_consumptionBLL.GetModel(ConsumerId);
        //        if (Consumption == null || Consumption.IsDeleted > 0)
        //        {
        //            return ApiResult.Error("没有该消费记录信息");
        //        }
        //        Consumption.BoxNumber = BoxNumber;
        //        Consumption.Money = Money;
        //        Consumption.LastModificationTime = DateTime.Now;
        //        var UpdateConsumption = t_consumptionBLL.Update(Consumption);
        //        if (UpdateConsumption < 0)
        //        {
        //            return ApiResult.Error("更新消费记录失败");
        //        }
        //        return ApiResult.Success(true);
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResult.Error(ex.Message);
        //    }
        //}
    }
}
