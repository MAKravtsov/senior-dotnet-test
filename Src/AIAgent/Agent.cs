

using AIAgent.Responders;
using Microsoft.Extensions.Hosting;

namespace AIAgent
{
    internal class Agent : BackgroundService
    {
        private IAiResponder _aiResponder;

        public Agent(
            IAiResponder aiResponder)
        {
            _aiResponder = aiResponder ?? throw new ArgumentNullException(nameof(aiResponder));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _aiResponder.Prepare(stoppingToken);
        }
    }
}
