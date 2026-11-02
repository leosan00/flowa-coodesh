using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderAccumulator.Application.Infrastructure;
using OrderAccumulator.Application.ReadModels;
using OrderAccumulator.Application.UseCases;
using OrderAccumulator.Application.UseCases.GetExposures;
using OrderAccumulator.Application.UseCases.GetExposures.Models;
using OrderAccumulator.Application.UseCases.GetOrderHistory;
using OrderAccumulator.Application.UseCases.GetOrderHistory.Models;
using OrderAccumulator.Application.UseCases.PlaceOrder;
using OrderAccumulator.Application.UseCases.PlaceOrder.Models;
using OrderAccumulator.Infrastructure.Messaging;
using OrderAccumulator.Infrastructure.Persistence;

namespace OrderAccumulator.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<IUseCase<PlaceOrderRequestModel, PlaceOrderResponseModel>, PlaceOrderUseCase>();
        services.AddScoped<IUseCase<GetExposuresRequestModel, IReadOnlyList<ExposureView>>, GetExposuresUseCase>();
        services.AddScoped<IUseCase<GetOrderHistoryRequestModel, IReadOnlyList<OrderHistoryItem>>, GetOrderHistoryUseCase>();

        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default não está configurada.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IExposureRepository, ExposureRepository>();
        services.AddScoped<IExposureReadModel, ExposureReadModel>();

        return services;
    }

    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        // Um produtor para a aplicação toda: é thread-safe e o container o descarta (com flush) ao desligar.
        services.AddSingleton<IProducer<string, string>>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KafkaOptions>>().Value;

            return new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = options.BootstrapServers,
                ClientId = "order-accumulator",
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageTimeoutMs = 10_000
            }).Build();
        });

        services.AddSingleton<IOutboxPublisher, KafkaOutboxPublisher>();
        services.AddScoped<IOutboxStore, OutboxStore>();
        services.AddScoped<OutboxDispatcher>();
        services.AddHostedService<OutboxRelay>();

        services.AddScoped<IOrderHistoryStore, OrderHistoryStore>();
        services.AddScoped<OrderHistoryProjector>();
        services.AddHostedService<OrderHistoryConsumer>();

        return services;
    }
}
