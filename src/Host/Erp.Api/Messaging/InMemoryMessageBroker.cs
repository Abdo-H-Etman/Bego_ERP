using Erp.Infrastructure.Abstractions.Messaging;
using ILogger = Serilog.ILogger;

namespace Erp.Api.Messaging;

public class InMemoryMessageBroker(ILogger logger) : IMessageBroker
{
    private readonly ILogger _logger = logger.ForContext<InMemoryMessageBroker>();

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        _logger.Information("Message published: {MessageType} - {@Message}", typeof(TMessage).Name, message);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<TMessage>(Func<TMessage, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        _logger.Information("Subscribed to message type: {MessageType}", typeof(TMessage).Name);
        return Task.CompletedTask;
    }
}
