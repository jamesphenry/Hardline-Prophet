// src/HardlineProphet/Infrastructure/Persistence/JsonGameStateRepository.cs
using HardlineProphet.Core.Interfaces; // IGameStateRepository
using HardlineProphet.Core.Models; // GameState, PlayerStats
using System; // NotImplementedException
using System.IO; // Path, File, Directory
using System.Threading.Tasks; // Task

namespace HardlineProphet.Infrastructure.Persistence;

/// <summary>
/// Implements game state persistence using JSON files.
/// </summary>
public class JsonGameStateRepository : IGameStateRepository
{
    // TODO: Inject configuration for the base save path instead of hardcoding.
    private readonly string _saveBasePath;

    public JsonGameStateRepository()
    {
        // For now, save in a subdirectory of the user's local app data folder.
        // This is a common place to store application data per user.
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _saveBasePath = Path.Combine(appDataPath, "HardlineProphet", "Saves");

        // Ensure the base directory exists
        Directory.CreateDirectory(_saveBasePath);
    }


    /// <summary>
    /// Gets the full path for a user's save file.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <returns>The full path to the save file.</returns>
    private string GetSaveFilePath(string username)
    {
        // Basic sanitization to prevent path traversal issues, replace invalid chars.
        // A more robust sanitization might be needed depending on allowed usernames.
        var sanitizedUsername = string.Join("_", username.Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(sanitizedUsername))
        {
            sanitizedUsername = "default_user"; // Handle empty/invalid names
        }
        return Path.Combine(_saveBasePath, $"{sanitizedUsername}.save.json");
    }

    public Task<GameState> LoadStateAsync(string username)
    {
        var filePath = GetSaveFilePath(username);

        if (!File.Exists(filePath))
        {
            // File doesn't exist, return a new GameState with defaults
            var defaultState = new GameState
            {
                Username = username,
                Level = 1, // Default level 1
                Experience = 0.0,
                Credits = 100, // Default 100 credits
                Stats = new PlayerStats // Default stats
                {
                    HackSpeed = 5,
                    Stealth = 5,
                    DataYield = 0
                },
                Version = 2, // Current version
                // ActiveMissionIds and UnlockedPerkIds default to empty lists
                // Checksum defaults to null
                // IsDevSave defaults to false
            };
            return Task.FromResult(defaultState);
        }
        else
        {
            // File *does* exist - we haven't implemented loading it yet!
            // This part will be handled by the next test(s).
            throw new NotImplementedException("Loading existing save files is not yet implemented.");
        }
    }

    public Task SaveStateAsync(GameState gameState)
    {
        // Minimal implementation.
        throw new NotImplementedException("SaveStateAsync is not yet implemented.");
    }

    // We will add methods for serialization, checksums etc. here later.
}
