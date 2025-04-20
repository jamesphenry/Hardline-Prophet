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
        try { /* Act */ loadedState = await repository.LoadStateAsync(username); }
        finally { CleanupDirectory(testDir); }
        // Assert (remains the same)
        Check.That(loadedState).IsNotNull(); // ... and other checks
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

    [Fact]
    public async Task LoadStateAsync_WhenSaveFileExistsAndIsValid_ShouldReturnDeserializedGameState()
    {
        // Arrange
        var username = "ExistingPlayer";
        var testDir = GetTestDirectory();
        var repository = new JsonGameStateRepository(testDir);
        var originalState = new GameState
        { /* ... */
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
        string saveFilePath = await PersistenceTestHelper.SetupExistingSaveFileAsync(testDir, username, originalState);
        _output.WriteLine($"Test save file created at: {saveFilePath}");
        GameState loadedState = null!;
        Exception caughtException = null!;
        try { /* Act */ loadedState = await repository.LoadStateAsync(username); }
        catch (Exception ex) { caughtException = ex; }
        finally { CleanupDirectory(testDir); }
        // Assert (remains the same)
        Check.That(caughtException).IsNull();
        Check.That(loadedState).IsNotNull(); // ... and other checks
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

    [Fact]
    public async Task LoadStateAsync_WhenSaveFileIsCorrupted_ShouldThrowInvalidDataException()
    {
        // Arrange
        var username = "CorruptedUser";
        var testDir = GetTestDirectory();
        var repository = new JsonGameStateRepository(testDir);
        var saveFilePath = Path.Combine(testDir, $"{username}.save.json");

        // Write invalid JSON content
        string corruptedJson = "{ \"Username\": \"Corrupted\", \"Level\": \"NaN\", "; // Malformed JSON
        await File.WriteAllTextAsync(saveFilePath, corruptedJson);
        _output.WriteLine($"Created corrupted save file at: {saveFilePath}");

        // Act & Assert
        // Use xUnit's Assert.ThrowsAsync to check for the specific exception type
        // This test should FAIL initially because the repository currently catches JsonException
        // but rethrows it wrapped in InvalidDataException, which is correct.
        // Let's verify the InvalidDataException and potentially its InnerException.
        InvalidDataException caughtException = null!;
        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(async () => await repository.LoadStateAsync(username));

            // If Assert.ThrowsAsync doesn't throw (meaning the wrong exception or no exception was thrown),
            // the test will fail here implicitly, or we can add an explicit fail.
            // We expect it to succeed in catching InvalidDataException.

            // We can optionally add more checks on the caught exception if needed,
            // but Assert.ThrowsAsync already verifies the type.
            // For example, check the inner exception:
            var actualException = await Record.ExceptionAsync(async () => await repository.LoadStateAsync(username));
            Check.That(actualException).IsInstanceOf<InvalidDataException>();
            Check.That(actualException.InnerException).IsInstanceOf<JsonException>();

        }
        finally
        {
            CleanupDirectory(testDir);
        }
    }

    // --- Add more tests here later for checksums ---
}
