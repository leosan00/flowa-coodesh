using System.Text.Json;
using OrderAccumulator.Domain.Enums;
using OrderAccumulator.Infrastructure.Persistence.Entities;

namespace OrderAccumulator.Infrastructure.Messaging;

public sealed class OrderHistoryProjector(IOrderHistoryStore store)
{
    public async Task<bool> ProjectAsync(string json, CancellationToken cancellationToken = default)
    {
        var payload = Parse(json);

        var accepted = payload.Type switch
        {
            "OrderAccepted" => true,
            "OrderRejected" => false,
            _ => throw new InvalidEventException($"Tipo de evento desconhecido: '{payload.Type}'.")
        };

        var row = new OrderHistoryRow
        {
            EventId = payload.EventId,
            OrderId = payload.OrderId,
            Symbol = ParseEnum<Symbol>(payload.Symbol, "ativo"),
            Side = ParseEnum<Side>(payload.Side, "lado"),
            Quantity = payload.Quantity,
            Price = payload.Price,
            Accepted = accepted,
            Exposure = payload.Exposure,
            Reason = payload.Reason,
            OccurredAtUtc = payload.OccurredAt.UtcDateTime
        };

        return await store.TryAddAsync(row, cancellationToken);
    }

    private static OrderEventPayload Parse(string json)
    {
        try
        {
            return OutboxEventSerializer.Deserialize(json)
                ?? throw new InvalidEventException("A mensagem está vazia.");
        }
        catch (JsonException ex)
        {
            throw new InvalidEventException("A mensagem não é um JSON de evento válido.", ex);
        }
    }

    private static T ParseEnum<T>(string? value, string field) where T : struct, Enum =>
        Enum.GetNames<T>().Contains(value)
            ? Enum.Parse<T>(value!)
            : throw new InvalidEventException($"Valor inválido para {field}: '{value}'.");
}
