// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-feature-m2-flavor-events.1+7
// ║ 📄  File: FlavorEvent.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
using System.Text.Json.Serialization; // For JsonConverter, JsonStringEnumConverter

namespace HardlineProphet.Core.Models;

/// <summary>
/// Defines the types of triggers for flavor events.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))] // Serialize as string name
public enum FlavorEventTrigger
{
    Unknown = 0,
    OnTick,
    OnLogin,
    OnLevelUp,
    // Add more triggers later (e.g., OnMissionComplete, OnPurchase)
}

/// <summary>
/// Represents the effect of a flavor event (simplified for now).
/// </summary>
public record FlavorEventEffect
{
    // Example: For "+1 Stealth" -> Stat = "Stealth", Value = 1, IsPercentage = false
    // Example: For "+10% HackSpeed" -> Stat = "HackSpeed", Value = 10, IsPercentage = true
    // For M2, we might just log the text and not apply effects yet.
    // Let's keep it simple initially and just focus on triggering the text.
    // We can add structured properties later if needed.
    // For now, maybe just a placeholder or leave empty? Let's leave empty.
}

/// <summary>
/// Represents a single flavor event definition.
/// </summary>
public record FlavorEvent
{
    public string Id { get; init; } = string.Empty;
    public FlavorEventTrigger Trigger { get; init; } = FlavorEventTrigger.Unknown;

    /// <summary>
    /// Probability (0.0 to 1.0) of this event triggering when its condition is met.
    /// </summary>
    public double Chance { get; init; } = 0.0;

    /// <summary>
    /// The narrative text displayed when the event triggers.
    /// </summary>
    public string Text { get; init; } = "...static...";

    /// <summary>
    /// The effect applied by the event (optional). Structure TBD.
    /// </summary>
    public FlavorEventEffect? Effect { get; init; } = null; // Keep nullable
}
