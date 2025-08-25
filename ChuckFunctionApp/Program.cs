using ChuckFunctionApp.Infrastructure.API;
using ChuckFunctionApp.Infrastructure.Storage;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http.Headers;

var host = new HostBuilder()
    .ConfigureAppConfiguration(c =>
    {
        c.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
         .AddEnvironmentVariables();
    })
    .ConfigureLogging(lb => lb.AddConsole())
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;
        
        services.Configure<ConfigOptions>(ctx.Configuration.GetSection("Ingestion"));

        // HttpClient ç Polly (retry + timeout)
        services.AddHttpClient("Chuck", client =>
        {
            var baseUrl = cfg["Providers:ChuckNorris:BaseUrl"];
            var hostHeader = cfg["Providers:ChuckNorris:ApiHost"];
            var apiKey = cfg["Providers:ChuckNorris:ApiKey"];
            client.BaseAddress = new Uri(baseUrl!);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-RapidAPI-Host", hostHeader);
            client.DefaultRequestHeaders.Add("X-RapidAPI-Key", apiKey);
        })
        .AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(2 * attempt)))
        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30)));

        // Providers & repo
        services.AddSingleton<IJokesAPI, ChuckNorrisJokesAPI>();
        services.AddSingleton<IJokesRepository, JokesRepository>(sp =>
        {
            var repo = new JokesRepository("jokes.db");
            repo.InitializeAsync().GetAwaiter().GetResult();
            return repo;
        }); 

        services.AddSingleton<IJokesCollector, JokesCollector>();
        services.AddSingleton<DbInitializer>();

        // Azure Functions isolated
        services.AddLogging();

    })
    .Build();

// Database initialization - create table and constraint
using (var scope = host.Services.CreateScope())
{
    var init = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await init.EnsureCreatedAsync();
}

await host.RunAsync();
