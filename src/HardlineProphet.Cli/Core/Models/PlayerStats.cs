namespace HardlineProphet.Core.Models;

public class PlayerStats
{
    public int HackSpeed { get; set; }
    public int Stealth { get; set; }
    public int DataYield { get; set; }

    public PlayerStats(int hackSpeed = 0, int stealth = 0, int dataYield = 0)
    {
        HackSpeed = hackSpeed;
        Stealth = stealth;
        DataYield = dataYield;
    }
}

