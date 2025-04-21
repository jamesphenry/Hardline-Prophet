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
    private readonly bool _isDevMode; // Added field to store dev mode status
    private static readonly JsonSerializerOptions _deserializeOptions = new() { PropertyNameCaseInsensitive = true, };
    private static readonly JsonSerializerOptions _serializeOptionsSave = new() { WriteIndented = true, PropertyNamingPolicy = null };
    private static readonly JsonSerializerOptions _serializeOptionsChecksum = new() { WriteIndented = false, PropertyNamingPolicy = null };


    // Constructor updated to accept dev mode status
    public JsonGameStateRepository(string? basePath = null, bool isDevMode = false) // Added isDevMode parameter
    {
        _isDevMode = isDevMode; // Store dev mode status
        if (string.IsNullOrWhiteSpace(basePath))
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _saveBasePath = Path.Combine(appDataPath, "HardlineProphet", "Saves");
        }
        else { _saveBasePath = basePath; }
        Directory.CreateDirectory(_saveBasePath);
        Console.WriteLine($"Repository Initialized. Dev Mode: {_isDevMode}. Path: {_saveBasePath}"); // Log repo init
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
            var defaultState = new GameState { Username = username };
            return defaultState;
        }
        else
        {
            string json = await File.ReadAllTextAsync(filePath);
            GameState loadedState;
            int fileVersion = 1;

            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    if (document.RootElement.TryGetProperty("Version", out JsonElement versionElement) && versionElement.TryGetInt32(out int version)) { fileVersion = version; }
                }

                if (fileVersion < GameConstants.CurrentSaveVersion)
                {
                    loadedState = fileVersion switch
                    {
                        1 => MigrateV1ToV2(username, json), // V1 migration already handles checksum calculation
                        _ => throw new NotSupportedException($"Save file version {fileVersion} is not supported for migration.")
                    };
                    Console.WriteLine($"Migrated save file for user '{username}' from V{fileVersion} to V{loadedState.Version}.");
                    // NOTE: Migrated state now has a valid V2 checksum, normal validation below is skipped implicitly
                    // OR explicitly: if we modify MigrateV1ToV2 to not add checksum and handle it here.
                    // Current approach adds checksum in MigrateV1ToV2, so it should be valid V2 now.
                    // We might need a specific checksum validation call here if we want to be extra sure.
                    ValidateChecksum(loadedState, username); // Validate the checksum *after* migration too
                }
                else if (fileVersion == GameConstants.CurrentSaveVersion)
                {
                    loadedState = JsonSerializer.Deserialize<GameState>(json, _deserializeOptions)
                        ?? throw new InvalidDataException("Failed to deserialize save file content (result was null).");

                    // Validate checksum for current version, respecting dev mode
                    ValidateChecksum(loadedState, username);
                }
                else { throw new NotSupportedException($"Save file version {fileVersion} is newer than supported version {GameConstants.CurrentSaveVersion}."); }

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
        var migratedState = JsonSerializer.Deserialize<GameState>(v1Json, _deserializeOptions);
        if (migratedState == null) throw new InvalidDataException($"Failed to deserialize V1 save file during migration for user '{username}'.");
        string newChecksum = ComputeChecksum(migratedState);
        // Return V2 state with updated version and *newly calculated* V2 checksum
        return migratedState with { Version = GameConstants.CurrentSaveVersion, Checksum = newChecksum };
    }

    /// <summary>
    /// Validates the checksum of a loaded GameState object, skipping if in Dev Mode.
    /// Throws InvalidDataException if checksum is missing (in non-dev mode) or invalid.
    /// </summary>
    private void ValidateChecksum(GameState stateToValidate, string username)
    {
        // Skip validation entirely if in dev mode
        if (_isDevMode)
        {
            Console.WriteLine($"Dev Mode: Skipping checksum validation for user '{username}'.");
            // Check if checksum exists and warn if it's bad, but don't throw
            if (!string.IsNullOrEmpty(stateToValidate.Checksum))
            {
                string expectedChecksum = ComputeChecksum(stateToValidate);
                if (stateToValidate.Checksum != expectedChecksum)
                {
                    Console.WriteLine($"Dev Mode WARNING: Checksum mismatch for user '{username}'. File checksum: '{stateToValidate.Checksum}', Calculated: '{expectedChecksum}'. Loading anyway.");
                }
                else
                {
                    Console.WriteLine($"Dev Mode: Checksum is valid for user '{username}'.");
                }
            }
            else
            {
                Console.WriteLine($"Dev Mode: No checksum present for user '{username}'. Loading anyway.");
            }
            return; // Exit validation method
        }

        // --- Production Mode Validation ---
        if (string.IsNullOrEmpty(stateToValidate.Checksum))
        {
            // Enforce checksum for current version files in production mode
            throw new InvalidDataException($"Save file integrity check failed for user '{username}' (missing checksum in V{stateToValidate.Version} file).");
        }

        string expectedChecksumProd = ComputeChecksum(stateToValidate);
        if (stateToValidate.Checksum != expectedChecksumProd)
        {
            throw new InvalidDataException($"Save file integrity check failed for user '{username}' (checksum mismatch).");
        }
        // Checksum valid!
        Console.WriteLine($"Checksum validated successfully for user '{username}'.");
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
