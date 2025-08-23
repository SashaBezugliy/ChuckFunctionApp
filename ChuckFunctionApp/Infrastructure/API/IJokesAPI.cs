public interface IJokesAPI
{
    Task<IEnumerable<Joke>> GetAsync(int count, CancellationToken ct);
}
