using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ChuckFunctionApp.Infrastructure.API
{
    public class ChuckNorrisJokesAPI : IJokesAPI
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ChuckNorrisJokesAPI> _logger;

        public ChuckNorrisJokesAPI(IHttpClientFactory httpClientFactory, ILogger<ChuckNorrisJokesAPI> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<Joke>> GetAsync(int count, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("Chuck");
            var results = new List<Joke>();

            for (int i = 0; i < count; i++)
            {
                try
                {
                    var dto = await client.GetFromJsonAsync<ChuckDto>("jokes/random", ct);
                    if (dto?.Value is { Length: > 0 })
                    {
                        results.Add(new Joke
                        {
                            Text = dto.Value,
                            Source = "ChuckNorrisRapidApi"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch joke #{i}", i + 1);
                }
            }

            return results;
        }

        private sealed class ChuckDto
        {
            public string? Value { get; set; }
        }
    }
}
