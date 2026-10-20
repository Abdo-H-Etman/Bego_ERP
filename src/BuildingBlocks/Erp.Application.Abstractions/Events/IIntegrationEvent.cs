namespace Erp.Application.Abstractions.Events;

public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime OccurredOnUtc { get; }
}

public abstract record IntegrationEvent(Guid Id, DateTime OccurredOnUtc) : IIntegrationEvent
{
    protected IntegrationEvent() : this(Guid.NewGuid(), DateTime.UtcNow) { }
}

public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : class, IIntegrationEvent;
}
