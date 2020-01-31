namespace SimpleEventBus.RabbitMQ
{
    public class EventBusOptions
    {
        public int RetryCount { get; set; }
        public string Exchange { get; set; }
        public string QueueName { get; set; }
    }
}
