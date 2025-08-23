
public interface IJokesCollector
{
    Task CollectJokes(int count, CancellationToken ct);
}