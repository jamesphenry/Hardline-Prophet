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
    // Define missions needed for tests
    private readonly IReadOnlyDictionary<string, Mission> _emptyMissions = new Dictionary<string, Mission>();
    private readonly Mission _defaultMission = new Mission { Id = "test_mission_01", Name = "Test Mission", DurationTicks = 5, Reward = new MissionReward { Credits = 50, Xp = 10 } };
    private readonly IReadOnlyDictionary<string, Mission> _testMissions;


    public TickServiceTests(ITestOutputHelper output)
    {
        _output = output;
        // Setup missions dictionary containing the default test mission
        _testMissions = new Dictionary<string, Mission> { { _defaultMission.Id, _defaultMission } };
    }

    private const int CreditsPerTick_Placeholder = 10; // Keep for the first test using placeholder logic
    private const double BaseTickIntervalSeconds = 2.0; // Base interval used for calculation tests

    // Helper updated to include missions dictionary
    private TickService CreateDummyTickService(IReadOnlyDictionary<string, Mission>? missions = null)
    {
        // Pass null for MainLoop, provide empty missions if not specified
        return new TickService(() => new GameState(), _ => { }, _ => { }, missions ?? _emptyMissions, mainLoop: null);
    }

    [Theory]
    [InlineData(0, BaseTickIntervalSeconds * 1000)] // No speed boost -> base interval
    [InlineData(10, BaseTickIntervalSeconds * 1000 * 0.9)] // 10% speed boost -> 90% interval
    [InlineData(50, BaseTickIntervalSeconds * 1000 * 0.5)] // 50% speed boost -> 50% interval
    [InlineData(90, BaseTickIntervalSeconds * 1000 * 0.1)] // 90% speed boost -> 10% interval (min clamp)
    [InlineData(100, BaseTickIntervalSeconds * 1000 * 0.1)] // 100% speed boost -> 10% interval (min clamp)
    [InlineData(150, BaseTickIntervalSeconds * 1000 * 0.1)] // >100% speed boost -> 10% interval (min clamp)
    [InlineData(-20, BaseTickIntervalSeconds * 1000 * 1.0)] // Negative speed boost -> base interval (max clamp)
    public void CalculateTickIntervalMs_BasedOnHackSpeed_ReturnsCorrectDelay(int hackSpeed, double expectedIntervalMs)
    {
        // Arrange
        var gameState = new GameState
        {
            Stats = new PlayerStats { HackSpeed = hackSpeed } // Set the HackSpeed for the test case
        };
        var tickService = CreateDummyTickService(); // Uses empty missions

        // Act
        // Call the internal method (ensure TickService.cs has InternalsVisibleTo("HardlineProphet.Tests"))
        double actualIntervalMs = tickService.CalculateTickIntervalMs(gameState);

        // Assert
        // Use tolerance for floating-point comparisons
        Check.That(actualIntervalMs).IsCloseTo(expectedIntervalMs, 1e-9); // Check within a small tolerance
    }

    // --- New Test ---
    [Fact]
    public void ProcessTick_WhenNoActiveMission_AssignsDefaultMissionAndIncrementsProgress()
    {
        // Arrange
        var initialState = new GameState
        {
            Username = "MissionTester",
            ActiveMissionId = null, // Start with no active mission
            ActiveMissionProgress = 0
        };
        GameState? updatedState = null;
        Func<GameState?> getGameState = () => initialState;
        Action<GameState> updateGameState = (newState) => updatedState = newState;
        Action<string> logAction = (message) => _output.WriteLine($"LOG: {message}");

        // Use the service with our defined test mission
        var tickService = new TickService(getGameState, updateGameState, logAction, _testMissions, mainLoop: null);
        tickService.Start();

        // Act
        tickService.ProcessTick();

        // Assert
        Check.That(updatedState).IsNotNull();
        Check.That(updatedState.ActiveMissionId).IsEqualTo(_defaultMission.Id); // Check if default mission was assigned - Will Fail
        Check.That(updatedState.ActiveMissionProgress).IsEqualTo(1); // Check if progress was incremented - Will Fail
        //Check credits haven't changed yet (or check they match placeholder if that logic still runs)
        Check.That(updatedState.Credits).IsEqualTo(initialState.Credits);

    }
}
