namespace Erp.Infrastructure.Abstractions.Messaging;

public interface IMessageBroker
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class;
    Task SubscribeAsync<TMessage>(Func<TMessage, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where TMessage : class;
}
