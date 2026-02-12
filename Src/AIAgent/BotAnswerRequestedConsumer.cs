using AIAgent.Responders;
using MassTransit;
using Messages;

namespace AIAgent;

internal class BotAnswerRequestedConsumer : IConsumer<BotAnswerRequested>
{
    private IAiResponder _aiResponder;
    private IBus _bus;

    public BotAnswerRequestedConsumer(
        IAiResponder aiResponder,
        IBus bus)
    {
        _aiResponder = aiResponder ?? throw new ArgumentNullException(nameof(aiResponder));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    public async Task Consume(ConsumeContext<BotAnswerRequested> context)
    {
        var response = await _aiResponder.Respond(context.Message.Text, context.CancellationToken);
        
        var message = new BotAnswerReady
        {
            ChatId = context.Message.ChatId,
            Answer = response
        };

        await _bus.Publish(message, context.CancellationToken);
    }
}
