// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-feature-m2-flavor-events.1+8
// ║ 📄  File: PerkSelectionDialog.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
using System;
using System.Collections.Generic;
using System.Linq;
using NStack;
using Terminal.Gui;
using Terminal.Gui.Graphs; // For ustring

namespace HardlineProphet.UI.Dialogs;

/// <summary>
/// Represents a selectable starting perk.
/// </summary>
public record StartingPerk(string Id, string Name, string Description);

/// <summary>
/// Dialog for selecting one starting perk.
/// </summary>
public class PerkSelectionDialog : Dialog
{
    // Define the available starting perks based on Readme 3.4
    private static readonly List<StartingPerk> _startingPerks = new()
    {
        new("trace_dampener", "Trace Dampener", "-25% trace build-up rate."),
        new("stim_surge", "Stim Surge", "First 5 missions complete instantly."),
        new("seed_capital", "Seed Capital", "Start with an extra 500 credits."),
        new("soft_override", "Soft Override", "First failure is auto-converted to success."),
    };

    public string SelectedPerkId { get; private set; } = string.Empty;
    public bool Canceled { get; private set; } = true;

    private RadioGroup _radioGroup;
    private TextView _descriptionView;

    public PerkSelectionDialog()
    {
        Title = "Select Starting Perk (Choose One)";
        ColorScheme = Colors.Dialog;

        var perkNames = _startingPerks.Select(p => ustring.Make(p.Name)).ToArray();

        _radioGroup = new RadioGroup(perkNames)
        {
            X = 1,
            Y = 1,
            SelectedItem = 0, // Default to first perk
        };
        _radioGroup.SelectedItemChanged += (args) => UpdateDescription(args.SelectedItem);

        _descriptionView = new TextView()
        {
            X = Pos.Right(_radioGroup) + 2,
            Y = 1,
            Width = Dim.Fill(2),
            Height = Dim.Height(_radioGroup),
            ReadOnly = true,
            WordWrap = true,
            ColorScheme = Colors.Base,
        };

        // Use Orientation directly now that we know Graphs isn't needed here
        var separator = new LineView(Orientation.Vertical)
        {
            X = Pos.Right(_radioGroup) + 1,
            Y = 1,
            Height = Dim.Height(_radioGroup),
        };

        var okButton = new Button("Select", is_default: true)
        {
            X = Pos.Center() - 10,
            Y = Pos.Bottom(_radioGroup) + 1,
        };
        okButton.Clicked += () =>
        {
            if (_radioGroup.SelectedItem >= 0 && _radioGroup.SelectedItem < _startingPerks.Count)
            {
                SelectedPerkId = _startingPerks[_radioGroup.SelectedItem].Id; // Store the ID
                Canceled = false;
                Application.RequestStop();
            }
            else
            {
                MessageBox.ErrorQuery("Error", "Invalid perk selection.", "OK");
            }
        };

        var cancelButton = new Button("Cancel") { X = Pos.Right(okButton) + 1, Y = okButton.Y };
        cancelButton.Clicked += () =>
        {
            SelectedPerkId = string.Empty;
            Canceled = true;
            Application.RequestStop();
        };

        Add(_radioGroup, separator, _descriptionView, okButton, cancelButton);
        UpdateDescription(_radioGroup.SelectedItem);
        _radioGroup.SetFocus();
    }

    private void UpdateDescription(int selectedIndex)
    {
        if (selectedIndex >= 0 && selectedIndex < _startingPerks.Count)
        {
            _descriptionView.Text = _startingPerks[selectedIndex].Description;
        }
        else
        {
            _descriptionView.Text = "";
        }
        _descriptionView.SetNeedsDisplay();
    }
}
