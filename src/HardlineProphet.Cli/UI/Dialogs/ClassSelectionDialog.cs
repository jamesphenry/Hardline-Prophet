// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-feature-m2-flavor-events.1+7
// ║ 📄  File: ClassSelectionDialog.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
using System;
using System.Collections.Generic;
using System.Linq;
using HardlineProphet.Core.Models; // PlayerClass enum
using NStack; // For ustring
using Terminal.Gui;
using Terminal.Gui.Graphs; // Added using for Graphs.Orientation

namespace HardlineProphet.UI.Dialogs;

/// <summary>
/// Dialog for selecting the starting player class.
/// </summary>
public class ClassSelectionDialog : Dialog
{
    public PlayerClass SelectedClass { get; private set; } = PlayerClass.Undefined;
    public bool Canceled { get; private set; } = true;

    private RadioGroup _radioGroup;
    private TextView _descriptionView; // To show class description

    // Class descriptions based on Readme 3.4
    private readonly Dictionary<PlayerClass, string> _classDescriptions = new()
    {
        {
            PlayerClass.Runner,
            "Runner:\nFast, reckless intrusion.\nBonus: +10% HackSpeed, +5% Stealth, Bonus XP for fast missions."
        },
        {
            PlayerClass.Broker,
            "Broker:\nProfits from clean data resale.\nBonus: +5% Stealth, +10% DataYield, Starts with 250 credits."
        },
        {
            PlayerClass.Ghost,
            "Ghost:\nStealthy, avoids trace buildup.\nBonus: +5% HackSpeed, +15% Stealth, 10% reduced trace chance."
        },
    };

    public ClassSelectionDialog()
    {
        Title = "Choose Starting Class";
        ColorScheme = Colors.Dialog;

        var classNames = Enum.GetNames(typeof(PlayerClass))
            .Where(name => name != PlayerClass.Undefined.ToString())
            .ToArray();
        var ustringClassNames = classNames.Select(s => ustring.Make(s)).ToArray();

        _radioGroup = new RadioGroup(ustringClassNames)
        {
            X = 1,
            Y = 1,
            SelectedItem = 0,
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

        // Use the Orientation from Terminal.Gui.Graphs namespace
        var separator = new LineView(Orientation.Vertical) // Use correct enum
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
        { /* ... OK logic ... */
            if (Enum.TryParse<PlayerClass>(classNames[_radioGroup.SelectedItem], out var selected))
            {
                SelectedClass = selected;
                Canceled = false;
                Application.RequestStop();
            }
            else
            {
                MessageBox.ErrorQuery("Error", "Invalid class selection.", "OK");
            }
        };

        var cancelButton = new Button("Cancel") { X = Pos.Right(okButton) + 1, Y = okButton.Y };
        cancelButton.Clicked += () =>
        { /* ... Cancel logic ... */
            SelectedClass = PlayerClass.Undefined;
            Canceled = true;
            Application.RequestStop();
        };

        Add(_radioGroup, separator, _descriptionView, okButton, cancelButton);
        UpdateDescription(_radioGroup.SelectedItem);
        _radioGroup.SetFocus();
    }

    private void UpdateDescription(int selectedIndex)
    {
        // ... (UpdateDescription remains the same) ...
        var classNames = Enum.GetNames(typeof(PlayerClass))
            .Where(name => name != PlayerClass.Undefined.ToString())
            .ToArray();
        if (
            selectedIndex >= 0
            && selectedIndex < classNames.Length
            && Enum.TryParse<PlayerClass>(classNames[selectedIndex], out var selectedClass)
        )
        {
            if (_classDescriptions.TryGetValue(selectedClass, out var description))
            {
                _descriptionView.Text = description;
            }
            else
            {
                _descriptionView.Text = "No description available.";
            }
        }
        else
        {
            _descriptionView.Text = "";
        }
        _descriptionView.SetNeedsDisplay();
    }
}
