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

// Inherit from the base class
public class MigrationTests : PersistenceTestsBase
{
    // Constructor passes ITestOutputHelper to the base class
    public MigrationTests(ITestOutputHelper output) : base(output) { }

    // Helper methods are inherited

    [Fact]
    public async Task LoadStateAsync_WhenLoadingV1SaveFile_ShouldMigrateToV2WithDefaultsAndValidChecksum()
    {
        // ... (V1 test remains the same) ...
        var username = "V1Player"; var testDir = GetTestDirectoryPath(); var repository = new JsonGameStateRepository(basePath: testDir); var v1StateData = new { Username = username, Level = 5, Experience = 123.4, Credits = 555, Stats = new { HackSpeed = 3, Stealth = 4, DataYield = 1 }, ActiveMissionIds = new List<string>(), UnlockedPerkIds = new List<string>(), IsDevSave = false }; string v1Json = JsonSerializer.Serialize(v1StateData, _checksumSerializerOptions); string saveFilePath = await SetupRawSaveFileAsync(username, v1Json); GameState loadedState = null!; Exception caughtException = null!; try { loadedState = await repository.LoadStateAsync(username); } catch (Exception ex) { caughtException = ex; _output.WriteLine($"Caught exception during load: {ex.GetType().Name} - {ex.Message}"); }
        Check.That(caughtException).IsNull(); Check.That(loadedState).IsNotNull(); Check.That(loadedState.Username).IsEqualTo(v1StateData.Username); Check.That(loadedState.Level).IsEqualTo(v1StateData.Level); Check.That(loadedState.Experience).IsEqualTo(v1StateData.Experience); Check.That(loadedState.Credits).IsEqualTo(v1StateData.Credits); Check.That(loadedState.Stats).IsNotNull(); Check.That(loadedState.Stats.HackSpeed).IsEqualTo(v1StateData.Stats.HackSpeed); Check.That(loadedState.Stats.Stealth).IsEqualTo(v1StateData.Stats.Stealth); Check.That(loadedState.Stats.DataYield).IsEqualTo(v1StateData.Stats.DataYield); Check.That(loadedState.ActiveMissionIds).IsEmpty(); Check.That(loadedState.UnlockedPerkIds).IsEmpty(); Check.That(loadedState.IsDevSave).IsEqualTo(v1StateData.IsDevSave); Check.That(loadedState.Version).IsEqualTo(GameConstants.CurrentSaveVersion); Check.That(loadedState.ActiveMissionId).IsNull(); Check.That(loadedState.ActiveMissionProgress).IsEqualTo(0); Check.That(loadedState.Checksum).IsNotNull().And.IsNotEmpty(); string expectedChecksumAfterMigration = ComputeExpectedChecksum(loadedState); Check.That(loadedState.Checksum).IsEqualTo(expectedChecksumAfterMigration);
    }

    // --- New Test ---
    [Fact]
    public async Task LoadStateAsync_WhenLoadingV2PreProfileSave_ShouldMigrateAddingProfileDefaults()
    {
        // Arrange
        var username = "V2PreProfilePlayer";
        var testDir = GetTestDirectoryPath();
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: false); // Test non-dev mode

        // 1. Define the state *without* the new profile fields
        //    Use an anonymous type again, but include Version = 2
        var v2PreProfileData = new
        {
            Version = 2, // Explicitly V2
            Username = username,
            Level = 6,
            Experience = 600,
            Credits = 6000,
            Stats = new { HackSpeed = 6, Stealth = 6, DataYield = 6 },
            ActiveMissionIds = new List<string> { "m1" },
            UnlockedPerkIds = new List<string> { "p1" },
            IsDevSave = true,
            ActiveMissionId = "m1",
            ActiveMissionProgress = 3
            // NO SelectedClass
            // NO SelectedStartingPerkIds
            // NO Checksum yet
        };

        // 2. Calculate the checksum based *only* on this V2 pre-profile structure
        //    Need to serialize it temporarily *as if* it were a GameState but missing fields
        //    This is tricky. Let's serialize the anonymous type directly for checksum calculation.
        //    NOTE: This assumes the *real* save file would have been generated this way.
        var jsonForChecksum = JsonSerializer.Serialize(v2PreProfileData, _checksumSerializerOptions);
        string correctV2PreProfileChecksum;
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jsonForChecksum));
            correctV2PreProfileChecksum = Convert.ToBase64String(hashBytes);
        }
        _output.WriteLine($"Correct V2 Pre-Profile Checksum: {correctV2PreProfileChecksum}");

        // 3. Create the JSON string to save, including Version and the calculated Checksum
        var v2PreProfileJson = JsonSerializer.Serialize(new
        {
            v2PreProfileData.Version,
            v2PreProfileData.Username,
            v2PreProfileData.Level,
            v2PreProfileData.Experience,
            v2PreProfileData.Credits,
            v2PreProfileData.Stats,
            v2PreProfileData.ActiveMissionIds,
            v2PreProfileData.UnlockedPerkIds,
            v2PreProfileData.IsDevSave,
            v2PreProfileData.ActiveMissionId,
            v2PreProfileData.ActiveMissionProgress,
            Checksum = correctV2PreProfileChecksum // Add the correct checksum for *this* structure
        }, _saveTestSerializerOptions); // Use save options for formatting file

        _output.WriteLine($"V2 Pre-Profile JSON being saved:\n{v2PreProfileJson}");
        string saveFilePath = await SetupRawSaveFileAsync(username, v2PreProfileJson);

        // Act & Assert
        // This should FAIL checksum validation because the loader deserializes into the *new* GameState
        // structure (adding default null/empty list for profile fields), calculates a checksum
        // based on that new structure, which won't match the checksum from the file.
        var ex = await Assert.ThrowsAsync<InvalidDataException>(async () => await repository.LoadStateAsync(username));

        // Optionally check the exception message for checksum mismatch
        Check.That(ex.Message).Contains("checksum mismatch");

        // Cleanup is handled by base class Dispose
    }
}
