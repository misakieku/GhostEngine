using Ghost.Editor.AppStates;
using Ghost.Editor.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ghost.Editor.Services;

internal class AppStateService(params IEnumerable<IAppState<StateKey>> states)
{
    private readonly Dictionary<StateKey, IAppState<StateKey>> _states = states.ToDictionary(s => s.StateKy, s => s);
    private IAppState<StateKey>? _current;

    public async Task TransitionToAsync(StateKey stateKey, object? parameter = null)
    {
        var previous = _current;
        var next = _states[stateKey];

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

        _current = next;
    }
}