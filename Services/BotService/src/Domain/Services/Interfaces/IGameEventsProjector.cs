namespace BotService.Domain.Interfaces;

public interface IGameEventsProjector<TModel>
    where TModel : GameEventsProjection
{
    public TModel Apply(TModel model, object @event);
}
