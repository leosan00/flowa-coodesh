namespace OrderAccumulator.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "orders";

    public string ConsumerGroupId { get; set; } = "order-history";
}
