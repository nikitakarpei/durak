namespace GameService.Domain;

public abstract record PlayerAfkTimer;

public record ActivePlayerAfkTimer(string PlayerId, int TimerGeneration) : PlayerAfkTimer;

public record InactivePlayerAfkTimer(int? LastTimerGeneration) : PlayerAfkTimer;
