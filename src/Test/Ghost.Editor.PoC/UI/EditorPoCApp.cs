using Ghost.Editor.PoC.Ecs;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Editor.PoC.UI;

public class EditorPoCApp : Component
{
    private static readonly EngineHost s_host = new();

    public override Element Render()
    {
        var host = s_host;
        var (snapshotVer, setSnapshotVer) = UseState(0L, threadSafe: true);
        var (inputVal, setInputVal) = UseState(42.0);
        var (inputStep, setInputStep) = UseState(1.0);

        UseEffect(() =>
        {
            var timer = new System.Threading.Timer(_ =>
            {
                var curVer = host.Bridge.SnapshotVersion;
                setSnapshotVer(curVer);
            }, null, 0, 50);

            return () => timer.Dispose();
        }, Array.Empty<object>());

        var entityNode = host.CounterEntityNode;
        var compNode = entityNode.Components[0];

        var valProp = compNode.Properties.Find(p => p.Name == "Value");
        var stepProp = compNode.Properties.Find(p => p.Name == "Step");
        var autoProp = compNode.Properties.Find(p => p.Name == "AutoIncrement");

        var currentVal = valProp?.GetValue() is int v ? v : 0;
        var currentStep = stepProp?.GetValue() is int s ? s : 1;
        var currentAuto = autoProp?.GetValue() is bool a && a;

        return VStack(16,
            // Top Toolbar
            Border(HStack(16,
                TextBlock("GhostStudio — Minimal PoC (Bidirectional ECS Sync)").FontSize(18),
                TextBlock($"Frame: {host.FrameCount} | Snapshots: {snapshotVer}"),
                Button(host.IsPaused ? "Resume Sim" : "Pause Sim", () => host.IsPaused = !host.IsPaused),
                Button("Step 1 Frame", () => host.StepSingleFrame())
            )).Padding(12),

            // Main Editor Panels
            HStack(16,
                // Left Panel: Entity Hierarchy Node
                Border(VStack(8,
                    TextBlock("Scene Hierarchy").FontSize(16),
                    TextBlock($"Node: {entityNode.Name}"),
                    TextBlock($"EditorId: {entityNode.EditorId.ToString().Substring(0, 8)}..."),
                    TextBlock($"ECS Entity: ID={entityNode.EcsEntity.ID}, Gen={entityNode.EcsEntity.Generation}"),
                    TextBlock($"Components Count: {entityNode.Components.Count}")
                )).Padding(16),

                // Right Panel: Component Inspector Node
                Border(VStack(12,
                    TextBlock("Component Inspector (Reflected 3-Layer Proxy)").FontSize(16),
                    TextBlock($"Component: {compNode.DisplayName}"),

                    // Live readout display
                    Border(HStack(12,
                        TextBlock("Live ECS Value (from Worker Thread):").FontSize(14),
                        TextBlock($"{currentVal}").FontSize(22)
                    )).Padding(8),

                    // Value Property
                    HStack(8,
                        NumberBox(value: inputVal, onValueChanged: v => setInputVal(v), header: "Set Value"),
                        Button("Apply Value to ECS", () => valProp?.SetValue((int)inputVal))
                    ),

                    // Step Property
                    HStack(8,
                        NumberBox(value: inputStep, onValueChanged: v => setInputStep(v), header: "Step Size"),
                        Button("Apply Step to ECS", () => stepProp?.SetValue((int)inputStep))
                    ),

                    // AutoIncrement Property
                    ToggleSwitch(isOn: currentAuto, onIsOnChanged: on => autoProp?.SetValue(on), header: "Worker DAG Auto-Increment"),

                    // Presets & Stress Test
                    HStack(8,
                        Button("Set to 0", () => valProp?.SetValue(0)),
                        Button("Set to 100", () => valProp?.SetValue(100)),
                        Button("Stress (Set 9999)", () => valProp?.SetValue(9999))
                    )
                )).Padding(16)
            )
        ).Padding(16);
    }
}
