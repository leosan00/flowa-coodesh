namespace OrderAccumulator.Infrastructure.Messaging;

public sealed class InvalidEventException(string message, Exception? inner = null) : Exception(message, inner);
