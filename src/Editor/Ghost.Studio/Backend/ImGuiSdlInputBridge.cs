using Ghost.Engine.Input;
using Hexa.NET.ImGui;
using SDL;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Ghost.Studio.Backend;

internal sealed unsafe class ImGuiSdlInputBridge : IInputHandler
{
    private readonly ImGuiContextManager _contextManager;
    private int _windowWidth;
    private int _windowHeight;

    public ImGuiSdlInputBridge(ImGuiContextManager contextManager, int initialWidth, int initialHeight)
    {
        _contextManager = contextManager;
        _windowWidth = Math.Max(1, initialWidth);
        _windowHeight = Math.Max(1, initialHeight);
    }

    public void BeginFrame(float deltaTime)
    {
        _contextManager.MakeCurrent();
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(_windowWidth, _windowHeight);
        io.DisplayFramebufferScale = Vector2.One;
        io.DeltaTime = deltaTime > 0.0f ? deltaTime : (1.0f / 60.0f);
    }

    public void ProcessEvent(SDL_Event e)
    {
        _contextManager.MakeCurrent();
        var io = ImGui.GetIO();

        switch (e.Type)
        {
            case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
            case SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
            {
                if (e.window.data1 > 0 && e.window.data2 > 0)
                {
                    _windowWidth = e.window.data1;
                    _windowHeight = e.window.data2;
                }
                break;
            }
            case SDL_EventType.SDL_EVENT_WINDOW_SHOWN:
            case SDL_EventType.SDL_EVENT_WINDOW_DISPLAY_SCALE_CHANGED:
            {
                var window = SDL3.SDL_GetWindowFromID(e.window.windowID);
                if (window != null)
                {
                    var scale = SDL3.SDL_GetWindowDisplayScale(window);
                    if (scale > 0.0f)
                    {
                        _contextManager.SetDpiScale(scale);
                    }
                }
                break;
            }
            case SDL_EventType.SDL_EVENT_DISPLAY_CONTENT_SCALE_CHANGED:
            {
                var primary = SDL3.SDL_GetPrimaryDisplay();
                var scale = primary != 0 ? SDL3.SDL_GetDisplayContentScale(primary) : 1.0f;
                if (scale > 0.0f)
                {
                    _contextManager.SetDpiScale(scale);
                }
                break;
            }
            case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
            {
                io.AddMousePosEvent(e.motion.x, e.motion.y);
                break;
            }
            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
            {
                var mouseButton = MapMouseButton(e.button.button);
                if (mouseButton >= 0)
                {
                    io.AddMouseButtonEvent(mouseButton, e.Type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN);
                }
                break;
            }
            case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
            {
                io.AddMouseWheelEvent(e.wheel.x, e.wheel.y);
                break;
            }
            case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
            {
                io.AddMousePosEvent(-float.MaxValue, -float.MaxValue);
                break;
            }
            case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
            {
                io.AddFocusEvent(true);
                break;
            }
            case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
            {
                io.AddFocusEvent(false);
                break;
            }
            case SDL_EventType.SDL_EVENT_TEXT_INPUT:
            {
                var text = Marshal.PtrToStringUTF8((nint)e.text.text);
                if (!string.IsNullOrEmpty(text))
                {
                    io.AddInputCharactersUTF8(text);
                }
                break;
            }
            case SDL_EventType.SDL_EVENT_KEY_DOWN:
            case SDL_EventType.SDL_EVENT_KEY_UP:
            {
                UpdateModifierKeys(io, (SDL_Keymod)e.key.mod);
                var imguiKey = MapScancodeToImGuiKey(e.key.scancode);
                if (imguiKey != ImGuiKey.None)
                {
                    io.AddKeyEvent(imguiKey, e.Type == SDL_EventType.SDL_EVENT_KEY_DOWN);
                }
                break;
            }
        }
    }

    private static int MapMouseButton(byte button)
    {
        return button switch
        {
            1 => 0,
            3 => 1,
            2 => 2,
            4 => 3,
            5 => 4,
            _ => -1,
        };
    }

    private static void UpdateModifierKeys(ImGuiIOPtr io, SDL_Keymod mods)
    {
        io.AddKeyEvent(ImGuiKey.ModCtrl, (mods & SDL_Keymod.SDL_KMOD_CTRL) != 0);
        io.AddKeyEvent(ImGuiKey.ModShift, (mods & SDL_Keymod.SDL_KMOD_SHIFT) != 0);
        io.AddKeyEvent(ImGuiKey.ModAlt, (mods & SDL_Keymod.SDL_KMOD_ALT) != 0);
        io.AddKeyEvent(ImGuiKey.ModSuper, (mods & SDL_Keymod.SDL_KMOD_GUI) != 0);
    }

