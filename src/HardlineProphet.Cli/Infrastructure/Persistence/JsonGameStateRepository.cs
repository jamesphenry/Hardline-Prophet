// src/HardlineProphet/Infrastructure/Persistence/JsonGameStateRepository.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Interfaces; // IGameStateRepository
using HardlineProphet.Core.Models; // GameState, PlayerStats
using System; // NotImplementedException, Environment, Convert
using System.IO; // Path, File, Directory, IOException
using System.Security.Cryptography; // SHA256
using System.Text; // Encoding
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
    // Options for serialization (saving) - using WriteIndented for readability in file
    private static readonly JsonSerializerOptions _serializeOptionsSave = new()
    {
        WriteIndented = true, // Make saved file readable
        PropertyNamingPolicy = null
    };
    // Options for checksum calculation - MUST be consistent and non-indented
    private static readonly JsonSerializerOptions _serializeOptionsChecksum = new()
    {
        WriteIndented = false, // No indentation for checksum consistency
        PropertyNamingPolicy = null // Ensure exact property names
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
        Directory.CreateDirectory(_saveBasePath); // Ensure directory exists just before getting path
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

    public async Task SaveStateAsync(GameState gameState)
    {
        if (gameState == null) throw new ArgumentNullException(nameof(gameState));
        if (string.IsNullOrWhiteSpace(gameState.Username)) throw new ArgumentException("GameState must have a valid Username to save.", nameof(gameState));

        var filePath = GetSaveFilePath(gameState.Username);

        // Calculate checksum *before* saving
        string calculatedChecksum = ComputeChecksum(gameState);

        // Create a new state instance including the checksum using the 'with' expression
        var stateToSerialize = gameState with { Checksum = calculatedChecksum };

        try
        {
            // Serialize the state *with the checksum* to JSON
            string json = JsonSerializer.Serialize(stateToSerialize, _serializeOptionsSave);

            // Write the JSON to the file asynchronously
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (JsonException jsonEx)
        {
            throw new Exception($"Failed to serialize game state for user '{gameState.Username}'.", jsonEx);
        }
        catch (IOException ioEx)
        {
            throw new IOException($"Failed to write save file for user '{gameState.Username}'.", ioEx);
        }
    }

    /// <summary>
    /// Computes the SHA256 checksum for a GameState object.
    /// Serializes the state excluding the Checksum property itself.
    /// </summary>
    private string ComputeChecksum(GameState state)
    {
        // Use 'with' expression to create a copy with Checksum explicitly null
        var stateForHashing = state with { Checksum = null };

        // Serialize using consistent, non-indented options
        var json = JsonSerializer.Serialize(stateForHashing, _serializeOptionsChecksum);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }
}
