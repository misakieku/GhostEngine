using SDL;

namespace Ghost.Engine.Input;

public class InputProfileDatabase
{
    public const int FIRST_PERSON_CAMERA_PROFILE_ID = 1;

    private readonly Dictionary<int, InputMap> _profiles = new();

    public InputProfileDatabase()
    {
        RegisterProfile(FIRST_PERSON_CAMERA_PROFILE_ID, CreateDefaultFirstPersonCameraMap());
    }

    public void RegisterProfile(int profileId, InputMap map)
    {
        _profiles[profileId] = map;
    }

    public InputMap? GetProfile(int profileId)
    {
        _profiles.TryGetValue(profileId, out var map);
        return map;
    }

    public static InputMap CreateDefaultFirstPersonCameraMap()
    {
        var map = new InputMap();

        // 1. Move 2D: WASD + Gamepad Left Stick
        map.Composites2D.Add(new Composite2DBinding
        {
            AxisX = ActionAxis.MoveX,
            AxisY = ActionAxis.MoveY,
            Up = InputControl.Key(SDL_Keycode.SDLK_W),
            Down = InputControl.Key(SDL_Keycode.SDLK_S),
            Left = InputControl.Key(SDL_Keycode.SDLK_A),
            Right = InputControl.Key(SDL_Keycode.SDLK_D)
        });

        map.Sticks2D.Add(new Axis2DStickBinding
        {
            AxisX = ActionAxis.MoveX,
            AxisY = ActionAxis.MoveY,
            StickX = InputControl.GamepadAxis(SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX),
            StickY = InputControl.GamepadAxis(SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY),
            InvertX = false,
            InvertY = true, // In SDL, stick up is negative Y
            Deadzone = 0.15f
        });

        // 2. Elevation: E (Up) / Q (Down)
        map.Axes1D.Add(new Axis1DBinding
        {
            Axis = ActionAxis.Elevation,
            Positive = InputControl.Key(SDL_Keycode.SDLK_E),
            Negative = InputControl.Key(SDL_Keycode.SDLK_Q)
        });

        // 3. Look: Mouse Delta + Gamepad Right Stick
        map.MouseLook = new MouseDeltaBinding
        {
            AxisX = ActionAxis.LookX,
            AxisY = ActionAxis.LookY,
            Sensitivity = 1.0f
        };

        map.Sticks2D.Add(new Axis2DStickBinding
        {
            AxisX = ActionAxis.LookX,
            AxisY = ActionAxis.LookY,
            StickX = InputControl.GamepadAxis(SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTX),
            StickY = InputControl.GamepadAxis(SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTY),
            InvertX = false,
            InvertY = false,
            Deadzone = 0.15f
        });

        // 4. Sprint: Left Shift / Gamepad Left Stick click
        map.Buttons.Add(new ButtonBinding
        {
            Action = ActionButton.Sprint,
            Primary = InputControl.Key(SDL_Keycode.SDLK_LSHIFT),
            Secondary = InputControl.GamepadBtn(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK)
        });

        // 5. Toggle Mouse Lock: Escape
        map.Buttons.Add(new ButtonBinding
        {
            Action = ActionButton.ToggleMouseLock,
            Primary = InputControl.Key(SDL_Keycode.SDLK_ESCAPE),
            Secondary = InputControl.GamepadBtn(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START)
        });

        return map;
    }
}
