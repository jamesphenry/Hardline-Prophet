// tests/HardlineProphet.Tests/Services/TickServiceTests.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Extensions; // CalculateLevel()
using HardlineProphet.Core.Models;
using HardlineProphet.Services; // Implementation
using NFluent;
using System;
using System.Collections.Generic; // Dictionary
using System.Linq; // Added for LINQ methods like ContainsKey, ToList etc.
using Terminal.Gui; // MainLoop
using Xunit;
using Xunit.Abstractions;

namespace HardlineProphet.Tests.Services;

public class TickServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly IReadOnlyDictionary<string, Mission> _emptyMissions = new Dictionary<string, Mission>();
    // Define multiple test missions
    private readonly Mission _mission1 = new Mission { Id = "test_m1", Name = "Mission One", DurationTicks = 5, Reward = new MissionReward { Credits = 10, Xp = 1 } };
    private readonly Mission _mission2 = new Mission { Id = "test_m2", Name = "Mission Two", DurationTicks = 3, Reward = new MissionReward { Credits = 20, Xp = 2 } };
    private readonly Mission _mission3 = new Mission { Id = "test_m3", Name = "Mission Three", DurationTicks = 8, Reward = new MissionReward { Credits = 30, Xp = 3 } };
    private readonly IReadOnlyDictionary<string, Mission> _testMissions;


    public TickServiceTests(ITestOutputHelper output)
    {
        _output = output;
        // Setup missions dictionary containing multiple test missions
        _testMissions = new Dictionary<string, Mission> {
             { _mission1.Id, _mission1 },
             { _mission2.Id, _mission2 },
             { _mission3.Id, _mission3 }
         };
    }

    private const double BaseTickIntervalSeconds = 2.0;

    private TickService CreateDummyTickService(IReadOnlyDictionary<string, Mission>? missions = null)
    {
        // Pass null for MainLoop, provide specific missions if not specified
        return new TickService(() => new GameState(), _ => { }, _ => { }, missions ?? _testMissions, mainLoop: null);
    }

    [Theory]
    [InlineData(0, BaseTickIntervalSeconds * 1000)]
    // ... other InlineData ...
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
        var initialState = new GameState { Username = "MissionTester", ActiveMissionId = null, ActiveMissionProgress = 0, Credits = 500, Experience = 100, Level = 1 }; GameState? updatedState = null; Func<GameState?> getGameState = () => initialState; Action<GameState> updateGameState = (newState) => updatedState = newState; Action<string> logAction = (message) => _output.WriteLine($"LOG: {message}"); var tickService = new TickService(getGameState, updateGameState, logAction, _testMissions, mainLoop: null); tickService.Start(); tickService.ProcessTick(); Check.That(updatedState).IsNotNull(); Check.That(updatedState.ActiveMissionId).IsEqualTo(_mission1.Id); /* Assumes mission1 is first */ Check.That(updatedState.ActiveMissionProgress).IsEqualTo(1); Check.That(updatedState.Credits).IsEqualTo(initialState.Credits); Check.That(updatedState.Experience).IsEqualTo(initialState.Experience); Check.That(updatedState.Level).IsEqualTo(initialState.Level);
    }

    [Fact]
    public void ProcessTick_WhenMissionCompletes_AwardsRewardsResetsProgressAndUpdatesLevel()
    {
        // ... (test remains the same) ...
        var initialCredits = 100; var initialXp = 50.0; var initialLevel = 1; var missionToComplete = _mission2; var initialState = new GameState { Username = "MissionCompleter", ActiveMissionId = missionToComplete.Id, ActiveMissionProgress = missionToComplete.DurationTicks - 1, Credits = initialCredits, Experience = initialXp, Level = initialLevel }; GameState? updatedState = null; Func<GameState?> getGameState = () => initialState; Action<GameState> updateGameState = (newState) => updatedState = newState; Action<string> logAction = (message) => _output.WriteLine($"LOG: {message}"); var tickService = new TickService(getGameState, updateGameState, logAction, _testMissions, mainLoop: null); tickService.Start(); tickService.ProcessTick(); Check.That(updatedState).IsNotNull(); Check.That(updatedState.Credits).IsEqualTo(initialCredits + missionToComplete.Reward.Credits); Check.That(updatedState.Experience).IsEqualTo(initialXp + missionToComplete.Reward.Xp); Check.That(updatedState.ActiveMissionProgress).IsEqualTo(0); Check.That(updatedState.ActiveMissionId).IsEqualTo(missionToComplete.Id); int expectedLevel = (initialState with { Experience = initialXp + missionToComplete.Reward.Xp }).CalculateLevel(); Check.That(updatedState.Level).IsEqualTo(expectedLevel);
    }

    // --- Test for Random Mission Assignment ---
    [Fact]
    public void ProcessTick_WhenMissionCompletes_AssignsNewRandomMissionId()
    {
        // Arrange
        var missionToComplete = _mission1; // Start with mission 1
        var initialState = new GameState
        {
            Username = "MissionSwitcher",
            ActiveMissionId = missionToComplete.Id,
            ActiveMissionProgress = missionToComplete.DurationTicks - 1, // One tick away
        };
        GameState? updatedState = null;
        Func<GameState?> getGameState = () => initialState;
        Action<GameState> updateGameState = (newState) => updatedState = newState;
        Action<string> logAction = (message) => _output.WriteLine($"LOG: {message}");

        // Use the service with multiple test missions
        var tickService = new TickService(getGameState, updateGameState, logAction, _testMissions, mainLoop: null);
        tickService.Start();

        // Act
        tickService.ProcessTick(); // This tick should complete the mission and assign a new one

        // Assert
        // This test should FAIL now because ProcessTick currently keeps the same mission ID.
        Check.That(updatedState).IsNotNull();
        Check.That(updatedState.ActiveMissionProgress).IsEqualTo(0); // Progress should reset
        Check.That(updatedState.ActiveMissionId).IsNotNull(); // Should have an ID
        Check.That(_testMissions.Keys).Contains(updatedState.ActiveMissionId); // Should be a valid ID

        // --- Added Assertion ---
        // Check that the new mission is different from the completed one
        // (This assumes there's more than one mission available)
        if (_testMissions.Count > 1)
        {
            Check.That(updatedState.ActiveMissionId).IsNotEqualTo(missionToComplete.Id);
        }
        // ---------------------

        _output.WriteLine($"Completed '{missionToComplete.Id}', New mission is '{updatedState.ActiveMissionId}'");
    }
}
