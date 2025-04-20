// src/HardlineProphet/Infrastructure/Persistence/JsonGameStateRepository.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Interfaces; // IGameStateRepository
using HardlineProphet.Core.Models; // GameState, PlayerStats
using System; // NotImplementedException, Environment, Convert
using System.IO; // Path, File, Directory, IOException, InvalidDataException
using System.Security.Cryptography; // SHA256
using System.Text; // Encoding
using System.Text.Json; // JsonSerializer, JsonException, JsonSerializerOptions, JsonDocument, JsonElement
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
        // ... (constructor remains the same) ...
        if (string.IsNullOrWhiteSpace(basePath)) { string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); _saveBasePath = Path.Combine(appDataPath, "HardlineProphet", "Saves"); } else { _saveBasePath = basePath; }
        Directory.CreateDirectory(_saveBasePath);
    }

    private string GetSaveFilePath(string username)
    {
        // ... (GetSaveFilePath remains the same) ...
        var sanitizedUsername = string.Join("_", username.Split(Path.GetInvalidFileNameChars())); if (string.IsNullOrWhiteSpace(sanitizedUsername)) { sanitizedUsername = "default_user"; }
        Directory.CreateDirectory(_saveBasePath); return Path.Combine(_saveBasePath, $"{sanitizedUsername}.save.json");
    }

    public async Task<GameState> LoadStateAsync(string username)
    {
        var filePath = GetSaveFilePath(username);

        if (!File.Exists(filePath))
        {
            // Return default state for new user
            var defaultState = new GameState { Username = username };
            return defaultState;
        }
        else
        {
            // File exists, load, check version, migrate if necessary, validate checksum
            string json = await File.ReadAllTextAsync(filePath);
            GameState loadedState;
            int fileVersion = 1; // Assume V1 if Version property is missing

            try
            {
                // Peek at the version
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    if (document.RootElement.TryGetProperty("Version", out JsonElement versionElement) && versionElement.TryGetInt32(out int version))
                    {
                        fileVersion = version;
                    }
                }

                // --- Handle based on version ---
                if (fileVersion < GameConstants.CurrentSaveVersion)
                {
                    // Call migration logic based on the specific old version found
                    loadedState = fileVersion switch
                    {
                        1 => MigrateV1ToV2(username, json), // Call V1 migration helper
                        _ => throw new NotSupportedException($"Save file version {fileVersion} is not supported for migration.")
                    };
                    // Optional: Log migration event
                    Console.WriteLine($"Migrated save file for user '{username}' from V{fileVersion} to V{loadedState.Version}.");
                }
                else if (fileVersion == GameConstants.CurrentSaveVersion)
                {
                    // Load current version directly
                    loadedState = JsonSerializer.Deserialize<GameState>(json, _deserializeOptions)
                        ?? throw new InvalidDataException("Failed to deserialize save file content (result was null).");

                    // Validate checksum for current version
                    ValidateChecksum(loadedState, username); // Extracted validation logic (optional refactor)
                }
                else // Future version
                {
                    throw new NotSupportedException($"Save file version {fileVersion} is newer than supported version {GameConstants.CurrentSaveVersion}.");
                }

                return loadedState;
            }
            catch (JsonException jsonEx) { throw new InvalidDataException($"Failed to parse save file for user '{username}'. Invalid JSON.", jsonEx); }
            catch (IOException ioEx) { throw new IOException($"Failed to read save file for user '{username}'.", ioEx); }
            // Catch InvalidDataException from checksum/migration, NotSupportedException from version check
        }
    }

    /// <summary>
    /// Migrates a V1 save JSON string to a V2 GameState object.
    /// </summary>
    private GameState MigrateV1ToV2(string username, string v1Json)
    {
        // Deserialize V1 JSON into V2 record (handles missing fields with defaults)
        var migratedState = JsonSerializer.Deserialize<GameState>(v1Json, _deserializeOptions);
        if (migratedState == null) throw new InvalidDataException($"Failed to deserialize V1 save file during migration for user '{username}'.");

        // Explicitly set current version and recalculate checksum for the migrated state
        string newChecksum = ComputeChecksum(migratedState); // Checksum based on V2 structure with defaults
        return migratedState with
        {
            Version = GameConstants.CurrentSaveVersion,
            Checksum = newChecksum // Add a valid V2 checksum
        };
        // Note: We could optionally re-save the migrated file here to update it on disk.
    }

    /// <summary>
    /// Validates the checksum of a loaded GameState object.
    /// Throws InvalidDataException if checksum is missing or invalid.
    /// </summary>
    private void ValidateChecksum(GameState stateToValidate, string username)
    {
        if (string.IsNullOrEmpty(stateToValidate.Checksum))
        {
            // Enforce checksum for current version files
            throw new InvalidDataException($"Save file integrity check failed for user '{username}' (missing checksum in V{stateToValidate.Version} file).");
        }

        string expectedChecksum = ComputeChecksum(stateToValidate);
        if (stateToValidate.Checksum != expectedChecksum)
        {
            throw new InvalidDataException($"Save file integrity check failed for user '{username}' (checksum mismatch).");
        }
        // Checksum valid!
    }


    public async Task SaveStateAsync(GameState gameState)
    {
        // ... (SaveStateAsync remains the same) ...
        if (gameState == null) throw new ArgumentNullException(nameof(gameState)); if (string.IsNullOrWhiteSpace(gameState.Username)) throw new ArgumentException("GameState must have a valid Username to save.", nameof(gameState)); var filePath = GetSaveFilePath(gameState.Username); string calculatedChecksum = ComputeChecksum(gameState); var stateToSerialize = gameState with { Checksum = calculatedChecksum }; try { string json = JsonSerializer.Serialize(stateToSerialize, _serializeOptionsSave); await File.WriteAllTextAsync(filePath, json); } catch (JsonException jsonEx) { throw new Exception($"Failed to serialize game state for user '{gameState.Username}'.", jsonEx); } catch (IOException ioEx) { throw new IOException($"Failed to write save file for user '{gameState.Username}'.", ioEx); }
    }

    private string ComputeChecksum(GameState state)
    {
        // ... (ComputeChecksum remains the same) ...
        var stateForHashing = state with { Checksum = null }; var json = JsonSerializer.Serialize(stateForHashing, _serializeOptionsChecksum); using var sha256 = SHA256.Create(); var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json)); return Convert.ToBase64String(hashBytes);
    }
}
