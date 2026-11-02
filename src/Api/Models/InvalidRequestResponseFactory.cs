using Microsoft.AspNetCore.Mvc;
using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Api.Models;

/// <summary>Faz o erro de binding (JSON malformado, tipo errado, valor inválido) sair no mesmo formato das demais respostas.</summary>
public static class InvalidRequestResponseFactory
{
    public static readonly IReadOnlyDictionary<string, string> Messages = new Dictionary<string, string>
    {
        ["ativo"] = $"Ativo inválido. Use {string.Join(", ", Enum.GetNames<Symbol>())}.",
        ["lado"] = "Lado inválido. Use \"C\" (compra) ou \"V\" (venda).",
        ["quantidade"] = "Quantidade inválida. Informe um número inteiro.",
        ["preco"] = "Preço inválido. Informe um número decimal."
    };

    public static IActionResult Create(ActionContext context)
    {
        var messages = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .Select(entry => entry.Key.StartsWith("$.") ? entry.Key[2..] : entry.Key)
            .Where(Messages.ContainsKey)
            .Distinct()
            .Select(field => Messages[field])
            .ToList();

        var message = messages.Count > 0
            ? string.Join(" ", messages)
            : "Requisição inválida. O corpo deve ser um JSON com ativo, lado, quantidade e preco.";

        return new BadRequestObjectResult(OrderResponse.BadRequest(message));
    }
}
