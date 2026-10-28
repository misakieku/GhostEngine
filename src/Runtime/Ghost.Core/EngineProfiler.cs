using System.Diagnostics;

namespace Ghost.Core;

public static class EngineProfiler
{
    public struct ProfilingScope
    {
        public string Name
        {
            get; set;
        }

        public long StartTime
        {
            get; set;
        }

        public long EndTime
        {
            get; set;
        }
    }

    private class Impl
    {
        private readonly Stack<ProfilingScope> _profilingScopes = new Stack<ProfilingScope>(16);
        private ProfilingScope _activeScope;

        public void BeginProfilingScope(long startTime, string name)
        {
            _activeScope = new ProfilingScope
            {
                Name = name,
                StartTime = startTime
            };
        }

        public void EndProfilingScope(long endTime)
        {
            _activeScope.EndTime = endTime;
            _profilingScopes.Push(_activeScope);
        }

        public ProfilingScope[] Collect()
        {
            var scopes = _profilingScopes.ToArray();
            _profilingScopes.Clear();
            return scopes;
        }
    }

#if GHOST_ENABLE_PROFILING
    private static readonly ThreadLocal<Impl> s_impl = new ThreadLocal<Impl>(static () => new Impl());
#endif

    internal static Stopwatch? Stopwatch
    {
        get; set;
    } = null;

    internal static ProfilingScope[][]? CollectAll()
    {
#if GHOST_ENABLE_PROFILING
        var allScopes = new ProfilingScope[s_impl.Values.Count][];
        for (int i = 0; i < s_impl.Values.Count; i++)
        {
            allScopes[i] = s_impl.Values[i].Collect();
        }

        return allScopes;
#else
        return null;
#endif
    }

    [Conditional("GHOST_ENABLE_PROFILING")]
    public static void BeginProfilingScope(string name)
    {
#if GHOST_ENABLE_PROFILING
        s_impl.Value?.BeginProfilingScope(Stopwatch?.ElapsedTicks ?? 0, name);
#endif
    }

    [Conditional("GHOST_ENABLE_PROFILING")]
    public static void EndProfilingScope()
    {
#if GHOST_ENABLE_PROFILING
        s_impl.Value?.EndProfilingScope(Stopwatch?.ElapsedTicks ?? 0);
#endif
    }
}
