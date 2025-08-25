using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

public class JokesFunction
{
    private readonly IJokesCollector _service;
    private readonly ILogger<JokesFunction> _logger;
    private readonly ConfigOptions _options;

    public JokesFunction(IJokesCollector service, ILogger<JokesFunction> logger, IOptions<ConfigOptions> options)
    {
        _service = service;
        _logger = logger;
        _options = options.Value;
    }

    [Function("JokesTimer")]
    public async Task Run([TimerTrigger("%TimerSchedule%")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        await _service.CollectJokes(_options.BatchSize, cancellationToken);
        
        _logger.LogInformation($"JokesTimer function executed at: {DateTime.UtcNow}");
    }
}
