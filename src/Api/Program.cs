using System.Text.Json.Nodes;
using OrderAccumulator.Api.Extensions;
using OrderAccumulator.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidRequestResponseFactory.Create);
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "OrderAccumulator API";
        document.Info.Description = "Recebe ordens e controla a exposição financeira por ativo (limite de R$ 1.000.000).";
        return Task.CompletedTask;
    });

    // Exemplo válido pré-preenchido no Swagger UI, para testar sem editar o corpo.
    options.AddSchemaTransformer((schema, context, _) =>
    {
        if (context.JsonTypeInfo.Type == typeof(OrderRequest))
        {
            schema.Examples =
            [
                new JsonObject
                {
                    ["ativo"] = "PETR4",
                    ["lado"] = "C",
                    ["quantidade"] = 584,
                    ["preco"] = 54.87m
                }
            ];
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddUseCases();

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "OrderAccumulator API v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

app.Run();

public partial class Program;
