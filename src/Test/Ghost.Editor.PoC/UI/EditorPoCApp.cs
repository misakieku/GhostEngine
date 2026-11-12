using Ghost.Editor.PoC.Ecs;
using Ghost.Editor.PoC.Nodes;
using Ghost.Engine.Components;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Misaki.HighPerformance.Mathematics;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Editor.PoC.UI;

public class EditorPoCApp : Component
{
    private static readonly EngineHost s_host = new();

    public override Element Render()
    {
        var host = s_host;
        var (snapshotVer, setSnapshotVer) = UseState(0L, threadSafe: true);

        // Selected entity tracking
        var selectedEntity = host.SelectedEntity ?? host.Entities[0];
        var (selectedGuid, setSelectedGuid) = UseState(selectedEntity.EditorId);

        // Inputs for Transform
        var (posX, setPosX) = UseState(0.0);
        var (posY, setPosY) = UseState(0.0);
        var (posZ, setPosZ) = UseState(0.0);
        var (scaleX, setScaleX) = UseState(1.0);
        var (scaleY, setScaleY) = UseState(1.0);
        var (scaleZ, setScaleZ) = UseState(1.0);

        // Inputs for Counter
        var (inputVal, setInputVal) = UseState(42.0);
        var (inputStep, setInputStep) = UseState(1.0);

        // Poll engine state at 20 Hz
        UseEffect(() =>
        {
            var timer = new System.Threading.Timer(_ =>
            {
                var curVer = host.Bridge.SnapshotVersion;
                setSnapshotVer(curVer);
            }, null, 0, 50);

            return () => timer.Dispose();
        }, Array.Empty<object>());

        // Sync inputs when selected entity changes
        UseEffect(() =>
        {
            var transformComp = selectedEntity.Components.Find(c => c.ComponentType == typeof(LocalTransform));
            if (transformComp != null)
            {
                var posProp = transformComp.Properties.Find(p => p.Name == "position");
                var scaleProp = transformComp.Properties.Find(p => p.Name == "scale");
                if (posProp?.GetValue() is float3 p)
                {
                    setPosX(p.x);
                    setPosY(p.y);
                    setPosZ(p.z);
                }
                if (scaleProp?.GetValue() is float3 s)
                {
                    setScaleX(s.x);
                    setScaleY(s.y);
                    setScaleZ(s.z);
                }
            }

            var counterComp = selectedEntity.Components.Find(c => c.ComponentType == typeof(CounterComponent));
            if (counterComp != null)
            {
                var valProp = counterComp.Properties.Find(p => p.Name == "Value");
                var stepProp = counterComp.Properties.Find(p => p.Name == "Step");
                if (valProp?.GetValue() is int v)
                {
                    setInputVal(v);
                }
                if (stepProp?.GetValue() is int st)
                {
                    setInputStep(st);
                }
            }
        }, [selectedEntity.EditorId]);

        var entityButtons = host.Entities.Select(e =>
        {
            var isSelected = e.EditorId == selectedEntity.EditorId;
            return (Element)Border(HStack(12,
                TextBlock(GetEntityIcon(e.Name)).FontSize(18),
                VStack(2,
                    TextBlock(e.Name).FontSize(14),
                    TextBlock($"{e.Components.Count} Components | ECS ID: {e.EcsEntity.ID}").FontSize(11)
                )
            ))
            .Padding(10)
            .BorderBrush(isSelected ? "#0078D4" : "#40808080")
            .BorderThickness(isSelected ? 2 : 1)
            .CornerRadius(6)
            .OnPointerPressed((_, _) =>
            {
                host.SelectEntity(e);
                setSelectedGuid(e.EditorId);
            });
        }).ToArray();

        var componentCards = selectedEntity.Components.Select(comp => RenderComponentCard(selectedEntity, comp, host,
            posX, posY, posZ, setPosX, setPosY, setPosZ,
            scaleX, scaleY, scaleZ, setScaleX, setScaleY, setScaleZ,
            inputVal, setInputVal, inputStep, setInputStep)).ToArray();

        return VStack(12,
            // ── TOP TOOLBAR ──────────────────────────────────────────────
            Border(HStack(16,
                TextBlock("GhostStudio — Scene Hierarchy & Inspector").FontSize(18),
                TextBlock($"Sim: {(host.IsPaused ? "PAUSED" : "RUNNING @ 60 FPS")} | Frame: {host.FrameCount}"),
                Button(host.IsPaused ? "Resume Sim" : "Pause Sim", () => host.IsPaused = !host.IsPaused),
                Button("Step 1 Frame", () => host.StepSingleFrame())
            )).Padding(12).BorderBrush("#40808080").BorderThickness(1).CornerRadius(8),

            // ── MAIN SPLIT PANELS ────────────────────────────────────────
            HStack(12,
                // ── LEFT: SCENE HIERARCHY ────────────────────────────────
                Border(VStack(8,
                    HStack(8,
                        TextBlock("Scene Hierarchy").FontSize(16),
                        TextBlock($"({host.Entities.Count} entities)")
                    ),
                    VStack(6, entityButtons)
                ))
                .Padding(12).BorderBrush("#40808080").BorderThickness(1).CornerRadius(8).Width(320),

                // ── RIGHT: DYNAMIC COMPONENT INSPECTOR ───────────────────
                Border(VStack(12,
                    // Entity Header
                    Border(HStack(12,
                        TextBlock(GetEntityIcon(selectedEntity.Name)).FontSize(24),
                        VStack(4,
                            HStack(8,
                                TextBlock("Entity Name:").FontSize(13),
                                TextBox(value: selectedEntity.Name, onChanged: newName => selectedEntity.Name = newName)
                                    .OnLostFocus((_, _) => { /* auto-persisted on name edit */ })
                            ),
                            TextBlock($"GUID: {selectedEntity.EditorId} | Handle: Entity(ID={selectedEntity.EcsEntity.ID}, Gen={selectedEntity.EcsEntity.Generation})").FontSize(11)
                        )
                    )).Padding(10).BorderBrush("#40808080").BorderThickness(1).CornerRadius(6),

                    // Components list
                    VStack(10, componentCards)
                ))
                .Padding(12).BorderBrush("#40808080").BorderThickness(1).CornerRadius(8)
            )
        ).Padding(12).IsTabStop(true);
    }

