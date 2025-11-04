namespace Ghost.Editor.Core.AppState;

internal partial class AppStateMachine : IDisposable, IAsyncDisposable
{
    private Dictionary<StateKey, Lazy<IAppState>> _states = new();
    private IAppState? _current;

    public void RegisterState(StateKey key, Func<IAppState> stateFactory)
    {
        _states[key] = new(stateFactory);
    }

    public async Task TransitionToAsync(StateKey stateKey, object? parameter = null)
    {
        var previous = _current;
        if (!_states.TryGetValue(stateKey, out var next))
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

        _current = next.Value;
    }

    public void Dispose()
    {
        _states.Clear();

        _current?.OnExitingAsync().GetAwaiter().GetResult();
        _current?.OnExitedAsync().GetAwaiter().GetResult();
        _current = null;
    }

    public async ValueTask DisposeAsync()
    {
        _states.Clear();
        if (_current != null)
        {
            await _current.OnExitingAsync();
            await _current.OnExitedAsync();
        }
    }
}