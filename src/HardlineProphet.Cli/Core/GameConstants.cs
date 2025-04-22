// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-alpha.12
// ║ 📄  File: GameConstants.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
namespace HardlineProphet.Core;

/// <summary>
/// Defines global constants for the game.
/// </summary>
public static class GameConstants
{
    // --- Persistence ---
    /// <summary>
    /// The current version of the save game data structure.
    /// Increment this when breaking changes are made to GameState.
    /// V1: Initial release (pre-checksum, pre-mission fields)
    /// V2: Added Checksum, ActiveMissionId, ActiveMissionProgress, Profile fields
    /// V3: Added TraceLevel
    /// </summary>
    public const int CurrentSaveVersion = 3; // Incremented version

    // --- New Player Defaults ---
    public const int DefaultStartingLevel = 1;
    public const double DefaultStartingExperience = 0.0;
    public const int DefaultStartingCredits = 100; // Base, class/perks modify
    public const int DefaultStartingHackSpeed = 5;
    public const int DefaultStartingStealth = 5;
    public const int DefaultStartingDataYield = 0;

    // --- Progression ---
    public const double BaseXPForLeveling = 100.0;
}
