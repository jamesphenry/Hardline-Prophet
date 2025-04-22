// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-alpha.12
// ║ 📄  File: JsonGameStateRepository.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
using System; // Environment, Convert, ArgumentNullException, NotSupportedException
using System.IO; // Path, File, Directory, IOException, InvalidDataException
using System.Security.Cryptography; // SHA256
using System.Text; // Encoding
using System.Text.Json; // JsonSerializer, JsonException, JsonSerializerOptions, JsonDocument, JsonElement
using System.Threading.Tasks; // Task
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Interfaces; // IGameStateRepository
using HardlineProphet.Core.Models; // GameState, PlayerStats

namespace HardlineProphet.Infrastructure.Persistence;

public class JsonGameStateRepository : IGameStateRepository
{
    private readonly string _saveBasePath;
    private readonly bool _isDevMode;
    private static readonly JsonSerializerOptions _deserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    private static readonly JsonSerializerOptions _serializeOptionsSave = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
    };
    private static readonly JsonSerializerOptions _serializeOptionsChecksum = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null,
    };

    public JsonGameStateRepository(string? basePath = null, bool isDevMode = false)
    {
        _isDevMode = isDevMode;
        if (string.IsNullOrWhiteSpace(basePath))
        {
            string appDataPath = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            );
            _saveBasePath = Path.Combine(appDataPath, "HardlineProphet", "Saves");
        }
        else
        {
            _saveBasePath = basePath;
        }
        Directory.CreateDirectory(_saveBasePath);
        Console.WriteLine($"Repository Initialized. Dev Mode: {_isDevMode}. Path: {_saveBasePath}");
    }

    private string GetSaveFilePath(string username)
    {
        var sanitizedUsername = string.Join("_", username.Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(sanitizedUsername))
        {
            sanitizedUsername = "default_user";
        }
        Directory.CreateDirectory(_saveBasePath);
        return Path.Combine(_saveBasePath, $"{sanitizedUsername}.save.json");
    }

    public async Task<GameState> LoadStateAsync(string username)
    {
        var filePath = GetSaveFilePath(username);
        if (!File.Exists(filePath))
        {
            return new GameState { Username = username };
        }

        string json = await File.ReadAllTextAsync(filePath);
        int fileVersion = 1; // Assume V1 if Version property is missing

        try
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                if (
                    document.RootElement.TryGetProperty("Version", out JsonElement versionElement)
                    && versionElement.TryGetInt32(out int version)
                )
                {
                    fileVersion = version;
                }
            }

            GameState finalState;

            if (fileVersion < GameConstants.CurrentSaveVersion)
            {
                Console.WriteLine(
                    $"Detected old save version V{fileVersion} for user '{username}'. Migrating..."
                );
                finalState = fileVersion switch
                {
                    1 => MigrateV2ToV3(MigrateV1ToV2(username, json)), // Chain migrations V1->V2->V3
                    2 => MigrateV2ToV3(username, json), // Migrate V2->V3 directly
                    _ => throw new NotSupportedException(
                        $"Save file version {fileVersion} is not supported for migration."
                    ),
                };
                Console.WriteLine(
                    $"Migration complete for user '{username}' to V{finalState.Version}. Final checksum: {finalState.Checksum}"
                );
                // Checksum is recalculated and included in the returned 'finalState'.
                // No need for ValidateChecksum here as migration produces the final valid state.
            }
            else if (fileVersion == GameConstants.CurrentSaveVersion) // V3
            {
                finalState =
                    JsonSerializer.Deserialize<GameState>(json, _deserializeOptions)
                    ?? throw new InvalidDataException(
                        $"Failed to deserialize V{fileVersion} save file content (result was null)."
                    );
                ValidateChecksum(finalState, username); // Validate checksum for current version
            }
            else
            {
                throw new NotSupportedException(
                    $"Save file version {fileVersion} is newer than supported version {GameConstants.CurrentSaveVersion}."
                );
            }

            return finalState;
        }
        catch (JsonException jsonEx)
        {
            throw new InvalidDataException(
                $"Failed to parse save file for user '{username}'. Invalid JSON.",
                jsonEx
            );
        }
        catch (IOException ioEx)
        {
            throw new IOException($"Failed to read save file for user '{username}'.", ioEx);
        }
    }

    /// <summary> Migrates V1 JSON to V2 GameState object. </summary>
    private GameState MigrateV1ToV2(string username, string v1Json)
    {
        var migratedState = JsonSerializer.Deserialize<GameState>(v1Json, _deserializeOptions); // Deserializes into V3 structure with defaults
        if (migratedState == null)
            throw new InvalidDataException(
                $"Failed to deserialize V1 save file during migration for user '{username}'."
            );

        // Create the intermediate V2 state *before* calculating checksum
        var stateV2 = migratedState with
        {
            Version = 2,
        }; // Set version to 2
        string checksumV2 = ComputeChecksum(stateV2); // Calculate checksum based on V2 state
        return stateV2 with { Checksum = checksumV2 }; // Return V2 state with V2 checksum
    }

    /// <summary> Migrates V2 JSON string to V3 GameState object. </summary>
    private GameState MigrateV2ToV3(string username, string v2Json)
    {
        var migratedState = JsonSerializer.Deserialize<GameState>(v2Json, _deserializeOptions); // Deserializes into V3 structure with default TraceLevel
        if (migratedState == null)
            throw new InvalidDataException(
                $"Failed to deserialize V2 save file during migration for user '{username}'."
            );

        // Create the final V3 state *before* calculating checksum
        var stateV3 = migratedState with
        {
            Version = GameConstants.CurrentSaveVersion,
        }; // Set version to 3
        string checksumV3 = ComputeChecksum(stateV3); // Calculate checksum based on V3 state
        return stateV3 with { Checksum = checksumV3 }; // Return V3 state with V3 checksum
    }

    /// <summary> Migrates an already loaded V2 GameState object to V3. </summary>
    private GameState MigrateV2ToV3(GameState stateV2) // Overload for chaining from V1->V2
    {
        // State already has V3 structure from V1 deserialization, but Version=2 and Checksum=V2 checksum.
        // We need to set Version=3 and calculate checksum based on V3 structure.
        var stateV3 = stateV2 with
        {
            Version = GameConstants.CurrentSaveVersion,
        }; // Set version to 3
        string checksumV3 = ComputeChecksum(stateV3); // Calculate checksum based on V3 state
        return stateV3 with { Checksum = checksumV3 }; // Return V3 state with V3 checksum
    }

    private void ValidateChecksum(GameState stateToValidate, string username)
    {
        // ... (ValidateChecksum remains the same) ...
        if (_isDevMode)
        {
            Console.WriteLine($"Dev Mode: Skipping checksum validation for user '{username}'.");
            if (!string.IsNullOrEmpty(stateToValidate.Checksum))
            {
                string expectedChecksum = ComputeChecksum(stateToValidate);
                if (stateToValidate.Checksum != expectedChecksum)
                {
                    Console.WriteLine(
                        $"Dev Mode WARNING: Checksum mismatch for user '{username}'. File checksum: '{stateToValidate.Checksum}', Calculated: '{expectedChecksum}'. Loading anyway."
                    );
                }
                else
                {
                    Console.WriteLine($"Dev Mode: Checksum is valid for user '{username}'.");
                }
            }
            else
            {
                Console.WriteLine(
                    $"Dev Mode: No checksum present for user '{username}'. Loading anyway."
                );
            }
            return;
        }
        if (string.IsNullOrEmpty(stateToValidate.Checksum))
        {
            throw new InvalidDataException(
                $"Save file integrity check failed for user '{username}' (missing checksum in V{stateToValidate.Version} file)."
            );
        }
        string expectedChecksumProd = ComputeChecksum(stateToValidate);
        if (stateToValidate.Checksum != expectedChecksumProd)
        {
            throw new InvalidDataException(
                $"Save file integrity check failed for user '{username}' (checksum mismatch)."
            );
        }
        Console.WriteLine($"Checksum validated successfully for user '{username}'.");
    }

    public async Task SaveStateAsync(GameState gameState)
    {
        // ... (SaveStateAsync remains the same - ensures Version=Current and recalculates checksum) ...
        if (gameState == null)
            throw new ArgumentNullException(nameof(gameState));
        if (string.IsNullOrWhiteSpace(gameState.Username))
            throw new ArgumentException(
                "GameState must have a valid Username to save.",
                nameof(gameState)
            );
        var filePath = GetSaveFilePath(gameState.Username);
        string calculatedChecksum = ComputeChecksum(gameState);
        var stateToSerialize = gameState with
        {
            Version = GameConstants.CurrentSaveVersion,
            Checksum = calculatedChecksum,
        }; /* Ensure saving current version */
        try
        {
            string json = JsonSerializer.Serialize(stateToSerialize, _serializeOptionsSave);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (JsonException jsonEx)
        {
            throw new Exception(
                $"Failed to serialize game state for user '{gameState.Username}'.",
                jsonEx
            );
        }
        catch (IOException ioEx)
        {
            throw new IOException(
                $"Failed to write save file for user '{gameState.Username}'.",
                ioEx
            );
        }
    }

    private string ComputeChecksum(GameState state)
    {
        // ... (ComputeChecksum remains the same) ...
        var stateForHashing = state with
        {
            Checksum = null,
        };
        var json = JsonSerializer.Serialize(stateForHashing, _serializeOptionsChecksum);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }
}
