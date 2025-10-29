namespace GameService.Application;

public interface IGameCollection
{
    public void Create(string gameId, Domain.GameOption[] options);
    public void Delete(string gameId);
    public IGame Get(string gameId);
}
