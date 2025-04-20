// tests/HardlineProphet.Tests/Infrastructure/Persistence/JsonGameStateRepositoryTests/SaveTests.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Models;
using HardlineProphet.Infrastructure.Persistence;
using HardlineProphet.Tests.Helpers;
using NFluent;
using System;
using System.IO;
using System.Security.Cryptography; // Required for SHA256
using System.Text; // Required for Encoding
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
        WriteIndented = false, // Use non-indented for checksum calculation consistency
        PropertyNamingPolicy = null // Ensure exact property names as defined
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
        try { /* Act */ await repository.SaveStateAsync(stateToSave); }
        catch (Exception ex) { caughtException = ex; }
        finally { CleanupDirectory(testDir); }
        // Assert (Reading back the file)
        Check.That(File.Exists(expectedFilePath)).IsTrue();
        var json = await File.ReadAllTextAsync(expectedFilePath);
        deserializedState = JsonSerializer.Deserialize<GameState>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        // Assert (Checking the content)
        Check.That(caughtException).IsNull();
        Check.That(deserializedState).IsNotNull();
        // Assert: Detailed property checks (remain the same)
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
        Check.That(deserializedState.Checksum).IsNull();
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

        // Calculate the expected checksum based on the state *without* the checksum field
        // Use the 'with' expression for records to create a copy with Checksum = null
        var stateForChecksum = stateToSave with { Checksum = null };
        var jsonForChecksum = JsonSerializer.Serialize(stateForChecksum, _testSerializerOptions); // Use consistent options
        string expectedChecksum;
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jsonForChecksum));
            expectedChecksum = Convert.ToBase64String(hashBytes);
        }
        _output.WriteLine($"Calculated expected checksum: {expectedChecksum}");
        _output.WriteLine($"JSON used for checksum:\n{jsonForChecksum}");


        GameState deserializedState = null!;
        try
        {
            // Act
            await repository.SaveStateAsync(stateToSave); // This should calculate and add the checksum before saving

            // Assert
            Check.That(File.Exists(expectedFilePath)).IsTrue();
            var savedJson = await File.ReadAllTextAsync(expectedFilePath);
            _output.WriteLine($"Actual saved JSON:\n{savedJson}");
            deserializedState = JsonSerializer.Deserialize<GameState>(savedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Check that the checksum in the saved file is not null/empty and matches the expected one
            // This test will FAIL initially because SaveStateAsync doesn't implement checksum logic yet.
            Check.That(deserializedState).IsNotNull();
            Check.That(deserializedState.Checksum).IsNotNull().And.IsNotEmpty();
            Check.That(deserializedState.Checksum).IsEqualTo(expectedChecksum);

            // Also check other properties to ensure they weren't corrupted during save
            Check.That(deserializedState.Username).IsEqualTo(stateToSave.Username);
            Check.That(deserializedState.Level).IsEqualTo(stateToSave.Level);

        }
        finally
        {
            CleanupDirectory(testDir);
        }
    }
}
