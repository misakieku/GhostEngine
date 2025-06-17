namespace Ghost.Editor.Core.AppState;

internal class AppStateMachine
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
        var next = s_states[stateKey].Value;

        if (previous != null)
        {
            await previous.OnExitingAsync();
        }

        await next.OnEnteringAsync(parameter);

        if (previous != null)
        {
            await previous.OnExitedAsync();
        }

        await next.OnEnteredAsync(parameter);

        s_current = next;
    }
}