// tests/HardlineProphet.Tests/Helpers/PersistenceTestHelper.cs
using HardlineProphet.Core.Models;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace HardlineProphet.Tests.Helpers;

public static class PersistenceTestHelper
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true // Match potential default save options
        // Ensure options match what the repository expects or uses for saving eventually
    };

    /// <summary>
    /// Creates a dummy save file for testing purposes.
    /// Serializes the given state to JSON and writes it to the specified path.
    /// Does NOT include checksum calculation yet.
    /// </summary>
    /// <param name="directoryPath">The directory where the save file should be created.</param>
    /// <param name="username">The username for the save file name.</param>
    /// <param name="stateToSave">The GameState object to serialize and save.</param>
    /// <returns>The full path to the created save file.</returns>
    public static async Task<string> SetupExistingSaveFileAsync(string directoryPath, string username, GameState stateToSave)
    {
        // Construct path (basic sanitization included for consistency)
        var sanitizedUsername = string.Join("_", username.Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(sanitizedUsername))
        {
            sanitizedUsername = "default_user";
        }
        var saveFilePath = Path.Combine(directoryPath, $"{sanitizedUsername}.save.json");

        // Ensure directory exists
        Directory.CreateDirectory(directoryPath);

        // Serialize and save the file
        var json = JsonSerializer.Serialize(stateToSave, _serializerOptions);
        await File.WriteAllTextAsync(saveFilePath, json);

        return saveFilePath;
    }
}
