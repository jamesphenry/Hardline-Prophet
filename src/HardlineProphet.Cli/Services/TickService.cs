// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-alpha.12
// ║ 📄  File: TickService.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
// src/HardlineProphet/Services/TickService.cs
using System; // Func, Action, ArgumentNullException, Math, TimeSpan, Random
using System.Collections.Generic; // IReadOnlyDictionary, List
using System.Linq; // FirstOrDefault
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Extensions; // CalculateLevel()
using HardlineProphet.Core.Interfaces; // ITickService
using HardlineProphet.Core.Models; // GameState, Mission, FlavorEvent, FlavorEventTrigger
using Terminal.Gui; // MainLoop

namespace HardlineProphet.Services;

public class TickService : ITickService
{
    private readonly Func<GameState?> _getGameState;
    private readonly Action<GameState> _updateGameState;
    private readonly Action<string> _logAction;
    private readonly IReadOnlyDictionary<string, Mission> _missionDefinitions;
    private readonly IReadOnlyDictionary<FlavorEventTrigger, List<FlavorEvent>> _flavorEvents; // Added
    private readonly MainLoop? _mainLoop;
    private readonly Func<double> _rng;

    private bool _isRunning = false;
    private object? _timeoutToken = null;
    private static readonly Random _defaultRandom = new Random();

    private const double BaseTickIntervalSeconds = 2.0;
    private const double TraceIncreaseAmount = 0.5;

    public TickService(
        Func<GameState?> getGameState,
        Action<GameState> updateGameState,
        Action<string> logAction,
        IReadOnlyDictionary<string, Mission> missionDefinitions,
        IReadOnlyDictionary<FlavorEventTrigger, List<FlavorEvent>> flavorEvents, // Added parameter
        MainLoop? mainLoop = null,
        Func<double>? rng = null
    )
    {
        // ... (Constructor remains the same) ...
        _getGameState = getGameState ?? throw new ArgumentNullException(nameof(getGameState));
        _updateGameState =
            updateGameState ?? throw new ArgumentNullException(nameof(updateGameState));
        _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
        _missionDefinitions =
            missionDefinitions ?? throw new ArgumentNullException(nameof(missionDefinitions));
        _flavorEvents = flavorEvents ?? new Dictionary<FlavorEventTrigger, List<FlavorEvent>>();
        _mainLoop = mainLoop;
        _rng = rng ?? _defaultRandom.NextDouble;
        _logAction?.Invoke(
            $"TickService initialized. RNG set (Custom: {rng != null}). Flavor Events Loaded: {_flavorEvents.Count > 0}"
        );
    }

    public bool IsRunning => _isRunning;

    public void Start()
    {
        // ... (Start logic remains the same) ...
        if (_isRunning)
            return;
        _isRunning = true;
        _logAction?.Invoke("TickService started. Scheduling first tick...");
        if (_mainLoop != null)
        {
            ScheduleTick();
        }
        else
        {
            _logAction?.Invoke(
                "TickService started but MainLoop is null (likely testing). Tick scheduling skipped."
            );
        }
    }

    public void Stop()
    {
        // ... (Stop logic remains the same) ...
        if (!_isRunning)
            return;
        _isRunning = false;
        if (_mainLoop != null && _timeoutToken != null)
        {
            _mainLoop.RemoveTimeout(_timeoutToken);
            _timeoutToken = null;
            _logAction?.Invoke("TickService stopping. Removing timeout.");
        }
        else
        {
            _logAction?.Invoke(
                $"TickService stopping. MainLoop null? {_mainLoop == null}, Timeout token null? {_timeoutToken == null}"
            );
        }
    }

