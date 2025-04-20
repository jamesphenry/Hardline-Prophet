namespace HardlineProphet.Core.Models;

public record PlayerStats
{
    public int HackSpeed { get; init; } = GameConstants.DefaultStartingHackSpeed;
    public int Stealth { get; init; } = GameConstants.DefaultStartingStealth;
    public int DataYield { get; init; } = GameConstants.DefaultStartingDataYield;

    public PlayerStats() { }
}

