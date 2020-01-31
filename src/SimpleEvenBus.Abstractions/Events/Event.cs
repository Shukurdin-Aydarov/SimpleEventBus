using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleEvenBus.Abstractions.Events
{
    public class Event
    {
        public Guid Id { get; set; }

        public DateTimeOffset CreationDate { get; set; }
    }
}
