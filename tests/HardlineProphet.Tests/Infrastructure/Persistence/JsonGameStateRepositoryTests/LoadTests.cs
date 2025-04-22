// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-alpha.11
// ║ 📄  File: LoadTests.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Interfaces;
using HardlineProphet.Core.Models;
using HardlineProphet.Infrastructure.Persistence;
// using HardlineProphet.Tests.Helpers; // Using base class now
using NFluent;
using Xunit;
using Xunit.Abstractions;

namespace HardlineProphet.Tests.Infrastructure.Persistence.JsonGameStateRepositoryTests;

// Inherit from the base class
public class LoadTests : PersistenceTestsBase
{
    // Constructor passes ITestOutputHelper to the base class
    public LoadTests(ITestOutputHelper output)
        : base(output) { }

    // Helper methods (GetTestDirectoryPath, GetSaveFilePath, ComputeExpectedChecksum) are now inherited

    [Fact]
    public async Task LoadStateAsync_WhenSaveFileDoesNotExist_ShouldReturnNewGameStateWithDefaultValues()
    {
        // Arrange
        var username = "NewbieHacker";
        var testDir = GetTestDirectoryPath();
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: false);
        var saveFilePath = GetSaveFilePath(username);
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
        _output.WriteLine($"Ensured file does not exist at: {saveFilePath}");
        GameState loadedState = null!;
        loadedState = await repository.LoadStateAsync(username); // Act
        // Assert
        Check.That(loadedState).IsNotNull();
        Check.That(loadedState.Username).IsEqualTo(username);
        Check.That(loadedState.Level).IsEqualTo(GameConstants.DefaultStartingLevel);
        Check.That(loadedState.Experience).IsEqualTo(GameConstants.DefaultStartingExperience);
        Check.That(loadedState.Credits).IsEqualTo(GameConstants.DefaultStartingCredits);
        Check.That(loadedState.Stats).IsNotNull();
        // Check default stats individually
        var defaultStats = new PlayerStats();
        Check.That(loadedState.Stats.HackSpeed).IsEqualTo(defaultStats.HackSpeed);
        Check.That(loadedState.Stats.Stealth).IsEqualTo(defaultStats.Stealth);
        Check.That(loadedState.Stats.DataYield).IsEqualTo(defaultStats.DataYield);
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
        var testDir = GetTestDirectoryPath();
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: false);
        var originalState = new GameState
        {
            Username = username,
            Level = 5,
            Experience = 1234.5,
            Credits = 5000,
            Stats = new PlayerStats
            {
                HackSpeed = 10,
                Stealth = 8,
                DataYield = 2,
            }, // Create specific stats
            ActiveMissionIds = new List<string> { "mission_001" },
            UnlockedPerkIds = new List<string> { "perk_abc" },
            Version = GameConstants.CurrentSaveVersion,
            IsDevSave = true,
        };
        string correctChecksum = ComputeExpectedChecksum(originalState);
        var stateToSave = originalState with { Checksum = correctChecksum };
        var saveFilePath = GetSaveFilePath(username);
        var jsonToSave = JsonSerializer.Serialize(stateToSave, _saveTestSerializerOptions);
        await File.WriteAllTextAsync(saveFilePath, jsonToSave);
        _output.WriteLine($"Test save file created WITH CHECKSUM at: {saveFilePath}");

        GameState loadedState = null!;
        Exception caughtException = null!;
        try
        {
            loadedState = await repository.LoadStateAsync(username);
        } // Act
        catch (Exception ex)
        {
            caughtException = ex;
            _output.WriteLine($"Caught UNEXPECTED exception: {ex.GetType().Name} - {ex.Message}");
        }

        // Assert
        Check.That(caughtException).IsNull();
        Check.That(loadedState).IsNotNull();
        Check.That(loadedState.Username).IsEqualTo(originalState.Username);
        Check.That(loadedState.Level).IsEqualTo(originalState.Level);
        Check.That(loadedState.Experience).IsEqualTo(originalState.Experience);
        Check.That(loadedState.Credits).IsEqualTo(originalState.Credits);
        Check.That(loadedState.Stats).IsNotNull();
        // --- Check Stats Properties Individually ---
        Check.That(loadedState.Stats.HackSpeed).IsEqualTo(originalState.Stats.HackSpeed);
        Check.That(loadedState.Stats.Stealth).IsEqualTo(originalState.Stats.Stealth);
        Check.That(loadedState.Stats.DataYield).IsEqualTo(originalState.Stats.DataYield);
        // -----------------------------------------
        Check.That(loadedState.ActiveMissionIds).ContainsExactly(originalState.ActiveMissionIds);
        Check.That(loadedState.UnlockedPerkIds).ContainsExactly(originalState.UnlockedPerkIds);
        Check.That(loadedState.Version).IsEqualTo(originalState.Version);
        Check.That(loadedState.IsDevSave).IsEqualTo(originalState.IsDevSave);
        Check.That(loadedState.Checksum).IsEqualTo(correctChecksum);
    }

    [Fact]
    public async Task LoadStateAsync_WhenSaveFileIsCorrupted_ShouldThrowInvalidDataException()
    {
        // ... (test remains the same) ...
        var username = "CorruptedUser";
        var testDir = GetTestDirectoryPath();
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: false);
        var saveFilePath = GetSaveFilePath(username);
        string corruptedJson = "{ \"Username\": \"Corrupted\", \"Level\": \"NaN\", ";
        await File.WriteAllTextAsync(saveFilePath, corruptedJson);
        _output.WriteLine($"Created corrupted save file at: {saveFilePath}");
        await Assert.ThrowsAsync<InvalidDataException>(
            async () => await repository.LoadStateAsync(username)
        );
    }

    [Fact]
    public async Task LoadStateAsync_WhenChecksumIsValid_ShouldReturnLoadedState()
    {
        // ... (test remains the same) ...
        var username = "ValidChecksumUser";
        var testDir = GetTestDirectoryPath();
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: false);
        var originalState = new GameState
        {
            Username = username,
            Level = 7,
            Experience = 777.7,
            Credits = 700,
            Stats = new PlayerStats
            {
                HackSpeed = 7,
                Stealth = 7,
                DataYield = 7,
            },
            Version = GameConstants.CurrentSaveVersion,
        };
        string correctChecksum = ComputeExpectedChecksum(originalState);
        var stateToSave = originalState with { Checksum = correctChecksum };
        var saveFilePath = GetSaveFilePath(username);
        var jsonToSave = JsonSerializer.Serialize(stateToSave, _saveTestSerializerOptions);
        await File.WriteAllTextAsync(saveFilePath, jsonToSave);
        GameState loadedState = null!;
        Exception caughtException = null!;
        try
        {
            loadedState = await repository.LoadStateAsync(username);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }
        Check.That(caughtException).IsNull();
        Check.That(loadedState).IsNotNull();
        Check.That(loadedState.Username).IsEqualTo(originalState.Username);
        Check.That(loadedState.Level).IsEqualTo(originalState.Level);
        Check.That(loadedState.Checksum).IsEqualTo(correctChecksum);
    }

    [Fact]
    public async Task LoadStateAsync_WhenChecksumIsInvalid_ShouldThrowInvalidDataException()
    {
        // ... (test remains the same) ...
        var username = "InvalidChecksumUser";
        var testDir = GetTestDirectoryPath();
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: false);
        var originalState = new GameState
        {
            Username = username,
            Level = 8,
            Experience = 888.8,
            Credits = 800,
            Stats = new PlayerStats
            {
                HackSpeed = 8,
                Stealth = 8,
                DataYield = 8,
            },
            Version = GameConstants.CurrentSaveVersion,
        };
        var stateToSave = originalState with { Checksum = "THIS_IS_WRONG======" };
        var saveFilePath = GetSaveFilePath(username);
        var jsonToSave = JsonSerializer.Serialize(stateToSave, _saveTestSerializerOptions);
        await File.WriteAllTextAsync(saveFilePath, jsonToSave);
        await Assert.ThrowsAsync<InvalidDataException>(
            async () => await repository.LoadStateAsync(username)
        );
    }

    // --- Dev Mode Tests ---

    [Fact]
    public async Task LoadStateAsync_WhenDevModeAndChecksumIsInvalid_ShouldLoadSuccessfully()
    {
        // ... (test remains the same) ...
        var username = "DevInvalidChecksum";
        var testDir = GetTestDirectoryPath();
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: true);
        var originalState = new GameState
        {
            Username = username,
            Level = 9,
            Credits = 900,
            Version = GameConstants.CurrentSaveVersion,
        };
        var stateToSave = originalState with { Checksum = "THIS_IS_WRONG======" };
        var saveFilePath = GetSaveFilePath(username);
        var jsonToSave = JsonSerializer.Serialize(stateToSave, _saveTestSerializerOptions);
        await File.WriteAllTextAsync(saveFilePath, jsonToSave);
        _output.WriteLine(
            $"Saved file with INCORRECT checksum for dev mode test at: {saveFilePath}"
        );
        GameState loadedState = null!;
        Exception caughtException = null!;
        try
        {
            loadedState = await repository.LoadStateAsync(username);
        }
        catch (Exception ex)
        {
            caughtException = ex;
            _output.WriteLine(
                $"Caught UNEXPECTED exception in dev mode: {ex.GetType().Name} - {ex.Message}"
            );
        }
        Check.That(caughtException).IsNull();
        Check.That(loadedState).IsNotNull();
        Check.That(loadedState.Username).IsEqualTo(originalState.Username);
        Check.That(loadedState.Level).IsEqualTo(originalState.Level);
        Check.That(loadedState.Checksum).IsEqualTo("THIS_IS_WRONG======");
    }

    [Fact]
    public async Task LoadStateAsync_WhenDevModeAndChecksumIsMissing_ShouldLoadSuccessfully()
    {
        // ... (test remains the same) ...
        var username = "DevMissingChecksum";
        var testDir = GetTestDirectoryPath();
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: true);
        var originalState = new GameState
        {
            Username = username,
            Level = 11,
            Credits = 1100,
            Version = GameConstants.CurrentSaveVersion,
            Checksum = null,
        };
        var saveFilePath = GetSaveFilePath(username);
        var jsonToSave = JsonSerializer.Serialize(originalState, _saveTestSerializerOptions);
        await File.WriteAllTextAsync(saveFilePath, jsonToSave);
        _output.WriteLine($"Saved file with MISSING checksum for dev mode test at: {saveFilePath}");
        GameState loadedState = null!;
        Exception caughtException = null!;
        try
        {
            loadedState = await repository.LoadStateAsync(username);
        }
        catch (Exception ex)
        {
            caughtException = ex;
            _output.WriteLine(
                $"Caught UNEXPECTED exception in dev mode: {ex.GetType().Name} - {ex.Message}"
            );
        }
        Check.That(caughtException).IsNull();
        Check.That(loadedState).IsNotNull();
        Check.That(loadedState.Username).IsEqualTo(originalState.Username);
        Check.That(loadedState.Level).IsEqualTo(originalState.Level);
        Check.That(loadedState.Checksum).IsNull();
    }
}
