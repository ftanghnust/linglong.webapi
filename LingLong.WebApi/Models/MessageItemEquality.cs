using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LingLong.WebApi.Models
{
    public class MessageItemEquality : IEqualityComparer<MessageItem>
    {
        public bool Equals(MessageItem x, MessageItem y)
        {
            return x.StoreId == y.StoreId && x.UserId == y.UserId && x.UserOpenId == y.UserOpenId;
        }

        public int GetHashCode(MessageItem obj)
        {
            if (obj == null)
            {
                return 0;
            }
            else
            {
                return obj.ToString().GetHashCode();
            }
        }
    }
}