using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardlineProphet.Core.Models;

public class GameState
{
    public int Version { get; set; } = 2;
    public string Username { get; set; } = string.Empty;
    public int Level { get; set; }
    public double Experience { get; set; }
    public int Credits { get; set; }
    public PlayerStats Stats { get; set; } = new PlayerStats();
    public List<string> ActiveMissionIds { get; set; } = new List<string>();
    public List<string> UnlockedPerkIds { get; set; } = new List<string>();
    public string? Checksum { get; set; }
    public bool IsDevSave { get; set; } = false;
    // Add StartingClass, DifficultyModifiers etc. later
}