    private static ImGuiKey MapScancodeToImGuiKey(SDL_Scancode scancode)
    {
        return scancode switch
        {
            SDL_Scancode.SDL_SCANCODE_TAB => ImGuiKey.Tab,
            SDL_Scancode.SDL_SCANCODE_LEFT => ImGuiKey.LeftArrow,
            SDL_Scancode.SDL_SCANCODE_RIGHT => ImGuiKey.RightArrow,
            SDL_Scancode.SDL_SCANCODE_UP => ImGuiKey.UpArrow,
            SDL_Scancode.SDL_SCANCODE_DOWN => ImGuiKey.DownArrow,
            SDL_Scancode.SDL_SCANCODE_PAGEUP => ImGuiKey.PageUp,
            SDL_Scancode.SDL_SCANCODE_PAGEDOWN => ImGuiKey.PageDown,
            SDL_Scancode.SDL_SCANCODE_HOME => ImGuiKey.Home,
            SDL_Scancode.SDL_SCANCODE_END => ImGuiKey.End,
            SDL_Scancode.SDL_SCANCODE_INSERT => ImGuiKey.Insert,
            SDL_Scancode.SDL_SCANCODE_DELETE => ImGuiKey.Delete,
            SDL_Scancode.SDL_SCANCODE_BACKSPACE => ImGuiKey.Backspace,
            SDL_Scancode.SDL_SCANCODE_SPACE => ImGuiKey.Space,
            SDL_Scancode.SDL_SCANCODE_RETURN => ImGuiKey.Enter,
            SDL_Scancode.SDL_SCANCODE_ESCAPE => ImGuiKey.Escape,
            SDL_Scancode.SDL_SCANCODE_APOSTROPHE => ImGuiKey.Apostrophe,
            SDL_Scancode.SDL_SCANCODE_COMMA => ImGuiKey.Comma,
            SDL_Scancode.SDL_SCANCODE_MINUS => ImGuiKey.Minus,
            SDL_Scancode.SDL_SCANCODE_PERIOD => ImGuiKey.Period,
            SDL_Scancode.SDL_SCANCODE_SLASH => ImGuiKey.Slash,
            SDL_Scancode.SDL_SCANCODE_SEMICOLON => ImGuiKey.Semicolon,
            SDL_Scancode.SDL_SCANCODE_EQUALS => ImGuiKey.Equal,
            SDL_Scancode.SDL_SCANCODE_LEFTBRACKET => ImGuiKey.LeftBracket,
            SDL_Scancode.SDL_SCANCODE_BACKSLASH => ImGuiKey.Backslash,
            SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET => ImGuiKey.RightBracket,
            SDL_Scancode.SDL_SCANCODE_GRAVE => ImGuiKey.GraveAccent,
            SDL_Scancode.SDL_SCANCODE_CAPSLOCK => ImGuiKey.CapsLock,
            SDL_Scancode.SDL_SCANCODE_SCROLLLOCK => ImGuiKey.ScrollLock,
            SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR => ImGuiKey.NumLock,
            SDL_Scancode.SDL_SCANCODE_PRINTSCREEN => ImGuiKey.PrintScreen,
            SDL_Scancode.SDL_SCANCODE_PAUSE => ImGuiKey.Pause,
            SDL_Scancode.SDL_SCANCODE_KP_0 => ImGuiKey.Keypad0,
            SDL_Scancode.SDL_SCANCODE_KP_1 => ImGuiKey.Keypad1,
            SDL_Scancode.SDL_SCANCODE_KP_2 => ImGuiKey.Keypad2,
            SDL_Scancode.SDL_SCANCODE_KP_3 => ImGuiKey.Keypad3,
            SDL_Scancode.SDL_SCANCODE_KP_4 => ImGuiKey.Keypad4,
            SDL_Scancode.SDL_SCANCODE_KP_5 => ImGuiKey.Keypad5,
            SDL_Scancode.SDL_SCANCODE_KP_6 => ImGuiKey.Keypad6,
            SDL_Scancode.SDL_SCANCODE_KP_7 => ImGuiKey.Keypad7,
            SDL_Scancode.SDL_SCANCODE_KP_8 => ImGuiKey.Keypad8,
            SDL_Scancode.SDL_SCANCODE_KP_9 => ImGuiKey.Keypad9,
            SDL_Scancode.SDL_SCANCODE_KP_PERIOD => ImGuiKey.KeypadDecimal,
            SDL_Scancode.SDL_SCANCODE_KP_DIVIDE => ImGuiKey.KeypadDivide,
            SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY => ImGuiKey.KeypadMultiply,
            SDL_Scancode.SDL_SCANCODE_KP_MINUS => ImGuiKey.KeypadSubtract,
            SDL_Scancode.SDL_SCANCODE_KP_PLUS => ImGuiKey.KeypadAdd,
            SDL_Scancode.SDL_SCANCODE_KP_ENTER => ImGuiKey.KeypadEnter,
            SDL_Scancode.SDL_SCANCODE_KP_EQUALS => ImGuiKey.KeypadEqual,
            SDL_Scancode.SDL_SCANCODE_LCTRL => ImGuiKey.LeftCtrl,
            SDL_Scancode.SDL_SCANCODE_LSHIFT => ImGuiKey.LeftShift,
            SDL_Scancode.SDL_SCANCODE_LALT => ImGuiKey.LeftAlt,
            SDL_Scancode.SDL_SCANCODE_LGUI => ImGuiKey.LeftSuper,
            SDL_Scancode.SDL_SCANCODE_RCTRL => ImGuiKey.RightCtrl,
            SDL_Scancode.SDL_SCANCODE_RSHIFT => ImGuiKey.RightShift,
            SDL_Scancode.SDL_SCANCODE_RALT => ImGuiKey.RightAlt,
            SDL_Scancode.SDL_SCANCODE_RGUI => ImGuiKey.RightSuper,
            SDL_Scancode.SDL_SCANCODE_APPLICATION => ImGuiKey.Menu,
            SDL_Scancode.SDL_SCANCODE_0 => ImGuiKey.Key0,
            SDL_Scancode.SDL_SCANCODE_1 => ImGuiKey.Key1,
            SDL_Scancode.SDL_SCANCODE_2 => ImGuiKey.Key2,
            SDL_Scancode.SDL_SCANCODE_3 => ImGuiKey.Key3,
            SDL_Scancode.SDL_SCANCODE_4 => ImGuiKey.Key4,
            SDL_Scancode.SDL_SCANCODE_5 => ImGuiKey.Key5,
            SDL_Scancode.SDL_SCANCODE_6 => ImGuiKey.Key6,
            SDL_Scancode.SDL_SCANCODE_7 => ImGuiKey.Key7,
            SDL_Scancode.SDL_SCANCODE_8 => ImGuiKey.Key8,
            SDL_Scancode.SDL_SCANCODE_9 => ImGuiKey.Key9,
            SDL_Scancode.SDL_SCANCODE_A => ImGuiKey.A,
            SDL_Scancode.SDL_SCANCODE_B => ImGuiKey.B,
            SDL_Scancode.SDL_SCANCODE_C => ImGuiKey.C,
            SDL_Scancode.SDL_SCANCODE_D => ImGuiKey.D,
            SDL_Scancode.SDL_SCANCODE_E => ImGuiKey.E,
            SDL_Scancode.SDL_SCANCODE_F => ImGuiKey.F,
            SDL_Scancode.SDL_SCANCODE_G => ImGuiKey.G,
            SDL_Scancode.SDL_SCANCODE_H => ImGuiKey.H,
            SDL_Scancode.SDL_SCANCODE_I => ImGuiKey.I,
            SDL_Scancode.SDL_SCANCODE_J => ImGuiKey.J,
            SDL_Scancode.SDL_SCANCODE_K => ImGuiKey.K,
            SDL_Scancode.SDL_SCANCODE_L => ImGuiKey.L,
            SDL_Scancode.SDL_SCANCODE_M => ImGuiKey.M,
            SDL_Scancode.SDL_SCANCODE_N => ImGuiKey.N,
            SDL_Scancode.SDL_SCANCODE_O => ImGuiKey.O,
            SDL_Scancode.SDL_SCANCODE_P => ImGuiKey.P,
            SDL_Scancode.SDL_SCANCODE_Q => ImGuiKey.Q,
            SDL_Scancode.SDL_SCANCODE_R => ImGuiKey.R,
            SDL_Scancode.SDL_SCANCODE_S => ImGuiKey.S,
            SDL_Scancode.SDL_SCANCODE_T => ImGuiKey.T,
            SDL_Scancode.SDL_SCANCODE_U => ImGuiKey.U,
            SDL_Scancode.SDL_SCANCODE_V => ImGuiKey.V,
            SDL_Scancode.SDL_SCANCODE_W => ImGuiKey.W,
            SDL_Scancode.SDL_SCANCODE_X => ImGuiKey.X,
            SDL_Scancode.SDL_SCANCODE_Y => ImGuiKey.Y,
            SDL_Scancode.SDL_SCANCODE_Z => ImGuiKey.Z,
            SDL_Scancode.SDL_SCANCODE_F1 => ImGuiKey.F1,
            SDL_Scancode.SDL_SCANCODE_F2 => ImGuiKey.F2,
            SDL_Scancode.SDL_SCANCODE_F3 => ImGuiKey.F3,
            SDL_Scancode.SDL_SCANCODE_F4 => ImGuiKey.F4,
            SDL_Scancode.SDL_SCANCODE_F5 => ImGuiKey.F5,
            SDL_Scancode.SDL_SCANCODE_F6 => ImGuiKey.F6,
            SDL_Scancode.SDL_SCANCODE_F7 => ImGuiKey.F7,
            SDL_Scancode.SDL_SCANCODE_F8 => ImGuiKey.F8,
            SDL_Scancode.SDL_SCANCODE_F9 => ImGuiKey.F9,
            SDL_Scancode.SDL_SCANCODE_F10 => ImGuiKey.F10,
            SDL_Scancode.SDL_SCANCODE_F11 => ImGuiKey.F11,
            SDL_Scancode.SDL_SCANCODE_F12 => ImGuiKey.F12,
            _ => ImGuiKey.None,
        };
    }
}
