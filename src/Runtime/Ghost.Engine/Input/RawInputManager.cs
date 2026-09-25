using Misaki.HighPerformance.Mathematics;
using SDL;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Input;

public sealed unsafe class RawInputManager : IDisposable
{
    private const int KEY_COUNT = 512;
    private const int MAX_GAMEPADS = 4;

    private readonly bool[] _keyboard = new bool[KEY_COUNT];
    private uint _mouseButtonMask;
    private float2 _mouseDelta;
    private float2 _mouseWheel;

    private SDL_Window* _window;

    private readonly SDL_Gamepad*[] _gamepads = new SDL_Gamepad*[MAX_GAMEPADS];
    private readonly Dictionary<SDL_JoystickID, int> _joystickIdToPlayerIndex = new();

    public DeviceType ActiveDevice
    {
        get; private set;
    } = DeviceType.Keyboard;

    public float2 MouseDelta => _mouseDelta;
    public float2 MouseWheel => _mouseWheel;

    private bool _relativeMouseMode = true;

    public bool RelativeMouseMode
    {
        get => _window != null ? SDL3.SDL_GetWindowRelativeMouseMode(_window) : _relativeMouseMode;
        set
        {
            _relativeMouseMode = value;
            if (_window != null)
            {
                SDL3.SDL_SetWindowRelativeMouseMode(_window, value);
            }
        }
    }

    public bool CursorVisible
    {
        get => SDL3.SDL_CursorVisible();
        set
        {
            if (value)
            {
                SDL3.SDL_ShowCursor();
            }
            else
            {
                SDL3.SDL_HideCursor();
            }
        }
    }

    public void AttachWindow(SDL_Window* window)
    {
        _window = window;
        if (_relativeMouseMode && _window != null)
        {
            SDL3.SDL_SetWindowRelativeMouseMode(_window, true);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadButton(InputControl control)
    {
        switch (control.Device)
        {
            case DeviceType.Keyboard:
            {
                var scancode = (int)SDL3.SDL_GetScancodeFromKey((SDL_Keycode)control.ControlId, null);
                return scancode >= 0 && scancode < KEY_COUNT && _keyboard[scancode];
            }
            case DeviceType.Mouse:
            {
                var mask = 1u << (control.ControlId - 1);
                return (_mouseButtonMask & mask) != 0;
            }
            case DeviceType.Gamepad:
            {
                if (control.DeviceIndex >= MAX_GAMEPADS)
                {
                    return false;
                }

                var pad = _gamepads[control.DeviceIndex];
                if (pad == null)
                {
                    return false;
                }

                // If control ID corresponds to trigger axis, treat > 0.5f as button press
                if (control.ControlId == (ushort)SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER ||
                    control.ControlId == (ushort)SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHT_TRIGGER)
                {
                    return ReadAxis(control) > 0.5f;
                }

                return SDL3.SDL_GetGamepadButton(pad, (SDL_GamepadButton)control.ControlId);
            }
            default:
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadAxis(InputControl control, float deadzone = 0.15f)
    {
        switch (control.Device)
        {
            case DeviceType.Gamepad:
            {
                if (control.DeviceIndex >= MAX_GAMEPADS)
                {
                    return 0.0f;
                }

                var pad = _gamepads[control.DeviceIndex];
                if (pad == null)
                {
                    return 0.0f;
                }

                var rawValue = SDL3.SDL_GetGamepadAxis(pad, (SDL_GamepadAxis)control.ControlId);
                var normalized = rawValue / 32767.0f;

                if (math.abs(normalized) < deadzone)
                {
                    return 0.0f;
                }

                return math.clamp(normalized, -1.0f, 1.0f);
            }
            case DeviceType.Mouse:
            {
                return (MouseControl)control.ControlId switch
                {
                    MouseControl.WheelY => _mouseWheel.y,
                    MouseControl.WheelX => _mouseWheel.x,
                    MouseControl.DeltaX => _mouseDelta.x,
                    MouseControl.DeltaY => _mouseDelta.y,
                    _ => 0.0f
                };
            }
            case DeviceType.Keyboard:
            {
                return ReadButton(control) ? 1.0f : 0.0f;
            }
            default:
                return 0.0f;
        }
    }

    public void ProcessEvent(SDL_Event e)
    {
        switch (e.Type)
        {
            case SDL_EventType.SDL_EVENT_KEY_DOWN:
            {
                ActiveDevice = DeviceType.Keyboard;
                var scancode = (int)e.key.scancode;
                if (scancode >= 0 && scancode < KEY_COUNT)
                {
                    _keyboard[scancode] = true;
                }
                break;
            }
            case SDL_EventType.SDL_EVENT_KEY_UP:
            {
                var scancode = (int)e.key.scancode;
                if (scancode >= 0 && scancode < KEY_COUNT)
                {
                    _keyboard[scancode] = false;
                }
                break;
            }
            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
            {
                ActiveDevice = DeviceType.Mouse;
                _mouseButtonMask |= 1u << (e.button.button - 1);
                break;
            }
            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
            {
                _mouseButtonMask &= ~(1u << (e.button.button - 1));
                break;
            }
            case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
            {
                var delta = new float2(e.motion.xrel, e.motion.yrel);
                _mouseDelta += delta;
                ActiveDevice = DeviceType.Mouse;
                break;
            }
            case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
            {
                ActiveDevice = DeviceType.Mouse;
                _mouseWheel += new float2(e.wheel.x, e.wheel.y);
                break;
            }
            case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
            {
                var instanceId = e.gdevice.which;
                var gamepad = SDL3.SDL_OpenGamepad(instanceId);
                if (gamepad != null)
                {
                    for (var i = 0; i < MAX_GAMEPADS; i++)
                    {
                        if (_gamepads[i] == null)
                        {
                            _gamepads[i] = gamepad;
                            _joystickIdToPlayerIndex[instanceId] = i;
                            break;
                        }
                    }
                }
                break;
            }
            case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
            {
                var instanceId = e.gdevice.which;
                if (_joystickIdToPlayerIndex.Remove(instanceId, out var playerIndex))
                {
                    if (_gamepads[playerIndex] != null)
                    {
                        SDL3.SDL_CloseGamepad(_gamepads[playerIndex]);
                        _gamepads[playerIndex] = null;
                    }

                    if (ActiveDevice == DeviceType.Gamepad)
                    {
                        ActiveDevice = DeviceType.Keyboard;
                    }
                }
                break;
            }
            case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
            {
                ActiveDevice = DeviceType.Gamepad;
                break;
            }
            case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
            {
                if (Math.Abs(e.gaxis.value) > 8000)
                {
                    ActiveDevice = DeviceType.Gamepad;
                }
                break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndFrame()
    {
        _mouseDelta = float2.zero;
        _mouseWheel = float2.zero;
    }

    public void Dispose()
    {
        for (var i = 0; i < MAX_GAMEPADS; i++)
        {
            if (_gamepads[i] != null)
            {
                SDL3.SDL_CloseGamepad(_gamepads[i]);
                _gamepads[i] = null;
            }
        }
        _joystickIdToPlayerIndex.Clear();
    }
}
