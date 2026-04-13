using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SkillMind.Application.Interfaces;
using SkillMind.Core.Domain.Interfaces;
using SkillMind.Core.Domain.Settings;
using SkillMind.Infrastructure.Shared.Contexts;
using SkillMind.Infrastructure.Shared.Services;
using Stripe;
using Stripe.Checkout;

namespace SkillMind.Infrastructure.Shared;

public static class SharedLayerInjection
{
    public static void AddSharedLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IRedisContext>(_ => new RedisContext(connectionString));

        services.Configure<KafkaSettings>(configuration.GetSection("Kafka"));
        services.Configure<StripeConfigurations>(configuration.GetSection("Stripe"));

        var stripeSection = configuration.GetSection("Stripe");
        var stripeSecret = stripeSection["SecretKey"]
            ?? stripeSection["Secret_Key"]
            ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
        if (!string.IsNullOrWhiteSpace(stripeSecret))
            StripeConfiguration.ApiKey = stripeSecret;

        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<KafkaSettings>>().Value;
            var config = new ProducerConfig { BootstrapServers = settings.BrokerAddress };
            return new ProducerBuilder<string, string>(config).Build();
        });

        services.AddSingleton<IAdminClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<KafkaSettings>>().Value;
            var config = new AdminClientConfig { BootstrapServers = settings.BrokerAddress };
            return new AdminClientBuilder(config).Build();
        });

        services.AddSingleton<IKafkaEventService, KafkaEventService>();
        services.AddScoped<PriceService>();
        services.AddScoped<SessionService>();
        services.AddScoped<StripeServices>();
    }
}