// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-feature-m2-flavor-events.1+7
// ║ 📄  File: GameStateExtensionsTests.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
using System;
using HardlineProphet.Core; // GameConstants (will add BaseXP constant later)
using HardlineProphet.Core.Extensions; // Math
using HardlineProphet.Core.Models;
using NFluent;
using Xunit;

namespace HardlineProphet.Tests.Core.Models;

public class GameStateExtensionsTests
{
    // Define BaseXP here for test consistency, will also add to GameConstants
    // Using double for potential precision in intermediate calculations
    private const double BaseXP = 100.0;

    // Helper to create GameState with specific XP
    private GameState CreateStateWithXp(double xp) => new GameState { Experience = xp };

    [Theory]
    // Test cases based on formula: Level = floor((XP / BaseXP)^(2/3)) + 1
    // Level 1 boundaries (BaseXP * 1^1.5 = 100 XP needed for Level 2)
    [InlineData(0.0, 1)]
    [InlineData(50.0, 1)]
    [InlineData(99.9, 1)]
    // Level 2 boundaries (BaseXP * 2^1.5 = ~282.84 XP needed for Level 3)
    [InlineData(100.0, 2)]
    [InlineData(150.0, 2)]
    [InlineData(282.8, 2)] // Close boundary
    // Level 3 boundaries (BaseXP * 3^1.5 = ~519.61 XP needed for Level 4)
    [InlineData(282.9, 3)] // Just over boundary
    [InlineData(400.0, 3)]
    [InlineData(519.6, 3)] // Close boundary
    // Level 4 boundary
    [InlineData(519.7, 4)] // Just over boundary
    // Higher level example (BaseXP * 9^1.5 = 2700 XP needed for Level 10)
    [InlineData(2699.9, 9)] // Close boundary
    [InlineData(2700.0, 10)]
    public void CalculateLevel_ReturnsCorrectLevelForExperience(
        double experience,
        int expectedLevel
    )
    {
        // Arrange
        var gameState = CreateStateWithXp(experience);

        // Act
        int actualLevel = gameState.CalculateLevel();

        // Assert
        Check.That(actualLevel).IsEqualTo(expectedLevel);
    }

    // Optional: Add test for negative XP if that's possible? Assume XP >= 0 for now.
    [Fact]
    public void CalculateLevel_WhenExperienceIsNegative_ShouldReturnLevel1()
    {
        // Arrange
        var gameState = CreateStateWithXp(-50.0);

        // Act
        int actualLevel = gameState.CalculateLevel(); // Will fail compile

        // Assert
        Check.That(actualLevel).IsEqualTo(1); // Expect level 1 for negative XP
    }
}
