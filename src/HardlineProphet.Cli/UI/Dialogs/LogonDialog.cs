// src/HardlineProphet/UI/Dialogs/LogonDialog.cs
using Terminal.Gui;
using System; // String

namespace HardlineProphet.UI.Dialogs;

public class LogonDialog : Dialog
{
    public TextField UsernameText;
    public bool Canceled = true; // Default to canceled

    public LogonDialog()
    {
        Title = "Logon";
        ColorScheme = Colors.Dialog; // Use default dialog scheme or inject custom

        var usernameLabel = new Label("Username:")
        {
            X = 1,
            Y = 1
        };

        UsernameText = new TextField("")
        {
            X = Pos.Right(usernameLabel) + 1,
            Y = 1,
            Width = Dim.Fill(2) // Fill width minus padding
        };

        // Buttons
        var okButton = new Button("Logon", is_default: true)
        {
            X = Pos.Center() - 10, // Position buttons
            Y = Pos.Bottom(UsernameText) + 1
        };
        okButton.Clicked += () =>
        {
            // Basic validation - ensure username is not empty
            if (!string.IsNullOrWhiteSpace(UsernameText.Text?.ToString()))
            {
                Canceled = false;
                Application.RequestStop(); // Stop the dialog loop
            }
            else
            {
                MessageBox.ErrorQuery("Error", "Username cannot be empty.", "OK");
                UsernameText.SetFocus();
            }
        };

        var cancelButton = new Button("Cancel")
        {
            X = Pos.Right(okButton) + 1,
            Y = okButton.Y
        };
        cancelButton.Clicked += () =>
        {
            Canceled = true;
            Application.RequestStop(); // Stop the dialog loop
        };

        Add(usernameLabel, UsernameText, okButton, cancelButton);

        // Set focus
        UsernameText.SetFocus();
    }

    // Helper to get the entered username
    public string GetUsername() => UsernameText.Text?.ToString() ?? string.Empty;
}
