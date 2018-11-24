using LingLong.Bll;
using LingLong.Common;
using LingLong.Common.lib;
using LingLong.Common.WebApi;
using LingLong.WebApi.Models;
using LingLong.WebApi.Models.RequestDto.Customer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Xml;
using System.Xml.Linq;

namespace LingLong.WebApi.Controllers
{
    /// <summary>
    /// 客户相关接口 控制器
    /// 用户对门店评分，门店整体评分是怎么计算出来的？  取平均值 小数点后1位四舍五入
    /// 消息列表中的服务经理聊天信息怎么产生的？如果有多个经理，怎么显示？
    /// 打赏分配： 门店分配是指的谁？当前打赏的服务人员的上级还是门店经理 还是门店管理员？  超级管理员
    /// 一个门店有几个代理？  1个
    /// 打赏分配：对于服务人员  门店 代理商 若都是禁用状态，分配的打赏金额给谁？平台？
    /// 支付成功了才能写 消费记录、自动发送消息？？？
    /// 关联该服务人员的上级时，若上级被禁用了还要关联吗？
    /// </summary>
    public class CustomerController : ApiController
    {
        /// <summary>
        /// 获取 session_key 和 openid 
        /// </summary>
        /// <param name="JsCode">登录时获取的 code</param>
        /// <returns></returns>
        [HttpGet, Route("Customer/GetCustomerOpenId")]
        public HttpResponseMessage GetCustomerOpenId(string JsCode)
        {
            try
            {
                if (string.IsNullOrEmpty(JsCode))
                {
                    return ApiResult.Error("参数JsCode不能为空");
                }

                string CustomerAppID = ConfigurationManager.AppSettings["CustomerAppID"].ToString();
                string CustomerAppSecret = ConfigurationManager.AppSettings["CustomerAppSecret"].ToString();

                string result = WxHelper.GetOpenId(JsCode, CustomerAppID, CustomerAppSecret);

                return ApiResult.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 客户注册（自动）
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Customer/CustomerAutoRegister")]
        public HttpResponseMessage CustomerAutoRegister(CustomerAutoRegisterRequestDto RequestDto)
        {
            try
            {
                if (string.IsNullOrEmpty(RequestDto.OpenId))
                {
                    return ApiResult.Error("参数OpenId错误");
                }
                var customer = t_customerBLL.GetListByWhere(string.Format("where OpenId='{0}'", RequestDto.OpenId)).FirstOrDefault();
                if (customer == null)
                {
                    //注册
                    var customerInsert = t_customerBLL.Insert(new Model.t_customer
                    {
                        CustomerType = 0,  //客户类型（0:自动；1：服务员手动添加）
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

                    if (customerInsert <= 0)
                    {
                        return ApiResult.Error("客户自动注册失败");
                    }
                }

                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取客户信息
        /// </summary>
        /// <param name="OpenId">客户微信唯一标识OpenId</param>
        /// <returns></returns>
        [HttpGet, Route("Customer/GetCustomerInfo")]
        public HttpResponseMessage GetCustomerInfo(string OpenId)
        {
            try
            {
                //1.参数验证
                if (string.IsNullOrEmpty(OpenId))
                {
                    return ApiResult.Error("参数OpenId不能为空");
                }

                //2.获取客户信息
                var customer = t_customerBLL.GetListByWhere(string.Format("where OpenId='{0}' and IsDeleted=0", OpenId)).FirstOrDefault();
                if (customer == null)
                {
                    return ApiResult.Error("没有该客户信(或已删除）");
                }

                //3.返回结果
                return ApiResult.Success(new
                {
                    CustomerId = customer.ID,
                    customer.AvatarUrl,
                    Nickname = string.IsNullOrEmpty(customer.Nickname) ? customer.TrueName : customer.Nickname,
                    customer.PhoneNumber,
                    customer.PassWord
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 修改客户信息
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Customer/UpdateCustomerInfo")]
        public HttpResponseMessage UpdateCustomerInfo(UpdateCustomerInfoRequestDto RequestDto)
        {
            try
            {
                //1.参数验证
                if (string.IsNullOrEmpty(RequestDto.OpenId))
                {
                    return ApiResult.Error("参数OpenId不能为空");
                }
                //2.获取客户信息
                var customer = t_customerBLL.GetListByWhere(string.Format("where OpenId='{0}' and IsDeleted=0", RequestDto.OpenId)).FirstOrDefault();
                if (customer == null)
                {
                    return ApiResult.Error("没有该客户信息(或已删除）");
                }

                if (!string.IsNullOrEmpty(RequestDto.PhoneNumber))
                {
                    if (RequestDto.PhoneNumber.Length != 11)
                    {
                        return ApiResult.Error("手机号码格式错误");
                    }
                    var customerInDB = t_customerBLL.GetListByWhere(string.Format("where PhoneNumber='{0}' and ID != {1} and CustomerType=0", RequestDto.PhoneNumber, customer.ID)).FirstOrDefault();
                    if (customerInDB != null)
                    {
                        return ApiResult.Error("该手机号码已经被注册了");
                    }
                }
                customer.Nickname = RequestDto.Nickname;
                customer.AvatarUrl = RequestDto.AvatarUrl;
                customer.PhoneNumber = RequestDto.PhoneNumber;
                customer.LastModificationTime = DateTime.Now;
                if (t_customerBLL.Update(customer) > 0)
                {
                    return ApiResult.Success(true);
                }
                else
                {
                    return ApiResult.Error("更新客户信息失败");
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 修改客户密码
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Customer/UpdateCustomerPassWord")]
        public HttpResponseMessage UpdateCustomerPassWord(UpdateCustomerPassWordRequestDto RequestDto)
        {
            try
            {
                //1.参数验证
                if (string.IsNullOrEmpty(RequestDto.OpenId))
                {
                    return ApiResult.Error("参数OpenId不能为空");
                }
                if (string.IsNullOrEmpty(RequestDto.PassWord))
                {
                    return ApiResult.Error("客户密码不能为空");
                }

                //2.获取客户信息
                var customer = t_customerBLL.GetListByWhere(string.Format("where OpenId='{0}' and IsDeleted=0", RequestDto.OpenId)).FirstOrDefault();
                if (customer == null)
                {
                    return ApiResult.Error("没有该客户信息(或已删除）");
                }
                customer.PassWord = RequestDto.PassWord;
                customer.LastModificationTime = DateTime.Now;
                if (t_customerBLL.Update(customer) > 0)
                {
                    return ApiResult.Success(true);
                }
                else
                {
                    return ApiResult.Error("更新客户信息失败");
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取商户信息（客户服务评价页面加载调用）
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">商户Id</param>
        /// <returns></returns>
        [HttpGet, Route("Customer/GetBusinessInfo")]
        public HttpResponseMessage GetBusinessInfo(int StoreId, int BusinessId)
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
                var store = t_storeBLL.GetModel(StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息(或已删除)");
                }

                //2.获取商户信息
                var business = t_businessBLL.GetListByWhere(string.Format("where ID={0} and IsDeleted=0 ", BusinessId)).FirstOrDefault();
                if (business == null)
                {
                    return ApiResult.Error("没有该商户信息(或已删除)");
                }

                //获取门店服务人员信息
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", StoreId, business.ID)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店没有该服务人员信息");
                }
                if (StoreBusiness.State == 1)
                {
                    return ApiResult.Error("该商户已被禁用");
                }

                //3.返回结果
                return ApiResult.Success(new
                {
                    business.ID,
                    business.AvatarUrl,
                    Nickname = string.IsNullOrEmpty(business.Nickname) ? business.TrueName : business.Nickname
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 写入关联门店、服务员信息（服务评价页面加载、门店服务人发送门店推荐页面加载时调用）
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Customer/JoinStoreAndBusiness")]
        public HttpResponseMessage JoinStoreAndBusiness(JoinStoreAndBusinessRequestDto RequestDto)
        {
            try
            {
                //1.参数验证
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (RequestDto.CustomerId <= 0)
                {
                    return ApiResult.Error("参数CustomerId错误");
                }
                if (!(new List<int> { 1, 2, 3 }).Contains(RequestDto.JoinType))
                {
                    return ApiResult.Error("参数JoinType错误");
                }
                var customer = t_customerBLL.GetModel(RequestDto.CustomerId);
                if (customer == null || customer.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该客户信息(或已删除）");
                }
                var store = t_storeBLL.GetModel(RequestDto.StoreId);
                if (store == null || store.IsDeleted > 0 || store.State != 1)
                {
                    return ApiResult.Error("没有该门店信息(或被禁用)");
                }
                if (RequestDto.JoinType == 1 || RequestDto.JoinType == 2)
                {
                    if (RequestDto.BusinessId <= 0)
                    {
                        return ApiResult.Error("参数BusinessId错误");
                    }
                    var business = t_businessBLL.GetModel(RequestDto.BusinessId);
                    if (business == null || business.IsDeleted > 0)
                    {
                        return ApiResult.Error("没有该服务员信息(或已删除）");
                    }

                    var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", RequestDto.StoreId, RequestDto.BusinessId)).FirstOrDefault();
                    if (StoreBusiness == null)
                    {
                        return ApiResult.Error("该门店下没有该服务员信息");
                    }
                    if (StoreBusiness.State == 1)
                    {
                        return ApiResult.Error("该商户已被禁用");
                    }
                    var StoreCustomerBusiness = t_store_customer_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1} and CustomerId={2}", RequestDto.StoreId, RequestDto.BusinessId, RequestDto.CustomerId)).FirstOrDefault();
                    if (StoreCustomerBusiness == null)
                    {
                        var InsertStoreCustomerBusiness = t_store_customer_businessBLL.Insert(new Model.t_store_customer_business
                        {
                            StoreId = RequestDto.StoreId,
                            BusinessId = RequestDto.BusinessId,
                            CustomerId = RequestDto.CustomerId
                        });
                        if (InsertStoreCustomerBusiness < 0)
                        {
                            return ApiResult.Error("写入关联门店商户记录失败");
                        }

                        if (RequestDto.JoinType == 1)
                        {
                            //关联该服务人员的上级
                            if (StoreBusiness.ParentId > 0)
                            {
                                var businessParent = t_businessBLL.GetModel(StoreBusiness.ParentId);
                                if (businessParent != null && businessParent.IsDeleted == 0) //|| businessParent.State == 0  ！！！！
                                {
                                    var InsertStoreCustomerBusinessParent = t_store_customer_businessBLL.Insert(new Model.t_store_customer_business
                                    {
                                        StoreId = RequestDto.StoreId,
                                        BusinessId = businessParent.ID,
                                        CustomerId = RequestDto.CustomerId
                                    });
                                    if (InsertStoreCustomerBusinessParent < 0)
                                    {
                                        return ApiResult.Error("写入上级关联门店商户记录失败");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    var StoreCustomer = t_store_customer_businessBLL.GetListByWhere(string.Format("where StoreId={0} and CustomerId={1}", RequestDto.StoreId, RequestDto.CustomerId)).FirstOrDefault();
                    if (StoreCustomer == null)
                    {
                        var InsertStoreCustomerBusiness = t_store_customer_businessBLL.Insert(new Model.t_store_customer_business
                        {
                            StoreId = RequestDto.StoreId,
                            BusinessId = RequestDto.BusinessId,
                            CustomerId = RequestDto.CustomerId
                        });
                        if (InsertStoreCustomerBusiness < 0)
                        {
                            return ApiResult.Error("写入关联门店商户记录失败");
                        }
                    }
                }
                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 服务评价
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Customer/ServiceEvaluation")]
        public HttpResponseMessage ServiceEvaluation(ServiceEvaluationRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (RequestDto.CustomerId <= 0)
                {
                    return ApiResult.Error("参数CustomerId错误");
                }
                if (RequestDto.BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                if (!(new List<int> { 1, 2, 3 }).Contains(RequestDto.BusinessScore))
                {
                    return ApiResult.Error("参数BusinessScore错误");
                }

                var customer = t_customerBLL.GetModel(RequestDto.CustomerId);
                if (customer == null || customer.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该客户信息(或已删除）");
                }
                var store = t_storeBLL.GetModel(RequestDto.StoreId);
                if (store == null || store.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该门店信息(或已删除）");
                }
                var business = t_businessBLL.GetModel(RequestDto.BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息(或已删除）");
                }
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", RequestDto.StoreId, RequestDto.BusinessId)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店下没有该服务员信息");
                }
                if (StoreBusiness.State == 1)
                {
                    return ApiResult.Error("该商户已被禁用");
                }
                if (StoreBusiness.RoleId != 3)
                {
                    return ApiResult.Error("只有服务员才能被评价");
                }
                #endregion

                var InsertServiceEvaluation = t_service_evaluationBLL.Insert(new Model.t_service_evaluation
                {
                    BusinessId = RequestDto.BusinessId,
                    StoreId = RequestDto.StoreId,
                    CustomerId = RequestDto.CustomerId,
                    EvaluateTime = DateTime.Now,
                    Score = RequestDto.BusinessScore,
                    IsDeleted = 0,
                    CreationTime = DateTime.Now
                });
                if (InsertServiceEvaluation < 0)
                {
                    return ApiResult.Error("写入服务评价记录失败");
                }

                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取打赏页面信息
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="BusinessId">服务员Id</param>
        /// <returns></returns>
        [HttpGet, Route("Customer/GetRewardInfo")]
        public HttpResponseMessage GetRewardInfo(int StoreId, int BusinessId)
        {
            try
            {
                //1.参数验证
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", StoreId, BusinessId)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店下没有该服务员信息");
                }
                if (StoreBusiness.State == 1)
                {
                    return ApiResult.Error("该商户已被禁用");
                }
                if (StoreBusiness.RoleId != 3)
                {
                    return ApiResult.Error("只有服务员才能被评价");
                }

                //2.获取商户信息
                var business = t_businessBLL.GetListByWhere(string.Format("where ID={0} and IsDeleted=0", BusinessId)).FirstOrDefault();
                if (business == null)
                {
                    return ApiResult.Error("没有该商户信息(或已删除）");
                }

                //获取打赏配置商品
                var plans = t_reward_planBLL.GetList();
                List<Model.t_reward_goods> goods = new List<Model.t_reward_goods>();
                if (plans.Any())
                {
                    int PlanId = 0;
                    var plantmp = plans.Where(o => o.IsDeleted == 0 && o.IsUse == 1);
                    if (plantmp.Count() > 1)
                    {
                        return ApiResult.Error("打赏方案配置有误");
                    }
                    if (plantmp.Count() == 0)
                    {
                        var plantmp01 = plans.Where(o => o.IsDeleted == 0 && o.IsDefault == 1);
                        if (plantmp01.Count() != 1)
                        {
                            return ApiResult.Error("打赏方案配置有误");
                        }
                        PlanId = plantmp01.FirstOrDefault().ID;
                    }
                    PlanId = plantmp.FirstOrDefault().ID;

                    //获取打赏商品
                    goods = t_reward_goodsBLL.GetListByWhere(string.Format("where PlanId={0} and IsDeleted=0", PlanId)).ToList();
                }

                //3.返回结果
                return ApiResult.Success(new
                {
                    BusinessId = business.ID,
                    business.AvatarUrl,
                    Nickname = string.IsNullOrEmpty(business.TrueName) ? business.Nickname : business.TrueName,
                    Goods = goods.Select(o => new
                    {
                        o.ID,
                        o.GoodsImgUrl,
                        o.GoodsName,
                        o.Money
                    })
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 新增打赏记录（单击打赏支付按钮时调用）
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns>返回支付需要的五个参数</returns>
        [HttpPost, Route("Customer/AddRewardAndPrePay")]
        public HttpResponseMessage AddRewardAndPrePay(AddRewardRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (string.IsNullOrEmpty(RequestDto.OpenId))
                {
                    return ApiResult.Error("OpenId不能为空");
                }
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (RequestDto.CustomerId <= 0)
                {
                    return ApiResult.Error("参数CustomerId错误");
                }
                if (RequestDto.BusinessId <= 0)
                {
                    return ApiResult.Error("参数BusinessId错误");
                }
                if (RequestDto.GoodsId <= 0)
                {
                    return ApiResult.Error("参数GoodsId错误");
                }
                if (RequestDto.Money <= 0)
                {
                    return ApiResult.Error("参数Money错误");
                }

                var customer = t_customerBLL.GetModel(RequestDto.CustomerId);
                if (customer == null || customer.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该客户信息(或已删除）");
                }
                var store = t_storeBLL.GetModel(RequestDto.StoreId);
                if (store == null || store.IsDeleted > 0 || store.State != 1)
                {
                    return ApiResult.Error("没有该门店信息(或被禁用)");
                }
                var business = t_businessBLL.GetModel(RequestDto.BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息(或被禁用)");
                }
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", RequestDto.StoreId, RequestDto.BusinessId)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店下没有该服务员信息");
                }
                if (StoreBusiness.State == 1)
                {
                    return ApiResult.Error("该商户已被禁用");
                }
                if (StoreBusiness.RoleId != 3)
                {
                    return ApiResult.Error("只有服务员才能被打赏");
                }

                var goods = t_reward_goodsBLL.GetModel(RequestDto.GoodsId);
                if (goods == null || goods.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该打赏商品信息");
                }
                #endregion

                var model = new Model.t_reward
                {
                    BusinessId = RequestDto.BusinessId,
                    StoreId = RequestDto.StoreId,
                    CustomerId = RequestDto.CustomerId,
                    RewardTime = DateTime.Now,
                    Money = RequestDto.Money,
                    GoodsId = RequestDto.GoodsId,
                    IsDeleted = 0,
                    CreationTime = DateTime.Now
                };

                return WxHelper.Pay(RequestDto.OpenId, (int)Math.Round(RequestDto.Money * 100, 0), model);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }



        /// <summary>
        /// 获取客户门店列表（没有被禁用的门店）
        /// </summary>
        /// <param name="CustomerId">客户Id</param>
        /// <returns></returns>
        [HttpGet, Route("Customer/GetCustomerStoreList")]
        public HttpResponseMessage GetCustomerStoreList(int CustomerId)
        {
            try
            {
                if (CustomerId <= 0)
                {
                    return ApiResult.Error("参数CustomerId错误");
                }

                var customer = t_customerBLL.GetModel(CustomerId);
                if (customer == null || customer.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该客户信息(或已删除)");
                }

                var isDefaultStore = int.Parse(System.Web.Configuration.WebConfigurationManager.AppSettings["IsDefaultStore"]);
                if (isDefaultStore == 0)
                {
                    var CustomeStoreBusiness = t_store_customer_businessBLL.GetListByWhere(string.Format("where CustomerId={0}", CustomerId));
                    if (CustomeStoreBusiness.Any())
                    {
                        var CustomeStores = CustomeStoreBusiness.GroupBy(o => o.StoreId).Select(o => o.Key);
                        var result = t_storeBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0 and State=1",
                            string.Join(",", CustomeStores)));  //获取没有被禁用的门店
                        return ApiResult.Success(result.Select(o => new
                        {
                            o.ID,
                            o.StoreName,
                            o.Score,
                            o.StoreImgUrl,
                            o.Address,
                            o.PhoneNumber
                        }));
                    }
                    else
                    {
                        return ApiResult.Success(new List<Model.t_store>());
                    }
                }
                else
                {
                    //审核阶段
                    IList<Model.t_store> result = new List<Model.t_store>();
                    //默认展示
                    var defaultStoreId = isDefaultStore;
                    var defaultStore = t_storeBLL.GetModel(defaultStoreId);

                    var CustomeStoreBusiness = t_store_customer_businessBLL.GetListByWhere(string.Format("where CustomerId={0}", CustomerId));
                    if (CustomeStoreBusiness.Any())
                    {
                        var CustomeStores = CustomeStoreBusiness.GroupBy(o => o.StoreId).Select(o => o.Key);
                        result = t_storeBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0 and State=1",
                            string.Join(",", CustomeStores))).ToList();  //获取没有被禁用的门店
                        if (result.Any(o => o.ID != defaultStoreId))
                        {
                            result.Add(defaultStore);
                        }

                        return ApiResult.Success(result.Select(o => new
                        {
                            o.ID,
                            o.StoreName,
                            o.Score,
                            o.StoreImgUrl,
                            o.Address,
                            o.PhoneNumber
                        }));
                    }
                    else
                    {
                        //默认展示
                        result.Add(defaultStore);
                        return ApiResult.Success(result.Select(o => new
                        {
                            o.ID,
                            o.StoreName,
                            o.Score,
                            o.StoreImgUrl,
                            o.Address,
                            o.PhoneNumber
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取客户门店详情（没有被禁用的服务人员）
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="CustomerId">客户Id</param>
        /// <returns></returns>
        [HttpGet, Route("Customer/GetCustomerStoreDetail")]
        public HttpResponseMessage GetCustomerStoreDetail(int CustomerId, int StoreId)
        {
            try
            {
                if (CustomerId <= 0)
                {
                    return ApiResult.Error("参数CustomerId错误");
                }
                if (StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
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
                List<Model.t_business> businessList = new List<Model.t_business>();
                var CustomeStoreBusiness = t_store_customer_businessBLL.GetListByWhere(string.Format("where CustomerId={0} and StoreId={1}", CustomerId, StoreId));
                if (CustomeStoreBusiness.Any())
                {
                    //获取门店没有被禁用的员工列表
                    var StoreBusinessList = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and State=0", StoreId)).Select(o => o.BusinessId);
                    var BusinessIds = CustomeStoreBusiness.Where(o => StoreBusinessList.Any(p => p == o.BusinessId));
                    if (BusinessIds.Any())
                    {
                        businessList = t_businessBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0 ", string.Join(",", BusinessIds.Select(o => o.BusinessId)))).ToList();  //获取没有被禁用的服务人员   
                    }
                }

                return ApiResult.Success(new
                {
                    store.StoreName,
                    store.StoreImgUrl,
                    store.Address,
                    store.PhoneNumber,
                    store.Score,
                    BusinessList = businessList.OrderBy(o => o.Nickname).Select(o => new
                    {
                        o.ID,
                        Nickname = string.IsNullOrEmpty(o.Nickname) ? o.TrueName : o.Nickname,
                        o.AvatarUrl,
                        o.PhoneNumber
                    })
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 发送消息（单击残忍离开按钮时、聊天窗口发送按钮调用）
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Customer/SendMessageByCustomer")]
        public HttpResponseMessage SendMessageByCustomer(SendMessageByCustomerRequestDto RequestDto)
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
                var customer = t_customerBLL.GetModel(RequestDto.SendUserId);
                if (customer == null || customer.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该客户信息(或已删除)");
                }
                var business = t_businessBLL.GetModel(RequestDto.AcceptUserId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息(或已删除)");
                }
                //获取门店服务人员信息
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", RequestDto.StoreId, RequestDto.AcceptUserId)).FirstOrDefault();
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
                    SendOpenId = customer.OpenId,
                    SendUserId = RequestDto.SendUserId,
                    AcceptOpenId = business.OpenId,
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
        /// 获取客户消息列表
        /// </summary>
        /// <param name="CustomerId">客户Id</param>
        /// <returns></returns>
        [HttpGet, Route("Customer/GetMessageListByCustomer")]
        public HttpResponseMessage GetMessageListByCustomer(int CustomerId)
        {
            try
            {
                if (CustomerId <= 0)
                {
                    return ApiResult.Error("参数CustomerId错误");
                }
                var customer = t_customerBLL.GetModel(CustomerId);
                if (customer == null || customer.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该客户信息(或已删除)");
                }

                //获取与客户相关的消息
                var MessageList = t_messageBLL.GetListByWhere(string.Format("where (SendOpenId='{0}' or AcceptOpenId='{0}') and IsDeleted=0", customer.OpenId));
                List<MessageItem> result = new List<MessageItem>();
                if (MessageList.Any())
                {
                    //获取客户发送消息 门店服务员列表
                    var SendBusinessList = MessageList.Where(o => o.SendOpenId == customer.OpenId).GroupBy(o => new { o.StoreId, o.AcceptUserId, o.AcceptOpenId }).
                        Select(o => new MessageItem
                        {
                            UserOpenId = o.Key.AcceptOpenId,
                            UserId = o.Key.AcceptUserId,
                            StoreId = o.Key.StoreId
                        });

                    //获取客户接受消息 门店服务员列表
                    var AcceptBusinessList = MessageList.Where(o => o.AcceptOpenId == customer.OpenId).GroupBy(o => new { o.StoreId, o.SendUserId, o.SendOpenId }).
                        Select(o => new MessageItem
                        {
                            UserOpenId = o.Key.SendOpenId,
                            UserId = o.Key.SendUserId,
                            StoreId = o.Key.StoreId
                        });

                    var UnionBusinessList = SendBusinessList.Union(AcceptBusinessList, new MessageItemEquality());  //消息人员列表

                    foreach (var item in UnionBusinessList)
                    {
                        var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", item.StoreId, item.UserId)).FirstOrDefault();
                        if (StoreBusiness == null || StoreBusiness.State == 1)  //员工被禁用了，不出现消息列表中
                        {
                            continue;
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
                                NoReadCount = MessageItemList.Where(o => o.AcceptUserId == CustomerId && o.State == 0).Count()
                            });
                        }
                    }

                    if (result.Any())
                    {
                        //获取服务人员信息
                        var businessList = t_businessBLL.GetListByWhere(string.Format("where ID IN ({0}) and IsDeleted=0 ", string.Join(",", result.Select(o => o.UserId))));
                        var leftJoin = from r in result
                                       join b in businessList on r.UserId equals b.ID into temp
                                       from t in temp.DefaultIfEmpty()
                                       select new MessageItem
                                       {
                                           StoreId = r.StoreId,
                                           UserId = r.UserId,
                                           UserOpenId = r.UserOpenId,
                                           Content = r.Content,
                                           SendTime = r.SendTime,
                                           NoReadCount = r.NoReadCount,
                                           Nickname = (t == null ? "" : string.IsNullOrEmpty(t.Nickname) ? t.TrueName : t.Nickname),
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
        ///  获取指定服务人员消息列表
        /// </summary>
        /// <param name="StoreId">门店Id</param>
        /// <param name="CustomerId">客户Id</param>
        /// <param name="BusinessId">服务员Id</param>
        /// <returns></returns>
        [HttpGet, Route("Customer/GetMessageListInBusiness")]
        public HttpResponseMessage GetMessageListInBusiness(int StoreId, int CustomerId, int BusinessId)
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
                if (business == null || business.IsDeleted > 0)  //|| business.State != 0
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
                    var NoRead = MessageList.Where(o => o.State == 0 && o.AcceptUserId == CustomerId);
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
                        Nickname = string.IsNullOrEmpty(customer.Nickname) ? customer.TrueName : customer.Nickname
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
        ///  门店评价
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Customer/StoreEvaluation")]
        public HttpResponseMessage StoreEvaluation(StoreEvaluationRequestDto RequestDto)
        {
            try
            {
                //1.参数验证
                if (RequestDto.StoreId <= 0)
                {
                    return ApiResult.Error("参数StoreId错误");
                }
                if (RequestDto.CustomerId <= 0)
                {
                    return ApiResult.Error("参数CustomerId错误");
                }
                if (RequestDto.StoreScore < 1 || RequestDto.StoreScore > 5)
                {
                    return ApiResult.Error("参数StoreScore错误");
                }
                var customer = t_customerBLL.GetModel(RequestDto.CustomerId);
                if (customer == null || customer.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该客户信息（或已删除）");
                }
                var store = t_storeBLL.GetModel(RequestDto.StoreId);
                if (store == null || store.IsDeleted > 0 || store.State != 1)
                {
                    return ApiResult.Error("没有该门店信息(或被禁用)");
                }

                var InsertStoreComment = t_store_commentBLL.Insert(new Model.t_store_comment
                {
                    StoreId = RequestDto.StoreId,
                    CustomerId = RequestDto.CustomerId,
                    Score = RequestDto.StoreScore,
                    CommentTime = DateTime.Now,
                    IsDeleted = 0,
                    CreationTime = DateTime.Now
                });
                if (InsertStoreComment < 0)
                {
                    return ApiResult.Error("写入门店评价记录失败");
                }

                //获取该门店的所有评价得分
                var storeCommentList = t_store_commentBLL.GetListByWhere(string.Format("where StoreId={0} and IsDeleted=0", RequestDto.StoreId));
                float ScoreAverage = storeCommentList.Average(o => o.Score);

                store.Score = transmitNum(ScoreAverage);
                store.LastModificationTime = DateTime.Now;
                var UpdateStore = t_storeBLL.Update(store);
                if (UpdateStore < 0)
                {
                    return ApiResult.Error("更新门店平均评分失败");
                }
                return ApiResult.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult.Error(ex.Message);
            }
        }

        /// <summary>
        /// 创建客户微信小程序二维码（B接口）
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Customer/CreateCustomerWxCode")]
        public HttpResponseMessage CreateCustomerWxCode(CustomerCreateWxCodeRequestDto RequestDto)
        {
            try
            {
                if (string.IsNullOrEmpty(RequestDto.scene))
                {
                    return ApiResult.Error("参数scene错误");
                }
                string AgentAppID = ConfigurationManager.AppSettings["CustomerAppID"].ToString();
                string AgentAppSecret = ConfigurationManager.AppSettings["CustomerAppSecret"].ToString();
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
        /// 微信支付之后回调方法
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("Customer/Notify")]
        public HttpResponseMessage Notify()
        {
            string xmlData = GetPostStr();//获取请求数据
            string return_msg = "";
            if (xmlData == "")
            {
                return_msg = "<xml><return_code><![CDATA[FAIL]]></return_code><return_msg><![CDATA[微信返回数据为空]]></return_msg></xml>";
            }
            else
            {
                var dic = new Dictionary<string, string>
                {
                    {"return_code", "SUCCESS"},
                    {"return_msg","OK"}
                };

                var sb = new StringBuilder();
                sb.Append("<xml>");
                foreach (var d in dic)
                {
                    sb.Append("<" + d.Key + ">" + d.Value + "</" + d.Key + ">");
                }
                sb.Append("</xml>");

                //把数据重新返回给客户端
                DataSet ds = new DataSet();
                StringReader stram = new StringReader(xmlData);
                XmlTextReader datareader = new XmlTextReader(stram);
                ds.ReadXml(datareader);
                if (ds.Tables[0].Rows[0]["return_code"].ToString() == "SUCCESS")
                {
                    string wx_appid = "";//微信开放平台审核通过的应用APPID
                    string wx_mch_id = "";//微信支付分配的商户号
                    string wx_nonce_str = "";//     随机字符串，不长于32位
                    string wx_sign = "";//签名，详见签名算法
                    string wx_result_code = "";//SUCCESS/FAIL
                    string wx_return_code = "";
                    string wx_openid = "";//用户在商户appid下的唯一标识
                    string wx_is_subscribe = "";//用户是否关注公众账号，Y-关注，N-未关注，仅在公众账号类型支付有效
                    string wx_trade_type = "";//    APP
                    string wx_bank_type = "";//     银行类型，采用字符串类型的银行标识，银行类型见银行列表
                    string wx_fee_type = "";//  货币类型，符合ISO4217标准的三位字母代码，默认人民币：CNY，其他值列表详见货币类型
                    string wx_transaction_id = "";//微信支付订单号
                    string wx_out_trade_no = "";//商户系统的订单号，与请求一致。
                    string wx_time_end = "";//  支付完成时间，格式为yyyyMMddHHmmss，如2009年12月25日9点10分10秒表示为20091225091010。其他详见时间规则
                    int wx_total_fee = -1;//    订单总金额，单位为分
                    int wx_cash_fee = -1;//现金支付金额订单现金支付金额，详见支付金额
                    #region  数据解析
                    //列 是否存在
                    string signstr = "";//需要前面的字符串
                                        //wx_appid
                    if (ds.Tables[0].Columns.Contains("appid"))
                    {
                        wx_appid = ds.Tables[0].Rows[0]["appid"].ToString();
                        if (!string.IsNullOrEmpty(wx_appid))
                        {
                            signstr += "appid=" + wx_appid;
                        }
                    }
                    //wx_bank_type
                    if (ds.Tables[0].Columns.Contains("bank_type"))
                    {
                        wx_bank_type = ds.Tables[0].Rows[0]["bank_type"].ToString();
                        if (!string.IsNullOrEmpty(wx_bank_type))
                        {
                            signstr += "&bank_type=" + wx_bank_type;
                        }
                    }
                    //wx_cash_fee
                    if (ds.Tables[0].Columns.Contains("cash_fee"))
                    {
                        wx_cash_fee = Convert.ToInt32(ds.Tables[0].Rows[0]["cash_fee"].ToString());
                        signstr += "&cash_fee=" + wx_cash_fee;
                    }
                    //wx_fee_type
                    if (ds.Tables[0].Columns.Contains("fee_type"))
                    {
                        wx_fee_type = ds.Tables[0].Rows[0]["fee_type"].ToString();
                        if (!string.IsNullOrEmpty(wx_fee_type))
                        {
                            signstr += "&fee_type=" + wx_fee_type;
                        }
                    }
                    //wx_is_subscribe
                    if (ds.Tables[0].Columns.Contains("is_subscribe"))
                    {
                        wx_is_subscribe = ds.Tables[0].Rows[0]["is_subscribe"].ToString();
                        if (!string.IsNullOrEmpty(wx_is_subscribe))
                        {
                            signstr += "&is_subscribe=" + wx_is_subscribe;
                        }
                    }
                    //wx_mch_id
                    if (ds.Tables[0].Columns.Contains("mch_id"))
                    {
                        wx_mch_id = ds.Tables[0].Rows[0]["mch_id"].ToString();
                        if (!string.IsNullOrEmpty(wx_mch_id))
                        {
                            signstr += "&mch_id=" + wx_mch_id;
                        }
                    }
                    //wx_nonce_str
                    if (ds.Tables[0].Columns.Contains("nonce_str"))
                    {
                        wx_nonce_str = ds.Tables[0].Rows[0]["nonce_str"].ToString();
                        if (!string.IsNullOrEmpty(wx_nonce_str))
                        {
                            signstr += "&nonce_str=" + wx_nonce_str;
                        }
                    }
                    //wx_openid
                    if (ds.Tables[0].Columns.Contains("openid"))
                    {
                        wx_openid = ds.Tables[0].Rows[0]["openid"].ToString();
                        if (!string.IsNullOrEmpty(wx_openid))
                        {
                            signstr += "&openid=" + wx_openid;
                        }
                    }
                    //wx_out_trade_no
                    if (ds.Tables[0].Columns.Contains("out_trade_no"))
                    {
                        wx_out_trade_no = ds.Tables[0].Rows[0]["out_trade_no"].ToString();
                        if (!string.IsNullOrEmpty(wx_out_trade_no))
                        {
                            signstr += "&out_trade_no=" + wx_out_trade_no;
                        }
                    }
                    //wx_result_code 
                    if (ds.Tables[0].Columns.Contains("result_code"))
                    {
                        wx_result_code = ds.Tables[0].Rows[0]["result_code"].ToString();
                        if (!string.IsNullOrEmpty(wx_result_code))
                        {
                            signstr += "&result_code=" + wx_result_code;
                        }
                    }
                    //wx_result_code 
                    if (ds.Tables[0].Columns.Contains("return_code"))
                    {
                        wx_return_code = ds.Tables[0].Rows[0]["return_code"].ToString();
                        if (!string.IsNullOrEmpty(wx_return_code))
                        {
                            signstr += "&return_code=" + wx_return_code;
                        }
                    }
                    //wx_sign 
                    if (ds.Tables[0].Columns.Contains("sign"))
                    {
                        wx_sign = ds.Tables[0].Rows[0]["sign"].ToString();
                        //if (!string.IsNullOrEmpty(wx_sign))
                        //{
                        //    signstr += "&sign=" + wx_sign;
                        //}
                    }
                    //wx_time_end
                    if (ds.Tables[0].Columns.Contains("time_end"))
                    {
                        wx_time_end = ds.Tables[0].Rows[0]["time_end"].ToString();
                        if (!string.IsNullOrEmpty(wx_time_end))
                        {
                            signstr += "&time_end=" + wx_time_end;
                        }
                    }
                    //wx_total_fee
                    if (ds.Tables[0].Columns.Contains("total_fee"))
                    {
                        wx_total_fee = Convert.ToInt32(ds.Tables[0].Rows[0]["total_fee"].ToString());
                        signstr += "&total_fee=" + wx_total_fee;
                    }
                    //wx_trade_type
                    if (ds.Tables[0].Columns.Contains("trade_type"))
                    {
                        wx_trade_type = ds.Tables[0].Rows[0]["trade_type"].ToString();
                        if (!string.IsNullOrEmpty(wx_trade_type))
                        {
                            signstr += "&trade_type=" + wx_trade_type;
                        }
                    }
                    //wx_transaction_id
                    if (ds.Tables[0].Columns.Contains("transaction_id"))
                    {
                        wx_transaction_id = ds.Tables[0].Rows[0]["transaction_id"].ToString();
                        if (!string.IsNullOrEmpty(wx_transaction_id))
                        {
                            signstr += "&transaction_id=" + wx_transaction_id;
                        }
                    }
                    #endregion
                    //追加key 密钥
                    signstr += "&key=" + System.Web.Configuration.WebConfigurationManager.AppSettings["key"].ToString();
                    //签名正确
                    string orderStrwhere = "ordernumber='" + wx_out_trade_no + "'";
                    if (wx_sign == System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(signstr, "MD5").ToUpper())
                    {
                        //签名正确   处理订单操作逻辑
                        if (wx_result_code == "SUCCESS")
                        {
                            var reward = t_rewardBLL.GetListByWhere(string.Format("where out_trade_no='{0}'", wx_out_trade_no)).FirstOrDefault();
                            if (reward != null)
                            {
                                NotifyCallback(reward, wx_transaction_id);
                            }
                        }

                        return_msg = "<xml><return_code><![CDATA[SUCCESS]]></return_code><return_msg><![CDATA[OK]]></return_msg></xml>";
                    }
                    else
                    {
                        //追加备注信息
                        return_msg = "<xml><return_code><![CDATA[FAIL]]></return_code><return_msg><![CDATA[签名校验失败]]></return_msg></xml>";
                    }
                }
                else
                {
                    // 返回信息，如非空，为错误原因  签名失败 参数格式校验错误
                    return_msg = "<xml><return_code><![CDATA[FAIL]]></return_code><return_msg><![CDATA[" + ds.Tables[0].Rows[0]["return_msg"].ToString() + "]]></return_msg></xml>";
                }
            }

            return new HttpResponseMessage
            {
                Content = new StringContent(return_msg, new UTF8Encoding(false), "text/plain")
            };
        }

        //获得Post过来的数据
        private string GetPostStr()
        {
            Int32 intLen = Convert.ToInt32(System.Web.HttpContext.Current.Request.InputStream.Length);
            byte[] b = new byte[intLen];
            System.Web.HttpContext.Current.Request.InputStream.Read(b, 0, intLen);
            return System.Text.Encoding.UTF8.GetString(b);
        }

        ///// <summary>
        ///// 接收从微信支付后台发送过来的数据并验证签名
        ///// </summary>
        ///// <returns>微信支付后台返回的数据</returns>
        //private WxPayData GetNotifyData()
        //{
        //    //接收从微信后台POST过来的数据
        //    System.IO.Stream s = System.Web.HttpContext.Current.Request.InputStream;
        //    int count = 0;
        //    byte[] buffer = new byte[1024];
        //    StringBuilder builder = new StringBuilder();
        //    while ((count = s.Read(buffer, 0, 1024)) > 0)
        //    {
        //        builder.Append(Encoding.UTF8.GetString(buffer, 0, count));
        //    }
        //    s.Flush();
        //    s.Close();
        //    s.Dispose();

        //    //转换数据格式并验证签名
        //    WxPayData data = new WxPayData();
        //    try
        //    {
        //        data.FromXml(builder.ToString());
        //    }
        //    catch (WxPayException ex)
        //    {
        //        //若签名错误，则立即返回结果给微信支付后台
        //        WxPayData res = new WxPayData();
        //        res.SetValue("return_code", "FAIL");
        //        res.SetValue("return_msg", ex.Message);
        //        res.ToXml();

        //    }

        //    return data;
        //}

        ///// <summary>
        ///// 付款结果处理
        ///// </summary>
        //private static void PayResult(string ResultMsg)
        //{
        //    if (!string.IsNullOrEmpty(ResultMsg))
        //    {
        //        var xml = new XmlDocument();
        //        xml.LoadXml(ResultMsg);
        //        //处理返回的值
        //        DataSet ds = new DataSet();
        //        StringReader stram = new StringReader(ResultMsg);
        //        XmlTextReader reader = new XmlTextReader(stram);
        //        ds.ReadXml(reader);
        //        string return_code = ds.Tables[0].Rows[0]["return_code"].ToString();
        //        if (return_code.ToUpper() == "SUCCESS")
        //        {
        //            //通信成功  
        //            string result_code = ds.Tables[0].Rows[0]["result_code"].ToString();//业务结果  
        //            if (result_code.ToUpper() == "SUCCESS")
        //            {
        //                string appid = ds.Tables[0].Rows[0]["appid"].ToString();
        //                string attach = ds.Tables[0].Rows[0]["attach"].ToString();
        //                string mch_id = ds.Tables[0].Rows[0]["mch_id"].ToString();
        //                string openid = ds.Tables[0].Rows[0]["openid"].ToString();
        //                Int32 total_fee = Convert.ToInt32(ds.Tables[0].Rows[0]["total_fee"].ToString());
        //                string transaction_id = ds.Tables[0].Rows[0]["transaction_id"].ToString();


        //            }
        //            else
        //            {
        //                //Log4Helper.ErrorInfo("GXL", "支付失败:" + ResultMsg);
        //            }
        //        }
        //        else
        //        {
        //            // Log4Helper.ErrorInfo("GXL", "支付失败:" + ResultMsg);
        //        }
        //    }
        //}

        /// <summary>
        /// 打赏支付回调方法
        /// </summary>
        /// <param name="RequestDto">请求对象</param>
        /// <returns></returns>
        [HttpPost, Route("Customer/RewardPayCallback")]
        private HttpResponseMessage RewardPayCallback(RewardPayCallbackRequestDto RequestDto)
        {
            try
            {
                #region 参数验证
                if (RequestDto.RewardId <= 0)
                {
                    return ApiResult.Error("参数RewardId错误");
                }
                if (string.IsNullOrEmpty(RequestDto.PaymentNo))
                {
                    return ApiResult.Error("支付单号不能为空");
                }
                if (RequestDto.State < 0 || RequestDto.State > 1)
                {
                    return ApiResult.Error("参数State错误");
                }

                var reward = t_rewardBLL.GetModel(RequestDto.RewardId);
                if (reward == null || reward.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该打赏记录");
                }
                if (reward.StoreId <= 0)
                {
                    return ApiResult.Error("该打赏记录门店信息有误");
                }
                if (!string.IsNullOrEmpty(reward.PaymentNo))
                {
                    return ApiResult.Error("该打赏记录已支付");
                }
                var store = t_storeBLL.GetModel(reward.StoreId);
                if (store == null || store.IsDeleted > 0 || store.State != 1)
                {
                    return ApiResult.Error("没有该门店信息(或被禁用)");
                }

                var business = t_businessBLL.GetModel(reward.BusinessId);
                if (business == null || business.IsDeleted > 0)
                {
                    return ApiResult.Error("没有该服务员信息(或被禁用)");
                }
                var StoreBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and BusinessId={1}", reward.StoreId, reward.BusinessId)).FirstOrDefault();
                if (StoreBusiness == null)
                {
                    return ApiResult.Error("该门店下没有该服务员信息");
                }
                if (StoreBusiness.State == 1)
                {
                    return ApiResult.Error("该商户已被禁用");
                }
                if (StoreBusiness.RoleId != 3)
                {
                    return ApiResult.Error("只有服务员才能被打赏");
                }
                #endregion

                #region 支付成功需要的模型变量
                Model.t_reward_distribution distributionModel = new Model.t_reward_distribution();
                Model.t_business businessService = new Model.t_business();
                Model.t_wallet walletService = new Model.t_wallet();
                Model.t_business businessManager = new Model.t_business();
                Model.t_wallet walletManager = new Model.t_wallet();
                Model.t_agent_store agentStore = new Model.t_agent_store();
                Model.t_agent agent = new Model.t_agent();
                Model.t_wallet walletAgent = new Model.t_wallet();
                Model.t_wallet walletPlatform = new Model.t_wallet();
                #endregion

                #region 支付成功
                if (RequestDto.State == 1)
                {
                    #region 获取分配比例规则
                    var distributions = t_reward_distributionBLL.GetList();
                    if (distributions.Any())
                    {
                        var distributionUse = distributions.Where(o => o.IsDeleted == 0 && o.IsUse == 1).FirstOrDefault();
                        if (distributionUse == null)
                        {
                            var distributionDefault = distributions.Where(o => o.IsDeleted == 0 && o.IsDefault == 1).FirstOrDefault();
                            if (distributionDefault != null)
                            {
                                distributionModel = distributionDefault;
                            }
                        }
                        else
                        {
                            distributionModel = distributionUse;
                        }
                    }
                    #endregion

                    if (distributionModel != null)
                    {
                        if (distributionModel.BusinessRatio + distributionModel.AgentRatio + distributionModel.StoreRatio + distributionModel.PlatformRatio != 1)
                        {
                            return ApiResult.Error("打赏分成比例有误");
                        }
                        //服务人员
                        businessService = t_businessBLL.GetModel(reward.BusinessId);
                        if (businessService != null)
                        {
                            walletService = t_walletBLL.GetListByWhere(string.Format("where OpendId='{0}' and StoreId={1}", businessService.OpenId, reward.StoreId)).FirstOrDefault();
                        }

                        //门店超级管理员
                        var storeBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and RoleId=0", reward.StoreId)).FirstOrDefault();
                        if (storeBusiness == null)
                        {
                            return ApiResult.Error("该门店没有超级管理员");
                        }

                        businessManager = t_businessBLL.GetListByWhere(string.Format("where ID ={0} and IsDeleted=0", storeBusiness.BusinessId)).FirstOrDefault();
                        if (businessManager != null)
                        {
                            walletManager = t_walletBLL.GetListByWhere(string.Format("where OpendId='{0}' and StoreId={1}", businessManager.OpenId, reward.StoreId)).FirstOrDefault();
                        }

                        //代理商
                        agentStore = t_agent_storeBLL.GetListByWhere(string.Format("where StoreId={0}", reward.StoreId)).FirstOrDefault();
                        if (agentStore != null)  //有关联的代理商
                        {
                            agent = t_agentBLL.GetModel(agentStore.AgentId);
                            if (agent != null)
                            {
                                walletAgent = t_walletBLL.GetListByWhere(string.Format("where OpendId='{0}'", agent.OpenId)).FirstOrDefault();
                            }
                        }

                        //平台
                        walletPlatform = t_walletBLL.GetListByWhere(string.Format("where OpendId='System'")).FirstOrDefault();
                    }
                }
                #endregion

                //开启事务
                using (var connection = CommonBll.GetOpenMySqlConnection())
                {
                    IDbTransaction transaction = connection.BeginTransaction();

                    #region 更新打赏记录中的支付单号
                    reward.PaymentNo = RequestDto.PaymentNo;
                    reward.LastModificationTime = DateTime.Now;
                    var UpdateReward = t_rewardBLL.UpdateByTrans(reward, connection, transaction);
                    if (UpdateReward < 0)
                    {
                        transaction.Rollback();
                        return ApiResult.Error("更新打赏记录失败");
                    }
                    #endregion

                    #region 写入支付记录信息
                    var InsertPayment = t_paymentBLL.InsertByTrans(new Model.t_payment
                    {
                        PaymentNo = RequestDto.PaymentNo,
                        CustomerId = reward.CustomerId,
                        GoodsId = reward.GoodsId,
                        State = RequestDto.State,
                        Money = reward.Money,
                        PaymentTime = DateTime.Now
                    }, connection, transaction);
                    if (InsertPayment < 0)
                    {
                        transaction.Rollback();
                        return ApiResult.Error("写入支付记录信息失败");
                    }
                    #endregion

                    #region 支付成功
                    if (RequestDto.State == 1)
                    {
                        if (distributionModel != null)
                        {
                            #region 服务人员
                            //if (businessService != null && businessService.IsDeleted == 0 && businessService.State == 0)  //未删除且启用状态服务人员才能分配打赏
                            //{
                            if (businessService != null && !string.IsNullOrEmpty(businessService.OpenId))
                            {
                                var resultService = this.AddRewardDetailAndWalletByTrans(distributionModel.BusinessRatio, businessService.OpenId, reward.StoreId, RequestDto.RewardId, reward.Money, 3, walletService, connection, transaction);
                                if (!string.IsNullOrEmpty(resultService))
                                {
                                    transaction.Rollback();
                                    return ApiResult.Error(resultService);
                                }
                            }
                            //}
                            #endregion

                            #region 门店超级管理员
                            //if (businessManager != null && businessManager.IsDeleted == 0 && businessManager.State == 0)  //未删除且启用状态服务人员才能分配打赏
                            //{
                            if (businessManager != null && !string.IsNullOrEmpty(businessManager.OpenId))
                            {
                                var resultManager = this.AddRewardDetailAndWalletByTrans(distributionModel.StoreRatio, businessManager.OpenId, reward.StoreId, RequestDto.RewardId, reward.Money, 2, walletManager, connection, transaction);
                                if (!string.IsNullOrEmpty(resultManager))
                                {
                                    transaction.Rollback();
                                    return ApiResult.Error(resultManager);
                                }
                            }
                            //}
                            #endregion

                            #region 代理商
                            //if (agent != null && agent.IsDeleted == 0 && agent.State == 1) //代理商未删除且状态为启用才能进行打赏分配
                            //{
                            if (agent != null && !string.IsNullOrEmpty(agent.OpenId))
                            {
                                var resultAgent = this.AddRewardDetailAndWalletByTrans(distributionModel.AgentRatio, agent.OpenId, reward.StoreId, RequestDto.RewardId, reward.Money, 1, walletAgent, connection, transaction);
                                if (!string.IsNullOrEmpty(resultAgent))
                                {
                                    transaction.Rollback();
                                    return ApiResult.Error(resultAgent);
                                }
                            }
                            //}
                            #endregion

                            #region 平台
                            var resultLast = this.AddRewardDetailAndWalletByTrans(distributionModel.PlatformRatio, "System", reward.StoreId, RequestDto.RewardId, reward.Money, 0, walletPlatform, connection, transaction);
                            if (!string.IsNullOrEmpty(resultLast))
                            {
                                transaction.Rollback();
                                return ApiResult.Error(resultLast);
                            }
                            #endregion
                        }

                        #region 发送消息
                        var InsertMessage = t_messageBLL.InsertByTrans(new Model.t_message
                        {
                            StoreId = reward.StoreId,
                            SendUserId = reward.BusinessId,
                            AcceptUserId = reward.CustomerId,
                            Content = "谢谢评价，你在这里可以随时找我聊天哦",
                            MessageType = 0,
                            SendTime = DateTime.Now,
                            State = 0,
                            IsDeleted = 0,
                            CreationTime = DateTime.Now
                        }, connection, transaction);
                        if (InsertMessage < 0)
                        {
                            return ApiResult.Error("写入消息信息失败");
                        }
                        #endregion

                        #region 消费记录
                        var InsertConsumption = t_consumptionBLL.InsertByTrans(new Model.t_consumption
                        {
                            BusinessId = reward.BusinessId,
                            CustomerId = reward.CustomerId,
                            StoreId = reward.StoreId,
                            RecordType = 0,
                            ConsumeTime = DateTime.Now,
                            IsDeleted = 0,
                            CreationTime = DateTime.Now
                        }, connection, transaction);
                        if (InsertConsumption < 0)
                        {
                            transaction.Rollback();
                            return ApiResult.Error("写入消费记录信息失败");
                        }
                        #endregion
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

        #region 辅助方法
        private float transmitNum(float f)
        {
            if ((f - (int)f) < 0.5 && (f - (int)f > 0))
            {
                return (int)f;
            }
            if ((f - (int)f) >= 0.5)
            {
                return (int)f + (float)0.5;
            }
            if ((f - (int)f) == 0)
            {
                return f;
            }
            return f;
        }

        private string AddRewardDetailAndWalletByTrans(decimal Ratio, string OpenId, int StoreId, int RewardId, decimal RewardMoney, int UserType,
            Model.t_wallet Wallet, IDbConnection connection, IDbTransaction trans)
        {
            var InsertRewardDetail = t_reward_detailBLL.InsertByTrans(new Model.t_reward_detail
            {
                DistributionRatio = (float)Ratio,
                BenefitMoney = RewardMoney * Ratio,
                RewardId = RewardId,
                RewardMoney = RewardMoney,
                UserType = UserType,
                OpenrId = OpenId
            }, connection, trans);
            if (InsertRewardDetail < 0)
            {
                return "写入打赏明细记录失败";
            }

            if (Wallet == null)
            {
                var InsertWallet = t_walletBLL.InsertByTrans(new Model.t_wallet
                {
                    StoreId = StoreId,
                    OpenId = OpenId,
                    Balance = RewardMoney * Ratio,
                    Withdraw = 0,
                    IsDeleted = 0,
                    CreationTime = DateTime.Now
                }, connection, trans);
                if (InsertWallet < 0)
                {
                    return "写入钱包记录失败";
                }
            }
            else
            {
                Wallet.Balance = Wallet.Balance + RewardMoney * Ratio;
                Wallet.LastModificationTime = DateTime.Now;
                var UpdateWallet = t_walletBLL.UpdateByTrans(Wallet, connection, trans);
                if (UpdateWallet < 0)
                {
                    return "更新钱包记录失败";
                }
            }

            return "";
        }

        private void NotifyCallback(Model.t_reward reward, string wx_transaction_id)
        {
            LogHelper.WriteLog(string.Format("{0}回调处理开始,打赏记录：{1}", wx_transaction_id, JsonHelper.SerializeObject(reward)));

            #region 支付成功需要的模型变量
            Model.t_reward_distribution distributionModel = new Model.t_reward_distribution();
            Model.t_business businessService = new Model.t_business();
            Model.t_wallet walletService = new Model.t_wallet();
            Model.t_business businessManager = new Model.t_business();
            Model.t_wallet walletManager = new Model.t_wallet();
            Model.t_agent_store agentStore = new Model.t_agent_store();
            Model.t_agent agent = new Model.t_agent();
            Model.t_wallet walletAgent = new Model.t_wallet();
            Model.t_wallet walletPlatform = new Model.t_wallet();
            #endregion

            #region 获取分配比例规则
            var isFind = false;
            var storeinfo = t_storeBLL.GetListByWhere($"where IsDeleted=0 and StoreId={reward.StoreId}")
                 .FirstOrDefault();
            if (storeinfo != null && storeinfo.PlanId >0)
            {
                var distribution = t_reward_distributionBLL.GetListByWhere($"where ID={storeinfo.PlanId} and IsDeleted=0").FirstOrDefault();
                if (distribution != null)
                {
                    isFind = true;
                    distributionModel = distribution;
                }
            }
 
            if (!isFind)
            {
                var distributions = t_reward_distributionBLL.GetList();
                if (distributions.Any())
                {
                    var distributionUse = distributions.FirstOrDefault(o => o.IsDeleted == 0 && o.IsUse == 1);
                    if (distributionUse == null)
                    {
                        var distributionDefault = distributions.FirstOrDefault(o => o.IsDeleted == 0 && o.IsDefault == 1);
                        if (distributionDefault != null)
                        {
                            distributionModel = distributionDefault;
                        }
                    }
                    else
                    {
                        distributionModel = distributionUse;
                    }
                }
            }
            #endregion

            if (distributionModel != null)
            {
                LogHelper.WriteLog(string.Format("获得分配比例：{0}", JsonHelper.SerializeObject(distributionModel)));

                if (distributionModel.BusinessRatio + distributionModel.AgentRatio + distributionModel.StoreRatio + distributionModel.PlatformRatio == 1)
                {
                    LogHelper.WriteLog(string.Format("获取服务人员信息以及钱包信息"));
                    //服务人员
                    businessService = t_businessBLL.GetModel(reward.BusinessId);
                    if (businessService != null)
                    {
                        walletService = t_walletBLL.GetListByWhere(string.Format("where OpenId='{0}' and StoreId={1}", businessService.OpenId, reward.StoreId)).FirstOrDefault();
                    }

                    LogHelper.WriteLog(string.Format("获取门店超级管理员信息以及钱包信息"));
                    //门店超级管理员
                    var storeBusiness = t_store_businessBLL.GetListByWhere(string.Format("where StoreId={0} and RoleId=0", reward.StoreId)).FirstOrDefault();
                    if (storeBusiness != null)
                    {
                        businessManager = t_businessBLL.GetListByWhere(string.Format("where ID ={0} and IsDeleted=0", storeBusiness.BusinessId)).FirstOrDefault();
                        if (businessManager != null)
                        {
                            walletManager = t_walletBLL.GetListByWhere(string.Format("where OpenId='{0}' and StoreId={1}", businessManager.OpenId, reward.StoreId)).FirstOrDefault();
                        }
                    }

                    LogHelper.WriteLog(string.Format("获取代理商信息以及钱包信息"));
                    //代理商
                    agentStore = t_agent_storeBLL.GetListByWhere(string.Format("where StoreId={0}", reward.StoreId)).FirstOrDefault();
                    if (agentStore != null)  //有关联的代理商
                    {
                        agent = t_agentBLL.GetModel(agentStore.AgentId);
                        if (agent != null)
                        {
                            walletAgent = t_walletBLL.GetListByWhere(string.Format("where OpenId='{0}'", agent.OpenId)).FirstOrDefault();
                        }
                    }

                    LogHelper.WriteLog(string.Format("获取平台信息以及钱包信息"));
                    //平台
                    walletPlatform = t_walletBLL.GetListByWhere(string.Format("where OpenId='System'")).FirstOrDefault();
                }

            }

            LogHelper.WriteLog(string.Format("开启事务"));

            //开启事务
            using (var connection = CommonBll.GetOpenMySqlConnection())
            {
                IDbTransaction transaction = connection.BeginTransaction();

                LogHelper.WriteLog(string.Format("更新打赏记录中的支付单号"));

                #region 更新打赏记录中的支付单号
                reward.result_code = "SUCCESS";
                reward.PaymentNo = wx_transaction_id;
                reward.LastModificationTime = DateTime.Now;
                var UpdateReward = t_rewardBLL.UpdateByTrans(reward, connection, transaction);
                if (UpdateReward < 0)
                {
                    transaction.Rollback();
                    //return ApiResult.Error("更新打赏记录失败");
                    return;
                }
                #endregion

                //#region 写入支付记录信息
                //var InsertPayment = t_paymentBLL.InsertByTrans(new Model.t_payment
                //{
                //    PaymentNo = wx_transaction_id,
                //    CustomerId = reward.CustomerId,
                //    GoodsId = reward.GoodsId,
                //    State = 1,
                //    Money = reward.Money,
                //    PaymentTime = DateTime.Now
                //}, connection, transaction);
                //if (InsertPayment < 0)
                //{
                //    transaction.Rollback();
                //    return ApiResult.Error("写入支付记录信息失败");
                //}
                //#endregion

                #region 支付成功
                if (distributionModel != null)
                {
                    #region 服务人员
                    if (businessService != null && !string.IsNullOrEmpty(businessService.OpenId))
                    {
                        LogHelper.WriteLog(string.Format("写入服务人员打赏分成比例：{0}", reward.Money * distributionModel.BusinessRatio));

                        var resultService = this.AddRewardDetailAndWalletByTrans(distributionModel.BusinessRatio, businessService.OpenId, reward.StoreId, reward.ID, reward.Money, 3, walletService, connection, transaction);
                        if (!string.IsNullOrEmpty(resultService))
                        {
                            transaction.Rollback();
                            //return ApiResult.Error(resultService);
                            return;
                        }
                    }
                    #endregion

                    #region 门店超级管理员
                    if (businessManager != null && !string.IsNullOrEmpty(businessManager.OpenId))
                    {
                        LogHelper.WriteLog(string.Format("写入门店超级管理员打赏分成比例：{0}", reward.Money * distributionModel.StoreRatio));
                        var resultManager = this.AddRewardDetailAndWalletByTrans(distributionModel.StoreRatio, businessManager.OpenId, reward.StoreId, reward.ID, reward.Money, 2, walletManager, connection, transaction);
                        if (!string.IsNullOrEmpty(resultManager))
                        {
                            transaction.Rollback();
                            //return ApiResult.Error(resultManager);
                            return;
                        }
                    }
                    #endregion

                    #region 代理商
                    if (agent != null && !string.IsNullOrEmpty(agent.OpenId))
                    {
                        LogHelper.WriteLog(string.Format("写入代理商打赏分成比例：{0}", reward.Money * distributionModel.AgentRatio));
                        var resultAgent = this.AddRewardDetailAndWalletByTrans(distributionModel.AgentRatio, agent.OpenId, reward.StoreId, reward.ID, reward.Money, 1, walletAgent, connection, transaction);
                        if (!string.IsNullOrEmpty(resultAgent))
                        {
                            transaction.Rollback();
                            //return ApiResult.Error(resultAgent);
                            return;
                        }
                    }
                    #endregion

                    #region 平台
                    LogHelper.WriteLog(string.Format("写入平台打赏分成比例：{0}", reward.Money * distributionModel.PlatformRatio));
                    var resultLast = this.AddRewardDetailAndWalletByTrans(distributionModel.PlatformRatio, "System", reward.StoreId, reward.ID, reward.Money, 0, walletPlatform, connection, transaction);
                    if (!string.IsNullOrEmpty(resultLast))
                    {

                        transaction.Rollback();
                        //return ApiResult.Error(resultLast);
                        return;
                    }
                    #endregion
                }

                #region 发送消息
                //var InsertMessage = t_messageBLL.InsertByTrans(new Model.t_message
                //{
                //    StoreId = reward.StoreId,
                //    SendUserId = reward.BusinessId,
                //    AcceptUserId = reward.CustomerId,
                //    Content = "谢谢评价，你在这里可以随时找我聊天哦",
                //    MessageType = 0,
                //    SendTime = DateTime.Now,
                //    State = 0,
                //    IsDeleted = 0,
                //    CreationTime = DateTime.Now
                //}, connection, transaction);
                //if (InsertMessage < 0)
                //{
                //    return ApiResult.Error("写入消息信息失败");
                //}
                #endregion

                #region 消费记录
                LogHelper.WriteLog(string.Format("插入消费记录"));
                var InsertConsumption = t_consumptionBLL.InsertByTrans(new Model.t_consumption
                {
                    BusinessId = reward.BusinessId,
                    CustomerId = reward.CustomerId,
                    StoreId = reward.StoreId,
                    RecordType = 0,
                    ConsumeTime = DateTime.Now,
                    IsDeleted = 0,
                    CreationTime = DateTime.Now
                }, connection, transaction);
                if (InsertConsumption < 0)
                {
                    transaction.Rollback();
                    //return ApiResult.Error("写入消费记录信息失败");
                    return;
                }
                #endregion
                #endregion

                LogHelper.WriteLog(string.Format("回调通知处理成功"));
                transaction.Commit();
            }
        }
        #endregion

    }
}
