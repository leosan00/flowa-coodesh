using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Application.Infrastructure;
using OrderAccumulator.Application.UseCases;
using OrderAccumulator.Application.UseCases.PlaceOrder;
using OrderAccumulator.Application.UseCases.PlaceOrder.Models;
using OrderAccumulator.Infrastructure.Persistence;

namespace OrderAccumulator.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Registra os use cases da Application.</summary>
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<IUseCase<PlaceOrderRequestModel, PlaceOrderResponseModel>, PlaceOrderUseCase>();

        return services;
    }

    /// <summary>Registra o SQL Server (conexão em <c>ConnectionStrings:Default</c>) e os repositórios.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default não está configurada.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IExposureRepository, ExposureRepository>();
        services.AddScoped<IExposureReadModel, ExposureReadModel>();

        return services;
    }
}
