// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-feature-m2-flavor-events.1+7
// ║ 📄  File: TickServiceTests.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
// tests/HardlineProphet.Tests/Services/TickServiceTests.cs

using HardlineProphet.Core.Extensions; // CalculateLevel()
using HardlineProphet.Core.Models; // Added FlavorEvent, FlavorEventTrigger
using HardlineProphet.Services; // Implementation
using NFluent;
using Terminal.Gui; // MainLoop
using Xunit.Abstractions;

namespace HardlineProphet.Tests.Services;

public class TickServiceTests
{
    private readonly ITestOutputHelper _output;

    // Missions for setup
    private readonly IReadOnlyDictionary<string, Mission> _emptyMissions =
        new Dictionary<string, Mission>();
    private readonly Mission _mission1 = new Mission
    {
        Id = "test_m1",
        Name = "Mission One",
        DurationTicks = 5,
        Reward = new MissionReward { Credits = 10, Xp = 1 },
        TraceRisk = 0.1,
    };
    private readonly Mission _mission2 = new Mission
    {
        Id = "test_m2",
        Name = "Mission Two",
        DurationTicks = 3,
        Reward = new MissionReward { Credits = 20, Xp = 100 },
        TraceRisk = 0.5,
    };
    private readonly Mission _mission3 = new Mission
    {
        Id = "test_m3",
        Name = "Mission Three",
        DurationTicks = 8,
        Reward = new MissionReward { Credits = 30, Xp = 3 },
        TraceRisk = 0.0,
    };
    private readonly IReadOnlyDictionary<string, Mission> _testMissions;

    // Flavor Events for setup
    private readonly IReadOnlyDictionary<FlavorEventTrigger, List<FlavorEvent>> _emptyFlavorEvents =
        new Dictionary<FlavorEventTrigger, List<FlavorEvent>>();
    private readonly FlavorEvent _testTickEvent = new FlavorEvent
    {
        Id = "test_tick",
        Trigger = FlavorEventTrigger.OnTick,
        Chance = 0.5,
        Text = "Static crackles...",
    }; // Test event
    private readonly IReadOnlyDictionary<FlavorEventTrigger, List<FlavorEvent>> _testFlavorEvents;

