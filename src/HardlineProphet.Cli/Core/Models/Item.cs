// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-feature-m2-flavor-events.1+8
// ║ 📄  File: Item.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
using System.Text.Json.Serialization; // For potential future attributes

namespace HardlineProphet.Core.Models;

/// <summary>
/// Represents an item available for purchase in the shop.
/// </summary>
public record Item
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = "Unknown Item";
    public int Cost { get; init; } = 0;

    /// <summary>
    /// User-facing description of the item's effect.
    /// </summary>
    public string EffectDescription { get; init; } = "Does something mysterious.";

    // TODO: Potentially add structured effect data later instead of just description
    // public EffectData Effect { get; init; }
}
