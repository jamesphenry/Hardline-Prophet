using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardlineProphet.Core.Models;

public record GameState
{
    public int Version { get; init; } = GameConstants.CurrentSaveVersion;
    public string Username { get; init; } = string.Empty;
    public int Level { get; init; } = GameConstants.DefaultStartingLevel;
    public double Experience { get; init; } = GameConstants.DefaultStartingExperience;
    public int Credits { get; init; } = GameConstants.DefaultStartingCredits;
    public PlayerStats Stats { get; init; } = new PlayerStats(); // Use the PlayerStats record
    public List<string> ActiveMissionIds { get; init; } = new List<string>();
    public List<string> UnlockedPerkIds { get; init; } = new List<string>();

    // Checksum needs to be mutable if calculated *after* initial state creation/load
    // Or we create a new record instance when calculating checksum before save.
    // Let's keep it init for now, and handle checksum calculation during save later.
    public string? Checksum { get; init; } = null;

    public bool IsDevSave { get; init; } = false;
    // --- New Mission Tracking Properties ---
    /// <summary>
    /// The ID of the currently active mission. Null if no mission active.
    /// For M1, we'll likely just assign the default one if null.
    /// </summary>
    public string? ActiveMissionId { get; init; } = null;

    /// <summary>
    /// Progress counter for the active mission (e.g., number of ticks completed).
    /// </summary>
    public int ActiveMissionProgress { get; init; } = 0;
}

