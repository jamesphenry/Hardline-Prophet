﻿// tests/HardlineProphet.Tests/Infrastructure/Persistence/JsonGameStateRepositoryTests/LoadTests.cs
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

    // Helper methods (GetTestDirectoryPath, GetSaveFilePath, ComputeExpectedChecksum, SetupRawSaveFileAsync) are now inherited

    [Fact]
    public async Task LoadStateAsync_WhenSaveFileDoesNotExist_ShouldReturnNewGameStateWithDefaultValues()
    {
        // Arrange
        var username = "NewbieHacker";
        var testDir = GetTestDirectoryPath(); // Use inherited helper
        // Test in non-dev mode by default
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: false);
        var saveFilePath = GetSaveFilePath(username); // Use inherited helper
        if (File.Exists(saveFilePath)) { File.Delete(saveFilePath); }
        _output.WriteLine($"Ensured file does not exist at: {saveFilePath}");
        GameState loadedState = null!;
        // No need for try/finally for cleanup, base class Dispose handles it
        loadedState = await repository.LoadStateAsync(username); // Act
        // Assert
        Check.That(loadedState).IsNotNull();
        Check.That(loadedState.Username).IsEqualTo(username);
        Check.That(loadedState.Level).IsEqualTo(GameConstants.DefaultStartingLevel);
        Check.That(loadedState.Experience).IsEqualTo(GameConstants.DefaultStartingExperience);
        Check.That(loadedState.Credits).IsEqualTo(GameConstants.DefaultStartingCredits);
        Check.That(loadedState.Stats).IsNotNull().And.IsEqualTo(new PlayerStats()); // Check against default PlayerStats
        Check.That(loadedState.ActiveMissionIds).IsEmpty();
        Check.That(loadedState.UnlockedPerkIds).IsEmpty();
        Check.That(loadedState.Version).IsEqualTo(GameConstants.CurrentSaveVersion);
        Check.That(loadedState.Checksum).IsNull();
        Check.That(loadedState.IsDevSave).IsFalse();
    }

    [Fact]
    public async Task LoadStateAsync_WhenSaveFileExistsAndIsValid_ShouldReturnDeserializedGameState() // Now assumes valid includes checksum
    {
        // Arrange
        var username = "ExistingPlayer";
        var testDir = GetTestDirectoryPath(); // Use inherited helper
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: false); // Test in non-dev mode
        // 1. Create original state (without checksum)
        var originalState = new GameState
        {
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
        // 2. Calculate the correct checksum
        string correctChecksum = ComputeExpectedChecksum(originalState); // Use inherited helper
        // 3. Create the state object *with* the correct checksum to save
        var stateToSave = originalState with { Checksum = correctChecksum };
        // 4. Serialize stateToSave and write it to the file manually
        var saveFilePath = GetSaveFilePath(username); // Use inherited helper
        var jsonToSave = JsonSerializer.Serialize(stateToSave, _saveTestSerializerOptions); // Use base options
        await File.WriteAllTextAsync(saveFilePath, jsonToSave);
        _output.WriteLine($"Test save file created WITH CHECKSUM at: {saveFilePath}");

        GameState loadedState = null!; Exception caughtException = null!;
        try { loadedState = await repository.LoadStateAsync(username); } // Act
        catch (Exception ex) { caughtException = ex; _output.WriteLine($"Caught UNEXPECTED exception: {ex.GetType().Name} - {ex.Message}"); }
        // No finally needed for cleanup

        // Assert
        Check.That(caughtException).IsNull(); Check.That(loadedState).IsNotNull();
        // Assert: Detailed property checks
        Check.That(loadedState.Username).IsEqualTo(originalState.Username); Check.That(loadedState.Level).IsEqualTo(originalState.Level); Check.That(loadedState.Experience).IsEqualTo(originalState.Experience); Check.That(loadedState.Credits).IsEqualTo(originalState.Credits); Check.That(loadedState.Stats).IsNotNull().And.IsEqualTo(originalState.Stats); Check.That(loadedState.ActiveMissionIds).ContainsExactly(originalState.ActiveMissionIds); Check.That(loadedState.UnlockedPerkIds).ContainsExactly(originalState.UnlockedPerkIds); Check.That(loadedState.Version).IsEqualTo(originalState.Version); Check.That(loadedState.IsDevSave).IsEqualTo(originalState.IsDevSave); Check.That(loadedState.Checksum).IsEqualTo(correctChecksum);
    }

    [Fact]
    public async Task LoadStateAsync_WhenSaveFileIsCorrupted_ShouldThrowInvalidDataException()
    {
        var username = "CorruptedUser"; var testDir = GetTestDirectoryPath(); var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: false); var saveFilePath = GetSaveFilePath(username); string corruptedJson = "{ \"Username\": \"Corrupted\", \"Level\": \"NaN\", "; await File.WriteAllTextAsync(saveFilePath, corruptedJson); _output.WriteLine($"Created corrupted save file at: {saveFilePath}");
        await Assert.ThrowsAsync<InvalidDataException>(async () => await repository.LoadStateAsync(username)); // Act & Assert
                                                                                                               // Cleanup handled by base class Dispose
    }

    [Fact]
    public async Task LoadStateAsync_WhenChecksumIsValid_ShouldReturnLoadedState()
    {
        var username = "ValidChecksumUser"; var testDir = GetTestDirectoryPath(); var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: false); var originalState = new GameState { Username = username, Level = 7, Experience = 777.7, Credits = 700, Stats = new PlayerStats { HackSpeed = 7, Stealth = 7, DataYield = 7 }, Version = GameConstants.CurrentSaveVersion }; string correctChecksum = ComputeExpectedChecksum(originalState); var stateToSave = originalState with { Checksum = correctChecksum }; var saveFilePath = GetSaveFilePath(username); var jsonToSave = JsonSerializer.Serialize(stateToSave, _saveTestSerializerOptions); await File.WriteAllTextAsync(saveFilePath, jsonToSave); GameState loadedState = null!; Exception caughtException = null!; try { loadedState = await repository.LoadStateAsync(username); } catch (Exception ex) { caughtException = ex; }
        // No finally needed for cleanup
        Check.That(caughtException).IsNull(); Check.That(loadedState).IsNotNull(); Check.That(loadedState.Username).IsEqualTo(originalState.Username); Check.That(loadedState.Level).IsEqualTo(originalState.Level); Check.That(loadedState.Checksum).IsEqualTo(correctChecksum);
    }

    [Fact]
    public async Task LoadStateAsync_WhenChecksumIsInvalid_ShouldThrowInvalidDataException() // Implicitly tests non-dev mode
    {
        var username = "InvalidChecksumUser"; var testDir = GetTestDirectoryPath(); var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: false); var originalState = new GameState { Username = username, Level = 8, Experience = 888.8, Credits = 800, Stats = new PlayerStats { HackSpeed = 8, Stealth = 8, DataYield = 8 }, Version = GameConstants.CurrentSaveVersion }; var stateToSave = originalState with { Checksum = "THIS_IS_WRONG======" }; var saveFilePath = GetSaveFilePath(username); var jsonToSave = JsonSerializer.Serialize(stateToSave, _saveTestSerializerOptions); await File.WriteAllTextAsync(saveFilePath, jsonToSave);
        await Assert.ThrowsAsync<InvalidDataException>(async () => await repository.LoadStateAsync(username)); // Act & Assert
                                                                                                               // Cleanup handled by base class Dispose
    }

    // --- Dev Mode Tests ---

    [Fact]
    public async Task LoadStateAsync_WhenDevModeAndChecksumIsInvalid_ShouldLoadSuccessfully()
    {
        // Arrange
        var username = "DevInvalidChecksum";
        var testDir = GetTestDirectoryPath();
        // Create repository IN DEV MODE
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: true);
        var originalState = new GameState { Username = username, Level = 9, Credits = 900, Version = GameConstants.CurrentSaveVersion };
        // Save with an INCORRECT checksum
        var stateToSave = originalState with { Checksum = "THIS_IS_WRONG======" };
        var saveFilePath = GetSaveFilePath(username);
        var jsonToSave = JsonSerializer.Serialize(stateToSave, _saveTestSerializerOptions);
        await File.WriteAllTextAsync(saveFilePath, jsonToSave);
        _output.WriteLine($"Saved file with INCORRECT checksum for dev mode test at: {saveFilePath}");

        GameState loadedState = null!; Exception caughtException = null!;
        try { loadedState = await repository.LoadStateAsync(username); } // Act
        catch (Exception ex) { caughtException = ex; _output.WriteLine($"Caught UNEXPECTED exception in dev mode: {ex.GetType().Name} - {ex.Message}"); }
        // No finally needed for cleanup

        // Assert
        Check.That(caughtException).IsNull(); // Expect NO exception
        Check.That(loadedState).IsNotNull(); Check.That(loadedState.Username).IsEqualTo(originalState.Username); Check.That(loadedState.Level).IsEqualTo(originalState.Level); Check.That(loadedState.Checksum).IsEqualTo("THIS_IS_WRONG======");
    }

    [Fact]
    public async Task LoadStateAsync_WhenDevModeAndChecksumIsMissing_ShouldLoadSuccessfully()
    {
        // Arrange
        var username = "DevMissingChecksum";
        var testDir = GetTestDirectoryPath();
        // Create repository IN DEV MODE
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: true);
        // Create state WITHOUT checksum
        var originalState = new GameState { Username = username, Level = 11, Credits = 1100, Version = GameConstants.CurrentSaveVersion, Checksum = null };
        var saveFilePath = GetSaveFilePath(username);
        var jsonToSave = JsonSerializer.Serialize(originalState, _saveTestSerializerOptions);
        await File.WriteAllTextAsync(saveFilePath, jsonToSave);
        _output.WriteLine($"Saved file with MISSING checksum for dev mode test at: {saveFilePath}");

        GameState loadedState = null!; Exception caughtException = null!;
        try { loadedState = await repository.LoadStateAsync(username); } // Act
        catch (Exception ex) { caughtException = ex; _output.WriteLine($"Caught UNEXPECTED exception in dev mode: {ex.GetType().Name} - {ex.Message}"); }
        // No finally needed for cleanup

        // Assert
        Check.That(caughtException).IsNull(); // Expect NO exception
        Check.That(loadedState).IsNotNull(); Check.That(loadedState.Username).IsEqualTo(originalState.Username); Check.That(loadedState.Level).IsEqualTo(originalState.Level); Check.That(loadedState.Checksum).IsNull(); // Checksum should be null as it was missing
    }
}
