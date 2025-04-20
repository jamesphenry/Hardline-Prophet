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
    private readonly MainLoop _mainLoop; // Added MainLoop dependency

    private bool _isRunning = false;
    private object? _timeoutToken = null; // To store MainLoop.AddTimeout result

    private const int CreditsPerTick = 10;
    private const double BaseTickIntervalSeconds = 2.0;

    // Constructor updated to accept MainLoop
    public TickService(Func<GameState?> getGameState, Action<GameState> updateGameState, Action<string> logAction, MainLoop mainLoop)
    {
        _getGameState = getGameState ?? throw new ArgumentNullException(nameof(getGameState));
        _updateGameState = updateGameState ?? throw new ArgumentNullException(nameof(updateGameState));
        _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
        _mainLoop = mainLoop ?? throw new ArgumentNullException(nameof(mainLoop)); // Store MainLoop
    }

    public bool IsRunning => _isRunning;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _logAction?.Invoke("TickService started. Scheduling first tick...");
        // Schedule the first tick using the calculated interval
        ScheduleTick();
    }

    public void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        // Remove any pending timeout
        if (_timeoutToken != null)
        {
            _logAction?.Invoke("TickService stopping. Removing timeout.");
            _mainLoop.RemoveTimeout(_timeoutToken);
            _timeoutToken = null;
        }
        else
        {
            _logAction?.Invoke("TickService stopping. No active timeout.");
        }
    }

    /// <summary>
    /// Processes a single game tick. Should primarily be called by the MainLoop callback.
    /// </summary>
    public void ProcessTick() // Keeping public for potential manual trigger/testing
    {
        // Note: _isRunning check is now primarily handled by the TickCallback return value
        var currentState = _getGameState();
        if (currentState == null) // Only need to check for null state now
        {
            _logAction?.Invoke($"ProcessTick exiting: currentState is null.");
            Stop(); // Stop if state is lost
            return;
        }

        _logAction?.Invoke($"Processing tick for {currentState.Username}...");
        var newCredits = currentState.Credits + CreditsPerTick;
        // _logAction?.Invoke($"Calculated newCredits = {newCredits}"); // Reduce noise maybe
        var newState = currentState with { Credits = newCredits };
        // _logAction?.Invoke($"Created newState with Credits = {newState.Credits}. Calling _updateGameState...");
        _updateGameState(newState);
        // _logAction?.Invoke($"_updateGameState called.");
        _logAction?.Invoke($"Tick processed. Awarded {CreditsPerTick} credits. Total: {newCredits}");
    }

    /// <summary>
    /// Schedules the next tick callback using MainLoop.AddTimeout.
    /// </summary>
    private void ScheduleTick()
    {
        if (!_isRunning) return; // Don't schedule if stopped

        // Remove existing timeout just in case (shouldn't be necessary with callback logic)
        if (_timeoutToken != null)
        {
            _mainLoop.RemoveTimeout(_timeoutToken);
        }

        var intervalMs = CalculateTickIntervalMs(_getGameState());
        _logAction?.Invoke($"Scheduling next tick in {intervalMs}ms.");
        _timeoutToken = _mainLoop.AddTimeout(TimeSpan.FromMilliseconds(intervalMs), TickCallback);
    }

    /// <summary>
    /// The callback function executed by MainLoop for each tick.
    /// </summary>
    /// <returns>True if the timer should continue, false otherwise.</returns>
    private bool TickCallback(MainLoop loop) // Parameter name changed to loop
    {
        if (!_isRunning) return false; // Stop repeating if Stop() was called

        try
        {
            ProcessTick();
        }
        catch (Exception ex)
        {
            // Log exceptions during tick processing
            _logAction?.Invoke($"!!! ERROR during ProcessTick: {ex.Message}");
            // Optionally stop the service on error, or just log and continue? Let's log and continue for now.
            // Stop();
            // return false; // Stop repeating on error
        }

        // Reschedule the next tick implicitly by returning true,
        // MainLoop will use the same timespan. Or recalculate? Let's recalculate.
        // No, AddTimeout repeats with the *same* timespan if callback returns true.
        // To change interval, we need to Stop/RemoveTimeout and Start/AddTimeout again?
        // Let's stick to the simple repeating timer for now. Interval is set once on Start.
        // We can add logic to recalculate/reschedule in ProcessTick if needed later.
        return _isRunning; // Return true to keep the timer repeating if still running
    }


    internal double CalculateTickIntervalMs(GameState? state)
    {
        var baseIntervalMs = BaseTickIntervalSeconds * 1000;
        if (state?.Stats == null)
        {
            // _logAction?.Invoke($"State or Stats null, returning base interval: {baseIntervalMs}ms");
            return baseIntervalMs;
        }
        var hackSpeed = state.Stats.HackSpeed;
        // _logAction?.Invoke($"Calculating interval with HackSpeed: {hackSpeed}");
        var speedFactor = Math.Clamp(1.0 - (hackSpeed / 100.0), 0.1, 1.0);
        var calculatedInterval = baseIntervalMs * speedFactor;
        // _logAction?.Invoke($"Calculated speedFactor: {speedFactor}, final interval: {calculatedInterval}ms");
        return calculatedInterval;
    }
}
