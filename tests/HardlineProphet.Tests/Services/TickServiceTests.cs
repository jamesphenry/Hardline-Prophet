// tests/HardlineProphet.Tests/Services/TickServiceTests.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Extensions; // CalculateLevel()
using HardlineProphet.Core.Models;
using HardlineProphet.Services; // Implementation
using NFluent;
using System;
using System.Collections.Generic; // Dictionary
using Terminal.Gui; // MainLoop
using Xunit;
using Xunit.Abstractions;

namespace HardlineProphet.Tests.Services;

public class TickServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly IReadOnlyDictionary<string, Mission> _emptyMissions = new Dictionary<string, Mission>();
    // Define a test mission with known duration/rewards
    private readonly Mission _defaultMission = new Mission
    {
        Id = "test_mission_01",
        Name = "Test Mission",
        DurationTicks = 5, // Make duration easy to test
        Reward = new MissionReward { Credits = 50, Xp = 10.5 } // Use specific rewards
    };
    // Mission designed to cause level up from 1 to 2 (needs 100 XP)
    private readonly Mission _levelUpMission = new Mission
    {
        Id = "level_up_mission_01",
        Name = "Level Up Mission",
        DurationTicks = 3,
        Reward = new MissionReward { Credits = 10, Xp = 100 } // Enough XP to guarantee level up from 1
    };
    private readonly IReadOnlyDictionary<string, Mission> _testMissions;


    public TickServiceTests(ITestOutputHelper output)
    {
        _output = output;
        // Setup missions dictionary containing the test missions
        _testMissions = new Dictionary<string, Mission> {
                 { _defaultMission.Id, _defaultMission },
                 { _levelUpMission.Id, _levelUpMission }
             };
    }

    private const double BaseTickIntervalSeconds = 2.0;

    private TickService CreateDummyTickService(IReadOnlyDictionary<string, Mission>? missions = null)
    {
        return new TickService(() => new GameState(), _ => { }, _ => { }, missions ?? _emptyMissions, mainLoop: null);
    }

    [Theory]
    [InlineData(0, BaseTickIntervalSeconds * 1000)]
    // ... other InlineData ...
    [InlineData(150, BaseTickIntervalSeconds * 1000 * 0.1)]
    [InlineData(-20, BaseTickIntervalSeconds * 1000 * 1.0)]
    public void CalculateTickIntervalMs_BasedOnHackSpeed_ReturnsCorrectDelay(int hackSpeed, double expectedIntervalMs)
    {
        // ... (test remains the same) ...
        var gameState = new GameState { Stats = new PlayerStats { HackSpeed = hackSpeed } }; var tickService = CreateDummyTickService(); double actualIntervalMs = tickService.CalculateTickIntervalMs(gameState); Check.That(actualIntervalMs).IsCloseTo(expectedIntervalMs, 1e-9);
    }

    [Fact]
    public void ProcessTick_WhenNoActiveMission_AssignsDefaultMissionAndIncrementsProgress()
    {
        // ... (test remains the same) ...
        var initialState = new GameState { Username = "MissionTester", ActiveMissionId = null, ActiveMissionProgress = 0, Credits = 500, Experience = 100, Level = 2 }; GameState? updatedState = null; Func<GameState?> getGameState = () => initialState; Action<GameState> updateGameState = (newState) => updatedState = newState; Action<string> logAction = (message) => _output.WriteLine($"LOG: {message}"); var tickService = new TickService(getGameState, updateGameState, logAction, _testMissions, mainLoop: null); tickService.Start(); tickService.ProcessTick(); Check.That(updatedState).IsNotNull(); Check.That(updatedState.ActiveMissionId).IsEqualTo(_defaultMission.Id); Check.That(updatedState.ActiveMissionProgress).IsEqualTo(1); Check.That(updatedState.Credits).IsEqualTo(initialState.Credits); Check.That(updatedState.Experience).IsEqualTo(initialState.Experience); Check.That(updatedState.Level).IsEqualTo(initialState.Level); // Level shouldn't change here
    }

    [Fact]
    public void ProcessTick_WhenMissionCompletes_AwardsRewardsResetsProgressAndUpdatesLevel() // Updated test name
    {
        // Arrange
        var initialCredits = 100;
        var initialXp = 50.0; // Start at Level 1 (needs 100 for L2)
        var initialLevel = 1;
        var missionToComplete = _levelUpMission; // Use mission that gives enough XP

        var initialState = new GameState
        {
            Username = "MissionCompleter",
            ActiveMissionId = missionToComplete.Id, // Level up mission is active
            ActiveMissionProgress = missionToComplete.DurationTicks - 1, // One tick away
            Credits = initialCredits,
            Experience = initialXp,
            Level = initialLevel // Start at level 1
        };
        GameState? updatedState = null;
        Func<GameState?> getGameState = () => initialState;
        Action<GameState> updateGameState = (newState) => updatedState = newState;
        Action<string> logAction = (message) => _output.WriteLine($"LOG: {message}");

        // Use the service with our defined test missions
        var tickService = new TickService(getGameState, updateGameState, logAction, _testMissions, mainLoop: null);
        tickService.Start();

        // Act
        tickService.ProcessTick(); // This tick should complete the mission

        // Assert
        // This test will FAIL on the Level check because TickService doesn't update it yet.
        Check.That(updatedState).IsNotNull();
        // Check rewards were added
        Check.That(updatedState.Credits).IsEqualTo(initialCredits + missionToComplete.Reward.Credits);
        Check.That(updatedState.Experience).IsEqualTo(initialXp + missionToComplete.Reward.Xp); // Will be 50 + 100 = 150
                                                                                                // Check progress was reset
        Check.That(updatedState.ActiveMissionProgress).IsEqualTo(0);
        // Check mission ID remains the same
        Check.That(updatedState.ActiveMissionId).IsEqualTo(missionToComplete.Id);
        // Check Level was updated (150 XP should be Level 2)
        Check.That(updatedState.Level).IsEqualTo(2); // <<< NEW ASSERTION (will fail)
    }
}
