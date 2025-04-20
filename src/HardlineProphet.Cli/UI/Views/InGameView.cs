// src/HardlineProphet/UI/Views/InGameView.cs
using HardlineProphet.Core.Models; // GameState, PlayerStats, Mission
using Terminal.Gui;
using System;
using System.Collections.Generic; // List for LogView source

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
        Width = Dim.Fill();
        Height = Dim.Fill();

        // --- Status Pane ---
        var statusFrame = new FrameView("Status")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(35), // Adjust width as needed
            Height = Dim.Percent(50) // Adjust height as needed
        };

        _usernameLabel = new Label("Username: ?") { X = 1, Y = 1 };
        _levelLabel = new Label("Level: ?") { X = 1, Y = Pos.Bottom(_usernameLabel) };
        _xpLabel = new Label("XP: ?") { X = 1, Y = Pos.Bottom(_levelLabel) };
        _creditsLabel = new Label("Credits: ?") { X = 1, Y = Pos.Bottom(_xpLabel) };
        _hackSpeedLabel = new Label("Hack Speed: ?") { X = 1, Y = Pos.Bottom(_creditsLabel) };
        // Add other stat labels here...

        statusFrame.Add(_usernameLabel, _levelLabel, _xpLabel, _creditsLabel, _hackSpeedLabel);

        // --- Mission Pane ---
        var missionFrame = new FrameView("Active Mission")
        {
            X = Pos.Right(statusFrame) + 1,
            Y = 0,
            Width = Dim.Fill(1),
            Height = 5 // Height for label and progress bar
        };

        _missionLabel = new Label("Mission: None") { X = 1, Y = 1 };
        _progressBar = new ProgressBar()
        {
            X = 1,
            Y = Pos.Bottom(_missionLabel), // Below mission name
            Width = Dim.Fill(1), // Fill frame width
            Height = 1,
            Fraction = 0f,
            ColorScheme = Colors.Base // Or a custom scheme
        };
        missionFrame.Add(_missionLabel, _progressBar);


        // --- Log Window ---
        var logFrame = new FrameView("Log")
        {
            X = 0, // Span full width below status/mission
            Y = Pos.Bottom(statusFrame), // Position below status frame
            Width = Dim.Fill(),
            Height = Dim.Fill(1) // Fill remaining height
        };

        _logView = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true // Wrap long lines
        };
        logFrame.Add(_logView);


        Add(statusFrame, missionFrame, logFrame); // Add all frames
    }

    /// <summary>
    /// Updates the displayed information based on the current GameState.
    /// </summary>
    public void UpdateState(GameState? state)
    {
        if (state == null) return; // Do nothing if state is null

        // --- Update Status Labels ---
        _usernameLabel.Text = $"Username: {state.Username}";
        _levelLabel.Text = $"Level: {state.Level}"; // Display level (calculation TBD)
        _xpLabel.Text = $"XP: {state.Experience:F1}"; // Display current XP
        _creditsLabel.Text = $"Credits: {state.Credits}";
        _hackSpeedLabel.Text = $"Hack Speed: {state.Stats?.HackSpeed ?? 0}";

        // --- Update Mission Display ---
        string missionName = "None";
        float progressFraction = 0f;

        // Check if there's an active mission and definition exists
        if (!string.IsNullOrEmpty(state.ActiveMissionId) &&
            ApplicationState.LoadedMissions != null &&
            ApplicationState.LoadedMissions.TryGetValue(state.ActiveMissionId, out var missionDef))
        {
            missionName = missionDef.Name;
            // Calculate progress fraction, avoid division by zero
            if (missionDef.DurationTicks > 0)
            {
                progressFraction = (float)state.ActiveMissionProgress / missionDef.DurationTicks;
            }
        }

        _missionLabel.Text = $"Mission: {missionName}";
        _progressBar.Fraction = Math.Clamp(progressFraction, 0f, 1f); // Update progress bar

        // Mark view for redraw
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

        _logView.Text = string.Join(Environment.NewLine, _logMessages); // Use Environment.NewLine

        // Scroll to bottom - Ensure this runs on the UI thread
        // Application.MainLoop.Invoke is handled by the caller (UI.cs delegate)
        _logView.ScrollTo(_logMessages.Count - 1);

        SetNeedsDisplay();
    }
}
