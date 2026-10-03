namespace Ghost.Engine.Input;

/// <summary>
/// Defines an interface for handling input events in the engine. Classes that implement this interface can process SDL events, allowing them to respond to user input such as keyboard presses, mouse movements, and other interactions.
/// The interface provides a method for processing SDL events, enabling the implementing class to define custom behavior based on the received input events.
/// </summary>
public interface IInputHandler
{
    /// <summary>
    /// Processes an SDL event. This method is called whenever an SDL event occurs, allowing the implementing class to handle input events such as keyboard presses, mouse movements, and other user interactions.
    /// </summary>
    /// <param name="e">The SDL event to process.</param>
    void ProcessEvent(SDL.SDL_Event e);
}
