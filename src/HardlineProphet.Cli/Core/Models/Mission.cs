// src/HardlineProphet/Core/Models/Mission.cs
using System.Text.Json.Serialization; // For potential future attributes

namespace HardlineProphet.Core.Models;

/// <summary>
/// Represents the definition of a mission reward.
/// </summary>
public record MissionReward
{
    public int Credits { get; init; } = 0;
    public double Xp { get; init; } = 0.0;
}

/// <summary>
/// Represents the definition of a single mission template.
/// </summary>
public record Mission
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = "Unnamed Mission";
    // Duration in number of ticks required to complete
    public int DurationTicks { get; init; } = 10; // Default to 10 ticks
    public MissionReward Reward { get; init; } = new MissionReward(); // Default empty reward

    // --- New Property ---
    /// <summary>
    /// Chance (0.0 to 1.0) per tick of increasing global TraceLevel while active.
    /// </summary>
    public double TraceRisk { get; init; } = 0.0; // Default to 0 risk
}
