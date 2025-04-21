// src/HardlineProphet/UI/Views/InGameView.cs
using HardlineProphet.Core.Models; // GameState, PlayerStats, Mission
using Terminal.Gui;
using System;
using System.Collections.Generic; // List for LogView source
using System.Linq; // Added for Any check

namespace HardlineProphet.UI.Views;

/// <summary>
/// The main view displayed after successful logon, showing game status and logs.
/// </summary>
public class InGameView : View // Using View instead of Window to embed in MainWindow
{
    private Label _usernameLabel;
    private Label _levelLabel;
    private Label _xpLabel;
    private Label _creditsLabel;
    private Label _hackSpeedLabel;
    private Label _missionLabel; // Added label for mission name

    private ProgressBar _progressBar;
    private TextView _logView; // Using TextView for simple scrollable log

    // Store log messages
    private List<string> _logMessages = new List<string>();
    private const int MaxLogLines = 100; // Limit log history

    public InGameView()
    {
        // ... (Constructor remains the same) ...
        Width = Dim.Fill(); Height = Dim.Fill();
        var statusFrame = new FrameView("Status") { X = 0, Y = 0, Width = Dim.Percent(35), Height = Dim.Percent(50) };
        _usernameLabel = new Label("Username: ?") { X = 1, Y = 1 }; _levelLabel = new Label("Level: ?") { X = 1, Y = Pos.Bottom(_usernameLabel) }; _xpLabel = new Label("XP: ?") { X = 1, Y = Pos.Bottom(_levelLabel) }; _creditsLabel = new Label("Credits: ?") { X = 1, Y = Pos.Bottom(_xpLabel) }; _hackSpeedLabel = new Label("Hack Speed: ?") { X = 1, Y = Pos.Bottom(_creditsLabel) }; statusFrame.Add(_usernameLabel, _levelLabel, _xpLabel, _creditsLabel, _hackSpeedLabel);
        var missionFrame = new FrameView("Active Mission") { X = Pos.Right(statusFrame) + 1, Y = 0, Width = Dim.Fill(1), Height = 5 };
        _missionLabel = new Label("Mission: None") { X = 1, Y = 1 }; _progressBar = new ProgressBar() { X = 1, Y = Pos.Bottom(_missionLabel), Width = Dim.Fill(1), Height = 1, Fraction = 0f, ColorScheme = Colors.Base }; missionFrame.Add(_missionLabel, _progressBar);
        var logFrame = new FrameView("Log") { X = 0, Y = Pos.Bottom(statusFrame), Width = Dim.Fill(), Height = Dim.Fill(1) };
        _logView = new TextView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true, WordWrap = true }; logFrame.Add(_logView);
        Add(statusFrame, missionFrame, logFrame);
    }

    /// <summary>
    /// Updates the displayed information based on the current GameState.
    /// </summary>
    public void UpdateState(GameState? state)
    {
        // ... (UpdateState remains the same) ...
        if (state == null) return;
        _usernameLabel.Text = $"Username: {state.Username}"; _levelLabel.Text = $"Level: {state.Level}"; _xpLabel.Text = $"XP: {state.Experience:F1}"; _creditsLabel.Text = $"Credits: {state.Credits}"; _hackSpeedLabel.Text = $"Hack Speed: {state.Stats?.HackSpeed ?? 0}";
        string missionName = "None"; float progressFraction = 0f;
        if (!string.IsNullOrEmpty(state.ActiveMissionId) && ApplicationState.LoadedMissions != null && ApplicationState.LoadedMissions.TryGetValue(state.ActiveMissionId, out var missionDef))
        { missionName = missionDef.Name; if (missionDef.DurationTicks > 0) { progressFraction = (float)state.ActiveMissionProgress / missionDef.DurationTicks; } else { progressFraction = 0f; } }
        _missionLabel.Text = $"Mission: {missionName}"; _progressBar.Fraction = Math.Clamp(progressFraction, 0f, 1f);
        SetNeedsDisplay();
    }

    /// <summary>
    /// Adds a message to the log view.
    /// </summary>
    public void AddLogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logMessages.Add($"[{timestamp}] {message}");

        while (_logMessages.Count > MaxLogLines) { _logMessages.RemoveAt(0); }

        // Store current state before text update
        var currentTop = _logView.TopRow;
        var wasAtBottom = currentTop >= Math.Max(0, _logMessages.Count - 1 - _logView.Bounds.Height); // Check if we were scrolled near the bottom *before* adding the new line

        _logView.Text = string.Join(Environment.NewLine, _logMessages);

        // Use Invoke to ensure UI updates happen on the main thread
        Application.MainLoop.Invoke(() => {
            // Calculate the desired top row to keep the last line visible at the bottom
            // Note: This might not be perfect with word-wrap, as line count != message count.
            int desiredTopRow = Math.Max(0, _logMessages.Count - _logView.Bounds.Height);

            // Only autoscroll if the user was already near the bottom
            // or maybe always scroll? Let's always scroll for now.
            // if (wasAtBottom)
            // {
            _logView.TopRow = desiredTopRow;
            // }
            // else
            // {
            // Optional: If user scrolled up manually, maybe don't force scroll down?
            // For now, always scroll to bottom.
            //     _logView.TopRow = desiredTopRow;
            // }

            _logView.SetNeedsDisplay();
        });
    }
}
