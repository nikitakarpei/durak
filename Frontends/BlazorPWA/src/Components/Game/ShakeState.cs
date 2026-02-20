namespace BlazorPWA.Components.Game;

public sealed class ShakeState
{
    public string? ActiveId { get; private set; }
    public bool IsActive { get; private set; }

    public async Task TriggerAsync(string id, Func<Task> onChange, int durationMs = 350)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        ActiveId = id;
        await onChange();

        await Task.Delay(durationMs);

        ActiveId = null;
        IsActive = false;
        await onChange();
    }
}
