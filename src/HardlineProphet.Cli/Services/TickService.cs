// src/HardlineProphet/Services/TickService.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Extensions; // CalculateLevel()
using HardlineProphet.Core.Interfaces; // ITickService
using HardlineProphet.Core.Models; // GameState, Mission
using System; // Func, Action, ArgumentNullException, Math, TimeSpan, Random
using System.Collections.Generic; // IReadOnlyDictionary
using System.Linq; // FirstOrDefault
using Terminal.Gui; // MainLoop

namespace HardlineProphet.Services;

public class TickService : ITickService
{
    private readonly Func<GameState?> _getGameState;
    private readonly Action<GameState> _updateGameState;
    private readonly Action<string> _logAction;
    private readonly IReadOnlyDictionary<string, Mission> _missionDefinitions;
    private readonly MainLoop? _mainLoop;
    private readonly Func<double> _rng; // Function to get random double [0.0, 1.0)

    private bool _isRunning = false;
    private object? _timeoutToken = null;
    private static readonly Random _defaultRandom = new Random(); // Default RNG if none provided

    private const double BaseTickIntervalSeconds = 2.0;
    private const double TraceIncreaseAmount = 0.5; // How much TraceLevel increases per risk success

    // Constructor updated to accept optional RNG delegate
    public TickService(
        Func<GameState?> getGameState,
        Action<GameState> updateGameState,
        Action<string> logAction,
        IReadOnlyDictionary<string, Mission> missionDefinitions,
        MainLoop? mainLoop = null,
        Func<double>? rng = null)
    {
        _getGameState = getGameState ?? throw new ArgumentNullException(nameof(getGameState));
        _updateGameState = updateGameState ?? throw new ArgumentNullException(nameof(updateGameState));
        _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
        _missionDefinitions = missionDefinitions ?? throw new ArgumentNullException(nameof(missionDefinitions));
        _mainLoop = mainLoop;
        _rng = rng ?? _defaultRandom.NextDouble;
        _logAction?.Invoke($"TickService initialized. RNG set (Custom: {rng != null}).");
    }

    public bool IsRunning => _isRunning;

    public void Start()
    {
        if (_isRunning) return; _isRunning = true; _logAction?.Invoke("TickService started. Scheduling first tick..."); if (_mainLoop != null) { ScheduleTick(); } else { _logAction?.Invoke("TickService started but MainLoop is null (likely testing). Tick scheduling skipped."); }
    }

    public void Stop()
    {
        if (!_isRunning) return; _isRunning = false; if (_mainLoop != null && _timeoutToken != null) { _mainLoop.RemoveTimeout(_timeoutToken); _timeoutToken = null; _logAction?.Invoke("TickService stopping. Removing timeout."); } else { _logAction?.Invoke($"TickService stopping. MainLoop null? {_mainLoop == null}, Timeout token null? {_timeoutToken == null}"); }
    }

