using System.Threading.Tasks;
using SimpleEvenBus.Abstractions.Events;

namespace SimpleEvenBus.Abstractions
{
    public interface IEventHandler
    {
        ValueTask HandleAsync(Event @event);
    }

    public interface IEventHandler<in TEvent> : IEventHandler 
        where TEvent : Event
    {
        ValueTask HandleAsync(TEvent @event);
    }
}
