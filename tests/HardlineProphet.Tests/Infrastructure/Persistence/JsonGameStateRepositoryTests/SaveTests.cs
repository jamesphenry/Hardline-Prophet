// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-feature-m2-flavor-events.1+8
// ║ 📄  File: SaveTests.cs
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
using HardlineProphet.Core.Models;
using HardlineProphet.Infrastructure.Persistence;
using NFluent;
using Xunit;
using Xunit.Abstractions;

namespace HardlineProphet.Tests.Infrastructure.Persistence.JsonGameStateRepositoryTests;

// Inherit from the base class
public class SaveTests : PersistenceTestsBase
{
    // Constructor passes ITestOutputHelper to the base class
    public SaveTests(ITestOutputHelper output)
        : base(output) { }

    // Helper methods are inherited

    [Fact]
    public async Task SaveStateAsync_ShouldSerializeStateAndWriteToFile()
    {
        // Arrange
        var username = "SavingPlayer";
        var testDir = GetTestDirectoryPath();
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: false);
        var stateToSave = new GameState
        {
            Username = username,
            Level = 10,
            Experience = 9999.9,
            Credits = 12345,
            Stats = new PlayerStats
            {
                HackSpeed = 15,
                Stealth = 12,
                DataYield = 5,
            },
            ActiveMissionIds = new List<string> { "mission_save_test" },
            UnlockedPerkIds = new List<string> { "perk_save_test" },
            Version = GameConstants.CurrentSaveVersion,
            IsDevSave = false,
        };
        var expectedFilePath = GetSaveFilePath(username);
        if (File.Exists(expectedFilePath))
        {
            File.Delete(expectedFilePath);
        }

        Exception caughtException = null!;
        GameState deserializedState = null!; // Initialized to null
        try
        {
            // Act
            await repository.SaveStateAsync(stateToSave);
            // Assert (Reading back the file)
            Check.That(File.Exists(expectedFilePath)).IsTrue();
            var json = await File.ReadAllTextAsync(expectedFilePath);
            deserializedState = JsonSerializer.Deserialize<GameState>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
        catch (Exception ex)
        {
            caughtException = ex;
            _output.WriteLine(
                $"!!! Exception caught: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}"
            );
        }
        // Cleanup handled by base class Dispose

        // Assert (Checking the content)
        Check.That(caughtException).IsNull();
        Check.That(deserializedState).IsNotNull(); // Check not null first

        // Assert: Detailed property checks (Use null-forgiving '!' after IsNotNull check)
        Check.That(deserializedState!.Username).IsEqualTo(stateToSave.Username);
        Check.That(deserializedState!.Level).IsEqualTo(stateToSave.Level);
        Check.That(deserializedState!.Experience).IsEqualTo(stateToSave.Experience);
        Check.That(deserializedState!.Credits).IsEqualTo(stateToSave.Credits);
        Check.That(deserializedState!.Stats).IsNotNull(); // Check nested Stats not null
        Check.That(deserializedState!.Stats.HackSpeed).IsEqualTo(stateToSave.Stats.HackSpeed);
        Check.That(deserializedState!.Stats.Stealth).IsEqualTo(stateToSave.Stats.Stealth);
        Check.That(deserializedState!.Stats.DataYield).IsEqualTo(stateToSave.Stats.DataYield);
        Check
            .That(deserializedState!.ActiveMissionIds)
            .ContainsExactly(stateToSave.ActiveMissionIds);
        Check.That(deserializedState!.UnlockedPerkIds).ContainsExactly(stateToSave.UnlockedPerkIds);
        Check.That(deserializedState!.Version).IsEqualTo(stateToSave.Version); // Version should match V3 now
        Check.That(deserializedState!.IsDevSave).IsEqualTo(stateToSave.IsDevSave);
        Check.That(deserializedState!.Checksum).IsNotNull().And.IsNotEmpty();
    }

    [Fact]
    public async Task SaveStateAsync_ShouldCalculateAndStoreChecksum()
    {
        // Arrange
        var username = "ChecksumPlayer";
        var testDir = GetTestDirectoryPath();
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: false);
        var stateToSave = new GameState
        {
            Username = username,
            Level = 3,
            Experience = 100.0,
            Credits = 50,
            Stats = new PlayerStats
            {
                HackSpeed = 6,
                Stealth = 7,
                DataYield = 1,
            },
            Version = GameConstants.CurrentSaveVersion,
            IsDevSave = false,
        };
        var expectedFilePath = GetSaveFilePath(username);
        if (File.Exists(expectedFilePath))
        {
            File.Delete(expectedFilePath);
        }
        string expectedChecksum = ComputeExpectedChecksum(stateToSave);
        _output.WriteLine($"Calculated expected checksum: {expectedChecksum}");

        GameState deserializedState = null!; // Initialized to null
        // Act
        await repository.SaveStateAsync(stateToSave);
        // Assert (Read back)
        Check.That(File.Exists(expectedFilePath)).IsTrue();
        var savedJson = await File.ReadAllTextAsync(expectedFilePath);
        deserializedState = JsonSerializer.Deserialize<GameState>(
            savedJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        // Cleanup handled by base class Dispose

        // Assert (Checksum)
        Check.That(deserializedState).IsNotNull(); // Check not null first
        Check.That(deserializedState!.Checksum).IsNotNull().And.IsNotEmpty(); // Use !
        Check.That(deserializedState!.Checksum).IsEqualTo(expectedChecksum);
        Check.That(deserializedState!.Username).IsEqualTo(stateToSave.Username);
        Check.That(deserializedState!.Level).IsEqualTo(stateToSave.Level);
    }
}
