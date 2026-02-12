using AIAgent;
using AIAgent.Configuration;
using AIAgent.Responders;
using AIAgent.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMq;

var builder = Host.CreateApplicationBuilder(args);

ConfigureAppSettings(builder.Configuration);

builder.Services.Configure<AgentConfig>(builder.Configuration.GetSection("Agent"));
builder.Services.AddSingleton<IMemoryStore, VolatileMemoryStore>();

builder.Services.AddSingleton<GeminiResponder>();
builder.Services.AddSingleton<OpenAiResponder>();
builder.Services.AddSingleton<SearchResponder>();
builder.Services.AddSingleton<IAiResponder>(sp =>
{
    var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AgentConfig>>().Value;
    return config.Type switch
    {
            ResponderType.Gemini => sp.GetRequiredService<GeminiResponder>(),
            ResponderType.OpenAI => sp.GetRequiredService<OpenAiResponder>(),
            ResponderType.Search => sp.GetRequiredService<SearchResponder>(),
        _ => throw new NotSupportedException($"Responder type '{config.Type}' is not supported.")
    };
});

builder.Services.AddHostedService<Agent>();

builder.Services.AddRabbitMq(builder.Configuration, x =>
{
    x.AddConsumer<BotAnswerRequestedConsumer>();
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