using System;
using System.Collections.Generic;

using SimpleEvenBus.Abstractions.Events;

namespace SimpleEvenBus.Abstractions
{
    public interface ISubscriptionsManager
    {
        bool IsEmpty { get; }

        event EventHandler<string> OnEventRemoved;

        void Subscribe<THandler>() where THandler : IEventHandler;
        void Subscribe(Type handlerType);
        
        void Unsubscribe<THandler>() where THandler : IEventHandler;
        void Unsubscribe(Type handlerType);
        
        void Clear();

        bool HasSubscriptionsForEvent(string eventName);
        bool HasSubscriptionsForEvent<TEvent>() where TEvent : Event;

        Type GetEventType(string eventName);
        string GetEventName<TEvent>() where TEvent : Event;

        IEnumerable<EventHandlerInfo> GetHandlersForEvent(string eventName);
        IEnumerable<EventHandlerInfo> GetHandlersForEvent<TEvent>() where TEvent : Event;
    }
}
