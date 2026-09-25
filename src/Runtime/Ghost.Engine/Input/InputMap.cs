using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.Input;

public enum ActionButton : byte
{
    Primary = 0,
    Secondary = 1,
    Jump = 2,
    Sprint = 3,
    Crouch = 4,
    Interact = 5,
    ToggleMouseLock = 6,
    Pause = 7,
    Menu = 8,
}

public enum ActionAxis : byte
{
    MoveX = 0,
    MoveY = 1,
    Elevation = 2,
    LookX = 3,
    LookY = 4,
    Zoom = 5,
    Throttle = 6,
    Steer = 7,
}

public struct ButtonBinding
{
    public ActionButton Action;
    public InputControl Primary;
    public InputControl Secondary;
}

public struct Axis1DBinding
{
    public ActionAxis Axis;
    public InputControl Positive;
    public InputControl Negative;
}

public struct Composite2DBinding
{
    public ActionAxis AxisX;
    public ActionAxis AxisY;
    public InputControl Up;
    public InputControl Down;
    public InputControl Left;
    public InputControl Right;
}

public struct Axis2DStickBinding
{
    public ActionAxis AxisX;
    public ActionAxis AxisY;
    public InputControl StickX;
    public InputControl StickY;
    public bool InvertX;
    public bool InvertY;
    public float Deadzone;
}

public struct MouseDeltaBinding
{
    public ActionAxis AxisX;
    public ActionAxis AxisY;
    public float Sensitivity;
}

public class InputMap
{
    public readonly List<ButtonBinding> Buttons = new();
    public readonly List<Axis1DBinding> Axes1D = new();
    public readonly List<Composite2DBinding> Composites2D = new();
    public readonly List<Axis2DStickBinding> Sticks2D = new();
    public MouseDeltaBinding? MouseLook;
}
