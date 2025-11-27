namespace GameService.Application;

public interface IGameCollection
{
    public Task CreateAsync(
        string gameId,
        Domain.GameOption[] options,
        byte[] randomSeed,
        CancellationToken cancellationToken
    );
    public void Delete(string gameId);
    public IGame Get(string gameId);
}