    private static Element RenderComponentCard(
        EntityNode entity, ComponentNode comp, EngineHost host,
        double posX, double posY, double posZ, Action<double> setPosX, Action<double> setPosY, Action<double> setPosZ,
        double scaleX, double scaleY, double scaleZ, Action<double> setScaleX, Action<double> setScaleY, Action<double> setScaleZ,
        double inputVal, Action<double> setInputVal, double inputStep, Action<double> setInputStep)
    {
        if (comp.ComponentType == typeof(LocalTransform))
        {
            return RenderTransformCard(comp, posX, posY, posZ, setPosX, setPosY, setPosZ, scaleX, scaleY, scaleZ, setScaleX, setScaleY, setScaleZ);
        }

        if (comp.ComponentType == typeof(CounterComponent))
        {
            return RenderCounterCard(comp, inputVal, setInputVal, inputStep, setInputStep);
        }

        return RenderGenericCard(comp);
    }

    private static Element RenderTransformCard(
        ComponentNode comp,
        double posX, double posY, double posZ, Action<double> setPosX, Action<double> setPosY, Action<double> setPosZ,
        double scaleX, double scaleY, double scaleZ, Action<double> setScaleX, Action<double> setScaleY, Action<double> setScaleZ)
    {
        var posProp = comp.Properties.Find(p => p.Name == "position");
        var scaleProp = comp.Properties.Find(p => p.Name == "scale");

        var livePos = posProp?.GetValue() is float3 p ? p : float3.zero;
        var liveScale = scaleProp?.GetValue() is float3 s ? s : new float3(1f);

        return Border(VStack(10,
            HStack(8,
                TextBlock("LocalTransform (struct IComponentData)").FontSize(15),
                TextBlock("— Auto-saves on Enter or Lost Focus").FontSize(11).Foreground("#808080")
            ),

            // Live Readout Bar
            Border(HStack(16,
                TextBlock("Live Position:").FontSize(12),
                TextBlock($"X: {livePos.x:F2}").FontSize(12).Foreground("#D83B01"),
                TextBlock($"Y: {livePos.y:F2}").FontSize(12).Foreground("#107C41"),
                TextBlock($"Z: {livePos.z:F2}").FontSize(12).Foreground("#0078D4")
            )).Padding(8).BorderBrush("#40808080").BorderThickness(1).CornerRadius(4),

            // Position Inputs (auto-commit on lost focus & enter)
            HStack(8,
                TextBlock("Pos:").FontSize(12),
                NumberBox(value: posX, onValueChanged: v =>
                {
                    if (double.IsNaN(v)) return;
                    setPosX(v);
                    var cur = posProp?.GetValue() is float3 p ? p : float3.zero;
                    posProp?.SetValue(new float3((float)v, cur.y, cur.z));
                }, header: "X"),
                NumberBox(value: posY, onValueChanged: v =>
                {
                    if (double.IsNaN(v)) return;
                    setPosY(v);
                    var cur = posProp?.GetValue() is float3 p ? p : float3.zero;
                    posProp?.SetValue(new float3(cur.x, (float)v, cur.z));
                }, header: "Y"),
                NumberBox(value: posZ, onValueChanged: v =>
                {
                    if (double.IsNaN(v)) return;
                    setPosZ(v);
                    var cur = posProp?.GetValue() is float3 p ? p : float3.zero;
                    posProp?.SetValue(new float3(cur.x, cur.y, (float)v));
                }, header: "Z")
            ),

            // Scale Inputs (auto-commit on lost focus & enter)
            HStack(8,
                TextBlock("Scale:").FontSize(12),
                NumberBox(value: scaleX, onValueChanged: v =>
                {
                    if (double.IsNaN(v)) return;
                    setScaleX(v);
                    var cur = scaleProp?.GetValue() is float3 s ? s : new float3(1f);
                    scaleProp?.SetValue(new float3((float)v, cur.y, cur.z));
                }, header: "X"),
                NumberBox(value: scaleY, onValueChanged: v =>
                {
                    if (double.IsNaN(v)) return;
                    setScaleY(v);
                    var cur = scaleProp?.GetValue() is float3 s ? s : new float3(1f);
                    scaleProp?.SetValue(new float3(cur.x, (float)v, cur.z));
                }, header: "Y"),
                NumberBox(value: scaleZ, onValueChanged: v =>
                {
                    if (double.IsNaN(v)) return;
                    setScaleZ(v);
                    var cur = scaleProp?.GetValue() is float3 s ? s : new float3(1f);
                    scaleProp?.SetValue(new float3(cur.x, cur.y, (float)v));
                }, header: "Z")
            ),

            // Presets
            HStack(8,
                Button("Copy Live -> Input", () =>
                {
                    setPosX(livePos.x);
                    setPosY(livePos.y);
                    setPosZ(livePos.z);
                    setScaleX(liveScale.x);
                    setScaleY(liveScale.y);
                    setScaleZ(liveScale.z);
                }),
                Button("Reset Pos (0,0,0)", () =>
                {
                    setPosX(0.0);
                    setPosY(0.0);
                    setPosZ(0.0);
                    posProp?.SetValue(float3.zero);
                }),
                Button("Reset Scale (1,1,1)", () =>
                {
                    setScaleX(1.0);
                    setScaleY(1.0);
                    setScaleZ(1.0);
                    scaleProp?.SetValue(new float3(1f));
                })
            )
        )).Padding(12).BorderBrush("#40808080").BorderThickness(1).CornerRadius(6);
    }

