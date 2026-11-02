namespace OrderAccumulator.Infrastructure.Messaging;

/// <summary>
/// Publica as mensagens pendentes do outbox, na ordem, e marca cada uma como processada logo depois de publicada.
/// Se a publicação falhar, para ali: as mensagens seguintes esperam, o que preserva a ordem. Se o processo cair entre
/// publicar e marcar, a mensagem é publicada de novo (entrega "pelo menos uma vez"); quem consome ignora duplicatas pelo EventId.
/// </summary>
public sealed class OutboxDispatcher(IOutboxStore store, IOutboxPublisher publisher)
{
    /// <returns>Quantas mensagens foram publicadas nesta chamada.</returns>
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
