using System;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SimpleEventBus.RabbitMQ
{
    [SuppressMessage("Design", "CA1063: Implement Dispose Correctly")]
    public class DefaultPersistentConnection : IPersistentConnection
    {
        private readonly IConnectionFactory connectionFactory;
        private readonly ILogger logger;
        private readonly int retryCount;

        private IConnection connection;

        private readonly object sync = new object();

        public DefaultPersistentConnection(IConnectionFactory connectionFactory, ILogger logger, int retryCount = 5)
        {
            this.logger = logger;
            this.retryCount = retryCount;
            this.connectionFactory = connectionFactory;
        }

        private bool disposed;
        public bool IsConnected => connection?.IsOpen == true && !disposed;

        public IModel CreateModel()
        {
            if (!IsConnected)
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");

            return connection.CreateModel();
        }
        
        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            try
            {
                connection.Dispose();
            }
            catch(IOException error)
            {
                logger.LogCritical(error, "An error occur at Dispose ({Error})", error.Message);
            }
        }
        
        public bool TryConnect()
        {
            logger.LogInformation("--- RabbitMQ Client is trying to connect ---");

            lock(sync)
            {
                const string message = "RabbitMQ Client could not connect after {Timeout}s ({ExceptionMessage})";
                var policy = Retry.Exponential(retryCount, (e,t) => logger.LogWarning(e, message, $"{t.TotalSeconds:n1}", e.Message));

                policy.Execute(() => connection = connectionFactory.CreateConnection());

                if (!IsConnected)
                {
                    logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

                    return false;
                }

                connection.CallbackException  += OnCallbackException;
                connection.ConnectionBlocked  += OnConnectionBlocked;
                connection.ConnectionShutdown += OnConnectionShutdown;
            }

            return true;
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (disposed) return;

            logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

            TryConnect();

        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (disposed) return;

            logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

            TryConnect();
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (disposed) return;

            logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

            TryConnect();
        }
    }
}
