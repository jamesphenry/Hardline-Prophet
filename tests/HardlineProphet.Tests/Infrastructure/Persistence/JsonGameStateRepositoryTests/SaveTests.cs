// tests/HardlineProphet.Tests/Infrastructure/Persistence/JsonGameStateRepositoryTests/SaveTests.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Models;
using HardlineProphet.Infrastructure.Persistence;
using HardlineProphet.Tests.Helpers; // If needed, though maybe not for basic save
using NFluent;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HardlineProphet.Tests.Infrastructure.Persistence.JsonGameStateRepositoryTests;

public class SaveTests
{
    private readonly ITestOutputHelper _output;

    public SaveTests(ITestOutputHelper output) { _output = output; }

    // Reusing helpers from LoadTests (Consider extracting to a common fixture or base class later if needed)
    private string GetTestDirectory()
    {
        string baseTestDir = Path.Combine(Path.GetTempPath(), "HardlineProphetTests");
        string testRunDir = Path.Combine(baseTestDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(testRunDir);
        _output.WriteLine($"Created test directory: {testRunDir}");
        return testRunDir;
    }
    private void CleanupDirectory(string testRunDir)
    {
        if (!string.IsNullOrEmpty(testRunDir) && Directory.Exists(testRunDir))
        {
            _output.WriteLine($"Cleaning up test directory: {testRunDir}");
            try { Directory.Delete(testRunDir, true); } catch { /* Ignore cleanup errors */ }
        }
    }

    [Fact]
    public async Task SaveStateAsync_ShouldSerializeStateAndWriteToFile()
    {
        // Arrange
        var username = "SavingPlayer";
        var testDir = GetTestDirectory();
        var repository = new JsonGameStateRepository(testDir); // Uses test directory

        var stateToSave = new GameState
        {
            Username = username, // Important: Username determines filename
            Level = 10,
            Experience = 9999.9,
            Credits = 12345,
            Stats = new PlayerStats { HackSpeed = 15, Stealth = 12, DataYield = 5 },
            ActiveMissionIds = new System.Collections.Generic.List<string> { "mission_save_test" },
            UnlockedPerkIds = new System.Collections.Generic.List<string> { "perk_save_test" },
            Version = GameConstants.CurrentSaveVersion,
            IsDevSave = false
            // Checksum will be handled later
        };

        var expectedFilePath = Path.Combine(testDir, $"{username}.save.json");
        // Ensure file doesn't exist before test
        if (File.Exists(expectedFilePath)) { File.Delete(expectedFilePath); }

        Exception caughtException = null!;
        try
        {
            // Act
            await repository.SaveStateAsync(stateToSave);

            // Assert
            // 1. Check if the file was created
            Check.That(File.Exists(expectedFilePath)).IsTrue();

            // 2. Read the file content and check if it deserializes correctly
            var json = await File.ReadAllTextAsync(expectedFilePath);
            var deserializedState = JsonSerializer.Deserialize<GameState>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // 3. Check if the deserialized state matches the original state
            Check.That(deserializedState).IsNotNull();
            // Compare all relevant properties
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
            Check.That(deserializedState.Checksum).IsNull(); // Checksum not implemented yet
        }
        catch (Exception ex)
        {
            caughtException = ex; // Catch unexpected exceptions during Act or Assert
        }
        finally
        {
            CleanupDirectory(testDir);
        }

        // Final check: Ensure no unexpected exception occurred during the process
        // This test should FAIL initially because SaveStateAsync throws NotImplementedException
        Check.That(caughtException).IsNull(); // This assertion will likely fail first!
    }

    // --- Add more tests here later for saving with checksums, handling errors etc. ---
}
