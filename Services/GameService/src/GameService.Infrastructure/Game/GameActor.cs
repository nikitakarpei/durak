using Akka.Actor;
using Akka.Event;
using Microsoft.Extensions.DependencyInjection;

namespace GameService.Infrastructure;

public class GameActor : ReceiveActor, ILogReceive
{
    private readonly ILoggingAdapter _log = Logging.GetLogger(Context);
    private readonly IServiceScope _scope;
    private readonly MediatR.IMediator _mediator;
    private readonly Domain.IRandomNumberGenerator _randomNumberGenerator;
    private readonly Domain.IPossibleMovesFinder _possibleMovesFinder;

    private readonly string _gameId;
    private IEnumerable<object> _domainEvents = [];

    public GameActor(IServiceProvider serviceProvider, string gameId)
    {
        _scope = serviceProvider.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        _randomNumberGenerator = _scope.ServiceProvider.GetRequiredService<Domain.IRandomNumberGenerator>();
        _possibleMovesFinder = _scope.ServiceProvider.GetRequiredService<Domain.IPossibleMovesFinder>();

        _gameId = gameId;

        ReceiveAsync<ExecuteCommandMessage>(HandleMessage);
    }

    protected override void PostStop()
    {
        _scope.Dispose();
    }

    // Message types
    public record struct ExecuteCommandMessage(object Command);

    private async Task HandleMessage(ExecuteCommandMessage message)
    {
        try
        {
            var gameAggregate = new Domain.GameAggregate(_randomNumberGenerator, _possibleMovesFinder);

            gameAggregate.Load(_domainEvents);

            gameAggregate.When((dynamic)message.Command);

            foreach (var change in gameAggregate.Changes)
            {
                await _mediator.Publish(CreateDomainEventNotification(change));
            }

            _domainEvents = _domainEvents.Concat(gameAggregate.Changes);
        }
        catch (Exception e)
        {
            Sender.Tell(new Status.Failure(e), Self);
        }
    }

    private MediatR.INotification CreateDomainEventNotification(object @event)
    {
        var eventType = @event.GetType();
        var notificationType = typeof(Application.GameDomainEventNotification<>).MakeGenericType(eventType);

        var notification =
            Activator.CreateInstance(notificationType, _gameId, @event)
            ?? throw new InvalidOperationException($"Failed to create notification for event: {eventType.Name}");

        return (MediatR.INotification)notification;
    }
}
