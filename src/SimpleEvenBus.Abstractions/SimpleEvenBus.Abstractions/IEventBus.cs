using System;

namespace SimpleEvenBus.Abstractions
{
    public interface IEventBus
    {
        void Publish();
    }
}
