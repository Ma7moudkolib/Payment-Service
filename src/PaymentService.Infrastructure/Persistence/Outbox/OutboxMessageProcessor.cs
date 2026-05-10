using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Options;

namespace PaymentService.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessageProcessor : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OutboxOptions> _options;
    private readonly ILogger<OutboxMessageProcessor> _logger;

    public OutboxMessageProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxMessageProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessBatchAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(_options.Value.PollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(_options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await PublishAsync(publishEndpoint, message, cancellationToken);
                message.MarkProcessed(DateTimeOffset.UtcNow);
            }
            catch (Exception exception)
            {
                message.MarkFailed(exception.Message);
                _logger.LogError(
                    exception,
                    "Failed to publish outbox message {OutboxMessageId} of type {OutboxMessageType}.",
                    message.Id,
                    message.Type);
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task PublishAsync(
        IPublishEndpoint publishEndpoint,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        var messageType = Type.GetType(message.Type, throwOnError: false);
        if (messageType is not null)
        {
            var typedMessage = JsonSerializer.Deserialize(message.Payload, messageType, JsonOptions);
            if (typedMessage is not null)
            {
                await publishEndpoint.Publish(typedMessage, messageType, cancellationToken);
                return;
            }
        }

        await publishEndpoint.Publish(
            new OutboxMessageEnvelope(message.Id, message.Type, message.Payload, message.OccurredAtUtc),
            cancellationToken);
    }
}
