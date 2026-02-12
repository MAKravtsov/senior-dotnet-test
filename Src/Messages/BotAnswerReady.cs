namespace Messages;

public class BotAnswerReady
{
    public required long ChatId { get; init; }

    public required string Answer { get; init; }
}
