// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-feature-m2-flavor-events.1+9
// ║ 📄  File: IGameStateRepository.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
using System.Threading.Tasks; // Required for Task
using HardlineProphet.Core.Models; // Access to GameState

namespace HardlineProphet.Core.Interfaces;

/// <summary>
/// Defines the contract for loading and saving game state data.
/// </summary>
public interface IGameStateRepository
{
    /// <summary>
    /// Loads the game state for the specified username.
    /// If no state exists for the user, returns a new GameState with default values.
    /// </summary>
    /// <param name="username">The username to load the state for.</param>
    /// <returns>The loaded or new GameState.</returns>
    /// <exception cref="System.IO.InvalidDataException">Thrown if the save file is found but corrupted (e.g., checksum mismatch).</exception>
    Task<GameState> LoadStateAsync(string username);

    /// <summary>
    /// Saves the provided game state.
    /// The username within the GameState object determines the save file name.
    /// </summary>
    /// <param name="gameState">The game state to save.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveStateAsync(GameState gameState);
}
