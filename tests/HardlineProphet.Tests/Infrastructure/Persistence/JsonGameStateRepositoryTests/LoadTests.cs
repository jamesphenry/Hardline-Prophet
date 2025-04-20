// tests/HardlineProphet.Tests/Infrastructure/Persistence/JsonGameStateRepositoryTests/LoadTests.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Interfaces;
using HardlineProphet.Core.Models;
using HardlineProphet.Infrastructure.Persistence;
// using HardlineProphet.Tests.Helpers; // No longer needed if helpers are in base
using NFluent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HardlineProphet.Tests.Infrastructure.Persistence.JsonGameStateRepositoryTests;

// Inherit from the base class
public class LoadTests : PersistenceTestsBase
{
    // Constructor passes ITestOutputHelper to the base class
    public LoadTests(ITestOutputHelper output) : base(output) { }

    // Helper methods (GetTestDirectoryPath, GetSaveFilePath, ComputeExpectedChecksum) are now inherited

    [Fact]
    public async Task LoadStateAsync_WhenSaveFileDoesNotExist_ShouldReturnNewGameStateWithDefaultValues()
    {
        // Arrange
        var username = "NewbieHacker";
        var testDir = GetTestDirectoryPath(); // Use inherited helper
        var repository = new JsonGameStateRepository(testDir);
        var saveFilePath = GetSaveFilePath(username); // Use inherited helper
        if (File.Exists(saveFilePath)) { File.Delete(saveFilePath); }
        _output.WriteLine($"Ensured file does not exist at: {saveFilePath}");
        GameState loadedState = null!;
        // No need for try/finally for cleanup, base class Dispose handles it
        loadedState = await repository.LoadStateAsync(username);
        // Assert
        Check.That(loadedState).IsNotNull();
        Check.That(loadedState.Username).IsEqualTo(username); // ... other checks ...
        Check.That(loadedState.Level).IsEqualTo(GameConstants.DefaultStartingLevel);
        Check.That(loadedState.Experience).IsEqualTo(GameConstants.DefaultStartingExperience);
        Check.That(loadedState.Credits).IsEqualTo(GameConstants.DefaultStartingCredits);
        Check.That(loadedState.Stats).IsNotNull().And.IsEqualTo(new PlayerStats());
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
        var testDir = GetTestDirectoryPath(); // Use inherited helper
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
        string correctChecksum = ComputeExpectedChecksum(originalState); // Use inherited helper
        var stateToSave = originalState with { Checksum = correctChecksum };
        var saveFilePath = GetSaveFilePath(username); // Use inherited helper
        var jsonToSave = JsonSerializer.Serialize(stateToSave, _saveTestSerializerOptions); // Use base options
        await File.WriteAllTextAsync(saveFilePath, jsonToSave);
        _output.WriteLine($"Test save file created WITH CHECKSUM at: {saveFilePath}");

        GameState loadedState = null!;
        Exception caughtException = null!;
        try { loadedState = await repository.LoadStateAsync(username); } // Act
        catch (Exception ex) { caughtException = ex; _output.WriteLine($"Caught UNEXPECTED exception: {ex.GetType().Name} - {ex.Message}"); }
        // No finally needed for cleanup

        // Assert
        Check.That(caughtException).IsNull();
        Check.That(loadedState).IsNotNull();
        // Assert: Detailed property checks (remain the same)
        Check.That(loadedState.Username).IsEqualTo(originalState.Username); // ... other checks ...
        Check.That(loadedState.Level).IsEqualTo(originalState.Level);
        Check.That(loadedState.Experience).IsEqualTo(originalState.Experience);
        Check.That(loadedState.Credits).IsEqualTo(originalState.Credits);
        Check.That(loadedState.Stats).IsNotNull().And.IsEqualTo(originalState.Stats);
        Check.That(loadedState.ActiveMissionIds).ContainsExactly(originalState.ActiveMissionIds);
        Check.That(loadedState.UnlockedPerkIds).ContainsExactly(originalState.UnlockedPerkIds);
        Check.That(loadedState.Version).IsEqualTo(originalState.Version);
        Check.That(loadedState.IsDevSave).IsEqualTo(originalState.IsDevSave);
        Check.That(loadedState.Checksum).IsEqualTo(correctChecksum);
    }

    [Fact]
    public async Task LoadStateAsync_WhenSaveFileIsCorrupted_ShouldThrowInvalidDataException()
    {
        var username = "CorruptedUser";
        var testDir = GetTestDirectoryPath(); // Use inherited helper
        var repository = new JsonGameStateRepository(testDir);
        var saveFilePath = GetSaveFilePath(username); // Use inherited helper
        string corruptedJson = "{ \"Username\": \"Corrupted\", \"Level\": \"NaN\", ";
        await File.WriteAllTextAsync(saveFilePath, corruptedJson); // Write directly
        _output.WriteLine($"Created corrupted save file at: {saveFilePath}");
        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(async () => await repository.LoadStateAsync(username));
        // Cleanup handled by base class Dispose
    }

    [Fact]
    public async Task LoadStateAsync_WhenChecksumIsValid_ShouldReturnLoadedState()
    {
        var username = "ValidChecksumUser";
        var testDir = GetTestDirectoryPath(); // Use inherited helper
        var repository = new JsonGameStateRepository(testDir);
        var originalState = new GameState { Username = username, Level = 7, Experience = 777.7, Credits = 700, Stats = new PlayerStats { HackSpeed = 7, Stealth = 7, DataYield = 7 }, Version = GameConstants.CurrentSaveVersion };
        string correctChecksum = ComputeExpectedChecksum(originalState); // Use inherited helper
        var stateToSave = originalState with { Checksum = correctChecksum };
        var saveFilePath = GetSaveFilePath(username); // Use inherited helper
        var jsonToSave = JsonSerializer.Serialize(stateToSave, _saveTestSerializerOptions); // Use base options
        await File.WriteAllTextAsync(saveFilePath, jsonToSave);
        GameState loadedState = null!;
        Exception caughtException = null!;
        try { loadedState = await repository.LoadStateAsync(username); } // Act
        catch (Exception ex) { caughtException = ex; }
        // No finally needed for cleanup
        Check.That(caughtException).IsNull();
        Check.That(loadedState).IsNotNull();
        Check.That(loadedState.Username).IsEqualTo(originalState.Username);
        Check.That(loadedState.Level).IsEqualTo(originalState.Level);
        Check.That(loadedState.Checksum).IsEqualTo(correctChecksum);
    }

    [Fact]
    public async Task LoadStateAsync_WhenChecksumIsInvalid_ShouldThrowInvalidDataException()
    {
        var username = "InvalidChecksumUser";
        var testDir = GetTestDirectoryPath(); // Use inherited helper
        var repository = new JsonGameStateRepository(testDir);
        var originalState = new GameState { Username = username, Level = 8, Experience = 888.8, Credits = 800, Stats = new PlayerStats { HackSpeed = 8, Stealth = 8, DataYield = 8 }, Version = GameConstants.CurrentSaveVersion };
        var stateToSave = originalState with { Checksum = "THIS_IS_WRONG======" };
        var saveFilePath = GetSaveFilePath(username); // Use inherited helper
        var jsonToSave = JsonSerializer.Serialize(stateToSave, _saveTestSerializerOptions); // Use base options
        await File.WriteAllTextAsync(saveFilePath, jsonToSave);
        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(async () => await repository.LoadStateAsync(username));
        // Cleanup handled by base class Dispose
    }
}
