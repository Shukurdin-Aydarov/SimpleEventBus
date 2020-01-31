using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleEvenBus.Abstractions
{
    internal static class Throws
    {
        internal static void HandlerAlreadyRegistered(string handler, string eventName) 
        {
            throw new ArgumentException(
                $"Handler Type {handler} already registered for {eventName}", "handlerType");
        }

        internal static void DoesNotImplement(string fullTypeName, string fullInterfaceName)
        {
            throw new ArgumentException($"Type '{fullTypeName}' does not implement '{fullInterfaceName}'");
        }

        internal static void IfIEventHandlerNotImplemented(Type handlerType)
        {
            var interfaceType = typeof(IEventHandler);
            if (!interfaceType.IsAssignableFrom(handlerType))
                DoesNotImplement(handlerType.FullName, interfaceType.FullName);
        }
    }
}
