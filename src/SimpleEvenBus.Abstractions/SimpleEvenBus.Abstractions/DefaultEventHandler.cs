using System;

using SimpleEvenBus.Abstractions.Events;

namespace SimpleEvenBus.Abstractions
{
    public static class DefaultEventHandler
    {
        public static string GetEventNameByHandler(Type handlerType)
        {
            Throws.IfIEventHandlerNotImplemented(handlerType);

            return GetEventNameByHandlerInternal(handlerType);
        }

        public static string GetEventNameByHandler<THandler>() where THandler : IEventHandler
        {
            return GetEventNameByHandlerInternal(typeof(THandler));
        }

        internal static string GetEventNameByHandlerInternal(Type handlerType)
        {
            if (handlerType.IsGenericType)
                return handlerType.GenericTypeArguments[0].Name;

            return nameof(Event);
        }
    }
}
