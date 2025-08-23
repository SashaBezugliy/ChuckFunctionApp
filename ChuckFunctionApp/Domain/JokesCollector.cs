using ChuckFunctionApp.Infrastructure.API;
using ChuckFunctionApp.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

public class JokesCollector : IJokesCollector
{
    private readonly IJokesAPI _provider;
    private readonly IJokesRepository _repo;
    private readonly ILogger<JokesCollector> _logger;
    private readonly int MAXLENGTH = 200;

    public JokesCollector(IJokesAPI provider, IJokesRepository repo, ILogger<JokesCollector> logger)
    {
        _provider = provider;
        _repo = repo;
        _logger = logger;
    }

    public async Task CollectJokes(int count, CancellationToken ct)
    {
        var jokes = await _provider.GetAsync(count, ct);

        var jokesFiltered = jokes
            .Where(j => !string.IsNullOrWhiteSpace(j.Text) && j.Text.Length <= MAXLENGTH)
            .ToList();

        int saved = 0;
        int failed = 0;

        foreach (Joke joke in jokesFiltered)
        {
            try
            {
                var inserted = await _repo.InsertJokeAsync(joke);
                if (inserted == 1) saved++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert joke: {text}", joke.Text);
                failed++;
            }
        }
        _logger.LogInformation("Fetched from API: {fetched}, Filtered: {filtered}, " +
            "Inserted: {inserted}, Failed to insert: {failed}",
            jokes.Count(), jokesFiltered.Count, saved, failed);
    }
}
