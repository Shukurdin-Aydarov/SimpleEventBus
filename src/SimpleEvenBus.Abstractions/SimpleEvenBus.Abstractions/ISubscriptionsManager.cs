using System;
using System.Collections.Generic;

using SimpleEvenBus.Abstractions.Events;

namespace SimpleEvenBus.Abstractions
{
    public interface ISubscriptionsManager
    {
        void AddSubscription<THandler>() where THandler : IEventHandler;
        void RemoveSubscription<THandler>() where THandler : IEventHandler;
        void Clear();

        bool HasSubscriptionsForEvent(string eventName);
        bool HasSubscriptionsForEvent<TEvent>() where TEvent : Event;

        Type GetEventType(string eventName);
        string GetEventName<TEvent>() where TEvent : Event;

        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);
        IEnumerable<SubscriptionInfo> GetHandlersForEvent<TEvent>() where TEvent : Event;
    }
}
