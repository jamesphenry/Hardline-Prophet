// src/HardlineProphet/UI/Views/InGameView.cs
using HardlineProphet.Core.Models; // GameState, PlayerStats
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
    // Add labels for Stealth, DataYield later if needed for M1 display

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
            Height = Dim.Percent(40) // Adjust height as needed
        };

        _usernameLabel = new Label("Username: ?") { X = 1, Y = 1 };
        _levelLabel = new Label("Level: ?") { X = 1, Y = Pos.Bottom(_usernameLabel) };
        _xpLabel = new Label("XP: ?") { X = 1, Y = Pos.Bottom(_levelLabel) };
        _creditsLabel = new Label("Credits: ?") { X = 1, Y = Pos.Bottom(_xpLabel) };
        _hackSpeedLabel = new Label("Hack Speed: ?") { X = 1, Y = Pos.Bottom(_creditsLabel) };
        // Add other stat labels here...

        statusFrame.Add(_usernameLabel, _levelLabel, _xpLabel, _creditsLabel, _hackSpeedLabel);

        // --- Progress Bar ---
        // TODO: Add Mission Name Label later
        _progressBar = new ProgressBar()
        {
            X = Pos.Right(statusFrame) + 1,
            Y = 1, // Align near top
            Width = Dim.Fill(1),
            Height = 1, // ProgressBars are typically 1 line high
            Fraction = 0f,
            ColorScheme = Colors.Base // Or a custom scheme
        };


        // --- Log Window ---
        var logFrame = new FrameView("Log")
        {
            X = Pos.Right(statusFrame) + 1,
            Y = Pos.Bottom(_progressBar) + 1,
            Width = Dim.Fill(1),
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


        Add(statusFrame, _progressBar, logFrame);
    }

    /// <summary>
    /// Updates the displayed information based on the current GameState.
    /// </summary>
    public void UpdateState(GameState? state)
    {
        if (state == null) return; // Do nothing if state is null

        // Update labels - use ?. to handle potential nulls if needed, though state isn't null here
        _usernameLabel.Text = $"Username: {state.Username}";
        _levelLabel.Text = $"Level: {state.Level}";
        _xpLabel.Text = $"XP: {state.Experience:F1}"; // Format double
        _creditsLabel.Text = $"Credits: {state.Credits}";
        _hackSpeedLabel.Text = $"Hack Speed: {state.Stats?.HackSpeed ?? 0}"; // Use null conditional for nested Stats

        // TODO: Update ProgressBar based on active mission progress later
        // float progress = CalculateMissionProgress(state); // Placeholder
        // _progressBar.Fraction = progress;

        // Mark view for redraw
        SetNeedsDisplay();
    }

    /// <summary>
    /// Adds a message to the log view.
    /// </summary>
    public void AddLogMessage(string message)
    {
        // Add timestamp?
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logMessages.Add($"[{timestamp}] {message}");

        // Limit log history size
        while (_logMessages.Count > MaxLogLines)
        {
            _logMessages.RemoveAt(0);
        }

        // Update TextView content
        _logView.Text = string.Join("\n", _logMessages);

        // Scroll to bottom (needs Application.MainLoop.Invoke for thread safety if called from non-UI thread)
        // For now, assuming AddLogMessage is called via delegate from TickService which runs on MainLoop
        _logView.ScrollTo(_logMessages.Count - 1); // Scroll to the last line index

        SetNeedsDisplay();
    }
}
