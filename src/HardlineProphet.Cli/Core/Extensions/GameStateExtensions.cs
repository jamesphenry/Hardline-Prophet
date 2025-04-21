// src/HardlineProphet/Core/Extensions/GameStateExtensions.cs
using HardlineProphet.Core.Models; // GameState
using System; // Math

namespace HardlineProphet.Core.Extensions;

/// <summary>
/// Extension methods for the GameState record.
/// </summary>
public static class GameStateExtensions
{
    /// <summary>
    /// Calculates the player's current level based on their experience points.
    /// Uses an iterative approach to avoid floating-point precision issues near boundaries.
    /// Based on formula: XP Threshold to reach Level L = BaseXP * (L-1)^1.5
    /// </summary>
    /// <param name="gameState">The game state.</param>
    /// <returns>The calculated player level (minimum 1).</returns>
    public static int CalculateLevel(this GameState gameState)
    {
        double currentXp = gameState.Experience;

        // Handle base case and negative XP
        if (currentXp < GameConstants.BaseXPForLeveling) // XP needed to reach level 2 is BaseXP * (2-1)^1.5 = BaseXP
        {
            return 1;
        }

        int currentLevel = 1;
        while (true)
        {
            // Calculate XP needed to reach the *next* level (currentLevel + 1)
            // Threshold = BaseXP * ((currentLevel + 1) - 1)^1.5 = BaseXP * currentLevel^1.5
            double xpThresholdForNextLevel = GameConstants.BaseXPForLeveling * Math.Pow(currentLevel, 1.5);

            // If current XP is less than the threshold for the next level, then we are at the current level.
            if (currentXp < xpThresholdForNextLevel)
            {
                return currentLevel;
            }

            // Otherwise, increment the level and check the threshold for the level after that.
            currentLevel++;

            // Safety break for extremely high levels, adjust if needed
            if (currentLevel > 9999)
            {
                Console.Error.WriteLine("Warning: Max level calculation reached in CalculateLevel.");
                return currentLevel;
            }
        }
    }

    // Example threshold calculation (could be separate helper if needed elsewhere)
    private static double CalculateXpThreshold(int level)
    {
        if (level <= 1) return 0;
        return GameConstants.BaseXPForLeveling * Math.Pow(level - 1, 1.5);
    }
}
