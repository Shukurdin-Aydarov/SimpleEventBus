using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using SimpleEvenBus.Abstractions;
using SimpleEvenBus.Abstractions.Events;

namespace SimpleEventBus.RabbitMQ
{
    [SuppressMessage("Design", "CA1063: Implement IDisposable Correctly")]
    public class EventBus : IEventBus, IDisposable
    {
        public const byte PersistentDeliveryMode = 2;
        public static readonly string ExchangeType = "direct";

        private readonly ILogger logger;
        private readonly EventBusOptions options;
        private readonly IServiceProvider provider;
        private readonly IPersistentConnection connection;
        private readonly ISubscriptionsManager subscriptions;

        private IModel consumerChannel;

        public EventBus(IPersistentConnection connection, ISubscriptionsManager subscriptions, IServiceProvider provider, EventBusOptions options, ILogger<EventBus> logger)
        {
            this.logger = logger;
            this.options = options;
            this.provider = provider;
            this.connection = connection;
            this.subscriptions = subscriptions;

            this.subscriptions.OnEventRemoved += OnEventRemovedFromSubscriptions;
            consumerChannel = CreateConsumerChannel();
        }

        public void Publish(Event @event)
        {
            if (!connection.IsConnected)
                connection.TryConnect();

            
            var log = "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})";
            var policy = Retry.Exponential(options.RetryCount, (e, t) => logger.LogWarning(e, log, @event.Id, $"{t.TotalSeconds:n1}", e.Message));

            var eventName = @event.GetType().Name;
            logger.LogTrace("Creating ReabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id, eventName);

            using var channel = connection.CreateModel();
            logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);
            channel.ExchangeDeclare(options.Exchange, ExchangeType, false, false, null);

            var message = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(log);

            policy.Execute(() =>
            {
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = PersistentDeliveryMode;

                logger.LogTrace("Publishing event to RabbitMQ: {EventId}", @event.Id);
                
                channel.BasicPublish(options.Exchange, eventName, true, properties, body);
            });
        }

        public void Subscribe<THandler>() where THandler : IEventHandler
        {
            var handlerType = typeof(THandler);
            var eventName = DefaultEventHandler.GetEventNameByHandler(handlerType);
            SubscribeInternal(eventName);

            logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, handlerType.Name);

            subscriptions.Subscribe(handlerType);

            StartBasicConsume();
        }

        public void Unsubscribe<THandler>() where THandler : IEventHandler
        {
            var handlerType = typeof(THandler);
            var eventName = DefaultEventHandler.GetEventNameByHandler(handlerType);

            logger.LogInformation("Unsubscribing event {EventName} with {EventHandler}", eventName, handlerType.Name);

            subscriptions.Unsubscribe(handlerType);
        }

        public void Dispose()
        {
            consumerChannel?.Dispose();
            subscriptions?.Clear();
        }

        private void SubscribeInternal(string eventName)
        {
            var exist = subscriptions.HasSubscriptionsForEvent(eventName);
            if (exist) return;

            if (!connection.IsConnected)
                connection.TryConnect();

            using var channel = connection.CreateModel();
            channel.QueueBind(options.QueueName, options.Exchange, eventName, null);
        }

        private IModel CreateConsumerChannel()
        {
            if (!connection.IsConnected)
                connection.TryConnect();

            logger.LogTrace("--- Creating RabbitMQ consumer channel ---");

            var channel = connection.CreateModel();
            channel.ExchangeDeclare(options.Exchange, ExchangeType, false, false, null);
            channel.QueueDeclare(options.QueueName, true, false, false, null);

            channel.CallbackException += (s, args) =>
            {
                logger.LogWarning(args.Exception, "Recreating RabbitMQ consumer channel");

                consumerChannel.Dispose();
                consumerChannel = CreateConsumerChannel();
                StartBasicConsume();
            };

            return channel;
        }

        private void StartBasicConsume()
        {
            logger.LogTrace("--- Starting RabbitMQ basic consume ---");

            if (consumerChannel == null)
            {
                logger.LogError("StartBaicConsume cannot call on consumerChannel == null");
                return;
            }
            
            var consumer = new AsyncEventingBasicConsumer(consumerChannel);
            consumer.Received += OnConsumerReceived;
            consumerChannel.BasicConsume(options.QueueName, false, consumer);
        }

        private async Task OnConsumerReceived(object sender, BasicDeliverEventArgs args)
        {
            var eventName = args.RoutingKey;
            var message = Encoding.UTF8.GetString(args.Body);

            try
            {
                await ProcessEventAsync(eventName, message);
            }
            catch(Exception error)
            {
                logger.LogWarning(error, "----- ERROR Processing message \"{Message}\" of event {Event}", message, eventName);
            }

            // Even on exception we take the message off the queue.
            // You should be handled with a Dead Letter Exchange (DLX). 
            // For more information see: https://www.rabbitmq.com/dlx.html
            consumerChannel.BasicAck(args.DeliveryTag, false);
        }

        private async Task ProcessEventAsync(string eventName, string message)
        {
            logger.LogTrace("--- Processing RabbitMQ event: {EventName} ---", eventName);

            if (!subscriptions.HasSubscriptionsForEvent(eventName))
            {
                logger.LogWarning("--- No subscription for RabbitMQ event: {EvenName}", eventName);
                await Task.CompletedTask;
            }

            using var scope = provider.CreateScope();
            var services = scope.ServiceProvider;
            var handlers = subscriptions.GetHandlersForEvent(eventName);
            foreach(var hi in handlers)
            {
                if (!(services.GetService(hi.HandlerType) is IEventHandler handler)) continue;

                var eventType = subscriptions.GetEventType(eventName);
                var @event = (Event)JsonConvert.DeserializeObject(message, eventType);

                await Task.Yield();
                await handler.HandleAsync(@event);
            }
        }

        private void OnEventRemovedFromSubscriptions(object sender, string eventName)
        {
            if (!connection.IsConnected)
                connection.TryConnect();

            using var channel = connection.CreateModel();
            channel.QueueUnbind(options.QueueName, options.Exchange, eventName);

            if (subscriptions.IsEmpty)
                consumerChannel.Close();
        }
    }
}
