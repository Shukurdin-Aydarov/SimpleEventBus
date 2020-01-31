using System;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client.Exceptions;

namespace SimpleEventBus.RabbitMQ
{
    internal static class Retry
    {
        internal static RetryPolicy Exponential(int retryCount, Action<Exception, TimeSpan> onRetry)
        {
            return Policy.Handle<BrokerUnreachableException>()
                         .Or<SocketException>()
                         .WaitAndRetry(retryCount, Exponential, onRetry);
        }

        internal static TimeSpan Exponential(int retryAttempt)
        {
            return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
        }
    }
}