    private static Element RenderCounterCard(ComponentNode comp, double inputVal, Action<double> setInputVal, double inputStep, Action<double> setInputStep)
    {
        var valProp = comp.Properties.Find(p => p.Name == "Value");
        var stepProp = comp.Properties.Find(p => p.Name == "Step");
        var autoProp = comp.Properties.Find(p => p.Name == "AutoIncrement");

        var currentVal = valProp?.GetValue() is int v ? v : 0;
        var currentAuto = autoProp?.GetValue() is bool a && a;

        return Border(VStack(10,
            HStack(8,
                TextBlock("CounterComponent (Worker DAG Simulation)").FontSize(15),
                TextBlock("— Auto-saves on Enter or Lost Focus").FontSize(11).Foreground("#808080")
            ),

            Border(HStack(12,
                TextBlock("Live Worker Value:").FontSize(13),
                TextBlock($"{currentVal}").FontSize(22).Foreground("#107C41")
            )).Padding(8).BorderBrush("#40808080").BorderThickness(1).CornerRadius(4),

            HStack(8,
                NumberBox(value: inputVal, onValueChanged: v =>
                {
                    if (double.IsNaN(v)) return;
                    setInputVal(v);
                    valProp?.SetValue((int)v);
                }, header: "Value"),
                NumberBox(value: inputStep, onValueChanged: v =>
                {
                    if (double.IsNaN(v)) return;
                    setInputStep(v);
                    stepProp?.SetValue((int)v);
                }, header: "Step Size")
            ),

            ToggleSwitch(isOn: currentAuto, onIsOnChanged: on => autoProp?.SetValue(on), header: "Worker DAG Auto-Increment"),

            HStack(8,
                Button("Set 0", () => { setInputVal(0); valProp?.SetValue(0); }),
                Button("Set 100", () => { setInputVal(100); valProp?.SetValue(100); }),
                Button("Stress 9999", () => { setInputVal(9999); valProp?.SetValue(9999); })
            )
        )).Padding(12).BorderBrush("#40808080").BorderThickness(1).CornerRadius(6);
    }

    private static Element RenderGenericCard(ComponentNode comp)
    {
        var propRows = comp.Properties.Select(p =>
            (Element)HStack(8,
                TextBlock($"{p.Name}:").FontSize(12),
                TextBlock($"{p.GetValue() ?? "null"}").FontSize(12)
            )
        ).ToArray();

        return Border(VStack(6,
            TextBlock($"{comp.DisplayName} (Tag / Marker Component)").FontSize(14),
            VStack(4, propRows)
        )).Padding(10).BorderBrush("#40808080").BorderThickness(1).CornerRadius(6);
    }

    private static string GetEntityIcon(string name) => name switch
    {
        "Player" => "\uE77B",             // Contact / Person
        "Main Camera" => "\uE714",        // Camera
        "Directional Light" => "\uE706",  // Brightness / Light
        "Counter Worker" => "\uE768",     // Play / Worker
        "Spinning Cube" => "\uE74C",      // Shape / Cube
        _ => "\uE790"                     // Folder / General
    };
}
