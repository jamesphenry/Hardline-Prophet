// src/HardlineProphet/UI/Views/UI.cs
using HardlineProphet.Core.Models; // GameState, Mission
using HardlineProphet.Services; // TickService
using HardlineProphet.UI.Dialogs; // LogonDialog, ClassSelectionDialog, PerkSelectionDialog, ShopDialog
using Terminal.Gui;
using System; // MessageBox, Console, Exception, Func, Action
using System.Collections.Generic; // IReadOnlyDictionary
using System.IO; // InvalidDataException, IOException
using System.Reflection; // Assembly version retrieval
using System.Threading.Tasks;
using HardlineProphet.Core.Extensions; // Task

namespace HardlineProphet.UI.Views; // Ensure namespace matches folder structure

public static class UI
{
    // Keep reference to the main content window
    private static Window? _mainWindowContent;

    /// <summary>
    /// Creates the main application window, including menu bar and content area.
    /// </summary>
    /// <param name="scheme">The color scheme to use.</param>
    /// <returns>The main content window.</returns>
    public static Window CreateMainWindow(ColorScheme scheme)
    {
        string appVersion = GetApplicationVersion();
        _mainWindowContent = new Window($"Hardline Prophet - {appVersion}")
        { X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill(1), ColorScheme = scheme };

        // Create the menu bar
        var menu = new MenuBar(new MenuBarItem[]
        {
                new MenuBarItem("_System", new MenuItem[]
                {
                    new MenuItem("_Logon", "Log on to the system", async () => { /* ... Logon logic ... */ ApplicationState.TickServiceInstance?.Stop(); ApplicationState.TickServiceInstance = null; ApplicationState.CurrentGameState = null; ApplicationState.InGameViewInstance = null; AddLoggedOffStatus(); var logonDialog = new LogonDialog(); Application.Run(logonDialog); if (!logonDialog.Canceled && _mainWindowContent != null) { var username = logonDialog.GetUsername(); try { GameState loadedState = await ApplicationState.GameStateRepository!.LoadStateAsync(username); GameState finalState; if (loadedState.SelectedClass == null || loadedState.SelectedClass == PlayerClass.Undefined) { Application.MainLoop.Invoke(() => AddSetupStatus("New profile detected. Performing setup...")); var classDialog = new ClassSelectionDialog(); Application.Run(classDialog); if (classDialog.Canceled) { AddLoggedOffStatus(); return; } var chosenClass = classDialog.SelectedClass; var perkDialog = new PerkSelectionDialog(); Application.Run(perkDialog); if (perkDialog.Canceled) { AddLoggedOffStatus(); return; } var chosenPerkId = perkDialog.SelectedPerkId; PlayerStats startingStats = GameStateExtensions.GetDefaultStatsForClass(chosenClass); int startingCredits = GameStateExtensions.GetStartingCreditsForClass(chosenClass, chosenPerkId); finalState = loadedState with { SelectedClass = chosenClass, SelectedStartingPerkIds = new List<string> { chosenPerkId }, Stats = startingStats, Credits = startingCredits }; Application.MainLoop.Invoke(() => AddSetupStatus($"Class '{chosenClass}', Perk '{chosenPerkId}' selected. Setup complete.")); await Task.Delay(500); } else { finalState = loadedState; } ApplicationState.CurrentGameState = finalState; _mainWindowContent.RemoveAll(); ApplicationState.InGameViewInstance = new InGameView { Width = Dim.Fill(), Height = Dim.Fill() }; _mainWindowContent.Add(ApplicationState.InGameViewInstance); ApplicationState.InGameViewInstance.UpdateState(ApplicationState.CurrentGameState); Func<GameState?> getGameState = () => ApplicationState.CurrentGameState; Action<GameState> updateGameState = (newState) => { ApplicationState.CurrentGameState = newState; Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.UpdateState(newState)); }; Action<string> tickLog = (msg) => { Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.AddLogMessage(msg)); }; var missions = ApplicationState.LoadedMissions ?? new Dictionary<string, Mission>(); ApplicationState.TickServiceInstance = new TickService( getGameState, updateGameState, tickLog, missions, Application.MainLoop ); ApplicationState.TickServiceInstance.Start(); Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.AddLogMessage($"Logon successful for {username}.")); } catch (Exception ex) when (ex is InvalidDataException || ex is IOException) { MessageBox.ErrorQuery("Logon Failed", $"Failed to load save data: {ex.Message}", "OK"); ApplicationState.CurrentGameState = null; ApplicationState.InGameViewInstance = null; AddLoggedOffStatus(); } catch (Exception ex) { MessageBox.ErrorQuery("Logon Failed", $"An unexpected error occurred: {ex.Message}\n{ex.StackTrace}", "OK"); ApplicationState.CurrentGameState = null; ApplicationState.InGameViewInstance = null; AddLoggedOffStatus(); } } }),
                    new MenuItem("_Shutdown", "Save and exit", async () => { /* ... Shutdown logic ... */ var n = MessageBox.Query("Confirm", "Save and Shutdown?", "Yes", "No"); if (n == 0) { ApplicationState.TickServiceInstance?.Stop(); ApplicationState.TickServiceInstance = null; if (ApplicationState.CurrentGameState != null) { try { await ApplicationState.GameStateRepository!.SaveStateAsync(ApplicationState.CurrentGameState); } catch (Exception ex) { MessageBox.ErrorQuery("Save Failed", $"Could not save game state: {ex.Message}", "OK"); var quitAnyway = MessageBox.Query("Save Failed", "Could not save game state. Quit anyway?", "Yes", "No"); if (quitAnyway == 1) return; } } Application.RequestStop(); } }),
                }),
                // --- New Actions Menu ---
                new MenuBarItem("_Actions", new MenuItem[] // New top-level menu
                {
                    new MenuItem("_Shop", "Purchase cyberdeck upgrades", () => // Shop action
                    {
                        // Ensure player is logged in
                        if (ApplicationState.CurrentGameState == null || ApplicationState.InGameViewInstance == null)
                        {
                            MessageBox.ErrorQuery("Error", "You must be logged on to access the shop.", "OK");
                            return;
                        }

                        // Create and show the shop dialog
                        var shopDialog = new ShopDialog();
                        Application.Run(shopDialog);

                        // After the dialog closes, check if a purchase was made
                        if (shopDialog.PurchaseMade)
                        {
                             // If yes, refresh the main game view to show updated stats/credits
                             ApplicationState.InGameViewInstance.UpdateState(ApplicationState.CurrentGameState);
                             // Optional: Log purchase in main log?
                             // ApplicationState.InGameViewInstance.AddLogMessage("Accessed shop.");
                        }
                    }),
                    // Add other actions later (e.g., View Missions, View Stats/Perks)
                })
            // ----------------------
        });

        Application.Top.Add(menu);
        Application.Top.Add(_mainWindowContent);
        AddLoggedOffStatus();
        return _mainWindowContent;
    }

    private static void AddLoggedOffStatus()
    {
        // ... (AddLoggedOffStatus remains the same) ...
        if (_mainWindowContent != null) { _mainWindowContent.RemoveAll(); _mainWindowContent.Add(new Label("Status: Logged Off. Use System -> Logon.") { X = 1, Y = 1 }); _mainWindowContent.SetNeedsDisplay(); }
    }

    private static void AddSetupStatus(string message)
    {
        // ... (AddSetupStatus remains the same) ...
        if (_mainWindowContent != null) { _mainWindowContent.RemoveAll(); _mainWindowContent.Add(new Label(message) { X = 1, Y = 1 }); _mainWindowContent.SetNeedsDisplay(); Application.Refresh(); }
    }

    private static string GetApplicationVersion()
    {
        // ... (GetApplicationVersion remains the same) ...
        try { string infoVersion = GitVersionInformation.InformationalVersion; if (string.IsNullOrEmpty(infoVersion) || infoVersion == "0.0.0" || infoVersion.StartsWith("0.1.0+0")) { Console.WriteLine("[DEBUG] GitVersionInformation.InformationalVersion was null, empty, or default. Using fallback."); return "vDev"; } var versionParts = infoVersion.Split('+'); var versionWithoutMetadata = versionParts[0]; Console.WriteLine($"[DEBUG] Using GitVersionInformation.InformationalVersion: {infoVersion}"); return $"v{versionWithoutMetadata}"; } catch (Exception ex) { Console.Error.WriteLine($"Error getting application version via GitVersionInformation: {ex.Message}"); return "v?.?.?"; }
    }
}
