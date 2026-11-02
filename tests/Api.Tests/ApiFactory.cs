using Confluent.Kafka;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using OrderAccumulator.Application.Infrastructure;
using OrderAccumulator.Infrastructure.Messaging;
using OrderAccumulator.Infrastructure.Persistence;

namespace OrderAccumulator.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public Mock<IExposureRepository> Repository { get; } = new();

    public Mock<IExposureReadModel> ReadModel { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<Microsoft.EntityFrameworkCore.DbContextOptions<AppDbContext>>();
            services.RemoveAll<Microsoft.EntityFrameworkCore.DbContextOptions>();
            services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<AppDbContext>>();

            services.RemoveAll<IExposureRepository>();
            services.RemoveAll<IExposureReadModel>();
            services.AddSingleton(Repository.Object);
            services.AddSingleton(ReadModel.Object);

            RemoveMessaging(services);
        });
    }

    private static void RemoveMessaging(IServiceCollection services)
    {
        var jobs = services
            .Where(d => d.ServiceType == typeof(IHostedService)
                && (d.ImplementationType == typeof(OutboxRelay) || d.ImplementationType == typeof(OrderHistoryConsumer)))
            .ToList();
        foreach (var descriptor in jobs)
            services.Remove(descriptor);

        services.RemoveAll<OutboxDispatcher>();
        services.RemoveAll<IOutboxStore>();
        services.RemoveAll<IOutboxPublisher>();
        services.RemoveAll<OrderHistoryProjector>();
        services.RemoveAll<IOrderHistoryStore>();
        services.RemoveAll<IProducer<string, string>>();
    }

    public void ResetMocks()
    {
        Repository.Reset();
        ReadModel.Reset();
    }
}
