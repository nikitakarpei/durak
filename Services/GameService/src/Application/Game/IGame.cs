namespace GameService.Application;

public interface IGame
{
    public Task ExecuteAsync(Domain.GameCommand command, CancellationToken cancellationToken);
}
