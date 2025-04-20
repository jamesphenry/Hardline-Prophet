// src/HardlineProphet/Core/Interfaces/IGameStateRepository.cs
using HardlineProphet.Core.Models; // Access to GameState
using System.Threading.Tasks; // Required for Task

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
