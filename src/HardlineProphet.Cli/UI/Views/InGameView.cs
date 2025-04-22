// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-feature-m2-flavor-events.1+9
// ║ 📄  File: InGameView.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
using System;
using System.Collections.Generic; // List for LogView source
using System.Linq;
using HardlineProphet.Core.Models; // GameState, PlayerStats, Mission
using Terminal.Gui;
using Terminal.Gui.Graphs; // Added for Any check

namespace HardlineProphet.UI.Views;

/// <summary>
/// The main view displayed after successful logon, showing game status and logs.
/// </summary>
public class InGameView : View // Using View instead of Window to embed in MainWindow
{
    // Status Labels
    private Label _usernameLabel;
    private Label _levelLabel;
    private Label _xpLabel;
    private Label _creditsLabel;
    private Label _hackSpeedLabel;
    private Label _stealthLabel;
    private Label _dataYieldLabel;
    private Label _traceLevelLabel; // Added field for Trace

    // Mission Display
    private Label _missionLabel;
    private ProgressBar _progressBar;

    // Log Display
    private TextView _logView;
    private List<string> _logMessages = new List<string>();
    private const int MaxLogLines = 100;

    public InGameView()
    {
        Width = Dim.Fill();
        Height = Dim.Fill();

        // --- Status Pane ---
        var statusFrame = new FrameView("Status")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(35),
            Height = Dim.Percent(85), // Adjusted height
        };

        _usernameLabel = new Label("Username: ?") { X = 1, Y = 1 };
        _levelLabel = new Label("Level: ?") { X = 1, Y = Pos.Bottom(_usernameLabel) };
        _xpLabel = new Label("XP: ?") { X = 1, Y = Pos.Bottom(_levelLabel) };
        _creditsLabel = new Label("Credits: ?") { X = 1, Y = Pos.Bottom(_xpLabel) };
        var statDivider = new LineView(Orientation.Horizontal)
        {
            X = 1,
            Y = Pos.Bottom(_creditsLabel) + 1,
            Width = Dim.Fill(1),
        };
        _hackSpeedLabel = new Label("Hack Speed: ?") { X = 1, Y = Pos.Bottom(statDivider) + 1 };
        _stealthLabel = new Label("Stealth: ?") { X = 1, Y = Pos.Bottom(_hackSpeedLabel) };
        _dataYieldLabel = new Label("Data Yield: ?") { X = 1, Y = Pos.Bottom(_stealthLabel) };
        var traceDivider = new LineView(Orientation.Horizontal)
        {
            X = 1,
            Y = Pos.Bottom(_dataYieldLabel) + 1,
            Width = Dim.Fill(1),
        };
        // Initialize the Trace Level label
        _traceLevelLabel = new Label("Trace Level: ?") { X = 1, Y = Pos.Bottom(traceDivider) + 1 };

        statusFrame.Add(
            _usernameLabel,
            _levelLabel,
            _xpLabel,
            _creditsLabel,
            statDivider,
            _hackSpeedLabel,
            _stealthLabel,
            _dataYieldLabel,
            traceDivider,
            _traceLevelLabel
        );

        // --- Mission Pane ---
        var missionFrame = new FrameView("Active Mission")
        {
            X = Pos.Right(statusFrame) + 1,
            Y = 0,
            Width = Dim.Fill(1),
            Height = 5,
        };
        _missionLabel = new Label("Mission: None") { X = 1, Y = 1 };
        _progressBar = new ProgressBar()
        {
            X = 1,
            Y = Pos.Bottom(_missionLabel),
            Width = Dim.Fill(1),
            Height = 1,
            Fraction = 0f,
            ColorScheme = Colors.Base,
        };
        missionFrame.Add(_missionLabel, _progressBar);

        // --- Log Window ---
        var logFrame = new FrameView("Log")
        {
            X = Pos.Right(statusFrame) + 1,
            Y = Pos.Bottom(missionFrame),
            Width = Dim.Fill(1),
            Height = Dim.Fill(1),
        };
        _logView = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
        };
        logFrame.Add(_logView);

        Add(statusFrame, missionFrame, logFrame);
    }

    /// <summary>
    /// Updates the displayed information based on the current GameState.
    /// </summary>
    public void UpdateState(GameState? state)
    {
        if (state == null)
            return;

        // --- Update Status Labels ---
        _usernameLabel.Text = $"Username: {state.Username}";
        _levelLabel.Text = $"Level: {state.Level}";
        _xpLabel.Text = $"XP: {state.Experience:F1}";
        _creditsLabel.Text = $"Credits: {state.Credits:N0}";
        _hackSpeedLabel.Text = $"Hack Speed: {state.Stats?.HackSpeed ?? 0}";
        _stealthLabel.Text = $"Stealth: {state.Stats?.Stealth ?? 0}";
        _dataYieldLabel.Text = $"Data Yield: {state.Stats?.DataYield ?? 0}";
        // Update the Trace Level label text
        _traceLevelLabel.Text = $"Trace Level: {state.TraceLevel:F1} / 100.0";

        // --- Update Mission Display ---
        string missionName = "None";
        float progressFraction = 0f;
        if (
            !string.IsNullOrEmpty(state.ActiveMissionId)
            && ApplicationState.LoadedMissions != null
            && ApplicationState.LoadedMissions.TryGetValue(
                state.ActiveMissionId,
                out var missionDef
            )
        )
        {
            missionName = missionDef.Name;
            if (missionDef.DurationTicks > 0)
            {
                progressFraction = (float)state.ActiveMissionProgress / missionDef.DurationTicks;
            }
            else
            {
                progressFraction = state.ActiveMissionProgress > 0 ? 1f : 0f;
            }
        }
        _missionLabel.Text = $"Mission: {missionName}";
        _progressBar.Fraction = Math.Clamp(progressFraction, 0f, 1f);

        SetNeedsDisplay();
    }

    /// <summary>
    /// Adds a message to the log view.
    /// </summary>
    public void AddLogMessage(string message)
    {
        // ... (AddLogMessage remains the same) ...
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logMessages.Add($"[{timestamp}] {message}");
        while (_logMessages.Count > MaxLogLines)
        {
            _logMessages.RemoveAt(0);
        }
        _logView.Text = string.Join(Environment.NewLine, _logMessages);
        Application.MainLoop.Invoke(() =>
        {
            if (_logMessages.Any())
            {
                int desiredTopRow = Math.Max(0, _logMessages.Count - _logView.Bounds.Height);
                _logView.TopRow = desiredTopRow;
                _logView.SetNeedsDisplay();
            }
        });
    }
}
