using System;
using RabbitMQ.Client;

namespace SimpleEventBus.RabbitMQ
{
    public interface IPersistentConnection : IDisposable
    {
        bool IsConnected { get; }

        bool TryConnect();

        IModel CreateModel();
    }
}
