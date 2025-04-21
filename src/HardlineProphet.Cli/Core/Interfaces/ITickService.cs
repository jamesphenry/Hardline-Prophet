// src/HardlineProphet/Core/Interfaces/ITickService.cs
namespace HardlineProphet.Core.Interfaces;

/// <summary>
/// Manages the game's core tick loop responsible for idle progress.
/// </summary>
public interface ITickService
{
    /// <summary>
    /// Gets a value indicating whether the tick service is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the tick loop. Requires initialization first (via constructor).
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the tick loop.
    /// </summary>
    void Stop();
}
