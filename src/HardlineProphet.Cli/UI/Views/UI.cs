// src/HardlineProphet/UI/Views/UI.cs
using HardlineProphet.UI.Dialogs; // LogonDialog
using Terminal.Gui;
using System; // MessageBox
using System.Threading.Tasks; // Task

namespace HardlineProphet.UI.Views; // Ensure namespace matches folder

public static class UI
{
    public static Window CreateMainWindow(ColorScheme scheme)
    {
        // Create the menu bar
        var menu = new MenuBar(new MenuBarItem[]
        {
                new MenuBarItem("_System", new MenuItem[] // Renamed from Actions for clarity
                {
                    new MenuItem("_Logon", "Log on to the system", async () => // Made async
                    {
                        var logonDialog = new LogonDialog();
                        Application.Run(logonDialog); // Show the dialog modally

                        if (!logonDialog.Canceled)
                        {
                            var username = logonDialog.GetUsername();
                            try
                            {
                                // Use the static repository instance
                                ApplicationState.CurrentGameState = await ApplicationState.GameStateRepository.LoadStateAsync(username);
                                // TODO: Clear main window and show "In-Game" view controls
                                MessageBox.Query("Logon Success", $"Welcome, {ApplicationState.CurrentGameState.Username}!", "OK");
                            }
                            catch (InvalidDataException dataEx)
                            {
                                MessageBox.ErrorQuery("Logon Failed", $"Failed to load save data: {dataEx.Message}", "OK");
                                ApplicationState.CurrentGameState = null; // Ensure state is null on failure
                            }
                            catch (IOException ioEx)
                            {
                                MessageBox.ErrorQuery("Logon Failed", $"Failed to read save file: {ioEx.Message}", "OK");
                                ApplicationState.CurrentGameState = null;
                            }
                            catch (Exception ex) // Catch any other unexpected errors
                            {
                                MessageBox.ErrorQuery("Logon Failed", $"An unexpected error occurred: {ex.Message}", "OK");
                                ApplicationState.CurrentGameState = null;
                            }
                        }
                    }),
                    // Logoff might be needed later, but Shutdown handles saving for now
                    // new MenuItem("_Logoff", "Log off the system", () => { /* TODO */ }),
                    new MenuItem("_Shutdown", "Save and exit", async () => // Made async
                    {
                        // Confirm shutdown
                        var n = MessageBox.Query("Confirm", "Save and Shutdown?", "Yes", "No");
                        if (n == 0)
                        {
                            if (ApplicationState.CurrentGameState != null)
                            {
                                try
                                {
                                    // Use the static repository instance
                                    await ApplicationState.GameStateRepository.SaveStateAsync(ApplicationState.CurrentGameState);
                                    // Optional: Add success message?
                                }
                                catch (Exception ex)
                                {
                                    // Log error or show message box if save fails?
                                    MessageBox.ErrorQuery("Save Failed", $"Could not save game state: {ex.Message}", "OK");
                                    // Ask if user still wants to quit?
                                    var quitAnyway = MessageBox.Query("Save Failed", "Could not save game state. Quit anyway?", "Yes", "No");
                                    if (quitAnyway == 1) return; // Don't quit if they select No
                                }
                            }
                            Application.RequestStop(); // Use RequestStop for graceful exit
                        }
                    }),
                })
            // Add other menus later (e.g., _View, _Help)
        });

        // Main window content area
        var win = new Window("Hardline Prophet")
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1, // Leave space for menu
            ColorScheme = scheme
        };

        // Placeholder content - will be replaced by In-Game view after logon
        win.Add(
            new Label("Status: Logged Off") { X = 1, Y = 1 }
        );

        // Add the menu to the top-level view
        Application.Top.Add(menu);

        return win;
    }
}
