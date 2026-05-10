namespace PaymentService.Application.Abstractions;

public interface IIntegrationEventOutbox
{
    Task AddAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : notnull;
}