    public TickServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _testMissions = new Dictionary<string, Mission>
        {
            { _mission1.Id, _mission1 },
            { _mission2.Id, _mission2 },
            { _mission3.Id, _mission3 },
        };
        // Setup flavor events dictionary containing the test tick event
        _testFlavorEvents = new Dictionary<FlavorEventTrigger, List<FlavorEvent>>
        {
            {
                FlavorEventTrigger.OnTick,
                new List<FlavorEvent> { _testTickEvent }
            },
            // Add other trigger types here later if needed for setup
        };
    }

    private const double BaseTickIntervalSeconds = 2.0;
    private const double TraceIncreaseAmount = 0.5;

    // Updated helper to include flavor events dictionary and optional RNG
    private TickService CreateTestTickService(
        Func<GameState?> getGameState,
        Action<GameState> updateGameState,
        Action<string> logAction,
        IReadOnlyDictionary<string, Mission>? missions = null,
        IReadOnlyDictionary<FlavorEventTrigger, List<FlavorEvent>>? flavorEvents = null, // Added
        Func<double>? rng = null
    )
    {
        // Pass null for MainLoop
        return new TickService(
            getGameState,
            updateGameState,
            logAction,
            missions ?? _testMissions,
            flavorEvents ?? _emptyFlavorEvents, // Pass flavor events
            mainLoop: null,
            rng: rng
        );
    }

    // Simplified dummy service creator uses the helper above
    private TickService CreateDummyTickService(
        IReadOnlyDictionary<string, Mission>? missions = null,
        IReadOnlyDictionary<FlavorEventTrigger, List<FlavorEvent>>? flavorEvents = null
    )
    {
        return CreateTestTickService(
            () => new GameState(),
            _ => { },
            _ => { },
            missions,
            flavorEvents
        );
    }

    [Theory]
    [InlineData(0, BaseTickIntervalSeconds * 1000)]
    // ... other InlineData ...
    [InlineData(-20, BaseTickIntervalSeconds * 1000 * 1.0)]
    public void CalculateTickIntervalMs_BasedOnHackSpeed_ReturnsCorrectDelay(
        int hackSpeed,
        double expectedIntervalMs
    )
    {
        // ... (test remains the same) ...
        var gameState = new GameState { Stats = new PlayerStats { HackSpeed = hackSpeed } };
        var tickService = CreateDummyTickService();
        double actualIntervalMs = tickService.CalculateTickIntervalMs(gameState);
        Check.That(actualIntervalMs).IsCloseTo(expectedIntervalMs, 1e-9);
    }

    [Fact]
    public void ProcessTick_WhenNoActiveMission_AssignsDefaultMissionAndIncrementsProgress()
    {
        // ... (test remains the same) ...
        var initialState = new GameState
        {
            Username = "MissionTester",
            ActiveMissionId = null,
            ActiveMissionProgress = 0,
            Credits = 500,
            Experience = 100,
            Level = 1,
        };
        GameState? updatedState = null;
        Func<GameState?> getGameState = () => initialState;
        Action<GameState> updateGameState = (newState) => updatedState = newState;
        Action<string> logAction = (message) => _output.WriteLine($"LOG: {message}");
        var tickService = CreateTestTickService(getGameState, updateGameState, logAction);
        tickService.Start();
        tickService.ProcessTick();
        Check.That(updatedState).IsNotNull();
        Check.That(updatedState!.ActiveMissionId).IsEqualTo(_mission1.Id);
        Check.That(updatedState!.ActiveMissionProgress).IsEqualTo(1);
        Check.That(updatedState!.Credits).IsEqualTo(initialState.Credits);
        Check.That(updatedState!.Experience).IsEqualTo(initialState.Experience);
        Check.That(updatedState!.Level).IsEqualTo(initialState.Level);
        Check.That(updatedState!.TraceLevel).IsEqualTo(initialState.TraceLevel);
    }

    [Fact]
    public void ProcessTick_WhenMissionCompletes_AwardsRewardsResetsProgressAndUpdatesLevel()
    {
        // Arrange
        var initialCredits = 100;
        var initialXp = 50.0;
        var initialLevel = 1;
        var initialTrace = 0.0;
        var missionToComplete = _mission2; // TraceRisk = 0.5
        var initialState = new GameState
        {
            Username = "MissionCompleter",
            ActiveMissionId = missionToComplete.Id,
            ActiveMissionProgress = missionToComplete.DurationTicks - 1,
            Credits = initialCredits,
            Experience = initialXp,
            Level = initialLevel,
            TraceLevel = initialTrace,
        };
        GameState? updatedState = null;
        Func<GameState?> getGameState = () => initialState;
        Action<GameState> updateGameState = (newState) => updatedState = newState;
        Action<string> logAction = (message) => _output.WriteLine($"LOG: {message}");
        // Inject RNG that WILL cause trace increase (0.4 < 0.5)
        var tickService = CreateTestTickService(
            getGameState,
            updateGameState,
            logAction,
            rng: () => 0.4
        );
        tickService.Start();

        // Act
        tickService.ProcessTick();

        // Assert
        Check.That(updatedState).IsNotNull();
        Check
            .That(updatedState!.Credits)
            .IsEqualTo(initialCredits + missionToComplete.Reward.Credits);
        Check.That(updatedState!.Experience).IsEqualTo(initialXp + missionToComplete.Reward.Xp);
        Check.That(updatedState!.ActiveMissionProgress).IsEqualTo(0);
        int expectedLevel = (
            initialState with
            {
                Experience = initialXp + missionToComplete.Reward.Xp,
            }
        ).CalculateLevel();
        Check.That(updatedState!.Level).IsEqualTo(expectedLevel);
        // Check TraceLevel - EXPECT increase because RNG < TraceRisk
        double expectedTrace = initialTrace + TraceIncreaseAmount;
        Check.That(updatedState!.TraceLevel).IsCloseTo(expectedTrace, 1e-9); // Corrected Assertion
    }

    [Fact]
    public void ProcessTick_WhenMissionCompletes_AssignsNewRandomMissionId()
    {
        // ... (test remains the same) ...
        var missionToComplete = _mission1;
        var initialState = new GameState
        {
            Username = "MissionSwitcher",
            ActiveMissionId = missionToComplete.Id,
            ActiveMissionProgress = missionToComplete.DurationTicks - 1,
        };
        GameState? updatedState = null;
        Func<GameState?> getGameState = () => initialState;
        Action<GameState> updateGameState = (newState) => updatedState = newState;
        Action<string> logAction = (message) => _output.WriteLine($"LOG: {message}");
        var tickService = CreateTestTickService(getGameState, updateGameState, logAction);
        tickService.Start();
        tickService.ProcessTick();
        Check.That(updatedState).IsNotNull();
        Check.That(updatedState!.ActiveMissionProgress).IsEqualTo(0);
        Check.That(updatedState!.ActiveMissionId).IsNotNull();
        Check.That(_testMissions.Keys).Contains(updatedState!.ActiveMissionId);
        if (_testMissions.Count > 1)
        {
            Check.That(updatedState!.ActiveMissionId).IsNotEqualTo(missionToComplete.Id);
        }
        _output.WriteLine(
            $"Completed '{missionToComplete.Id}', New mission is '{updatedState.ActiveMissionId}'"
        );
    }

    // --- Tests for Trace Logic ---
    [Fact]
    public void ProcessTick_WhenTraceRiskCheckFails_TraceLevelDoesNotIncrease()
    { /* ... remains the same ... */
        var initialTrace = 10.0;
        var mission = _mission2;
        var initialState = new GameState
        {
            Username = "TraceTesterFail",
            ActiveMissionId = mission.Id,
            ActiveMissionProgress = 1,
            TraceLevel = initialTrace,
        };
        GameState? updatedState = null;
        Func<GameState?> getGameState = () => initialState;
        Action<GameState> updateGameState = (newState) => updatedState = newState;
        Action<string> logAction = (msg) => _output.WriteLine($"LOG: {msg}");
        var tickService = CreateTestTickService(
            getGameState,
            updateGameState,
            logAction,
            rng: () => 0.6
        );
        tickService.Start();
        tickService.ProcessTick();
        Check.That(updatedState).IsNotNull();
        Check.That(updatedState!.TraceLevel).IsEqualTo(initialTrace);
    }

    [Fact]
    public void ProcessTick_WhenTraceRiskCheckSucceeds_TraceLevelIncreases()
    { /* ... remains the same ... */
        var initialTrace = 10.0;
        var mission = _mission2;
        var initialState = new GameState
        {
            Username = "TraceTesterOK",
            ActiveMissionId = mission.Id,
            ActiveMissionProgress = 1,
            TraceLevel = initialTrace,
        };
        GameState? updatedState = null;
        Func<GameState?> getGameState = () => initialState;
        Action<GameState> updateGameState = (newState) => updatedState = newState;
        Action<string> logAction = (msg) => _output.WriteLine($"LOG: {msg}");
        var tickService = CreateTestTickService(
            getGameState,
            updateGameState,
            logAction,
            rng: () => 0.4
        );
        tickService.Start();
        tickService.ProcessTick();
        Check.That(updatedState).IsNotNull();
        double expectedTrace = initialTrace + TraceIncreaseAmount;
        Check.That(updatedState!.TraceLevel).IsCloseTo(expectedTrace, 1e-9);
    }

    [Fact]
    public void ProcessTick_WhenTraceLevelAtMax_DoesNotIncrease()
    { /* ... remains the same ... */
        var initialTrace = 100.0;
        var mission = _mission2;
        var initialState = new GameState
        {
            Username = "TraceTesterMax",
            ActiveMissionId = mission.Id,
            ActiveMissionProgress = 1,
            TraceLevel = initialTrace,
        };
        GameState? updatedState = null;
        Func<GameState?> getGameState = () => initialState;
        Action<GameState> updateGameState = (newState) => updatedState = newState;
        Action<string> logAction = (msg) => _output.WriteLine($"LOG: {msg}");
        var tickService = CreateTestTickService(
            getGameState,
            updateGameState,
            logAction,
            rng: () => 0.1
        );
        tickService.Start();
        tickService.ProcessTick();
        Check.That(updatedState).IsNotNull();
        Check.That(updatedState!.TraceLevel).IsEqualTo(100.0);
    }

    // --- New Tests for Flavor Events ---
    [Fact]
    public void ProcessTick_WhenOnTickEventCheckSucceeds_LogsEventText()
    {
        // Arrange
        var initialState = new GameState
        {
            Username = "FlavorTester",
            ActiveMissionId = _mission1.Id,
        }; // Need active mission
        GameState? updatedState = null;
        var loggedMessages = new List<string>(); // Capture log messages
        Func<GameState?> getGameState = () => initialState;
        Action<GameState> updateGameState = (newState) => updatedState = newState;
        Action<string> logAction = (msg) =>
        {
            _output.WriteLine($"LOG: {msg}");
            loggedMessages.Add(msg);
        }; // Capture logs

        // Use flavor events dict with our test event (_testTickEvent has Chance = 0.5)
        // Inject RNG that *always* returns < Chance (e.g., 0.1)
        var tickService = CreateTestTickService(
            getGameState,
            updateGameState,
            logAction,
            flavorEvents: _testFlavorEvents,
            rng: () => 0.1
        );
        tickService.Start();

        // Act
        tickService.ProcessTick();

        // Assert
        // This test will FAIL because ProcessTick doesn't check/log flavor events yet.
        Check.That(loggedMessages).Contains(_testTickEvent.Text);
    }

    [Fact]
    public void ProcessTick_WhenOnTickEventCheckFails_DoesNotLogEventText()
    {
        // Arrange
        var initialState = new GameState
        {
            Username = "FlavorTester",
            ActiveMissionId = _mission1.Id,
        };
        GameState? updatedState = null;
        var loggedMessages = new List<string>();
        Func<GameState?> getGameState = () => initialState;
        Action<GameState> updateGameState = (newState) => updatedState = newState;
        Action<string> logAction = (msg) =>
        {
            _output.WriteLine($"LOG: {msg}");
            loggedMessages.Add(msg);
        };

        // Use flavor events dict with our test event (_testTickEvent has Chance = 0.5)
        // Inject RNG that *always* returns >= Chance (e.g., 0.9)
        var tickService = CreateTestTickService(
            getGameState,
            updateGameState,
            logAction,
            flavorEvents: _testFlavorEvents,
            rng: () => 0.9
        );
        tickService.Start();

        // Act
        tickService.ProcessTick();

        // Assert
        // This test should PASS initially (as no event logic exists), but confirms setup.
        // Once logic is added, this ensures non-triggering works.
        Check.That(loggedMessages).Not.Contains(_testTickEvent.Text);
    }
}
