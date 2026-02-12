using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using MassTransit;
using Messages;

namespace Bot
{
    internal class BotRunner : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IBus _bus;

        public BotRunner(ITelegramBotClient botClient, IBus bus)
        {
            _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var receiverOptions = new ReceiverOptions { AllowedUpdates = [] };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandlePollingErrorAsync,
                receiverOptions,
                stoppingToken);

            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { Text: { } } message)
                return;

            var chatId = message.Chat.Id;

            var msg = new BotAnswerRequested
            {
                ChatId = chatId,
                Text = message.Text
            };

            await _bus.Publish(msg, cancellationToken);
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка Polling: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}