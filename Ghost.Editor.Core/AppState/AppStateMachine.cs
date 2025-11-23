using Ghost.Core;

namespace Ghost.Editor.Core.AppState;

internal partial class AppStateMachine : IDisposable, IAsyncDisposable
{
    private Dictionary<StateKey, Lazy<IAppState>> _states = new();
    private IAppState? _current;

    private bool _disposed;

    public void RegisterState(StateKey key, Func<IAppState> stateFactory)
    {
        _states[key] = new(stateFactory);
    }

    public async Task<Result> TransitionToAsync(StateKey stateKey, object? parameter = null)
    {
        var previous = _current;
        if (!_states.TryGetValue(stateKey, out var next))
        {
            return Result.Failure($"State '{stateKey}' not found.");
        }

        Result result;
        if (previous != null)
        {
            result = await previous.OnExitingAsync();
            if (result.IsFailure)
            {
                return result;
            }
        }

        result = await next.Value.OnEnteringAsync(parameter);
        if (result.IsFailure)
        {
            if (previous != null)
            {
                await previous.OnEnteredAsync(parameter);
            }

            return result;
        }

        if (previous != null)
        {
            result = await previous.OnExitedAsync();
            if (result.IsFailure)
            {
                await next.Value.OnExitedAsync();
                await previous.OnEnteredAsync(parameter);
                return result;
            }
        }

        result = await next.Value.OnEnteredAsync(parameter);
        if (result.IsFailure)
        {
            await next.Value.OnExitedAsync();

            if (previous != null)
            {
                await previous.OnEnteredAsync(parameter);
            }

            return result;
        }

        _current = next.Value;

        return Result.Success();
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _states.Clear();
        if (_current != null)
        {
            await _current.OnExitingAsync();
            await _current.OnExitedAsync();
        }

        _current = null;
        _disposed = true;
    }
}