// tests/HardlineProphet.Tests/Infrastructure/Persistence/JsonGameStateRepositoryTests/MigrationTests.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Models;
using HardlineProphet.Infrastructure.Persistence;
using NFluent;
using System;
using System.Collections.Generic; // Added
using System.IO;
using System.Security.Cryptography; // Added
using System.Text; // Added
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HardlineProphet.Tests.Infrastructure.Persistence.JsonGameStateRepositoryTests;

/// <summary>
/// Tests focused on migrating older save game versions.
/// </summary>
// Inherit from the base class
public class MigrationTests : PersistenceTestsBase
{
    // Constructor passes ITestOutputHelper to the base class
    public MigrationTests(ITestOutputHelper output) : base(output) { }

    // Helper methods (GetTestDirectoryPath, GetSaveFilePath, ComputeExpectedChecksum, SetupRawSaveFileAsync) are inherited

    [Fact]
    public async Task LoadStateAsync_WhenLoadingV1SaveFile_ShouldMigrateToV2WithDefaultsAndValidChecksum()
    {
        // Arrange
        var username = "V1Player";
        var testDir = GetTestDirectoryPath(); // Use inherited helper
        var repository = new JsonGameStateRepository(testDir); // Use test dir

        // Define V1 structure (missing Version, Checksum, ActiveMissionId, ActiveMissionProgress)
        var v1StateData = new
        {
            Username = username,
            Level = 5,
            Experience = 123.4,
            Credits = 555,
            Stats = new { HackSpeed = 3, Stealth = 4, DataYield = 1 },
            ActiveMissionIds = new List<string>(),
            UnlockedPerkIds = new List<string>(),
            IsDevSave = false
        };
        // Use non-indented options consistent with checksum calculation for test data
        string v1Json = JsonSerializer.Serialize(v1StateData, _checksumSerializerOptions);
        _output.WriteLine($"V1 JSON being saved:\n{v1Json}");
        // Use inherited helper to write raw JSON
        string saveFilePath = await SetupRawSaveFileAsync(username, v1Json);

        GameState loadedState = null!;
        Exception caughtException = null!;

        try
        {
            // Act
            loadedState = await repository.LoadStateAsync(username);
        }
        catch (Exception ex) { caughtException = ex; _output.WriteLine($"Caught exception during load: {ex.GetType().Name} - {ex.Message}"); }
        // No finally needed for cleanup - handled by base class Dispose

        // Assert
        Check.That(caughtException).IsNull(); // Expect successful migration
        Check.That(loadedState).IsNotNull();

        // Check original V1 data is preserved
        Check.That(loadedState.Username).IsEqualTo(v1StateData.Username);
        Check.That(loadedState.Level).IsEqualTo(v1StateData.Level);
        Check.That(loadedState.Experience).IsEqualTo(v1StateData.Experience);
        Check.That(loadedState.Credits).IsEqualTo(v1StateData.Credits);
        Check.That(loadedState.Stats).IsNotNull();
        Check.That(loadedState.Stats.HackSpeed).IsEqualTo(v1StateData.Stats.HackSpeed);
        Check.That(loadedState.Stats.Stealth).IsEqualTo(v1StateData.Stats.Stealth);
        Check.That(loadedState.Stats.DataYield).IsEqualTo(v1StateData.Stats.DataYield);
        Check.That(loadedState.ActiveMissionIds).IsEmpty();
        Check.That(loadedState.UnlockedPerkIds).IsEmpty();
        Check.That(loadedState.IsDevSave).IsEqualTo(v1StateData.IsDevSave);

        // Check new V2 fields have default values
        Check.That(loadedState.Version).IsEqualTo(GameConstants.CurrentSaveVersion);
        Check.That(loadedState.ActiveMissionId).IsNull();
        Check.That(loadedState.ActiveMissionProgress).IsEqualTo(0);

        // Check that a *valid* V2 checksum was added during migration
        Check.That(loadedState.Checksum).IsNotNull().And.IsNotEmpty();
        string expectedChecksumAfterMigration = ComputeExpectedChecksum(loadedState); // Use inherited helper
        Check.That(loadedState.Checksum).IsEqualTo(expectedChecksumAfterMigration);
    }
}
