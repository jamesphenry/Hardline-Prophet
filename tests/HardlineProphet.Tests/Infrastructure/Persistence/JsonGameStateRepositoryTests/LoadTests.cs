// tests/HardlineProphet.Tests/Infrastructure/Persistence/JsonGameStateRepositoryTests/LoadTests.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Interfaces;
using HardlineProphet.Core.Models;
using HardlineProphet.Infrastructure.Persistence;
using HardlineProphet.Tests.Helpers; // Added using for helpers
using NFluent;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HardlineProphet.Tests.Infrastructure.Persistence.JsonGameStateRepositoryTests;

public class LoadTests
{
    private readonly ITestOutputHelper _output;

    public LoadTests(ITestOutputHelper output) { _output = output; }

    private string GetTestDirectory()
    { /* ... same as before ... */
        string baseTestDir = Path.Combine(Path.GetTempPath(), "HardlineProphetTests");
        string testRunDir = Path.Combine(baseTestDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(testRunDir);
        _output.WriteLine($"Created test directory: {testRunDir}");
        return testRunDir;
    }
    private void CleanupDirectory(string testRunDir)
    { /* ... same as before ... */
        if (!string.IsNullOrEmpty(testRunDir) && Directory.Exists(testRunDir))
        {
            _output.WriteLine($"Cleaning up test directory: {testRunDir}");
            try { Directory.Delete(testRunDir, true); } catch { /* Ignore cleanup errors */ }
        }
    }


    [Fact]
    public async Task LoadStateAsync_WhenSaveFileDoesNotExist_ShouldReturnNewGameStateWithDefaultValues()
    {
        // Arrange
        var username = "NewbieHacker";
        var testDir = GetTestDirectory();
        var repository = new JsonGameStateRepository(testDir);
        var saveFilePath = Path.Combine(testDir, $"{username}.save.json");
        if (File.Exists(saveFilePath)) { File.Delete(saveFilePath); }
        _output.WriteLine($"Ensured file does not exist at: {saveFilePath}");
        GameState loadedState = null!;
        try
        {
            // Act
            loadedState = await repository.LoadStateAsync(username);
            // Assert (remains the same)
            Check.That(loadedState).IsNotNull();
            Check.That(loadedState.Username).IsEqualTo(username);
            Check.That(loadedState.Level).IsEqualTo(GameConstants.DefaultStartingLevel);
            Check.That(loadedState.Experience).IsEqualTo(GameConstants.DefaultStartingExperience);
            Check.That(loadedState.Credits).IsEqualTo(GameConstants.DefaultStartingCredits);
            Check.That(loadedState.Stats).IsNotNull();
            Check.That(loadedState.Stats.HackSpeed).IsEqualTo(GameConstants.DefaultStartingHackSpeed);
            Check.That(loadedState.Stats.Stealth).IsEqualTo(GameConstants.DefaultStartingStealth);
            Check.That(loadedState.Stats.DataYield).IsEqualTo(GameConstants.DefaultStartingDataYield);
            Check.That(loadedState.ActiveMissionIds).IsEmpty();
            Check.That(loadedState.UnlockedPerkIds).IsEmpty();
            Check.That(loadedState.Version).IsEqualTo(GameConstants.CurrentSaveVersion);
            Check.That(loadedState.Checksum).IsNull();
            Check.That(loadedState.IsDevSave).IsFalse();
        }
        finally { CleanupDirectory(testDir); }
    }

    [Fact]
    public async Task LoadStateAsync_WhenSaveFileExistsAndIsValid_ShouldReturnDeserializedGameState()
    {
        // Arrange
        var username = "ExistingPlayer";
        var testDir = GetTestDirectory();
        var repository = new JsonGameStateRepository(testDir);

        // Create the state we expect to load
        var originalState = new GameState
        { /* ... same as before ... */
            Username = username,
            Level = 5,
            Experience = 1234.5,
            Credits = 5000,
            Stats = new PlayerStats { HackSpeed = 10, Stealth = 8, DataYield = 2 },
            ActiveMissionIds = new System.Collections.Generic.List<string> { "mission_001" },
            UnlockedPerkIds = new System.Collections.Generic.List<string> { "perk_abc" },
            Version = GameConstants.CurrentSaveVersion,
            IsDevSave = true
        };

        // Use the helper to set up the save file
        string saveFilePath = await PersistenceTestHelper.SetupExistingSaveFileAsync(testDir, username, originalState);
        _output.WriteLine($"Test save file created at: {saveFilePath}");

        // Optional: Add delay/existence check if needed, but helper should handle writing reliably
        // await Task.Delay(150);
        // bool fileActuallyExists = File.Exists(saveFilePath);
        // Check.That(fileActuallyExists).IsTrue();

        GameState loadedState = null!;
        Exception caughtException = null!;
        try
        {
            // Act
            loadedState = await repository.LoadStateAsync(username);
        }
        catch (Exception ex) { caughtException = ex; } // Catch unexpected exceptions
        finally { CleanupDirectory(testDir); }

        // Assert - Check the loaded data matches the original
        Check.That(caughtException).IsNull();
        Check.That(loadedState).IsNotNull();
        Check.That(loadedState.Username).IsEqualTo(originalState.Username);
        Check.That(loadedState.Level).IsEqualTo(originalState.Level);
        Check.That(loadedState.Experience).IsEqualTo(originalState.Experience);
        Check.That(loadedState.Credits).IsEqualTo(originalState.Credits);
        Check.That(loadedState.Stats).IsNotNull();
        Check.That(loadedState.Stats.HackSpeed).IsEqualTo(originalState.Stats.HackSpeed);
        Check.That(loadedState.Stats.Stealth).IsEqualTo(originalState.Stats.Stealth);
        Check.That(loadedState.Stats.DataYield).IsEqualTo(originalState.Stats.DataYield);
        Check.That(loadedState.ActiveMissionIds).ContainsExactly(originalState.ActiveMissionIds);
        Check.That(loadedState.UnlockedPerkIds).ContainsExactly(originalState.UnlockedPerkIds);
        Check.That(loadedState.Version).IsEqualTo(originalState.Version);
        Check.That(loadedState.IsDevSave).IsEqualTo(originalState.IsDevSave);
        Check.That(loadedState.Checksum).IsNull();
    }

    // --- Add more tests here later ---
}
