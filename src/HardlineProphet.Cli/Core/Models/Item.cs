// src/HardlineProphet/Core/Models/Item.cs
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