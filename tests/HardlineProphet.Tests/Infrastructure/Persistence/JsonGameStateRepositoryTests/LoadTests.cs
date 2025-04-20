// tests/HardlineProphet.Tests/Infrastructure/Persistence/JsonGameStateRepositoryTests/LoadTests.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Interfaces;
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

public class LoadTests
{
    private readonly ITestOutputHelper _output;
    public LoadTests(ITestOutputHelper output) { _output = output; }

    // Shared serializer options for checksum calculation consistency
    private static readonly JsonSerializerOptions _checksumSerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null
    };
    // Shared serializer options for saving test files (can differ)
    private static readonly JsonSerializerOptions _saveTestSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };


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

    // Helper to compute checksum matching the repository logic for test verification
    private string ComputeExpectedChecksum(GameState state)
    {
        var stateForHashing = state with { Checksum = null };
        var json = JsonSerializer.Serialize(stateForHashing, _checksumSerializerOptions);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }


    [Fact]
    public async Task LoadStateAsync_WhenSaveFileDoesNotExist_ShouldReturnNewGameStateWithDefaultValues()
    {
        // ... (test remains the same) ...
        var username = "NewbieHacker"; var testDir = GetTestDirectory(); var repository = new JsonGameStateRepository(testDir); var saveFilePath = Path.Combine(testDir, $"{username}.save.json"); if (File.Exists(saveFilePath)) { File.Delete(saveFilePath); }
        GameState loadedState = null!; try { loadedState = await repository.LoadStateAsync(username); } finally { CleanupDirectory(testDir); }
        Check.That(loadedState).IsNotNull(); Check.That(loadedState.Username).IsEqualTo(username); Check.That(loadedState.Level).IsEqualTo(GameConstants.DefaultStartingLevel); Check.That(loadedState.Experience).IsEqualTo(GameConstants.DefaultStartingExperience); Check.That(loadedState.Credits).IsEqualTo(GameConstants.DefaultStartingCredits); Check.That(loadedState.Stats).IsNotNull().And.IsEqualTo(new PlayerStats()); Check.That(loadedState.ActiveMissionIds).IsEmpty(); Check.That(loadedState.UnlockedPerkIds).IsEmpty(); Check.That(loadedState.Version).IsEqualTo(GameConstants.CurrentSaveVersion); Check.That(loadedState.Checksum).IsNull(); Check.That(loadedState.IsDevSave).IsFalse();
    }

    [Fact]
    public async Task LoadStateAsync_WhenSaveFileExistsAndIsValid_ShouldReturnDeserializedGameState()
    {
        // ... (test remains the same - saves *without* checksum) ...
        var username = "ExistingPlayer"; var testDir = GetTestDirectory(); var repository = new JsonGameStateRepository(testDir); var originalState = new GameState { Username = username, Level = 5, Experience = 1234.5, Credits = 5000, Stats = new PlayerStats { HackSpeed = 10, Stealth = 8, DataYield = 2 }, ActiveMissionIds = new System.Collections.Generic.List<string> { "mission_001" }, UnlockedPerkIds = new System.Collections.Generic.List<string> { "perk_abc" }, Version = GameConstants.CurrentSaveVersion, IsDevSave = true }; string saveFilePath = await PersistenceTestHelper.SetupExistingSaveFileAsync(testDir, username, originalState); GameState loadedState = null!; Exception caughtException = null!; try { loadedState = await repository.LoadStateAsync(username); } catch (Exception ex) { caughtException = ex; } finally { CleanupDirectory(testDir); }
        Check.That(caughtException).IsNull(); Check.That(loadedState).IsNotNull(); Check.That(loadedState.Username).IsEqualTo(originalState.Username); Check.That(loadedState.Level).IsEqualTo(originalState.Level); Check.That(loadedState.Experience).IsEqualTo(originalState.Experience); Check.That(loadedState.Credits).IsEqualTo(originalState.Credits); Check.That(loadedState.Stats).IsNotNull().And.IsEqualTo(originalState.Stats); Check.That(loadedState.ActiveMissionIds).ContainsExactly(originalState.ActiveMissionIds); Check.That(loadedState.UnlockedPerkIds).ContainsExactly(originalState.UnlockedPerkIds); Check.That(loadedState.Version).IsEqualTo(originalState.Version); Check.That(loadedState.IsDevSave).IsEqualTo(originalState.IsDevSave); Check.That(loadedState.Checksum).IsNull();
    }

    [Fact]
    public async Task LoadStateAsync_WhenSaveFileIsCorrupted_ShouldThrowInvalidDataException()
    {
        // ... (test remains the same) ...
        var username = "CorruptedUser"; var testDir = GetTestDirectory(); var repository = new JsonGameStateRepository(testDir); var saveFilePath = Path.Combine(testDir, $"{username}.save.json"); string corruptedJson = "{ \"Username\": \"Corrupted\", \"Level\": \"NaN\", "; await File.WriteAllTextAsync(saveFilePath, corruptedJson); try { await Assert.ThrowsAsync<InvalidDataException>(async () => await repository.LoadStateAsync(username)); } finally { CleanupDirectory(testDir); }
    }

    [Fact]
    public async Task LoadStateAsync_WhenChecksumIsValid_ShouldReturnLoadedState()
    {
        // ... (test remains the same) ...
        var username = "ValidChecksumUser"; var testDir = GetTestDirectory(); var repository = new JsonGameStateRepository(testDir); var originalState = new GameState { Username = username, Level = 7, Experience = 777.7, Credits = 700, Stats = new PlayerStats { HackSpeed = 7, Stealth = 7, DataYield = 7 }, Version = GameConstants.CurrentSaveVersion }; string correctChecksum = ComputeExpectedChecksum(originalState); var stateToSave = originalState with { Checksum = correctChecksum }; var saveFilePath = Path.Combine(testDir, $"{username}.save.json"); var jsonToSave = JsonSerializer.Serialize(stateToSave, _saveTestSerializerOptions); await File.WriteAllTextAsync(saveFilePath, jsonToSave); GameState loadedState = null!; Exception caughtException = null!; try { loadedState = await repository.LoadStateAsync(username); } catch (Exception ex) { caughtException = ex; } finally { CleanupDirectory(testDir); }
        Check.That(caughtException).IsNull(); Check.That(loadedState).IsNotNull(); Check.That(loadedState.Username).IsEqualTo(originalState.Username); Check.That(loadedState.Level).IsEqualTo(originalState.Level); Check.That(loadedState.Checksum).IsEqualTo(correctChecksum);
    }

    [Fact]
    public async Task LoadStateAsync_WhenChecksumIsInvalid_ShouldThrowInvalidDataException()
    {
        // Arrange
        var username = "InvalidChecksumUser";
        var testDir = GetTestDirectory();
        var repository = new JsonGameStateRepository(testDir);

        // 1. Create original state
        var originalState = new GameState
        { /* ... state setup ... */
            Username = username,
            Level = 8,
            Experience = 888.8,
            Credits = 800,
            Stats = new PlayerStats { HackSpeed = 8, Stealth = 8, DataYield = 8 },
            Version = GameConstants.CurrentSaveVersion
        };

        // 2. Create the state object with an INCORRECT checksum
        var stateToSave = originalState with { Checksum = "THIS_IS_WRONG======" };

        // 3. Serialize stateToSave and write it to the file manually
        var saveFilePath = Path.Combine(testDir, $"{username}.save.json");
        var jsonToSave = JsonSerializer.Serialize(stateToSave, _saveTestSerializerOptions);
        await File.WriteAllTextAsync(saveFilePath, jsonToSave);
        _output.WriteLine($"Saved file with INCORRECT checksum to: {saveFilePath}");

        // Act & Assert
        // This test should FAIL initially because LoadStateAsync doesn't validate checksums yet.
        // It will likely load the data successfully without throwing InvalidDataException.
        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(async () => await repository.LoadStateAsync(username));
            // If it gets here, the expected exception wasn't thrown - test should fail.
        }
        finally
        {
            CleanupDirectory(testDir);
        }
    }
}
