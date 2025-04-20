// src/HardlineProphet/Services/TickService.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Interfaces; // ITickService
using HardlineProphet.Core.Models; // GameState
using System; // Func, Action, ArgumentNullException, Math, TimeSpan
using Terminal.Gui; // MainLoop

namespace HardlineProphet.Services;

/// <summary>
/// Implementation of the game's core tick loop service.
/// </summary>
public class TickService : ITickService
{
    // Dependencies provided via constructor
    private readonly Func<GameState?> _getGameState;
    private readonly Action<GameState> _updateGameState;
    private readonly Action<string> _logAction;
    private readonly MainLoop? _mainLoop; // Changed to nullable MainLoop

    private bool _isRunning = false;
    private object? _timeoutToken = null;

    private const int CreditsPerTick = 10;
    private const double BaseTickIntervalSeconds = 2.0;

    // Constructor updated to accept nullable MainLoop
    public TickService(Func<GameState?> getGameState, Action<GameState> updateGameState, Action<string> logAction, MainLoop? mainLoop = null) // Added default null
    {
        _getGameState = getGameState ?? throw new ArgumentNullException(nameof(getGameState));
        _updateGameState = updateGameState ?? throw new ArgumentNullException(nameof(updateGameState));
        _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
        _mainLoop = mainLoop; // Store potentially null MainLoop
    }

    public bool IsRunning => _isRunning;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _logAction?.Invoke("TickService started. Scheduling first tick...");
        // Only schedule if MainLoop is available
        if (_mainLoop != null)
        {
            ScheduleTick();
        }
        else
        {
            _logAction?.Invoke("TickService started but MainLoop is null (likely testing). Tick scheduling skipped.");
        }
    }

    public void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        // Only remove timeout if MainLoop and token exist
        if (_mainLoop != null && _timeoutToken != null)
        {
            _logAction?.Invoke("TickService stopping. Removing timeout.");
            _mainLoop.RemoveTimeout(_timeoutToken);
            _timeoutToken = null;
        }
        else
        {
            _logAction?.Invoke($"TickService stopping. MainLoop null? {_mainLoop == null}, Timeout token null? {_timeoutToken == null}");
        }
    }

    public void ProcessTick()
    {
        var currentState = _getGameState();
        if (currentState == null)
        {
            _logAction?.Invoke($"ProcessTick exiting: currentState is null.");
            Stop();
            return;
        }
        // _isRunning check is handled by TickCallback/Stop method

        _logAction?.Invoke($"Processing tick for {currentState.Username}...");
        var newCredits = currentState.Credits + CreditsPerTick;
        var newState = currentState with { Credits = newCredits };
        _updateGameState(newState);
        _logAction?.Invoke($"Tick processed. Awarded {CreditsPerTick} credits. Total: {newCredits}");
    }

    private void ScheduleTick()
    {
        // Guard against scheduling if stopped OR if mainloop isn't available
        if (!_isRunning || _mainLoop == null) return;

        if (_timeoutToken != null)
        {
            _mainLoop.RemoveTimeout(_timeoutToken); // Remove previous just in case
            _timeoutToken = null;
        }

        var intervalMs = CalculateTickIntervalMs(_getGameState());
        _logAction?.Invoke($"Scheduling next tick in {intervalMs}ms.");
        // Use null-conditional just in case, though guarded above
        _timeoutToken = _mainLoop?.AddTimeout(TimeSpan.FromMilliseconds(intervalMs), TickCallback);
    }

    private bool TickCallback(MainLoop loop)
    {
        if (!_isRunning) return false; // Stop repeating if Stop() was called

        try
        {
            ProcessTick();
        }
        catch (Exception ex)
        {
            _logAction?.Invoke($"!!! ERROR during ProcessTick: {ex.Message}");
            // Decide if loop should continue on error? For now, yes.
        }
        // Return true to keep repeating with the same interval
        return _isRunning;
    }


    internal double CalculateTickIntervalMs(GameState? state)
    {
        var baseIntervalMs = BaseTickIntervalSeconds * 1000;
        if (state?.Stats == null) { return baseIntervalMs; }
        var hackSpeed = state.Stats.HackSpeed;
        var speedFactor = Math.Clamp(1.0 - (hackSpeed / 100.0), 0.1, 1.0);
        var calculatedInterval = baseIntervalMs * speedFactor;
        return calculatedInterval;
    }
}
