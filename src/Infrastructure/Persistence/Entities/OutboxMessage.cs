namespace OrderAccumulator.Infrastructure.Persistence.Entities;

/// <summary>
/// Evento de domínio gravado na mesma transação da exposição. Um relay publica as mensagens
/// pendentes (<see cref="ProcessedAtUtc"/> nulo) e as marca como processadas.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>O mesmo <c>EventId</c> do evento de domínio (serve de chave de idempotência para quem consome).</summary>
    public Guid Id { get; set; }

    /// <summary><c>OrderAccepted</c> ou <c>OrderRejected</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Chave de particionamento da mensagem (o ativo).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Evento serializado em JSON.</summary>
    public string Payload { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}
