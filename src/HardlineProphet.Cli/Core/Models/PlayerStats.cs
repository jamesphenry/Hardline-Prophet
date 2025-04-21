// src/HardlineProphet/Core/Models/PlayerStats.cs
namespace HardlineProphet.Core.Models;

/// <summary>
/// Represents the player's statistics.
/// Using a class with settable properties to allow modification by upgrades.
/// </summary>
public class PlayerStats
{
    // Use get; set; to allow modification
    public int HackSpeed { get; set; } = GameConstants.DefaultStartingHackSpeed;
    public int Stealth { get; set; } = GameConstants.DefaultStartingStealth;
    public int DataYield { get; set; } = GameConstants.DefaultStartingDataYield;

    // Parameterless constructor for creation and deserialization
    public PlayerStats() { }

    // Optional: Add a constructor for explicit creation if desired elsewhere
    // public PlayerStats(int hackSpeed, int stealth, int dataYield)
    // {
    //     HackSpeed = hackSpeed;
    //     Stealth = stealth;
    //     DataYield = dataYield;
    // }

    // Note: Since this is a class, equality will be reference-based by default.
    // If value-based comparison is needed elsewhere (e.g., tests),
    // we might need to override Equals/GetHashCode or use helpers.
}
