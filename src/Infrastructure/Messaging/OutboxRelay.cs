using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OrderAccumulator.Infrastructure.Messaging;

public sealed class OutboxRelay(IServiceScopeFactory scopeFactory, ILogger<OutboxRelay> logger) : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();

                var published = await dispatcher.DispatchPendingAsync(BatchSize, stoppingToken);
                if (published > 0)
                    logger.LogInformation("Outbox: {Count} mensagem(ns) publicada(s).", published);

                if (published < BatchSize)
                    await Task.Delay(IdleDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox: falha ao publicar; nova tentativa em {Seconds}s.", ErrorDelay.TotalSeconds);
                await Delay(ErrorDelay, stoppingToken);
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
        }
    }
}
