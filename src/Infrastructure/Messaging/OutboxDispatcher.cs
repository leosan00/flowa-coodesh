namespace OrderAccumulator.Infrastructure.Messaging;

public sealed class OutboxDispatcher(IOutboxStore store, IOutboxPublisher publisher)
{
    public async Task<int> DispatchPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var pending = await store.GetPendingAsync(batchSize, cancellationToken);

        var published = 0;
        foreach (var message in pending)
        {
            await publisher.PublishAsync(message, cancellationToken);
            await store.MarkProcessedAsync(message.Id, cancellationToken);
            published++;
        }

        return published;
    }
}
