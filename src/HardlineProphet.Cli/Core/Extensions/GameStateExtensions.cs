// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-alpha.11
// ║ 📄  File: GameStateExtensions.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
using System; // Math
using System.Linq; // Contains
using HardlineProphet.Core.Models; // GameState, PlayerClass, PlayerStats

namespace HardlineProphet.Core.Extensions;

/// <summary>
/// Extension methods for the GameState record and related logic.
/// </summary>
public static class GameStateExtensions
{
    /// <summary>
    /// Calculates the player's current level based on their experience points.
    /// </summary>
    public static int CalculateLevel(this GameState gameState)
    {
        // ... (CalculateLevel logic remains the same) ...
        double currentXp = gameState.Experience;
        if (currentXp < GameConstants.BaseXPForLeveling)
        {
            return 1;
        }
        int currentLevel = 1;
        while (true)
        {
            double xpThresholdForNextLevel =
                GameConstants.BaseXPForLeveling * Math.Pow(currentLevel, 1.5);
            if (currentXp < xpThresholdForNextLevel)
            {
                return currentLevel;
            }
            currentLevel++;
            if (currentLevel > 9999)
            {
                Console.Error.WriteLine(
                    "Warning: Max level calculation reached in CalculateLevel."
                );
                return currentLevel;
            }
        }
    }

    /// <summary>
    /// Gets the default starting PlayerStats based on the selected class.
    /// </summary>
    public static PlayerStats GetDefaultStatsForClass(PlayerClass playerClass)
    {
        // Based on Readme 3.4 table (relative adjustments to base defaults)
        // Base defaults are defined in PlayerStats record init: HS=5, ST=5, DY=0
        int baseHackSpeed = GameConstants.DefaultStartingHackSpeed; // 5
        int baseStealth = GameConstants.DefaultStartingStealth; // 5
        int baseDataYield = GameConstants.DefaultStartingDataYield; // 0

        return playerClass switch
        {
            PlayerClass.Runner => new PlayerStats
            {
                HackSpeed = baseHackSpeed + 5,
                Stealth = baseStealth + 0,
                DataYield = baseDataYield + 0,
            }, // HS: 10, ST: 5, DY: 0 (+10% HS is effectively +5 from base 5?) - Revisit bonus interpretation later
            PlayerClass.Broker => new PlayerStats
            {
                HackSpeed = baseHackSpeed + 0,
                Stealth = baseStealth + 0,
                DataYield = baseDataYield + 10,
            }, // HS: 5, ST: 5, DY: 10 (+10% DY?)
            PlayerClass.Ghost => new PlayerStats
            {
                HackSpeed = baseHackSpeed + 0,
                Stealth = baseStealth + 10,
                DataYield = baseDataYield + 0,
            }, // HS: 5, ST: 15, DY: 0 (+15% ST?)
            _ => new PlayerStats(), // Default if class is Undefined
        };
        // Note: The "%" bonuses in the design doc might need clearer interpretation later.
        // For now, applying them as flat additions to the base default stats.
    }

    /// <summary>
    /// Gets the starting credits based on selected class and starting perk.
    /// </summary>
    public static int GetStartingCreditsForClass(PlayerClass playerClass, string? selectedPerkId)
    {
        int credits = GameConstants.DefaultStartingCredits; // Base starting credits

        // Class bonus (Broker starts with 250) - Design doc says +250, let's assume total 250.
        if (playerClass == PlayerClass.Broker)
        {
            credits = 250; // Explicit starting amount for Broker
        }

        // Perk bonus (Seed Capital adds 500)
        if (selectedPerkId == "seed_capital") // Match the ID defined in PerkSelectionDialog
        {
            credits += 500;
        }

        return credits;
    }
}
