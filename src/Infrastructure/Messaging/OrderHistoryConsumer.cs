using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OrderAccumulator.Infrastructure.Messaging;

public sealed class OrderHistoryConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<OrderHistoryConsumer> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var kafka = options.Value;
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            GroupId = kafka.ConsumerGroupId,
            ClientId = "order-accumulator-history",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true
        }).Build();

        consumer.Subscribe(kafka.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    logger.LogWarning("Histórico: erro ao ler do Kafka ({Reason}); nova tentativa em {Seconds}s.", ex.Error.Reason, RetryDelay.TotalSeconds);
                    await Delay(RetryDelay, stoppingToken);
                    continue;
                }

                if (result is not null)
                    await HandleAsync(consumer, result, stoppingToken);
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleAsync(IConsumer<string, string> consumer, ConsumeResult<string, string> result, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var projector = scope.ServiceProvider.GetRequiredService<OrderHistoryProjector>();

                var stored = await projector.ProjectAsync(result.Message.Value, cancellationToken);
                if (!stored)
                    logger.LogInformation("Histórico: evento repetido ignorado ({Partition}:{Offset}).", result.Partition.Value, result.Offset.Value);

                consumer.Commit(result);
                return;
            }
            catch (InvalidEventException ex)
            {
                logger.LogError(ex, "Histórico: mensagem inválida descartada ({Partition}:{Offset}).", result.Partition.Value, result.Offset.Value);
                consumer.Commit(result);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Histórico: falha ao gravar ({Partition}:{Offset}); nova tentativa em {Seconds}s.", result.Partition.Value, result.Offset.Value, RetryDelay.TotalSeconds);
                await Delay(RetryDelay, cancellationToken);
            }
        }
    }

    private static async Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // desligando
        }
    }
}
