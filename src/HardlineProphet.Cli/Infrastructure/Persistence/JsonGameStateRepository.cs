// src/HardlineProphet/Infrastructure/Persistence/JsonGameStateRepository.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Interfaces; // IGameStateRepository
using HardlineProphet.Core.Models; // GameState, PlayerStats
using System; // NotImplementedException, Environment, Convert
using System.IO; // Path, File, Directory, IOException, InvalidDataException
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
    private static readonly JsonSerializerOptions _deserializeOptions = new() { PropertyNameCaseInsensitive = true, };
    private static readonly JsonSerializerOptions _serializeOptionsSave = new() { WriteIndented = true, PropertyNamingPolicy = null };
    private static readonly JsonSerializerOptions _serializeOptionsChecksum = new() { WriteIndented = false, PropertyNamingPolicy = null };


    public JsonGameStateRepository(string? basePath = null)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _saveBasePath = Path.Combine(appDataPath, "HardlineProphet", "Saves");
        }
        else { _saveBasePath = basePath; }
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
            // Return default state for new user
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
            // File exists, load and validate
            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                var loadedState = JsonSerializer.Deserialize<GameState>(json, _deserializeOptions);

                if (loadedState == null)
                {
                    throw new InvalidDataException($"Failed to deserialize save file content for user '{username}'. Deserialized object was null.");
                }

                // --- Checksum Validation ---
                if (!string.IsNullOrEmpty(loadedState.Checksum))
                {
                    // If a checksum exists in the file, validate it
                    string expectedChecksum = ComputeChecksum(loadedState);
                    if (loadedState.Checksum != expectedChecksum)
                    {
                        // Checksums don't match, data integrity issue!
                        throw new InvalidDataException($"Save file integrity check failed for user '{username}' (checksum mismatch).");
                    }
                    // Checksums match, proceed.
                }
                // else
                // {
                //     // Checksum is null or empty in the file.
                //     // For now, we allow this (e.g., for older saves or saves made without checksum).
                //     // We could add stricter checks or version-based checks later if needed.
                // }

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
            // Catch InvalidDataException from checksum mismatch re-throw if needed, but it should propagate correctly.
        }
    }

    public async Task SaveStateAsync(GameState gameState)
    {
        if (gameState == null) throw new ArgumentNullException(nameof(gameState));
        if (string.IsNullOrWhiteSpace(gameState.Username)) throw new ArgumentException("GameState must have a valid Username to save.", nameof(gameState));

        var filePath = GetSaveFilePath(gameState.Username);
        string calculatedChecksum = ComputeChecksum(gameState);
        var stateToSerialize = gameState with { Checksum = calculatedChecksum };

        try
        {
            string json = JsonSerializer.Serialize(stateToSerialize, _serializeOptionsSave);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (JsonException jsonEx) { throw new Exception($"Failed to serialize game state for user '{gameState.Username}'.", jsonEx); }
        catch (IOException ioEx) { throw new IOException($"Failed to write save file for user '{gameState.Username}'.", ioEx); }
    }

    private string ComputeChecksum(GameState state)
    {
        var stateForHashing = state with { Checksum = null };
        var json = JsonSerializer.Serialize(stateForHashing, _serializeOptionsChecksum);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }
}
