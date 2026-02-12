using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace RabbitMq
{
    public static class ServiceCollectionExtensions
    {
        public static void AddRabbitMq(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<IRegistrationConfigurator> configurator)
        {
            services.Configure<RabbitMqConfig>(configuration.GetSection("RabbitMq"));

            services.AddMassTransit(x =>
            {
                configurator(x);
                x.UsingRabbitMq((context, cfg) =>
                {
                    var settings = context.GetRequiredService<IOptions<RabbitMqConfig>>().Value;
                    cfg.Host(new Uri($"rabbitmq://{settings.Host}:{settings.Port}{settings.VirtualHost}"), h =>
                    {
                        h.Username(settings.Username);
                        h.Password(settings.Password);
                    });
                    cfg.ConfigureEndpoints(context);
                });
            });
        }
    }
}
