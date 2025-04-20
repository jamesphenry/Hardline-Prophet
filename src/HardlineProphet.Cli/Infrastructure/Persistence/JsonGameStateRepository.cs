// src/HardlineProphet/Infrastructure/Persistence/JsonGameStateRepository.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Interfaces; // IGameStateRepository
using HardlineProphet.Core.Models; // GameState, PlayerStats
using System; // NotImplementedException, Environment
using System.IO; // Path, File, Directory, IOException
using System.Text.Json; // JsonSerializer, JsonException, JsonSerializerOptions
using System.Threading.Tasks; // Task

namespace HardlineProphet.Infrastructure.Persistence;

/// <summary>
/// Implements game state persistence using JSON files.
/// </summary>
public class JsonGameStateRepository : IGameStateRepository
{
    private readonly string _saveBasePath;
    // Options for deserialization (loading)
    private static readonly JsonSerializerOptions _deserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    // Options for serialization (saving) - using WriteIndented for readability
    private static readonly JsonSerializerOptions _serializeOptions = new()
    {
        WriteIndented = true,
        // PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Optional: if needed
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
        if (string.IsNullOrWhiteSpace(sanitizedUsername)) { sanitizedUsername = "default_user"; }
        Directory.CreateDirectory(_saveBasePath);
        return Path.Combine(_saveBasePath, $"{sanitizedUsername}.save.json");
    }

    public async Task<GameState> LoadStateAsync(string username)
    {
        var filePath = GetSaveFilePath(username);

        if (!File.Exists(filePath))
        {
            var defaultState = new GameState
            { /* ... defaults ... */
                Username = username,
                Level = GameConstants.DefaultStartingLevel,
                Experience = GameConstants.DefaultStartingExperience,
                Credits = GameConstants.DefaultStartingCredits,
                Stats = new PlayerStats { HackSpeed = GameConstants.DefaultStartingHackSpeed, Stealth = GameConstants.DefaultStartingStealth, DataYield = GameConstants.DefaultStartingDataYield },
                Version = GameConstants.CurrentSaveVersion,
            };
            return defaultState;
        }
        else
        {
            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                var loadedState = JsonSerializer.Deserialize<GameState>(json, _deserializeOptions);

                if (loadedState == null)
                {
                    throw new InvalidDataException($"Failed to deserialize save file content for user '{username}'. Deserialized object was null.");
                }
                // TODO: Add checksum validation here
                // TODO: Add version migration logic here

                return loadedState;
            }
            catch (JsonException jsonEx)
            {
                throw new InvalidDataException($"Failed to parse save file for user '{username}'. Invalid JSON.", jsonEx);
            }
            catch (IOException ioEx)
            {
                throw new IOException($"Failed to read save file for user '{username}'.", ioEx);
            }
        }
    }

    // Implement the Save method
    public async Task SaveStateAsync(GameState gameState)
    {
        // Ensure gameState and username are valid before proceeding
        if (gameState == null) throw new ArgumentNullException(nameof(gameState));
        if (string.IsNullOrWhiteSpace(gameState.Username)) throw new ArgumentException("GameState must have a valid Username to save.", nameof(gameState));

        var filePath = GetSaveFilePath(gameState.Username);

        // TODO: Calculate and set checksum on gameState *before* serializing later
        // gameState.Checksum = ComputeChecksum(gameState); // Example placeholder

        try
        {
            // Serialize the state to JSON
            string json = JsonSerializer.Serialize(gameState, _serializeOptions);

            // Write the JSON to the file asynchronously, overwriting if it exists
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (JsonException jsonEx)
        {
            // Handle potential errors during serialization
            // Consider logging this error
            throw new Exception($"Failed to serialize game state for user '{gameState.Username}'.", jsonEx); // Or a custom exception type
        }
        catch (IOException ioEx)
        {
            // Handle potential errors during file writing
            // Consider logging this error
            throw new IOException($"Failed to write save file for user '{gameState.Username}'.", ioEx);
        }
        // Catch other potential exceptions if needed
    }

    // TODO: Implement checksum logic later
    // private string ComputeChecksum(GameState state) { ... }
}
