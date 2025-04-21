// src/HardlineProphet/Services/TickService.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Extensions; // CalculateLevel()
using HardlineProphet.Core.Interfaces; // ITickService
using HardlineProphet.Core.Models; // GameState, Mission
using System; // Func, Action, ArgumentNullException, Math, TimeSpan, Random
using System.Collections.Generic; // IReadOnlyDictionary, List
using System.Linq; // FirstOrDefault, ToList, Where
using Terminal.Gui; // MainLoop

namespace HardlineProphet.Services;

public class TickService : ITickService
{
    private readonly Func<GameState?> _getGameState;
    private readonly Action<GameState> _updateGameState;
    private readonly Action<string> _logAction;
    private readonly MainLoop? _mainLoop;
    private readonly IReadOnlyDictionary<string, Mission> _missionDefinitions;

    private bool _isRunning = false;
    private object? _timeoutToken = null;
    private static readonly Random _random = new Random(); // Random instance for selection

    private const double BaseTickIntervalSeconds = 2.0;

    public TickService(
        Func<GameState?> getGameState,
        Action<GameState> updateGameState,
        Action<string> logAction,
        IReadOnlyDictionary<string, Mission> missionDefinitions,
        MainLoop? mainLoop = null)
    {
        // ... (Constructor remains the same) ...
        _getGameState = getGameState ?? throw new ArgumentNullException(nameof(getGameState)); _updateGameState = updateGameState ?? throw new ArgumentNullException(nameof(updateGameState)); _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction)); _missionDefinitions = missionDefinitions ?? throw new ArgumentNullException(nameof(missionDefinitions)); _mainLoop = mainLoop;
    }

    public bool IsRunning => _isRunning;

    public void Start()
    {
        // ... (Start logic remains the same) ...
        if (_isRunning) return; _isRunning = true; _logAction?.Invoke("TickService started. Scheduling first tick..."); if (_mainLoop != null) { ScheduleTick(); } else { _logAction?.Invoke("TickService started but MainLoop is null (likely testing). Tick scheduling skipped."); }
    }

    public void Stop()
    {
        // ... (Stop logic remains the same) ...
        if (!_isRunning) return; _isRunning = false; if (_mainLoop != null && _timeoutToken != null) { _mainLoop.RemoveTimeout(_timeoutToken); _timeoutToken = null; _logAction?.Invoke("TickService stopping. Removing timeout."); } else { _logAction?.Invoke($"TickService stopping. MainLoop null? {_mainLoop == null}, Timeout token null? {_timeoutToken == null}"); }
    }

    public void ProcessTick()
    {
        var currentState = _getGameState();
        if (currentState == null) { Stop(); return; }

        _logAction?.Invoke($"Processing tick for {currentState.Username}...");

        string? currentMissionId = currentState.ActiveMissionId;
        int currentProgress = currentState.ActiveMissionProgress;
        GameState newState;
        string? missionToLog = currentMissionId; // Capture for logging before potential change

        // --- Mission Logic ---
        // 1. Assign default mission if none is active or current is invalid
        if (string.IsNullOrEmpty(currentMissionId) || !_missionDefinitions.ContainsKey(currentMissionId))
        {
            string? defaultMissionId = _missionDefinitions.Keys.FirstOrDefault();
            if (!string.IsNullOrEmpty(defaultMissionId))
            {
                _logAction?.Invoke($"No active/valid mission. Assigning default: {defaultMissionId}");
                newState = currentState with { ActiveMissionId = defaultMissionId, ActiveMissionProgress = 1 };
                missionToLog = defaultMissionId; // Update for logging
                _logAction?.Invoke($"Mission '{missionToLog}' started, progress: 1");
            }
            else
            {
                _logAction?.Invoke($"WARNING: No active mission and no default missions loaded. Cannot progress.");
                return;
            }
        }
        // 2. If mission is active, increment progress and check for completion
        else
        {
            if (!_missionDefinitions.TryGetValue(currentMissionId, out var missionDef))
            {
                _logAction?.Invoke($"ERROR: Could not find definition for active mission ID '{currentMissionId}'. Skipping progress.");
                return;
            }

            currentProgress++;
            _logAction?.Invoke($"Mission '{currentMissionId}' progress: {currentProgress}/{missionDef.DurationTicks}");

            // --- Check for Mission Completion ---
            if (currentProgress >= missionDef.DurationTicks)
            {
                _logAction?.Invoke($"Mission '{currentMissionId}' completed!");
                var newCredits = currentState.Credits + missionDef.Reward.Credits;
                var newExperience = currentState.Experience + missionDef.Reward.Xp;
                var tempStateForLevelCalc = currentState with { Experience = newExperience };
                int newLevel = tempStateForLevelCalc.CalculateLevel();
                if (newLevel > currentState.Level) { _logAction?.Invoke($"LEVEL UP! Reached Level {newLevel}"); }

                // --- Select Next Mission ---
                string? nextMissionId = SelectNextMissionId(currentMissionId);
                missionToLog = nextMissionId; // Update for logging
                                              // -------------------------

                _logAction?.Invoke($"Awarded {missionDef.Reward.Credits} Cr, {missionDef.Reward.Xp:F1} XP. Level: {newLevel}. Selecting next mission: {nextMissionId ?? "None"}. Resetting progress.");

                // Update state with rewards, level, reset progress, and NEW mission ID
                newState = currentState with
                {
                    Credits = newCredits,
                    Experience = newExperience,
                    Level = newLevel,
                    ActiveMissionProgress = 0, // Reset progress
                    ActiveMissionId = nextMissionId // Assign the newly selected mission
                };
            }
            else
            {
                // Mission not complete, just update progress
                newState = currentState with { ActiveMissionProgress = currentProgress };
            }
            // ------------------------------------
        }

        // Update the application state
        _updateGameState(newState);
        _logAction?.Invoke($"Tick processed.");
    }

    /// <summary>
    /// Selects the next mission ID, attempting to pick a different one randomly.
    /// </summary>
    private string? SelectNextMissionId(string currentMissionId)
    {
        var allMissionIds = _missionDefinitions.Keys.ToList();
        if (allMissionIds.Count == 0)
        {
            return null; // No missions available
        }
        if (allMissionIds.Count == 1)
        {
            return allMissionIds[0]; // Only one mission, loop it
        }

        // Try to pick a different mission
        var possibleNextIds = allMissionIds.Where(id => id != currentMissionId).ToList();
        if (!possibleNextIds.Any())
        {
            // Should only happen if currentMissionId was invalid but somehow passed checks,
            // or if only one mission exists (handled above). Fallback to any mission.
            possibleNextIds = allMissionIds;
        }

        // Select randomly from the possible next missions
        int randomIndex = _random.Next(0, possibleNextIds.Count);
        return possibleNextIds[randomIndex];
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
