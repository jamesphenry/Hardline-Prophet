// src/HardlineProphet/UI/Views/UI.cs
using HardlineProphet.Core.Models; // GameState, Mission (needed for dictionary type)
using HardlineProphet.Services; // TickService
using HardlineProphet.UI.Dialogs; // LogonDialog
using Terminal.Gui;
using System; // MessageBox, Console, Exception, Func, Action
using System.Collections.Generic; // Added for IReadOnlyDictionary
using System.IO; // InvalidDataException, IOException
using System.Threading.Tasks; // Task

namespace HardlineProphet.UI.Views; // Ensure namespace matches folder structure

public static class UI
{
    // Keep reference to the main content window to add/remove views
    private static Window? _mainWindowContent;

    /// <summary>
    /// Creates the main application window, including menu bar and content area.
    /// </summary>
    /// <param name="scheme">The color scheme to use.</param>
    /// <returns>The main content window.</returns>
    public static Window CreateMainWindow(ColorScheme scheme)
    {
        // Create the content window where different views (Logged Off, InGame) will be placed.
        _mainWindowContent = new Window("Hardline Prophet")
        {
            X = 0,
            Y = 1, // Position below the menu bar
            Width = Dim.Fill(),
            Height = Dim.Fill(1), // Fill remaining space below menu
            ColorScheme = scheme
        };

        // Create the menu bar
        var menu = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_System", new MenuItem[]
            {
                new MenuItem("_Logon", "Log on to the system", async () => // Logon action
                {
                    // Stop services, clear state before attempting logon
                    ApplicationState.TickServiceInstance?.Stop();
                    ApplicationState.TickServiceInstance = null;
                    ApplicationState.CurrentGameState = null;
                    ApplicationState.InGameViewInstance = null; // Clear view instance too
                    AddLoggedOffStatus(); // Show logged off status initially

                    // Show Logon Dialog
                    var logonDialog = new LogonDialog();
                    Application.Run(logonDialog); // Run modally

                    // Process if Logon was not canceled
                    if (!logonDialog.Canceled && _mainWindowContent != null)
                    {
                        var username = logonDialog.GetUsername();
                        try
                        {
                            // --- Load Game State ---
                            GameState loadedState = await ApplicationState.GameStateRepository.LoadStateAsync(username);
                            ApplicationState.CurrentGameState = loadedState; // Store loaded state globally

                            // --- Create and Show InGameView ---
                            _mainWindowContent.RemoveAll(); // Clear "Logged Off" status
                            ApplicationState.InGameViewInstance = new InGameView { Width = Dim.Fill(), Height = Dim.Fill() };
                            _mainWindowContent.Add(ApplicationState.InGameViewInstance);
                            // Perform initial UI update with loaded state
                            ApplicationState.InGameViewInstance.UpdateState(ApplicationState.CurrentGameState);
                            // ----------------------------------


                            // --- Initialize and Start TickService ---
                            // Create delegates pointing to our static state and UI instance
                            Func<GameState?> getGameState = () => ApplicationState.CurrentGameState;
                            Action<GameState> updateGameState = (newState) => {
                                ApplicationState.CurrentGameState = newState;
                                // Update UI view when state changes (Invoke ensures UI thread safety)
                                Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.UpdateState(newState));
                            };
                            Action<string> tickLog = (msg) => {
                                // Log to UI view (Invoke ensures UI thread safety)
                                Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.AddLogMessage(msg));
                            };

                            // Ensure mission definitions are loaded (handle potential null - though Program.cs should init to empty dict)
                            var missions = ApplicationState.LoadedMissions ?? new Dictionary<string, Mission>();

                            // Instantiate, store, and start the service - CORRECTED ARGUMENTS
                            ApplicationState.TickServiceInstance = new TickService(
                                getGameState,           // Arg 1
                                updateGameState,        // Arg 2
                                tickLog,                // Arg 3
                                missions,               // Arg 4 - Pass loaded missions
                                Application.MainLoop    // Arg 5 - Pass the main loop
                            );
                            ApplicationState.TickServiceInstance.Start();
                            // -----------------------------------------

                            Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.AddLogMessage($"Logon successful for {username}."));

                        }
                        // Handle specific load/IO errors
                        catch (Exception ex) when (ex is InvalidDataException || ex is IOException)
                        {
                            MessageBox.ErrorQuery("Logon Failed", $"Failed to load save data: {ex.Message}", "OK");
                            ApplicationState.CurrentGameState = null;
                            ApplicationState.InGameViewInstance = null;
                            AddLoggedOffStatus(); // Show logged off status again
                        }
                        // Handle any other unexpected errors during logon/setup
                        catch (Exception ex)
                        {
                            MessageBox.ErrorQuery("Logon Failed", $"An unexpected error occurred: {ex.Message}\n{ex.StackTrace}", "OK");
                            ApplicationState.CurrentGameState = null;
                            ApplicationState.InGameViewInstance = null;
                            AddLoggedOffStatus(); // Show logged off status again
                        }
                    }
                }),
                new MenuItem("_Shutdown", "Save and exit", async () => // Shutdown action
                {
                    // Confirm shutdown
                    var n = MessageBox.Query("Confirm", "Save and Shutdown?", "Yes", "No");
                    if (n == 0) // User selected Yes
                    {
                         // Stop the tick service first to prevent ticks during save/shutdown
                         ApplicationState.TickServiceInstance?.Stop();
                         ApplicationState.TickServiceInstance = null; // Clear instance

                        // Save the current game state if it exists
                        if (ApplicationState.CurrentGameState != null)
                        {
                            try
                            {
                                await ApplicationState.GameStateRepository.SaveStateAsync(ApplicationState.CurrentGameState);
                            }
                            catch (Exception ex)
                            {
                                // Notify user if save fails, but allow quitting anyway
                                 MessageBox.ErrorQuery("Save Failed", $"Could not save game state: {ex.Message}", "OK");
                                 var quitAnyway = MessageBox.Query("Save Failed", "Could not save game state. Quit anyway?", "Yes", "No");
                                 if (quitAnyway == 1) return; // Don't quit if they select No
                             }
                        }
                        // Request the application to stop cleanly
                        Application.RequestStop();
                    }
                }),
                // Add other top-level menus later (e.g., _Game, _Help)
            })
        });

        // Add the menu to the top-level view (it manages its own position)
        Application.Top.Add(menu);
        // Add the main content window below the menu
        Application.Top.Add(_mainWindowContent);

        // Set initial state in the content window
        AddLoggedOffStatus();

        // Return the content window (though Application.Top manages overall layout)
        return _mainWindowContent;
    }

    /// <summary>
    /// Helper method to clear the main content window and show the logged off status.
    /// </summary>
    private static void AddLoggedOffStatus()
    {
        if (_mainWindowContent != null)
        {
            _mainWindowContent.RemoveAll(); // Clear any existing views
                                            // Add a simple label indicating the logged off state
            _mainWindowContent.Add(new Label("Status: Logged Off. Use System -> Logon.") { X = 1, Y = 1 });
            _mainWindowContent.SetNeedsDisplay(); // Ensure redraw
        }
    }
}
