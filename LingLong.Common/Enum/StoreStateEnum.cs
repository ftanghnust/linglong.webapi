using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LingLong.Common.Enum
{
    public enum StoreStateEnum
    {
        [Description("待审核")]
        Audited = 0,
        [Description("启用")]
        Enable = 1,
        [Description("禁用")]
        Disable = 2
    }
}
