// src/HardlineProphet/Core/GameConstants.cs
namespace HardlineProphet.Core;

/// <summary>
/// Defines global constants for the game.
/// </summary>
public static class GameConstants
{
    // --- Persistence ---
    public const int CurrentSaveVersion = 2;

    // --- New Player Defaults ---
    public const int DefaultStartingLevel = 1;
    public const double DefaultStartingExperience = 0.0;
    public const int DefaultStartingCredits = 100;
    public const int DefaultStartingHackSpeed = 5;
    public const int DefaultStartingStealth = 5;
    public const int DefaultStartingDataYield = 0;

    // --- Progression ---
    /// <summary>
    /// The base XP required for level calculations.
    /// XP for Level L = BaseXP * (L-1)^1.5
    /// </summary>
    public const double BaseXPForLeveling = 100.0;

    // --- Add other constants here later ---
}
