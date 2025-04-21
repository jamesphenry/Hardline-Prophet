// src/HardlineProphet/UI/Views/UI.cs
using HardlineProphet.Core; // GameConstants
using HardlineProphet.Core.Extensions; // GetDefaultStatsForClass, etc.
using HardlineProphet.Core.Models; // GameState, PlayerClass, Mission
using HardlineProphet.Services; // TickService
using HardlineProphet.UI.Dialogs; // LogonDialog, ClassSelectionDialog, PerkSelectionDialog
using Terminal.Gui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace HardlineProphet.UI.Views;

public static class UI
{
    private static Window? _mainWindowContent;

    public static Window CreateMainWindow(ColorScheme scheme)
    {
        string appVersion = GetApplicationVersion();
        _mainWindowContent = new Window($"Hardline Prophet - {appVersion}")
        { X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill(1), ColorScheme = scheme };

        var menu = new MenuBar(new MenuBarItem[]
        {
                new MenuBarItem("_System", new MenuItem[]
                {
                    new MenuItem("_Logon", "Log on to the system", async () =>
                    {
                        // Stop services, clear state
                        ApplicationState.TickServiceInstance?.Stop(); ApplicationState.TickServiceInstance = null; ApplicationState.CurrentGameState = null; ApplicationState.InGameViewInstance = null; AddLoggedOffStatus();

                        var logonDialog = new LogonDialog();
                        Application.Run(logonDialog);

                        if (!logonDialog.Canceled && _mainWindowContent != null)
                        {
                            var username = logonDialog.GetUsername();
                            try
                            {
                                // --- Load or Get Default State ---
                                GameState loadedState = await ApplicationState.GameStateRepository!.LoadStateAsync(username);
                                GameState finalState; // State after potential setup

                                // --- New Player Setup Flow ---
                                // Check if loaded state indicates a new player (e.g., class not set yet, or could add an IsNew flag from Load)
                                if (loadedState.SelectedClass == null || loadedState.SelectedClass == PlayerClass.Undefined)
                                {
                                    Application.MainLoop.Invoke(() => AddSetupStatus("New profile detected. Performing setup..."));

                                    // 1. Select Class
                                    var classDialog = new ClassSelectionDialog();
                                    Application.Run(classDialog);
                                    if (classDialog.Canceled) { AddLoggedOffStatus(); return; } // Abort if canceled
                                    var chosenClass = classDialog.SelectedClass;

                                    // 2. Select Perk
                                    var perkDialog = new PerkSelectionDialog();
                                    Application.Run(perkDialog);
                                    if (perkDialog.Canceled) { AddLoggedOffStatus(); return; } // Abort if canceled
                                    var chosenPerkId = perkDialog.SelectedPerkId;

                                    // 3. Apply Defaults/Choices using helper extensions
                                    PlayerStats startingStats = GameStateExtensions.GetDefaultStatsForClass(chosenClass);
                                    int startingCredits = GameStateExtensions.GetStartingCreditsForClass(chosenClass, chosenPerkId);

                                    // 4. Create final state using 'with' on the loaded (default) state
                                    finalState = loadedState with {
                                        SelectedClass = chosenClass,
                                        SelectedStartingPerkIds = new List<string> { chosenPerkId }, // Store selected perk ID
                                        Stats = startingStats,
                                        Credits = startingCredits
                                        // Ensure Version/Checksum are handled correctly by SaveStateAsync later
                                    };
                                     Application.MainLoop.Invoke(() => AddSetupStatus($"Class '{chosenClass}', Perk '{chosenPerkId}' selected. Setup complete."));
                                     await Task.Delay(500); // Brief pause to see message
                                }
                                else
                                {
                                    // Existing player, use the loaded state directly
                                    finalState = loadedState;
                                }
                                // -------------------------

                                // Store the final state (either newly setup or loaded existing)
                                ApplicationState.CurrentGameState = finalState;

                                // --- Create and Show InGameView ---
                                _mainWindowContent.RemoveAll();
                                ApplicationState.InGameViewInstance = new InGameView { Width = Dim.Fill(), Height = Dim.Fill() };
                                _mainWindowContent.Add(ApplicationState.InGameViewInstance);
                                ApplicationState.InGameViewInstance.UpdateState(ApplicationState.CurrentGameState);
                                // ----------------------------------

                                // --- Initialize and Start TickService ---
                                Func<GameState?> getGameState = () => ApplicationState.CurrentGameState;
                                Action<GameState> updateGameState = (newState) => { ApplicationState.CurrentGameState = newState; Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.UpdateState(newState)); };
                                Action<string> tickLog = (msg) => { Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.AddLogMessage(msg)); };
                                var missions = ApplicationState.LoadedMissions ?? new Dictionary<string, Mission>();
                                ApplicationState.TickServiceInstance = new TickService( getGameState, updateGameState, tickLog, missions, Application.MainLoop );
                                ApplicationState.TickServiceInstance.Start();
                                // -----------------------------------------

                                Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.AddLogMessage($"Logon successful for {username}."));

                            }
                            catch (Exception ex) when (ex is InvalidDataException || ex is IOException) { /* ... error handling ... */ MessageBox.ErrorQuery("Logon Failed", $"Failed to load save data: {ex.Message}", "OK"); ApplicationState.CurrentGameState = null; ApplicationState.InGameViewInstance = null; AddLoggedOffStatus(); }
                            catch (Exception ex) { /* ... error handling ... */ MessageBox.ErrorQuery("Logon Failed", $"An unexpected error occurred: {ex.Message}\n{ex.StackTrace}", "OK"); ApplicationState.CurrentGameState = null; ApplicationState.InGameViewInstance = null; AddLoggedOffStatus(); }
                        }
                    }),
                    new MenuItem("_Shutdown", "Save and exit", async () => { /* ... Shutdown logic ... */ var n = MessageBox.Query("Confirm", "Save and Shutdown?", "Yes", "No"); if (n == 0) { ApplicationState.TickServiceInstance?.Stop(); ApplicationState.TickServiceInstance = null; if (ApplicationState.CurrentGameState != null) { try { await ApplicationState.GameStateRepository!.SaveStateAsync(ApplicationState.CurrentGameState); } catch (Exception ex) { MessageBox.ErrorQuery("Save Failed", $"Could not save game state: {ex.Message}", "OK"); var quitAnyway = MessageBox.Query("Save Failed", "Could not save game state. Quit anyway?", "Yes", "No"); if (quitAnyway == 1) return; } } Application.RequestStop(); } }),
                })
        });

        Application.Top.Add(menu);
        Application.Top.Add(_mainWindowContent);
        AddLoggedOffStatus();
        return _mainWindowContent;
    }

    private static void AddLoggedOffStatus()
    {
        if (_mainWindowContent != null) { _mainWindowContent.RemoveAll(); _mainWindowContent.Add(new Label("Status: Logged Off. Use System -> Logon.") { X = 1, Y = 1 }); _mainWindowContent.SetNeedsDisplay(); }
    }

    // Helper to show temporary status during setup
    private static void AddSetupStatus(string message)
    {
        if (_mainWindowContent != null)
        {
            _mainWindowContent.RemoveAll();
            _mainWindowContent.Add(new Label(message) { X = 1, Y = 1 });
            _mainWindowContent.SetNeedsDisplay();
            Application.Refresh(); // Force refresh to show message immediately
        }
    }

    private static string GetApplicationVersion()
    {
        // ... (GetApplicationVersion remains the same) ...
        try { string infoVersion = GitVersionInformation.InformationalVersion; if (string.IsNullOrEmpty(infoVersion) || infoVersion == "0.0.0" || infoVersion.StartsWith("0.1.0+0")) { Console.WriteLine("[DEBUG] GitVersionInformation.InformationalVersion was null, empty, or default. Using fallback."); return "vDev"; } var versionParts = infoVersion.Split('+'); var versionWithoutMetadata = versionParts[0]; Console.WriteLine($"[DEBUG] Using GitVersionInformation.InformationalVersion: {infoVersion}"); return $"v{versionWithoutMetadata}"; } catch (Exception ex) { Console.Error.WriteLine($"Error getting application version via GitVersionInformation: {ex.Message}"); return "v?.?.?"; }
    }
}
