using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Abstractions;
using PaymentService.Domain.Interfaces;
using PaymentService.Infrastructure.Gateways;
using PaymentService.Infrastructure.Gateways.Stripe;
using PaymentService.Infrastructure.Options;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Persistence.Outbox;
using PaymentService.Infrastructure.Persistence.Repositories;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PaymentDatabase")
            ?? throw new InvalidOperationException("Connection string 'PaymentDatabase' is not configured.");

        services.AddPaymentPersistencePostgreSql(connectionString);
        services.AddPaymentGateways(configuration);
        services.AddOutbox(configuration);
        services.AddRabbitMq(configuration);

        return services;
    }

    public static IServiceCollection AddPaymentPersistencePostgreSql(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<OutboxMessageWriter>();

        return services;
    }

    public static IServiceCollection AddPaymentPersistenceSqlServer(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<OutboxMessageWriter>();

        return services;
    }

    private static IServiceCollection AddPaymentGateways(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<StripeOptions>()
            .Configure(options =>
            {
                options.ApiKey = configuration[$"{StripeOptions.SectionName}:ApiKey"] ?? string.Empty;
            });

        services.AddScoped<StripePaymentGateway>();
        services.AddScoped<IPaymentGateway>(provider =>
            new ResilientPaymentGateway(
                provider.GetRequiredService<StripePaymentGateway>(),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ResilientPaymentGateway>>()));
        services.AddScoped<IPaymentGatewayResolver, PaymentGatewayResolver>();

        return services;
    }

    private static IServiceCollection AddOutbox(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<OutboxOptions>()
            .Configure(options =>
            {
                options.BatchSize = GetInt(configuration, $"{OutboxOptions.SectionName}:BatchSize", options.BatchSize);
                options.PollingIntervalSeconds = GetInt(
                    configuration,
                    $"{OutboxOptions.SectionName}:PollingIntervalSeconds",
                    options.PollingIntervalSeconds);
            });

        services.AddHostedService<OutboxMessageProcessor>();
        services.AddScoped<IIntegrationEventOutbox, OutboxMessageWriter>();

        return services;
    }

    private static IServiceCollection AddRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rabbitMqOptions = new RabbitMqOptions
        {
            Host = configuration[$"{RabbitMqOptions.SectionName}:Host"] ?? "localhost",
            VirtualHost = configuration[$"{RabbitMqOptions.SectionName}:VirtualHost"] ?? "/",
            Username = configuration[$"{RabbitMqOptions.SectionName}:Username"] ?? "guest",
            Password = configuration[$"{RabbitMqOptions.SectionName}:Password"] ?? "guest",
            Port = (ushort)GetInt(configuration, $"{RabbitMqOptions.SectionName}:Port", 5672)
        };

        services.AddMassTransit(bus =>
        {
            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.Port, rabbitMqOptions.VirtualHost, host =>
                {
                    host.Username(rabbitMqOptions.Username);
                    host.Password(rabbitMqOptions.Password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static int GetInt(IConfiguration configuration, string key, int defaultValue) =>
        int.TryParse(configuration[key], out var value) ? value : defaultValue;
}
