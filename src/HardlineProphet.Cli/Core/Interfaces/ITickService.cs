// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-alpha.12
// ║ 📄  File: ITickService.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
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
