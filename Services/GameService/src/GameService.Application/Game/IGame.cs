namespace GameService.Application;

public interface IGame
{
    public void Execute(Domain.GameCommand command);
}
