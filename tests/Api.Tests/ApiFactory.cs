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

/// <summary>
/// Sobe a Api em memória sem banco: o <see cref="AppDbContext"/> é removido (qualquer acesso acidental ao banco
/// falha na hora) e os repositórios são mocks controlados pelo teste.
/// </summary>
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

    /// <summary>Sem o job do outbox e sem Kafka: nenhum deles faz parte do contrato HTTP testado aqui.</summary>
    private static void RemoveMessaging(IServiceCollection services)
    {
        var relay = services.Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(OutboxRelay)).ToList();
        foreach (var descriptor in relay)
            services.Remove(descriptor);

        services.RemoveAll<OutboxDispatcher>();
        services.RemoveAll<IOutboxStore>();
        services.RemoveAll<IOutboxPublisher>();
        services.RemoveAll<IProducer<string, string>>();
    }

    /// <summary>Limpa configurações e chamadas registradas pelos mocks, deixando-os prontos para o próximo teste.</summary>
    public void ResetMocks()
    {
        Repository.Reset();
        ReadModel.Reset();
    }
}
