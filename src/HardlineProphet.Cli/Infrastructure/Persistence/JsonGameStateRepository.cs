// src/HardlineProphet/Infrastructure/Persistence/JsonGameStateRepository.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Interfaces; // IGameStateRepository
using HardlineProphet.Core.Models; // GameState, PlayerStats
using System; // NotImplementedException, Environment
using System.IO; // Path, File, Directory, IOException
using System.Text.Json; // JsonSerializer, JsonException
using System.Threading.Tasks; // Task

namespace HardlineProphet.Infrastructure.Persistence;

/// <summary>
/// Implements game state persistence using JSON files.
/// </summary>
public class JsonGameStateRepository : IGameStateRepository
{
    private readonly string _saveBasePath;
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true, // Be flexible on load
        // Add other options as needed (e.g., converters)
    };


    public JsonGameStateRepository(string? basePath = null)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _saveBasePath = Path.Combine(appDataPath, "HardlineProphet", "Saves");
        }
        else
        {
            _saveBasePath = basePath;
        }
        Directory.CreateDirectory(_saveBasePath);
    }

    private string GetSaveFilePath(string username)
    {
        var sanitizedUsername = string.Join("_", username.Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(sanitizedUsername))
        {
            sanitizedUsername = "default_user";
        }
        Directory.CreateDirectory(_saveBasePath); // Ensure directory exists just before getting path
        return Path.Combine(_saveBasePath, $"{sanitizedUsername}.save.json");
    }

    // Marked async now as it performs actual async I/O
    public async Task<GameState> LoadStateAsync(string username)
    {
        var filePath = GetSaveFilePath(username);
        // Console.WriteLine($"DEBUG: Checking for file at: {filePath}"); // Keep for debugging if needed

        if (!File.Exists(filePath))
        {
            // Console.WriteLine($"DEBUG: File not found. Returning default state.");
            // File doesn't exist, return a new GameState using constants
            var defaultState = new GameState
            { /* ... defaults using GameConstants ... */
                Username = username,
                Level = GameConstants.DefaultStartingLevel,
                Experience = GameConstants.DefaultStartingExperience,
                Credits = GameConstants.DefaultStartingCredits,
                Stats = new PlayerStats { HackSpeed = GameConstants.DefaultStartingHackSpeed, Stealth = GameConstants.DefaultStartingStealth, DataYield = GameConstants.DefaultStartingDataYield },
                Version = GameConstants.CurrentSaveVersion,
            };
            return defaultState; // No need for Task.FromResult when method is async
        }
        else
        {
            // Console.WriteLine($"DEBUG: File found. Reading and deserializing.");
            // File exists, read and deserialize
            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                var loadedState = JsonSerializer.Deserialize<GameState>(json, _serializerOptions);

                if (loadedState == null)
                {
                    // Handle case where JSON is valid but represents null
                    throw new InvalidDataException($"Failed to deserialize save file content for user '{username}'. Deserialized object was null.");
                }
                // TODO: Add checksum validation here later
                // TODO: Add version migration logic here later

                return loadedState;
            }
            catch (JsonException jsonEx)
            {
                // Throw a specific exception type if JSON is invalid
                throw new InvalidDataException($"Failed to parse save file for user '{username}'. Invalid JSON.", jsonEx);
            }
            catch (IOException ioEx)
            {
                // Handle potential file reading errors
                throw new IOException($"Failed to read save file for user '{username}'.", ioEx);
            }
            // Catch other potential exceptions if needed
        }
    }

    public Task SaveStateAsync(GameState gameState)
    {
        // Still needs implementation
        throw new NotImplementedException("SaveStateAsync is not yet implemented.");
    }
}
