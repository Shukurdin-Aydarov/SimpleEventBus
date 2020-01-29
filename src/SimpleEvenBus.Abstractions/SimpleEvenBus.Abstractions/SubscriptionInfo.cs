using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleEvenBus.Abstractions
{
    public struct SubscriptionInfo
    {
        public SubscriptionInfo(Type handlerType)
        {
            HandlerType = handlerType;
        }

        public Type HandlerType { get; }
    }
}
