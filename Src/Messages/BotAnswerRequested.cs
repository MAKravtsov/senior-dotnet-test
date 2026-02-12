namespace Messages;

public class BotAnswerRequested
{
    public required string Text { get; init; }

    public required long ChatId { get; init; }
}
