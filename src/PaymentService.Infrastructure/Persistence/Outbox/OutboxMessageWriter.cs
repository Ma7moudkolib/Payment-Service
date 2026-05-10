using System.Text.Json;
using PaymentService.Application.Abstractions;

namespace PaymentService.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessageWriter : IIntegrationEventOutbox
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ApplicationDbContext _dbContext;

    public OutboxMessageWriter(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : notnull
    {
        var type = typeof(TMessage).AssemblyQualifiedName ?? typeof(TMessage).FullName!;
        var payload = JsonSerializer.Serialize(message, JsonOptions);
        await _dbContext.OutboxMessages.AddAsync(new OutboxMessage(type, payload), cancellationToken);
    }
}
