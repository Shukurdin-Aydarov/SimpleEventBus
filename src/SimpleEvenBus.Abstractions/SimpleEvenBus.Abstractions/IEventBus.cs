using SimpleEvenBus.Abstractions.Events;

namespace SimpleEvenBus.Abstractions
{
    public interface IEventBus
    {
        void Publish(Event @event);

        void Subscribe<THandler>() where THandler : IEventHandler;
        void Unsubscribe<THandler>() where THandler : IEventHandler;
    }
}
