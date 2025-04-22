// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-alpha.11
// ║ 📄  File: MigrationTests.cs
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

public class MigrationTests : PersistenceTestsBase
{
    public MigrationTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public async Task LoadStateAsync_WhenLoadingV1SaveFile_ShouldMigrateToV3WithDefaultsAndValidChecksum() // V1 -> V3 now
    {
        // Arrange
        var username = "V1Player";
        var testDir = GetTestDirectoryPath();
        var repository = new JsonGameStateRepository(basePath: testDir);
        var v1StateData = new
        {
            Username = username,
            Level = 5,
            Experience = 123.4,
            Credits = 555,
            Stats = new
            {
                HackSpeed = 3,
                Stealth = 4,
                DataYield = 1,
            },
            ActiveMissionIds = new List<string>(),
            UnlockedPerkIds = new List<string>(),
            IsDevSave = false,
        };
        string v1Json = JsonSerializer.Serialize(v1StateData, _checksumSerializerOptions);
        string saveFilePath = await SetupRawSaveFileAsync(username, v1Json);
        GameState loadedState = null!;
        Exception caughtException = null!;

        try
        {
            loadedState = await repository.LoadStateAsync(username);
        } // Act
        catch (Exception ex)
        {
            caughtException = ex;
            _output.WriteLine($"Caught exception during load: {ex.GetType().Name} - {ex.Message}");
        }

        // Assert
        Check.That(caughtException).IsNull();
        Check.That(loadedState).IsNotNull();
        // Check V1 data
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
        // Check V2+ fields have defaults
        Check.That(loadedState.ActiveMissionId).IsNull();
        Check.That(loadedState.ActiveMissionProgress).IsEqualTo(0);
        Check.That(loadedState.SelectedClass).IsNull();
        Check.That(loadedState.SelectedStartingPerkIds).IsEmpty();
        Check.That(loadedState.TraceLevel).IsEqualTo(0.0); // Check new V3 field default
        // Check final version and checksum are V3
        Check.That(loadedState.Version).IsEqualTo(GameConstants.CurrentSaveVersion); // Expect 3
        Check.That(loadedState.Checksum).IsNotNull().And.IsNotEmpty();
        string expectedChecksumAfterMigration = ComputeExpectedChecksum(loadedState); // Checksum based on final V3 structure
        Check.That(loadedState.Checksum).IsEqualTo(expectedChecksumAfterMigration);
    }

    [Fact]
    public async Task LoadStateAsync_WhenLoadingV2PreTraceSave_ShouldMigrateToV3AddingTraceLevelDefault() // V2 -> V3 now
    {
        // Arrange
        var username = "V2PreTracePlayer";
        var testDir = GetTestDirectoryPath();
        var repository = new JsonGameStateRepository(basePath: testDir, isDevMode: false);
        var v2PreTraceData = new
        {
            Version = 2,
            Username = username,
            Level = 6,
            Experience = 600,
            Credits = 6000,
            Stats = new
            {
                HackSpeed = 6,
                Stealth = 6,
                DataYield = 6,
            },
            ActiveMissionIds = new List<string> { "m1" },
            UnlockedPerkIds = new List<string> { "p1" },
            IsDevSave = true,
            ActiveMissionId = "m1",
            ActiveMissionProgress = 3,
            SelectedClass = PlayerClass.Ghost,
            SelectedStartingPerkIds = new List<string> { "trace_dampener" }, /* No TraceLevel */
        };
        var jsonForChecksum = JsonSerializer.Serialize(v2PreTraceData, _checksumSerializerOptions);
        string correctV2PreTraceChecksum;
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jsonForChecksum));
            correctV2PreTraceChecksum = Convert.ToBase64String(hashBytes);
        }
        var v2PreTraceJson = JsonSerializer.Serialize(
            new
            {
                v2PreTraceData.Version,
                v2PreTraceData.Username,
                v2PreTraceData.Level,
                v2PreTraceData.Experience,
                v2PreTraceData.Credits,
                v2PreTraceData.Stats,
                v2PreTraceData.ActiveMissionIds,
                v2PreTraceData.UnlockedPerkIds,
                v2PreTraceData.IsDevSave,
                v2PreTraceData.ActiveMissionId,
                v2PreTraceData.ActiveMissionProgress,
                v2PreTraceData.SelectedClass,
                v2PreTraceData.SelectedStartingPerkIds,
                Checksum = correctV2PreTraceChecksum,
            },
            _saveTestSerializerOptions
        );
        string saveFilePath = await SetupRawSaveFileAsync(username, v2PreTraceJson);

        GameState loadedState = null!;
        Exception caughtException = null!;
        try
        {
            loadedState = await repository.LoadStateAsync(username);
        } // Act
        catch (Exception ex)
        {
            caughtException = ex;
            _output.WriteLine($"Caught exception during load: {ex.GetType().Name} - {ex.Message}");
        }

        // Assert
        // Now expect successful migration, not an exception
        Check.That(caughtException).IsNull();
        Check.That(loadedState).IsNotNull();
        // Check original V2 data preserved
        Check.That(loadedState.Username).IsEqualTo(v2PreTraceData.Username);
        Check.That(loadedState.Level).IsEqualTo(v2PreTraceData.Level);
        Check.That(loadedState.Experience).IsEqualTo(v2PreTraceData.Experience);
        Check.That(loadedState.Credits).IsEqualTo(v2PreTraceData.Credits);
        Check.That(loadedState.Stats).IsNotNull();
        Check.That(loadedState.Stats.HackSpeed).IsEqualTo(v2PreTraceData.Stats.HackSpeed);
        Check.That(loadedState.Stats.Stealth).IsEqualTo(v2PreTraceData.Stats.Stealth);
        Check.That(loadedState.Stats.DataYield).IsEqualTo(v2PreTraceData.Stats.DataYield);
        Check.That(loadedState.ActiveMissionIds).ContainsExactly(v2PreTraceData.ActiveMissionIds);
        Check.That(loadedState.UnlockedPerkIds).ContainsExactly(v2PreTraceData.UnlockedPerkIds);
        Check.That(loadedState.IsDevSave).IsEqualTo(v2PreTraceData.IsDevSave);
        Check.That(loadedState.ActiveMissionId).IsEqualTo(v2PreTraceData.ActiveMissionId);
        Check
            .That(loadedState.ActiveMissionProgress)
            .IsEqualTo(v2PreTraceData.ActiveMissionProgress);
        Check.That(loadedState.SelectedClass).IsEqualTo(v2PreTraceData.SelectedClass);
        Check
            .That(loadedState.SelectedStartingPerkIds)
            .ContainsExactly(v2PreTraceData.SelectedStartingPerkIds);
        // Check V3 field default
        Check.That(loadedState.TraceLevel).IsEqualTo(0.0);
        // Check final version and checksum are V3
        Check.That(loadedState.Version).IsEqualTo(GameConstants.CurrentSaveVersion); // Expect 3
        Check.That(loadedState.Checksum).IsNotNull().And.IsNotEmpty();
        string expectedChecksumAfterMigration = ComputeExpectedChecksum(loadedState); // Checksum based on final V3 structure
        Check.That(loadedState.Checksum).IsEqualTo(expectedChecksumAfterMigration);
    }
}
