using System;
using System.Linq;
using System.Collections.Generic;

using SimpleEvenBus.Abstractions.Events;

namespace SimpleEvenBus.Abstractions
{
    public class DefaultSubscriptionManager : ISubscriptionsManager
    {
        private readonly Dictionary<string, List<EventHandlerInfo>> handlers;
        private readonly HashSet<Type> eventTypes;

        public event EventHandler<string> OnEventRemoved;

        public bool IsEmpty => handlers.Count is 0;

        public DefaultSubscriptionManager()
        {
            handlers = new Dictionary<string, List<EventHandlerInfo>>();
            eventTypes = new HashSet<Type>();
        }

        public void Subscribe<THandler>() where THandler : IEventHandler
        {
            var handlerType = typeof(THandler);

            SubscribeInternal(handlerType);
        }

        public void Subscribe(Type handlerType)
        {
            Throws.IfIEventHandlerNotImplemented(handlerType);

            SubscribeInternal(handlerType);
        }

        private void SubscribeInternal(Type handlerType)
        {
            var eventName = DefaultEventHandler.GetEventNameByHandlerInternal(handlerType);

            if (!HasSubscriptionsForEvent(eventName))
                handlers[handlerType.Name] = new List<EventHandlerInfo>();

            if (handlers[handlerType.Name].Any(s => s.HandlerType == handlerType))
                Throws.HandlerAlreadyRegistered(handlerType.Name, eventName);

            handlers[eventName].Add(new EventHandlerInfo(handlerType));
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

        public IEnumerable<EventHandlerInfo> GetHandlersForEvent(string eventName)
        {
            return handlers[eventName];
        }

        public IEnumerable<EventHandlerInfo> GetHandlersForEvent<TEvent>() where TEvent : Event
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

        public void Unsubscribe<THandler>() where THandler : IEventHandler
        {
            var handlerType = typeof(THandler);

            UnsubscribeInternal(handlerType);
        }

        public void Unsubscribe(Type handlerType)
        {
            Throws.IfIEventHandlerNotImplemented(handlerType);

            UnsubscribeInternal(handlerType);
        }

        private void UnsubscribeInternal(Type handlerType)
        {
            var eventName = DefaultEventHandler.GetEventNameByHandlerInternal(handlerType);

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
                
                OnEventRemoved?.Invoke(this, eventName);
            }
        }
    }
}
