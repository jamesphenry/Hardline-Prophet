// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-alpha.11
// ║ 📄  File: PlayerStatsTests.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Models;
using NFluent;
using Xunit;

namespace HardlineProphet.Tests.Core.Models;

public class PlayerStatsTests
{
    [Fact]
    public void ApplyUpgrade_WithFlatBonus_ShouldIncreaseCorrectStat()
    {
        // Arrange
        var stats = new PlayerStats
        {
            HackSpeed = 10,
            Stealth = 5,
            DataYield = 0,
        }; // Start with known values
        var item = new Item
        {
            Id = "scrambler",
            Name = "Test Scrambler",
            Cost = 1,
            EffectDescription = "+5 Stealth",
        };
        var expectedStealth = stats.Stealth + 5;

        // Act
        stats.ApplyUpgrade(item);

        // Assert
        Check.That(stats.Stealth).IsEqualTo(expectedStealth);
        Check.That(stats.HackSpeed).IsEqualTo(10); // Ensure other stats unchanged
        Check.That(stats.DataYield).IsEqualTo(0);
    }

    [Fact]
    public void ApplyUpgrade_WithPercentageBonus_ShouldIncreaseCorrectStat()
    {
        // Arrange
        var stats = new PlayerStats
        {
            HackSpeed = 20,
            Stealth = 10,
            DataYield = 5,
        }; // Start with known values
        var item = new Item
        {
            Id = "accelerator",
            Name = "Test Accelerator",
            Cost = 1,
            EffectDescription = "+10% HackSpeed",
        };
        // Calculate expected: 10% of 20 is 2. Expected = 20 + 2 = 22
        // Using Math.Ceiling for percentage increase to handle potential fractions favorably? Or Floor? Let's use Ceiling for now.
        var expectedHackSpeed = stats.HackSpeed + (int)Math.Ceiling(stats.HackSpeed * 0.10);

        // Act
        stats.ApplyUpgrade(item);

        // Assert
        Check.That(stats.HackSpeed).IsEqualTo(expectedHackSpeed);
        Check.That(stats.Stealth).IsEqualTo(10); // Ensure other stats unchanged
        Check.That(stats.DataYield).IsEqualTo(5);
    }

    [Fact]
    public void ApplyUpgrade_WithUnknownEffect_ShouldNotChangeStats()
    {
        // Arrange
        var stats = new PlayerStats
        {
            HackSpeed = 10,
            Stealth = 10,
            DataYield = 10,
        };
        var initialStats = new
        {
            stats.HackSpeed,
            stats.Stealth,
            stats.DataYield,
        }; // Capture initial values
        var item = new Item
        {
            Id = "mystery",
            Name = "Mystery Meat",
            Cost = 1,
            EffectDescription = "??? Profit ???",
        };

        // Act
        stats.ApplyUpgrade(item);

        // Assert
        Check.That(stats.HackSpeed).IsEqualTo(initialStats.HackSpeed);
        Check.That(stats.Stealth).IsEqualTo(initialStats.Stealth);
        Check.That(stats.DataYield).IsEqualTo(initialStats.DataYield);
    }

    // TODO: Add tests for DataYield flat/percentage bonuses
    // TODO: Add tests for multiple applications, edge cases (negative stats?)
}
