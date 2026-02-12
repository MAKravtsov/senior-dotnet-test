using Bot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMq;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

// Читаем конфигурацию через вспомогательный метод: если существует appsettings.private.json — используем его, иначе — appsettings.json
ConfigureAppSettings(builder.Configuration);

builder.Services.Configure<BotConfig>(builder.Configuration.GetSection("Bot"));
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var config = sp.GetRequiredService<IOptions<BotConfig>>();
    return new TelegramBotClient(config.Value.BotToken);
});

builder.Services.AddHostedService<BotRunner>();

builder.Services.AddRabbitMq(builder.Configuration, x =>
{
    x.AddConsumer<BotAnswerReadyConsumer>();
});

var host = builder.Build();
await host.RunAsync();

static void ConfigureAppSettings(ConfigurationManager configuration)
{
    var basePath = AppContext.BaseDirectory;
    configuration.SetBasePath(basePath);

    var privateFile = Path.Combine(basePath, "appsettings.private.json");
    if (File.Exists(privateFile))
    {
        configuration.AddJsonFile("appsettings.private.json", optional: false, reloadOnChange: true);
    }
    else
    {
        configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    }
}
