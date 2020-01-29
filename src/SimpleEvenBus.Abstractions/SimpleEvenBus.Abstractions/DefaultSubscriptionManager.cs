using System;
using System.Linq;
using System.Collections.Generic;

using SimpleEvenBus.Abstractions.Events;

namespace SimpleEvenBus.Abstractions
{
    public class DefaultSubscriptionManager : ISubscriptionsManager
    {
        private readonly Dictionary<string, List<SubscriptionInfo>> handlers;
        private readonly HashSet<Type> eventTypes;

        public DefaultSubscriptionManager()
        {
            handlers = new Dictionary<string, List<SubscriptionInfo>>();
            eventTypes = new HashSet<Type>();
        }

        public void AddSubscription<THandler>() where THandler : IEventHandler
        {
            var handlerType = typeof(THandler);
            var eventName = DefaultEventHandler.GetEventNameByHandler(handlerType);

            if (!HasSubscriptionsForEvent(eventName))
                handlers[handlerType.Name] = new List<SubscriptionInfo>();

            if (handlers[handlerType.Name].Any(s => s.HandlerType == handlerType))
                Throws.HandlerAlreadyRegistered(handlerType.Name, eventName);

            handlers[eventName].Add(new SubscriptionInfo(handlerType));
        }

        public void Clear() => handlers.Clear();

        public string GetEventName<TEvent>() where TEvent : Event
        {
            return typeof(TEvent).Name;
        }

        public Type GetEventType(string eventName)
        {
            return eventTypes.SingleOrDefault(e => e.Name == eventName);
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName)
        {
            return handlers[eventName];
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<TEvent>() where TEvent : Event
        {
            var eventName = GetEventName<TEvent>();
            return GetHandlersForEvent(eventName);
        }

        public bool HasSubscriptionsForEvent(string eventName) => handlers.ContainsKey(eventName);

        public bool HasSubscriptionsForEvent<TEvent>() where TEvent : Event
        {
            var eventName = GetEventName<TEvent>();
            return HasSubscriptionsForEvent(eventName);
        }

        public void RemoveSubscription<THandler>() where THandler : IEventHandler
        {
            var handlerType = typeof(THandler);
            var eventName = DefaultEventHandler.GetEventNameByHandler(handlerType);

            if (!HasSubscriptionsForEvent(eventName)) return;

            var handler = handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);

            if (handler.Equals(default)) return;

            handlers[eventName].Remove(handler);
            if (handlers[eventName].Count is 0)
            {
                handlers.Remove(eventName);
                var eventType = eventTypes.SingleOrDefault(e => e.Name == eventName);
                if (eventType != null)
                    eventTypes.Remove(eventType);
            }
        }
    }
}
