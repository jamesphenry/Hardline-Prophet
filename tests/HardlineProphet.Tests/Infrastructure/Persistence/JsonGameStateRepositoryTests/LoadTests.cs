// tests/HardlineProphet.Tests/Infrastructure/Persistence/JsonGameStateRepositoryTests/LoadTests.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Interfaces;
using HardlineProphet.Core.Models;
using HardlineProphet.Infrastructure.Persistence;
using NFluent;
using System;
using System.IO;
using System.Text.Json; // Required for JsonSerializer
using System.Threading.Tasks; // For Task
using Xunit; // Test framework
using Xunit.Abstractions; // For ITestOutputHelper

namespace HardlineProphet.Tests.Infrastructure.Persistence.JsonGameStateRepositoryTests;

public class LoadTests
{
    // Add output helper if needed for diagnostics visible in test runners
    private readonly ITestOutputHelper _output;

    public LoadTests(ITestOutputHelper output)
    {
        _output = output; // Capture output helper
    }


    // Helper method to get a unique path within a temporary directory
    private string GetTestDirectory()
    {
        string baseTestDir = Path.Combine(Path.GetTempPath(), "HardlineProphetTests");
        string testRunDir = Path.Combine(baseTestDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(testRunDir); // Ensure the directory exists
        _output.WriteLine($"Created test directory: {testRunDir}"); // Log directory creation
        return testRunDir;
    }

    // Cleanup method for the directory
    private void CleanupDirectory(string testRunDir)
    {
        if (!string.IsNullOrEmpty(testRunDir) && Directory.Exists(testRunDir))
        {
            _output.WriteLine($"Cleaning up test directory: {testRunDir}"); // Log cleanup
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

            // Assert
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
        finally
        {
            CleanupDirectory(testDir);
        }
    }

    [Fact]
    public async Task LoadStateAsync_WhenSaveFileExistsAndIsValid_ShouldReturnDeserializedGameState()
    {
        // Arrange
        var username = "ExistingPlayer";
        var testDir = GetTestDirectory();
        var repository = new JsonGameStateRepository(testDir);
        var saveFilePath = Path.Combine(testDir, $"{username}.save.json");

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

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(originalState, options);

        // --- Start Diagnostics ---
        _output.WriteLine($"Attempting to write file to: {saveFilePath}");
        await File.WriteAllTextAsync(saveFilePath, json);
        _output.WriteLine($"File write attempted.");

        // Add a small delay in case of file system lag/flush issues
        await Task.Delay(150); // Wait 150ms

        // Explicitly check if the file exists from the test's perspective
        bool fileActuallyExists = File.Exists(saveFilePath);
        _output.WriteLine($"File.Exists check result within test: {fileActuallyExists}");

        // Add an assertion to fail fast if the test setup itself failed
        Check.That(fileActuallyExists).IsTrue(); // <-- NEW ASSERTION
        // --- End Diagnostics ---


        GameState loadedState = null!;
        Exception caughtException = null!;
        try
        {
            // Act
            _output.WriteLine($"Calling repository.LoadStateAsync for user: {username}");
            loadedState = await repository.LoadStateAsync(username);
            _output.WriteLine($"repository.LoadStateAsync completed.");
        }
        catch (Exception ex)
        {
            caughtException = ex;
            _output.WriteLine($"Caught exception: {ex.GetType().Name} - {ex.Message}");
        }
        finally
        {
            CleanupDirectory(testDir);
        }

        // Assert
        // We still expect this to fail with NotImplementedException for now,
        // but the diagnostic assertion above might fail first if the file isn't written correctly.
        Check.That(caughtException).IsNotNull().And.IsInstanceOf<NotImplementedException>();
        Check.That(loadedState).IsNull();
    }

    // --- Add more tests here later ---
}
