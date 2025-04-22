// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-alpha.11
// ║ 📄  File: PlayerStats.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
using System; // Added for Math, StringSplitOptions, StringComparison
using System.Globalization; // Added for CultureInfo

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

    /// <summary>
    /// Applies the effect of a purchased item to these stats based on EffectDescription.
    /// Parses formats like "+5 Stealth" or "+10% HackSpeed".
    /// </summary>
    /// <param name="item">The item whose effect should be applied.</param>
    public void ApplyUpgrade(Item item)
    {
        // TODO: Refactor for M2/M3 - Use structured effect data on Item instead of parsing strings.
        //       Parsing EffectDescription is brittle and hard to extend reliably.
        if (item == null || string.IsNullOrWhiteSpace(item.EffectDescription))
        {
            Console.WriteLine($"DEBUG: ApplyUpgrade called with null item or empty effect.");
            return; // Cannot apply null/empty effect
        }

        string effect = item.EffectDescription.Trim();
        Console.WriteLine($"DEBUG: Applying upgrade '{item.Name}' with effect '{effect}'");

        // Simple parsing logic - see TODO above.
        try
        {
            string[] parts = effect.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return; // Need at least value and stat name

            string valuePart = parts[0];
            string statNamePart = parts[parts.Length - 1]; // Assume stat name is last part
            bool isPercentage = valuePart.Contains('%');

            // Clean up value string (remove '+' and '%')
            valuePart = valuePart.Replace("+", "").Replace("%", "");

            if (double.TryParse(valuePart, CultureInfo.InvariantCulture, out double value))
            {
                // Determine target stat (case-insensitive)
                // Apply flat or percentage bonus
                if (statNamePart.Equals("Stealth", StringComparison.OrdinalIgnoreCase))
                {
                    if (isPercentage)
                        Stealth += (int)Math.Ceiling(Stealth * (value / 100.0));
                    else
                        Stealth += (int)value;
                    Console.WriteLine($"DEBUG: Applied {effect} to Stealth. New value: {Stealth}");
                }
                else if (statNamePart.Equals("HackSpeed", StringComparison.OrdinalIgnoreCase))
                {
                    if (isPercentage)
                        HackSpeed += (int)Math.Ceiling(HackSpeed * (value / 100.0));
                    else
                        HackSpeed += (int)value;
                    Console.WriteLine(
                        $"DEBUG: Applied {effect} to HackSpeed. New value: {HackSpeed}"
                    );
                }
                else if (statNamePart.Equals("DataYield", StringComparison.OrdinalIgnoreCase))
                {
                    if (isPercentage)
                        DataYield += (int)Math.Ceiling(DataYield * (value / 100.0)); // Using Ceiling for consistency
                    else
                        DataYield += (int)value;
                    Console.WriteLine(
                        $"DEBUG: Applied {effect} to DataYield. New value: {DataYield}"
                    );
                }
                else
                {
                    Console.WriteLine(
                        $"DEBUG: Unknown stat name '{statNamePart}' in effect '{effect}'"
                    );
                }
            }
            else
            {
                Console.WriteLine(
                    $"DEBUG: Could not parse value '{valuePart}' in effect '{effect}'"
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERROR: Failed to parse or apply effect '{effect}'. Details: {ex.Message}"
            );
        }

        Console.WriteLine(
            $"DEBUG: Stats after apply: HS={HackSpeed}, ST={Stealth}, DY={DataYield}"
        );
    }
}
