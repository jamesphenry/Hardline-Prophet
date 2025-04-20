// tests/HardlineProphet.Tests/Services/TickServiceTests.cs
using HardlineProphet.Core; // GameConstants
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
    private readonly Mission _defaultMission = new Mission { Id = "test_mission_01", Name = "Test Mission", DurationTicks = 5, Reward = new MissionReward { Credits = 50, Xp = 10.5 } };
    private readonly IReadOnlyDictionary<string, Mission> _testMissions;

    public TickServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _testMissions = new Dictionary<string, Mission> { { _defaultMission.Id, _defaultMission } };
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
        var initialState = new GameState { Username = "MissionTester", ActiveMissionId = null, ActiveMissionProgress = 0, Credits = 500, Experience = 100 }; GameState? updatedState = null; Func<GameState?> getGameState = () => initialState; Action<GameState> updateGameState = (newState) => updatedState = newState; Action<string> logAction = (message) => _output.WriteLine($"LOG: {message}"); var tickService = new TickService(getGameState, updateGameState, logAction, _testMissions, mainLoop: null); tickService.Start(); tickService.ProcessTick(); Check.That(updatedState).IsNotNull(); Check.That(updatedState.ActiveMissionId).IsEqualTo(_defaultMission.Id); Check.That(updatedState.ActiveMissionProgress).IsEqualTo(1); Check.That(updatedState.Credits).IsEqualTo(initialState.Credits); Check.That(updatedState.Experience).IsEqualTo(initialState.Experience);
    }

    // --- Test for Mission Completion ---
    [Fact]
    public void ProcessTick_WhenMissionCompletes_AwardsRewardsAndResetsProgress()
    {
        // Arrange
        var initialCredits = 100;
        var initialXp = 50.0;
        var initialState = new GameState
        {
            Username = "MissionCompleter",
            ActiveMissionId = _defaultMission.Id, // Mission is active
            ActiveMissionProgress = _defaultMission.DurationTicks - 1, // One tick away from completion
            Credits = initialCredits,
            Experience = initialXp
        };
        GameState? updatedState = null;
        Func<GameState?> getGameState = () => initialState;
        Action<GameState> updateGameState = (newState) => updatedState = newState;
        Action<string> logAction = (message) => _output.WriteLine($"LOG: {message}");

        var tickService = new TickService(getGameState, updateGameState, logAction, _testMissions, mainLoop: null);
        tickService.Start();

        Exception caughtException = null!; // Initialize to null
        try
        {
            // Act
            _output.WriteLine("Calling ProcessTick...");
            tickService.ProcessTick(); // This tick should complete the mission
            _output.WriteLine("ProcessTick completed.");
            // --- NO ASSERTIONS SHOULD BE INSIDE THIS TRY BLOCK ---
        }
        catch (Exception ex)
        {
            caughtException = ex;
            _output.WriteLine($"LOG: Caught Exception: {ex.GetType().Name} - {ex.Message}");
        }
        // No finally needed as cleanup is handled by base class / test runner

        // Assert
        // These assertions run AFTER the try-catch block
        Check.That(caughtException).IsNull(); // Check that ProcessTick didn't throw unexpectedly
        Check.That(updatedState).IsNotNull(); // Check that the update delegate was called
        // Check rewards were added
        Check.That(updatedState.Credits).IsEqualTo(initialCredits + _defaultMission.Reward.Credits);
        Check.That(updatedState.Experience).IsEqualTo(initialXp + _defaultMission.Reward.Xp);
        // Check progress was reset (for looping in M1)
        Check.That(updatedState.ActiveMissionProgress).IsEqualTo(0);
        // Check mission ID remains the same (looping)
        Check.That(updatedState.ActiveMissionId).IsEqualTo(_defaultMission.Id);
    }
}
