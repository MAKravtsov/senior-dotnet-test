namespace AIAgent.Responders;

internal interface IAiResponder
{
    Task Prepare(CancellationToken cancellationToken);

    Task<string> Respond(string message, CancellationToken cancellationToken);
}