    public void ProcessTick()
    {
        var currentState = _getGameState();
        if (currentState == null)
        {
            Stop();
            return;
        }

        //_logAction?.Invoke($"Processing tick for {currentState.Username}...");

        string? currentMissionId = currentState.ActiveMissionId;
        int currentProgress = currentState.ActiveMissionProgress;
        double currentTrace = currentState.TraceLevel;
        GameState newState = currentState; // Start with current state
        string? missionToLog = currentMissionId;
        bool missionCompletedThisTick = false; // Flag for event triggers

        // --- Mission Logic ---
        if (
            string.IsNullOrEmpty(currentMissionId)
            || !_missionDefinitions.ContainsKey(currentMissionId)
        )
        {
            // Assign default mission...
            string? defaultMissionId = _missionDefinitions.Keys.FirstOrDefault();
            if (!string.IsNullOrEmpty(defaultMissionId))
            { /* ... assign default ... */
                _logAction?.Invoke($"No active/valid mission. Assigning default: {defaultMissionId}");
                newState = currentState with
                {
                    ActiveMissionId = defaultMissionId,
                    ActiveMissionProgress = 1,
                    TraceLevel = currentTrace,
                };
                missionToLog = defaultMissionId;
                _logAction?.Invoke($"Mission '{missionToLog}' started");
            }
            else
            {
                _logAction?.Invoke(
                    $"WARNING: No active mission and no default missions loaded. Cannot progress."
                );
                return;
            }
        }
        else
        {
            if (!_missionDefinitions.TryGetValue(currentMissionId, out var missionDef))
            {
                _logAction?.Invoke(
                    $"ERROR: Could not find definition for active mission ID '{currentMissionId}'. Skipping progress."
                );
                return;
            }

            currentProgress++;
            //_logAction?.Invoke(
            //    $"Mission '{currentMissionId}' progress: {currentProgress}/{missionDef.DurationTicks}"
            //);

            // --- Trace Increase Logic ---
            if (missionDef.TraceRisk > 0)
            {
                double randomValue = _rng();
                if (randomValue < missionDef.TraceRisk)
                {
                    currentTrace = Math.Clamp(currentTrace + TraceIncreaseAmount, 0.0, 100.0);
                    _logAction?.Invoke($"Trace increased to {currentTrace:F1}");
                }
                else
                {
                    //_logAction?.Invoke(
                    //    $"Trace risk check failed (Rolled {randomValue:F3} vs Risk {missionDef.TraceRisk:P1})."
                    //);
                }
            }

            // --- Check for Mission Completion ---
            if (currentProgress >= missionDef.DurationTicks)
            {
                missionCompletedThisTick = true; // Set flag
                // ... (Calculate rewards, level, select next mission) ...
                _logAction?.Invoke($"Mission '{currentMissionId}' completed!");
                var newCredits = currentState.Credits + missionDef.Reward.Credits;
                var newExperience = currentState.Experience + missionDef.Reward.Xp;
                var tempStateForLevelCalc = currentState with { Experience = newExperience };
                int newLevel = tempStateForLevelCalc.CalculateLevel();
                if (newLevel > currentState.Level)
                {
                    _logAction?.Invoke($"LEVEL UP! Reached Level {newLevel}"); /* TODO: Trigger OnLevelUp event here? */
                }
                string? nextMissionId = SelectNextMissionId(currentMissionId);
                missionToLog = nextMissionId;
                _logAction?.Invoke(
                    $"Awarded {missionDef.Reward.Credits} Cr, {missionDef.Reward.Xp:F1} XP. Level: {newLevel}."
                );
                currentProgress = 0;
                newState = currentState with
                {
                    Credits = newCredits,
                    Experience = newExperience,
                    Level = newLevel,
                    ActiveMissionProgress = currentProgress,
                    ActiveMissionId = nextMissionId,
                    TraceLevel = currentTrace,
                };
            }
            else
            {
                // Mission not complete, update progress and trace
                newState = currentState with
                {
                    ActiveMissionProgress = currentProgress,
                    TraceLevel = currentTrace,
                };
            }
        }

        // --- Flavor Event Trigger Logic (OnTick) ---
        TriggerFlavorEvents(FlavorEventTrigger.OnTick, ref newState); // Pass newState by ref if effects modify it
        // -------------------------------------------

        // --- Trigger other events based on flags ---
        // if (missionCompletedThisTick) { TriggerFlavorEvents(FlavorEventTrigger.OnMissionComplete, ref newState); }
        // if (newState.Level > currentState.Level) { TriggerFlavorEvents(FlavorEventTrigger.OnLevelUp, ref newState); }
        // -------------------------------------------

        _updateGameState(newState); // Update state *after* all modifications
        //_logAction?.Invoke($"Tick processed.");
    }

    /// <summary>
    /// Checks for and potentially triggers flavor events based on the trigger type.
    /// </summary>
    private void TriggerFlavorEvents(FlavorEventTrigger trigger, ref GameState state) // Pass state by ref
    {
        if (_flavorEvents.TryGetValue(trigger, out var potentialEvents))
        {
            foreach (var ev in potentialEvents)
            {
                double randomValue = _rng();
                if (randomValue < ev.Chance)
                {
                    _logAction?.Invoke(ev.Text); // Log the event text
                    // TODO: Apply ev.Effect to the 'state' object here later
                    // Example: if (ev.Effect?.Stat == "Stealth") state = state with { Stats = state.Stats with { Stealth = state.Stats.Stealth + (int)ev.Effect.Value } };
                    //_logAction?.Invoke($"Triggered flavor event: {ev.Id}");
                    // Should we allow multiple events per trigger type per tick? For now, yes.
                }
            }
        }
    }

    private string? SelectNextMissionId(string currentMissionId)
    { /* ... remains the same ... */
        var allMissionIds = _missionDefinitions.Keys.ToList();
        if (allMissionIds.Count == 0)
        {
            return null;
        }
        if (allMissionIds.Count == 1)
        {
            return allMissionIds[0];
        }
        var possibleNextIds = allMissionIds.Where(id => id != currentMissionId).ToList();
        if (!possibleNextIds.Any())
        {
            possibleNextIds = allMissionIds;
        }
        int randomIndex = _defaultRandom.Next(0, possibleNextIds.Count);
        return possibleNextIds[randomIndex];
    }

    private void ScheduleTick()
    { /* ... remains the same ... */
        if (!_isRunning || _mainLoop == null)
            return;
        if (_timeoutToken != null)
        {
            _mainLoop.RemoveTimeout(_timeoutToken);
            _timeoutToken = null;
        }
        var intervalMs = CalculateTickIntervalMs(_getGameState());
        //_logAction?.Invoke($"Scheduling next tick in {intervalMs}ms.");
        _timeoutToken = _mainLoop?.AddTimeout(TimeSpan.FromMilliseconds(intervalMs), TickCallback);
    }

    private bool TickCallback(MainLoop loop)
    { /* ... remains the same ... */
        if (!_isRunning)
            return false;
        try
        {
            ProcessTick();
        }
        catch (Exception ex)
        {
            _logAction?.Invoke($"!!! ERROR during ProcessTick: {ex.Message}");
        }
        return _isRunning;
    }

    internal double CalculateTickIntervalMs(GameState? state)
    { /* ... remains the same ... */
        var baseIntervalMs = BaseTickIntervalSeconds * 1000;
        if (state?.Stats == null)
        {
            return baseIntervalMs;
        }
        var hackSpeed = state.Stats.HackSpeed;
        var speedFactor = Math.Clamp(1.0 - (hackSpeed / 100.0), 0.1, 1.0);
        var calculatedInterval = baseIntervalMs * speedFactor;
        return calculatedInterval;
    }
}
