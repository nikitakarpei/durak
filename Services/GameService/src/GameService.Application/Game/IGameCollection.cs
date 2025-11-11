namespace GameService.Application;

public interface IGameCollection
{
    public void Create(string gameId, Domain.GameOption[] options, byte[] randomSeed);
    public void Delete(string gameId);
    public IGame Get(string gameId);
}
