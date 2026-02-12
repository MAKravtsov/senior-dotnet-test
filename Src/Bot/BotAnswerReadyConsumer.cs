using MassTransit;
using Messages;
using Telegram.Bot;

namespace Bot;

internal class BotAnswerReadyConsumer : IConsumer<BotAnswerReady>
{
    private readonly ITelegramBotClient _botClient;

    public BotAnswerReadyConsumer(
        ITelegramBotClient botClient)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
    }

    public async Task Consume(ConsumeContext<BotAnswerReady> context)
    {
        await _botClient.SendMessage(
            chatId: context.Message.ChatId,
            text: context.Message.Answer,
            cancellationToken: context.CancellationToken);
    }
}
