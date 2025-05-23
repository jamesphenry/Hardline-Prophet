﻿// tests/HardlineProphet.Tests/Infrastructure/Persistence/JsonGameStateRepositoryTests/SaveTests.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Models;
using HardlineProphet.Infrastructure.Persistence;
// using HardlineProphet.Tests.Helpers; // No longer needed
using NFluent;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HardlineProphet.Tests.Infrastructure.Persistence.JsonGameStateRepositoryTests;

// Inherit from the base class
public class SaveTests : PersistenceTestsBase
{
    // Constructor passes ITestOutputHelper to the base class
    public SaveTests(ITestOutputHelper output) : base(output) { }

    // Helper methods are inherited

    [Fact]
    public async Task SaveStateAsync_ShouldSerializeStateAndWriteToFile()
    {
        // Arrange
        var username = "SavingPlayer";
        var testDir = GetTestDirectoryPath(); // Use inherited helper
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

        var expectedFilePath = GetSaveFilePath(username); // Use inherited helper
        if (File.Exists(expectedFilePath)) { File.Delete(expectedFilePath); }

        Exception caughtException = null!;
        GameState deserializedState = null!;
        try
        {
            // Act
            await repository.SaveStateAsync(stateToSave);

            // Assert (Reading back the file)
            Check.That(File.Exists(expectedFilePath)).IsTrue();
            var json = await File.ReadAllTextAsync(expectedFilePath);
            deserializedState = JsonSerializer.Deserialize<GameState>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex) { caughtException = ex; _output.WriteLine($"!!! Exception caught: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}"); }
        // No finally needed for cleanup

        // Assert (Checking the content)
        Check.That(caughtException).IsNull();
        Check.That(deserializedState).IsNotNull();
        // Assert: Detailed property checks
        Check.That(deserializedState.Username).IsEqualTo(stateToSave.Username);
        Check.That(deserializedState.Level).IsEqualTo(stateToSave.Level);
        Check.That(deserializedState.Experience).IsEqualTo(stateToSave.Experience);
        Check.That(deserializedState.Credits).IsEqualTo(stateToSave.Credits);
        Check.That(deserializedState.Stats).IsNotNull().And.IsEqualTo(stateToSave.Stats);
        Check.That(deserializedState.ActiveMissionIds).ContainsExactly(stateToSave.ActiveMissionIds);
        Check.That(deserializedState.UnlockedPerkIds).ContainsExactly(stateToSave.UnlockedPerkIds);
        Check.That(deserializedState.Version).IsEqualTo(stateToSave.Version);
        Check.That(deserializedState.IsDevSave).IsEqualTo(stateToSave.IsDevSave);
        // Checksum is generated by SaveStateAsync, so it should NOT be null here
        Check.That(deserializedState.Checksum).IsNotNull().And.IsNotEmpty();
        // Optionally verify the checksum value itself if needed, though covered by checksum test
        // string expectedChecksum = ComputeExpectedChecksum(stateToSave);
        // Check.That(deserializedState.Checksum).IsEqualTo(expectedChecksum);
    }

    [Fact]
    public async Task SaveStateAsync_ShouldCalculateAndStoreChecksum()
    {
        // Arrange
        var username = "ChecksumPlayer";
        var testDir = GetTestDirectoryPath(); // Use inherited helper
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
        var expectedFilePath = GetSaveFilePath(username); // Use inherited helper
        if (File.Exists(expectedFilePath)) { File.Delete(expectedFilePath); }

        string expectedChecksum = ComputeExpectedChecksum(stateToSave); // Use inherited helper
        _output.WriteLine($"Calculated expected checksum: {expectedChecksum}");

        GameState deserializedState = null!;
        // No try/finally needed for cleanup
        // Act
        await repository.SaveStateAsync(stateToSave);

        // Assert (Read back)
        Check.That(File.Exists(expectedFilePath)).IsTrue();
        var savedJson = await File.ReadAllTextAsync(expectedFilePath);
        _output.WriteLine($"Actual saved JSON:\n{savedJson}");
        deserializedState = JsonSerializer.Deserialize<GameState>(savedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert (Checksum)
        Check.That(deserializedState).IsNotNull();
        Check.That(deserializedState.Checksum).IsNotNull().And.IsNotEmpty();
        Check.That(deserializedState.Checksum).IsEqualTo(expectedChecksum);
        Check.That(deserializedState.Username).IsEqualTo(stateToSave.Username);
        Check.That(deserializedState.Level).IsEqualTo(stateToSave.Level);
    }
}
