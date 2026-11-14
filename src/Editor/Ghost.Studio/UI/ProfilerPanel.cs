using Ghost.Editor.Core.Sync;
using Ghost.Studio.Backend;
using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Displays live engine telemetry KPIs and real-time <see cref="ImPlot"/> frame timing charts.
/// </summary>
internal sealed unsafe class ProfilerPanel
{
    private const int HISTORY_CAPACITY = 240;

    private readonly float[] _frameTimeHistory = new float[HISTORY_CAPACITY];
    private readonly float[] _orderedPlotBuffer = new float[HISTORY_CAPACITY];
    private readonly float[] _targetLine = new float[HISTORY_CAPACITY];
    private int _writeIndex;
    private int _sampleCount;
    private bool _paused;

    public ProfilerPanel()
    {
        for (var i = 0; i < HISTORY_CAPACITY; i++)
        {
            _targetLine[i] = 16.67f;
        }
    }

    public void RecordSample(float deltaTime)
    {
        if (_paused)
        {
            return;
        }

        var frameMs = MathF.Max(0.01f, deltaTime * 1000.0f);
        _frameTimeHistory[_writeIndex] = frameMs;
        _writeIndex = (_writeIndex + 1) % HISTORY_CAPACITY;
        if (_sampleCount < HISTORY_CAPACITY)
        {
            _sampleCount++;
        }
    }

    public void Draw(ImGuiContextManager contextManager, ref bool isOpen)
    {
        ImGui.SetNextWindowPos(new Vector2(650.0f, 640.0f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(610.0f, 220.0f), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("Profiler", ref isOpen))
        {
            ImGui.End();
            return;
        }

        ref readonly var tokens = ref contextManager.Tokens;

        if (ImGuiWidgets.ToggleButton(_paused ? "Resume" : "Pause", _paused, in tokens))
        {
            _paused = !_paused;
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset"))
        {
            Array.Clear(_frameTimeHistory);
            _writeIndex = 0;
            _sampleCount = 0;
        }

        ComputeStats(out var avgMs, out var minMs, out var maxMs);
        var fps = avgMs > 0.001f ? 1000.0f / avgMs : 0.0f;
        var managedMb = GC.GetTotalMemory(false) / (1024.0f * 1024.0f);
        var entityCount = EditorSyncBridge.Instance.LatestSnapshot?.EntityCount ?? 0;

        ImGui.SameLine();
        ImGui.PushFont(contextManager.MonospaceFont, EditorTheme.FONT_SIZE_MONO);
        ImGui.TextColored(tokens.TextSecondary, $"  Avg: {avgMs:0.00} ms ({fps:0} FPS)  |  Min/Max: {minMs:0.00} / {maxMs:0.00} ms  |  Heap: {managedMb:0.0} MB  |  Entities: {entityCount}");
        ImGui.PopFont();

        CopyOrderedSamples();

        var plotSize = new Vector2(-1.0f, MathF.Max(110.0f, ImGui.GetContentRegionAvail().Y));
        if (ImPlot.BeginPlot("Frame Time (ms)##ProfilerPlot", plotSize, ImPlotFlags.NoMenus))
        {
            ImPlot.SetupAxes("Frame", "ms", ImPlotAxisFlags.NoTickLabels, ImPlotAxisFlags.AutoFit);
            ImPlot.SetupAxisLimits(ImAxis.X1, 0, HISTORY_CAPACITY - 1, ImPlotCond.Always);
            ImPlot.SetupAxisLimits(ImAxis.Y1, 0.0, MathF.Max(25.0f, maxMs * 1.25f), ImPlotCond.Once);

            fixed (float* samplePtr = _orderedPlotBuffer)
            fixed (float* targetPtr = _targetLine)
            {
                ImPlot.PlotLine("60 FPS Target (16.67 ms)", targetPtr, HISTORY_CAPACITY);
                if (_sampleCount > 0)
                {
                    ImPlot.PlotLine("CPU Frame Time", samplePtr, _sampleCount);
                }
            }

            ImPlot.EndPlot();
        }

        ImGui.End();
    }

    private void CopyOrderedSamples()
    {
        if (_sampleCount == 0)
        {
            return;
        }

        var start = _sampleCount < HISTORY_CAPACITY ? 0 : _writeIndex;
        for (var i = 0; i < _sampleCount; i++)
        {
            var srcIdx = (start + i) % HISTORY_CAPACITY;
            _orderedPlotBuffer[i] = _frameTimeHistory[srcIdx];
        }
    }

    private void ComputeStats(out float avgMs, out float minMs, out float maxMs)
    {
        if (_sampleCount == 0)
        {
            avgMs = 0.0f;
            minMs = 0.0f;
            maxMs = 0.0f;
            return;
        }

        var sum = 0.0f;
        minMs = float.MaxValue;
        maxMs = 0.0f;

        for (var i = 0; i < _sampleCount; i++)
        {
            var idx = _sampleCount < HISTORY_CAPACITY ? i : (_writeIndex + i) % HISTORY_CAPACITY;
            var val = _frameTimeHistory[idx];
            sum += val;
            if (val < minMs) { minMs = val; }
            if (val > maxMs) { maxMs = val; }
        }

        avgMs = sum / _sampleCount;
    }
}
