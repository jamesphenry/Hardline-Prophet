// tests/HardlineProphet.Tests/Infrastructure/Persistence/JsonGameStateRepositoryTests/SaveTests.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Models;
using HardlineProphet.Infrastructure.Persistence;
using HardlineProphet.Tests.Helpers;
using NFluent;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HardlineProphet.Tests.Infrastructure.Persistence.JsonGameStateRepositoryTests;

public class SaveTests
{
    private readonly ITestOutputHelper _output;
    public SaveTests(ITestOutputHelper output) { _output = output; }
    private string GetTestDirectory()
    { /* ... */
        string baseTestDir = Path.Combine(Path.GetTempPath(), "HardlineProphetTests");
        string testRunDir = Path.Combine(baseTestDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(testRunDir);
        _output.WriteLine($"Created test directory: {testRunDir}");
        return testRunDir;
    }
    private void CleanupDirectory(string testRunDir)
    { /* ... */
        if (!string.IsNullOrEmpty(testRunDir) && Directory.Exists(testRunDir))
        {
            _output.WriteLine($"Cleaning up test directory: {testRunDir}");
            try { Directory.Delete(testRunDir, true); } catch { /* Ignore cleanup errors */ }
        }
    }

    // Shared serializer options for consistency in tests
    private static readonly JsonSerializerOptions _testSerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null
    };
    private static readonly JsonSerializerOptions _saveTestSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };


    [Fact]
    public async Task SaveStateAsync_ShouldSerializeStateAndWriteToFile()
    {
        // Arrange
        var username = "SavingPlayer";
        var testDir = GetTestDirectory();
        var repository = new JsonGameStateRepository(testDir);
        var stateToSave = new GameState
        { /* ... state setup ... */
            Username = username,
            Level = 10,
            Experience = 9999.9,
            Credits = 12345,
            Stats = new PlayerStats { HackSpeed = 15, Stealth = 12, DataYield = 5 },
            ActiveMissionIds = new System.Collections.Generic.List<string> { "mission_save_test" },
            UnlockedPerkIds = new System.Collections.Generic.List<string> { "perk_save_test" },
            Version = GameConstants.CurrentSaveVersion,
            IsDevSave = false
        };
        var expectedFilePath = Path.Combine(testDir, $"{username}.save.json");
        if (File.Exists(expectedFilePath)) { File.Delete(expectedFilePath); }

        Exception caughtException = null!;
        GameState deserializedState = null!;
        try
        {
            // Act
            _output.WriteLine($"Calling SaveStateAsync for user: {username}");
            await repository.SaveStateAsync(stateToSave);
            _output.WriteLine($"SaveStateAsync completed.");

            // Assert (Reading back the file) - Moved inside try block after successful Act
            _output.WriteLine($"Checking if file exists at: {expectedFilePath}");
            Check.That(File.Exists(expectedFilePath)).IsTrue(); // <<< This is the failing line
            _output.WriteLine($"File exists check passed.");

            var json = await File.ReadAllTextAsync(expectedFilePath);
            _output.WriteLine($"Read back JSON:\n{json}");
            deserializedState = JsonSerializer.Deserialize<GameState>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        }
        catch (Exception ex)
        {
            // Log any exception caught during Act or Assert setup
            caughtException = ex;
            _output.WriteLine($"!!! Exception caught: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            CleanupDirectory(testDir);
        }

        // Assert (Checking the content)
        Check.That(caughtException).IsNull(); // Check if any exception was caught above
        Check.That(deserializedState).IsNotNull();

        // Assert: Detailed property checks
        Check.That(deserializedState.Username).IsEqualTo(stateToSave.Username);
        Check.That(deserializedState.Level).IsEqualTo(stateToSave.Level);
        Check.That(deserializedState.Experience).IsEqualTo(stateToSave.Experience);
        Check.That(deserializedState.Credits).IsEqualTo(stateToSave.Credits);
        Check.That(deserializedState.Stats).IsNotNull();
        Check.That(deserializedState.Stats.HackSpeed).IsEqualTo(stateToSave.Stats.HackSpeed);
        Check.That(deserializedState.Stats.Stealth).IsEqualTo(stateToSave.Stats.Stealth);
        Check.That(deserializedState.Stats.DataYield).IsEqualTo(stateToSave.Stats.DataYield);
        Check.That(deserializedState.ActiveMissionIds).ContainsExactly(stateToSave.ActiveMissionIds);
        Check.That(deserializedState.UnlockedPerkIds).ContainsExactly(stateToSave.UnlockedPerkIds);
        Check.That(deserializedState.Version).IsEqualTo(stateToSave.Version);
        Check.That(deserializedState.IsDevSave).IsEqualTo(stateToSave.IsDevSave);
        // Checksum is generated by SaveStateAsync, so it should NOT be null here
        Check.That(deserializedState.Checksum).IsNotNull().And.IsNotEmpty();
    }

    [Fact]
    public async Task SaveStateAsync_ShouldCalculateAndStoreChecksum()
    {
        // Arrange
        var username = "ChecksumPlayer";
        var testDir = GetTestDirectory();
        var repository = new JsonGameStateRepository(testDir);
        var stateToSave = new GameState
        { /* ... state setup ... */
            Username = username,
            Level = 3,
            Experience = 100.0,
            Credits = 50,
            Stats = new PlayerStats { HackSpeed = 6, Stealth = 7, DataYield = 1 },
            Version = GameConstants.CurrentSaveVersion,
            IsDevSave = false
        };
        var expectedFilePath = Path.Combine(testDir, $"{username}.save.json");
        if (File.Exists(expectedFilePath)) { File.Delete(expectedFilePath); }
        // Calculate the expected checksum
        var stateForChecksum = stateToSave with { Checksum = null };
        var jsonForChecksum = JsonSerializer.Serialize(stateForChecksum, _testSerializerOptions);
        string expectedChecksum;
        using (var sha256 = SHA256.Create()) { var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jsonForChecksum)); expectedChecksum = Convert.ToBase64String(hashBytes); }
        _output.WriteLine($"Calculated expected checksum: {expectedChecksum}");
        GameState deserializedState = null!;
        try { /* Act */ await repository.SaveStateAsync(stateToSave); } finally { /* Read back */ Check.That(File.Exists(expectedFilePath)).IsTrue(); var savedJson = await File.ReadAllTextAsync(expectedFilePath); _output.WriteLine($"Actual saved JSON:\n{savedJson}"); deserializedState = JsonSerializer.Deserialize<GameState>(savedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); CleanupDirectory(testDir); }
        // Assert
        Check.That(deserializedState).IsNotNull();
        Check.That(deserializedState.Checksum).IsNotNull().And.IsNotEmpty();
        Check.That(deserializedState.Checksum).IsEqualTo(expectedChecksum);
        Check.That(deserializedState.Username).IsEqualTo(stateToSave.Username); Check.That(deserializedState.Level).IsEqualTo(stateToSave.Level);
    }
}
