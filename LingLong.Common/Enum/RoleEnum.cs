using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LingLong.Common.Enum
{
    public enum RoleEnum
    {
        [Description("超级管理员")]
        SuperAdministrators = 0,
        [Description("管理员")]
        Administrators = 1,
        [Description("门店经理")]
        Manager = 2,
        [Description("服务人员")]
        Business = 3
    }
}
