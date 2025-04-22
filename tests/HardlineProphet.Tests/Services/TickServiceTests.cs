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
    private readonly Mission _mission1 = new Mission { Id = "test_m1", Name = "Mission One", DurationTicks = 5, Reward = new MissionReward { Credits = 10, Xp = 1 }, TraceRisk = 0.1 }; // 10%
    private readonly Mission _mission2 = new Mission { Id = "test_m2", Name = "Mission Two", DurationTicks = 3, Reward = new MissionReward { Credits = 20, Xp = 100 }, TraceRisk = 0.5 }; // 50%
    private readonly Mission _mission3 = new Mission { Id = "test_m3", Name = "Mission Three", DurationTicks = 8, Reward = new MissionReward { Credits = 30, Xp = 3 }, TraceRisk = 0.0 }; // 0%
    private readonly IReadOnlyDictionary<string, Mission> _testMissions;

    public TickServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _testMissions = new Dictionary<string, Mission> { { _mission1.Id, _mission1 }, { _mission2.Id, _mission2 }, { _mission3.Id, _mission3 } };
    }

    private const double BaseTickIntervalSeconds = 2.0;
    private const double TraceIncreaseAmount = 0.5; // Match constant in TickService

    // Updated helper to include optional RNG
    private TickService CreateTestTickService(
        Func<GameState?> getGameState,
        Action<GameState> updateGameState,
        Action<string> logAction,
        IReadOnlyDictionary<string, Mission>? missions = null,
        Func<double>? rng = null) // Added optional RNG
    {
        // Pass null for MainLoop
        return new TickService(getGameState, updateGameState, logAction, missions ?? _testMissions, mainLoop: null, rng: rng);
    }

    // Simplified dummy service creator uses the helper above
    private TickService CreateDummyTickService(IReadOnlyDictionary<string, Mission>? missions = null)
    {
        return CreateTestTickService(() => new GameState(), _ => { }, _ => { }, missions); // rng will be null -> default
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
        var initialState = new GameState { Username = "MissionTester", ActiveMissionId = null, ActiveMissionProgress = 0, Credits = 500, Experience = 100, Level = 1 }; GameState? updatedState = null; Func<GameState?> getGameState = () => initialState; Action<GameState> updateGameState = (newState) => updatedState = newState; Action<string> logAction = (message) => _output.WriteLine($"LOG: {message}"); var tickService = CreateTestTickService(getGameState, updateGameState, logAction); tickService.Start(); tickService.ProcessTick(); Check.That(updatedState).IsNotNull(); Check.That(updatedState!.ActiveMissionId).IsEqualTo(_mission1.Id); Check.That(updatedState!.ActiveMissionProgress).IsEqualTo(1); Check.That(updatedState!.Credits).IsEqualTo(initialState.Credits); Check.That(updatedState!.Experience).IsEqualTo(initialState.Experience); Check.That(updatedState!.Level).IsEqualTo(initialState.Level); Check.That(updatedState!.TraceLevel).IsEqualTo(initialState.TraceLevel); // Trace shouldn't change here
    }

    [Fact]
    public void ProcessTick_WhenMissionCompletes_AwardsRewardsResetsProgressAndUpdatesLevel()
    {
        // ... (test remains the same) ...
        var initialCredits = 100; var initialXp = 50.0; var initialLevel = 1; var missionToComplete = _mission2; var initialState = new GameState { Username = "MissionCompleter", ActiveMissionId = missionToComplete.Id, ActiveMissionProgress = missionToComplete.DurationTicks - 1, Credits = initialCredits, Experience = initialXp, Level = initialLevel }; GameState? updatedState = null; Func<GameState?> getGameState = () => initialState; Action<GameState> updateGameState = (newState) => updatedState = newState; Action<string> logAction = (message) => _output.WriteLine($"LOG: {message}"); var tickService = CreateTestTickService(getGameState, updateGameState, logAction); tickService.Start(); tickService.ProcessTick(); Check.That(updatedState).IsNotNull(); Check.That(updatedState!.Credits).IsEqualTo(initialCredits + missionToComplete.Reward.Credits); Check.That(updatedState!.Experience).IsEqualTo(initialXp + missionToComplete.Reward.Xp); Check.That(updatedState!.ActiveMissionProgress).IsEqualTo(0); int expectedLevel = (initialState with { Experience = initialXp + missionToComplete.Reward.Xp }).CalculateLevel(); Check.That(updatedState!.Level).IsEqualTo(expectedLevel); Check.That(updatedState!.TraceLevel).IsEqualTo(initialState.TraceLevel); // Assume trace doesn't increase on the *completion* tick itself
    }

    [Fact]
    public void ProcessTick_WhenMissionCompletes_AssignsNewRandomMissionId()
    {
        // ... (test remains the same) ...
        var missionToComplete = _mission1; var initialState = new GameState { Username = "MissionSwitcher", ActiveMissionId = missionToComplete.Id, ActiveMissionProgress = missionToComplete.DurationTicks - 1 }; GameState? updatedState = null; Func<GameState?> getGameState = () => initialState; Action<GameState> updateGameState = (newState) => updatedState = newState; Action<string> logAction = (message) => _output.WriteLine($"LOG: {message}"); var tickService = CreateTestTickService(getGameState, updateGameState, logAction); tickService.Start(); tickService.ProcessTick(); Check.That(updatedState).IsNotNull(); Check.That(updatedState!.ActiveMissionProgress).IsEqualTo(0); Check.That(updatedState!.ActiveMissionId).IsNotNull(); Check.That(_testMissions.Keys).Contains(updatedState!.ActiveMissionId); if (_testMissions.Count > 1) { Check.That(updatedState!.ActiveMissionId).IsNotEqualTo(missionToComplete.Id); }
        _output.WriteLine($"Completed '{missionToComplete.Id}', New mission is '{updatedState.ActiveMissionId}'");
    }

    // --- Tests for Trace Logic ---

    [Fact]
    public void ProcessTick_WhenTraceRiskCheckFails_TraceLevelDoesNotIncrease()
    {
        // Arrange
        var initialTrace = 10.0;
        var mission = _mission2; // TraceRisk = 0.5 (50%)
        var initialState = new GameState { Username = "TraceTesterFail", ActiveMissionId = mission.Id, ActiveMissionProgress = 1, TraceLevel = initialTrace };
        GameState? updatedState = null;
        Func<GameState?> getGameState = () => initialState; Action<GameState> updateGameState = (newState) => updatedState = newState; Action<string> logAction = (msg) => _output.WriteLine($"LOG: {msg}");
        // Inject RNG that *always* returns >= TraceRisk (e.g., 0.6)
        var tickService = CreateTestTickService(getGameState, updateGameState, logAction, rng: () => 0.6);
        tickService.Start();

        // Act
        tickService.ProcessTick();

        // Assert
        // Uncomment real assertion - Test should FAIL initially
        Check.That(updatedState).IsNotNull();
        Check.That(updatedState!.TraceLevel).IsEqualTo(initialTrace); // Expect no change
    }

    [Fact]
    public void ProcessTick_WhenTraceRiskCheckSucceeds_TraceLevelIncreases()
    {
        // Arrange
        var initialTrace = 10.0;
        var mission = _mission2; // TraceRisk = 0.5 (50%)
        var initialState = new GameState { Username = "TraceTesterOK", ActiveMissionId = mission.Id, ActiveMissionProgress = 1, TraceLevel = initialTrace };
        GameState? updatedState = null;
        Func<GameState?> getGameState = () => initialState; Action<GameState> updateGameState = (newState) => updatedState = newState; Action<string> logAction = (msg) => _output.WriteLine($"LOG: {msg}");
        // Inject RNG that *always* returns < TraceRisk (e.g., 0.4)
        var tickService = CreateTestTickService(getGameState, updateGameState, logAction, rng: () => 0.4);
        tickService.Start();

        // Act
        tickService.ProcessTick();

        // Assert
        // Uncomment real assertion - Test should FAIL initially
        Check.That(updatedState).IsNotNull();
        double expectedTrace = initialTrace + TraceIncreaseAmount; // Use constant
        Check.That(updatedState!.TraceLevel).IsCloseTo(expectedTrace, 1e-9); // Use IsCloseTo for double comparison
    }

    [Fact]
    public void ProcessTick_WhenTraceLevelAtMax_DoesNotIncrease()
    {
        // Arrange
        var initialTrace = 100.0; // Start at max trace
        var mission = _mission2; // TraceRisk = 0.5 (50%)
        var initialState = new GameState { Username = "TraceTesterMax", ActiveMissionId = mission.Id, ActiveMissionProgress = 1, TraceLevel = initialTrace };
        GameState? updatedState = null;
        Func<GameState?> getGameState = () => initialState; Action<GameState> updateGameState = (newState) => updatedState = newState; Action<string> logAction = (msg) => _output.WriteLine($"LOG: {msg}");
        // Inject RNG that *always* returns < TraceRisk (e.g., 0.1) - should trigger increase attempt
        var tickService = CreateTestTickService(getGameState, updateGameState, logAction, rng: () => 0.1);
        tickService.Start();

        // Act
        tickService.ProcessTick();

        // Assert
        // Uncomment real assertion - Test should FAIL initially
        Check.That(updatedState).IsNotNull();
        Check.That(updatedState!.TraceLevel).IsEqualTo(100.0); // Expect trace to be clamped at 100
    }
}