    public void ProcessTick()
    {
        var currentState = _getGameState();
        if (currentState == null) { Stop(); return; }

        _logAction?.Invoke($"Processing tick for {currentState.Username}...");

        string? currentMissionId = currentState.ActiveMissionId;
        int currentProgress = currentState.ActiveMissionProgress;
        double currentTrace = currentState.TraceLevel; // Get current trace before modifications
        GameState newState;
        string? missionToLog = currentMissionId;

        // --- Mission Logic ---
        if (string.IsNullOrEmpty(currentMissionId) || !_missionDefinitions.ContainsKey(currentMissionId))
        {
            // Assign default mission...
            string? defaultMissionId = _missionDefinitions.Keys.FirstOrDefault();
            if (!string.IsNullOrEmpty(defaultMissionId))
            {
                _logAction?.Invoke($"No active/valid mission. Assigning default: {defaultMissionId}");
                // TraceLevel remains unchanged when assigning a new mission
                newState = currentState with { ActiveMissionId = defaultMissionId, ActiveMissionProgress = 1, TraceLevel = currentTrace };
                missionToLog = defaultMissionId;
                _logAction?.Invoke($"Mission '{missionToLog}' started, progress: 1");
            }
            else
            {
                _logAction?.Invoke($"WARNING: No active mission and no default missions loaded. Cannot progress.");
                return; // Exit if no mission can be assigned
            }
        }
        else // Mission is active, process progress and trace
        {
            if (!_missionDefinitions.TryGetValue(currentMissionId, out var missionDef))
            {
                _logAction?.Invoke($"ERROR: Could not find definition for active mission ID '{currentMissionId}'. Skipping progress.");
                return; // Exit if definition missing
            }

            currentProgress++;
            _logAction?.Invoke($"Mission '{currentMissionId}' progress: {currentProgress}/{missionDef.DurationTicks}");

            // --- Trace Increase Logic ---
            if (missionDef.TraceRisk > 0) // Only check if there's actual risk
            {
                double randomValue = _rng(); // Get random number [0.0, 1.0)
                if (randomValue < missionDef.TraceRisk)
                {
                    // Success! Increase trace, clamping at 100.
                    currentTrace = Math.Clamp(currentTrace + TraceIncreaseAmount, 0.0, 100.0);
                    _logAction?.Invoke($"Trace risk check succeeded! Trace increased to {currentTrace:F1}");
                }
                else
                {
                    _logAction?.Invoke($"Trace risk check failed (Rolled {randomValue:F3} vs Risk {missionDef.TraceRisk:P1}).");
                }
            }
            // -----------------------------

            // --- Check for Mission Completion ---
            if (currentProgress >= missionDef.DurationTicks)
            {
                // ... (Calculate rewards, level, select next mission) ...
                _logAction?.Invoke($"Mission '{currentMissionId}' completed!"); var newCredits = currentState.Credits + missionDef.Reward.Credits; var newExperience = currentState.Experience + missionDef.Reward.Xp; var tempStateForLevelCalc = currentState with { Experience = newExperience }; int newLevel = tempStateForLevelCalc.CalculateLevel(); if (newLevel > currentState.Level) { _logAction?.Invoke($"LEVEL UP! Reached Level {newLevel}"); }
                string? nextMissionId = SelectNextMissionId(currentMissionId); missionToLog = nextMissionId; _logAction?.Invoke($"Awarded {missionDef.Reward.Credits} Cr, {missionDef.Reward.Xp:F1} XP. Level: {newLevel}. Selecting next mission: {nextMissionId ?? "None"}. Resetting progress."); currentProgress = 0;
                // Assign new state including updated trace
                newState = currentState with { Credits = newCredits, Experience = newExperience, Level = newLevel, ActiveMissionProgress = currentProgress, ActiveMissionId = nextMissionId, TraceLevel = currentTrace };
            }
            else
            {
                // Mission not complete, update progress and trace
                newState = currentState with { ActiveMissionProgress = currentProgress, TraceLevel = currentTrace };
            }
        }

        // Update the application state
        _updateGameState(newState);
        _logAction?.Invoke($"Tick processed.");
    }

    private string? SelectNextMissionId(string currentMissionId)
    {
        // ... (SelectNextMissionId remains the same) ...
        var allMissionIds = _missionDefinitions.Keys.ToList(); if (allMissionIds.Count == 0) { return null; }
        if (allMissionIds.Count == 1) { return allMissionIds[0]; }
        var possibleNextIds = allMissionIds.Where(id => id != currentMissionId).ToList(); if (!possibleNextIds.Any()) { possibleNextIds = allMissionIds; }
        int randomIndex = _defaultRandom.Next(0, possibleNextIds.Count); return possibleNextIds[randomIndex];
    }

    private void ScheduleTick()
    {
        // ... (ScheduleTick logic remains the same) ...
        if (!_isRunning || _mainLoop == null) return; if (_timeoutToken != null) { _mainLoop.RemoveTimeout(_timeoutToken); _timeoutToken = null; }
        var intervalMs = CalculateTickIntervalMs(_getGameState()); _logAction?.Invoke($"Scheduling next tick in {intervalMs}ms."); _timeoutToken = _mainLoop?.AddTimeout(TimeSpan.FromMilliseconds(intervalMs), TickCallback);
    }

    private bool TickCallback(MainLoop loop)
    {
        // ... (TickCallback logic remains the same) ...
        if (!_isRunning) return false; try { ProcessTick(); } catch (Exception ex) { _logAction?.Invoke($"!!! ERROR during ProcessTick: {ex.Message}"); }
        return _isRunning;
    }

    internal double CalculateTickIntervalMs(GameState? state)
    {
        // ... (CalculateTickIntervalMs logic remains the same) ...
        var baseIntervalMs = BaseTickIntervalSeconds * 1000; if (state?.Stats == null) { return baseIntervalMs; }
        var hackSpeed = state.Stats.HackSpeed; var speedFactor = Math.Clamp(1.0 - (hackSpeed / 100.0), 0.1, 1.0); var calculatedInterval = baseIntervalMs * speedFactor; return calculatedInterval;
    }
}
