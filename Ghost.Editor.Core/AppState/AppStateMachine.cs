namespace Ghost.Editor.Core.AppState;

internal partial class AppStateMachine : IDisposable, IAsyncDisposable
{
    private Dictionary<StateKey, Lazy<IAppState>> s_states = new();
    private IAppState? s_current;

    public void RegisterState(StateKey key, Func<IAppState> stateFactory)
    {
        s_states[key] = new(stateFactory);
    }

    public async Task TransitionToAsync(StateKey stateKey, object? parameter = null)
    {
        var previous = s_current;
        if (!s_states.TryGetValue(stateKey, out var next))
        {
            throw new InvalidOperationException($"State '{stateKey}' is not registered.");
        }

        if (previous != null)
        {
            await previous.OnExitingAsync();
        }

        await next.Value.OnEnteringAsync(parameter);

        if (previous != null)
        {
            await previous.OnExitedAsync();
        }

        await next.Value.OnEnteredAsync(parameter);

        s_current = next.Value;
    }

    public void Dispose()
    {
        s_states.Clear();

        s_current?.OnExitingAsync().GetAwaiter().GetResult();
        s_current?.OnExitedAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        s_states.Clear();
        if (s_current != null)
        {
            await s_current.OnExitingAsync();
            await s_current.OnExitedAsync();
        }
    }
}