using System;

using SimpleEvenBus.Abstractions.Events;

namespace SimpleEvenBus.Abstractions
{
    public static class DefaultEventHandler
    {
        internal static string GetEventNameByHandler(Type handlerType)
        {
            var interfaceType = typeof(IEventHandler);
            if (!interfaceType.IsAssignableFrom(handlerType))
                Throws.DoesNotImplement(handlerType.FullName, interfaceType.FullName);

            if (handlerType.IsGenericType)
                return handlerType.GenericTypeArguments[0].Name;

            return nameof(Event);
        }

        public static string GetEventNameByHandler<THandler>() where THandler : IEventHandler
        {
            return GetEventNameByHandler(typeof(THandler));
        }
    }
}
