namespace Ghost.Engine.Input;

public interface IInputHandler
{
    void ProcessEvent(SDL.SDL_Event e);
}
