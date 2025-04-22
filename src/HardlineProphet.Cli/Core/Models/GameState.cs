// src/HardlineProphet/Core/Models/GameState.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HardlineProphet.Core.Models;

/// <summary>
/// Represents the complete state of the game for a player.
/// Using a record for value-based equality.
/// </summary>
public record GameState
{
    // Persistence & Core Stats
    // Assuming CurrentSaveVersion might become 3 after adding profile fields? Let's keep it 2 for now and handle migration.
    public int Version { get; init; } = GameConstants.CurrentSaveVersion; // Still target V2 initially
    public string Username { get; init; } = string.Empty;
    public int Level { get; init; } = GameConstants.DefaultStartingLevel;
    public double Experience { get; init; } = GameConstants.DefaultStartingExperience;
    public int Credits { get; init; } = GameConstants.DefaultStartingCredits;
    public PlayerStats Stats { get; init; } = new PlayerStats();
    public List<string> ActiveMissionIds { get; init; } = new List<string>(); // Keep for now, maybe simplify later
    public List<string> UnlockedPerkIds { get; init; } = new List<string>(); // For permanent perks later
    public string? Checksum { get; init; } = null;
    public bool IsDevSave { get; init; } = false;

    // Mission Tracking
    public string? ActiveMissionId { get; init; } = null;
    public int ActiveMissionProgress { get; init; } = 0;

    // --- New Player Profile Properties ---
    /// <summary>
    /// The starting class selected by the player.
    /// Null if profile setup not complete or for older saves before migration.
    /// </summary>
    public PlayerClass? SelectedClass { get; init; } = null; // Default to null

    /// <summary>
    /// The IDs of the starting perks selected/granted during profile setup.
    /// (Using string IDs for flexibility, could be enum if perks are fixed).
    /// </summary>
    public List<string> SelectedStartingPerkIds { get; init; } = new List<string>(); // Default to empty list
                                                                                     // ------------------------------------
                                                                                     // --- New Trace Property ---
    /// <summary>
    /// Current trace level (0.0 to 100.0). Increased by risky actions.
    /// </summary>
    public double TraceLevel { get; init; } = 0.0; // Default to 0
    // TODO: Add Difficulty Modifiers property later
}
