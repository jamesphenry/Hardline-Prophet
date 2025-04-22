// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-feature-m2-flavor-events.1+8
// ║ 📄  File: UI.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet

using HardlineProphet.Core.Extensions; // Task
using HardlineProphet.Core.Models; // GameState, Mission
using HardlineProphet.Services; // TickService
using HardlineProphet.UI.Dialogs; // LogonDialog, ClassSelectionDialog, PerkSelectionDialog, ShopDialog
using Terminal.Gui;

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
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            ColorScheme = scheme,
        };

        var menu = new MenuBar(
            new MenuBarItem[]
            {
                new MenuBarItem(
                    "_System",
                    new MenuItem[]
                    {
                        new MenuItem(
                            "_Logon",
                            "Log on to the system",
                            async () =>
                            {
                                // Stop services, clear state
                                ApplicationState.TickServiceInstance?.Stop();
                                ApplicationState.TickServiceInstance = null;
                                ApplicationState.CurrentGameState = null;
                                ApplicationState.InGameViewInstance = null;
                                AddLoggedOffStatus();

                                var logonDialog = new LogonDialog();
                                Application.Run(logonDialog);

                                if (!logonDialog.Canceled && _mainWindowContent != null)
                                {
                                    var username = logonDialog.GetUsername();
                                    try
                                    {
                                        GameState loadedState =
                                            await ApplicationState.GameStateRepository!.LoadStateAsync(
                                                username
                                            );
                                        GameState finalState;

                                        if (
                                            loadedState.SelectedClass == null
                                            || loadedState.SelectedClass == PlayerClass.Undefined
                                        )
                                        {
                                            Application.MainLoop.Invoke(
                                                () =>
                                                    AddSetupStatus(
                                                        "New profile detected. Performing setup..."
                                                    )
                                            );
                                            var classDialog = new ClassSelectionDialog();
                                            Application.Run(classDialog);
                                            if (classDialog.Canceled)
                                            {
                                                AddLoggedOffStatus();
                                                return;
                                            }
                                            var chosenClass = classDialog.SelectedClass;
                                            var perkDialog = new PerkSelectionDialog();
                                            Application.Run(perkDialog);
                                            if (perkDialog.Canceled)
                                            {
                                                AddLoggedOffStatus();
                                                return;
                                            }
                                            var chosenPerkId = perkDialog.SelectedPerkId;
                                            PlayerStats startingStats =
                                                GameStateExtensions.GetDefaultStatsForClass(
                                                    chosenClass
                                                );
                                            int startingCredits =
                                                GameStateExtensions.GetStartingCreditsForClass(
                                                    chosenClass,
                                                    chosenPerkId
                                                );
                                            finalState = loadedState with
                                            {
                                                SelectedClass = chosenClass,
                                                SelectedStartingPerkIds = new List<string>
                                                {
                                                    chosenPerkId,
                                                },
                                                Stats = startingStats,
                                                Credits = startingCredits,
                                            };
                                            Application.MainLoop.Invoke(
                                                () =>
                                                    AddSetupStatus(
                                                        $"Class '{chosenClass}', Perk '{chosenPerkId}' selected. Setup complete."
                                                    )
                                            );
                                            await Task.Delay(500);
                                        }
                                        else
                                        {
                                            finalState = loadedState;
                                        }

                                        ApplicationState.CurrentGameState = finalState;

                                        // --- Create and Show InGameView ---
                                        _mainWindowContent.RemoveAll();
                                        ApplicationState.InGameViewInstance = new InGameView
                                        {
                                            Width = Dim.Fill(),
                                            Height = Dim.Fill(),
                                        };
                                        _mainWindowContent.Add(ApplicationState.InGameViewInstance);
                                        ApplicationState.InGameViewInstance.UpdateState(
                                            ApplicationState.CurrentGameState
                                        );

                                        // --- Initialize and Start TickService ---
                                        Func<GameState?> getGameState = () =>
                                            ApplicationState.CurrentGameState;
                                        Action<GameState> updateGameState = (newState) =>
                                        {
                                            ApplicationState.CurrentGameState = newState;
                                            Application.MainLoop.Invoke(
                                                () =>
                                                    ApplicationState.InGameViewInstance?.UpdateState(
                                                        newState
                                                    )
                                            );
                                        };
                                        Action<string> tickLog = (msg) =>
                                        {
                                            Application.MainLoop.Invoke(
                                                () =>
                                                    ApplicationState.InGameViewInstance?.AddLogMessage(
                                                        msg
                                                    )
                                            );
                                        };
                                        var missions =
                                            ApplicationState.LoadedMissions
                                            ?? new Dictionary<string, Mission>();
                                        // Ensure flavor events dictionary exists, even if empty
                                        var flavorEvents =
                                            ApplicationState.LoadedFlavorEvents
                                            ?? new Dictionary<
                                                FlavorEventTrigger,
                                                List<FlavorEvent>
                                            >();

                                        // Instantiate, store, and start the service - CORRECTED ARGUMENTS ORDER
                                        ApplicationState.TickServiceInstance = new TickService(
                                            getGameState, // Arg 1
                                            updateGameState, // Arg 2
                                            tickLog, // Arg 3
                                            missions, // Arg 4
                                            flavorEvents, // Arg 5 <<< Pass loaded flavor events
                                            Application.MainLoop // Arg 6 <<< Pass main loop
                                        // rng is optional, will use default
                                        );
                                        ApplicationState.TickServiceInstance.Start();

                                        Application.MainLoop.Invoke(
                                            () =>
                                                ApplicationState.InGameViewInstance?.AddLogMessage(
                                                    $"Logon successful for {username}."
                                                )
                                        );
                                    }
                                    catch (Exception ex)
                                        when (ex is InvalidDataException || ex is IOException)
                                    { /* ... error handling ... */
                                        MessageBox.ErrorQuery(
                                            "Logon Failed",
                                            $"Failed to load save data: {ex.Message}",
                                            "OK"
                                        );
                                        ApplicationState.CurrentGameState = null;
                                        ApplicationState.InGameViewInstance = null;
                                        AddLoggedOffStatus();
                                    }
                                    catch (Exception ex)
                                    { /* ... error handling ... */
                                        MessageBox.ErrorQuery(
                                            "Logon Failed",
                                            $"An unexpected error occurred: {ex.Message}\n{ex.StackTrace}",
                                            "OK"
                                        );
                                        ApplicationState.CurrentGameState = null;
                                        ApplicationState.InGameViewInstance = null;
                                        AddLoggedOffStatus();
                                    }
                                }
                            }
                        ),
                        new MenuItem(
                            "_Shutdown",
                            "Save and exit",
                            async () =>
                            { /* ... Shutdown logic ... */
                                var n = MessageBox.Query("Confirm", "Save and Shutdown?", "Yes", "No");
                                if (n == 0)
                                {
                                    ApplicationState.TickServiceInstance?.Stop();
                                    ApplicationState.TickServiceInstance = null;
                                    if (ApplicationState.CurrentGameState != null)
                                    {
                                        try
                                        {
                                            await ApplicationState.GameStateRepository!.SaveStateAsync(
                                                ApplicationState.CurrentGameState
                                            );
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBox.ErrorQuery(
                                                "Save Failed",
                                                $"Could not save game state: {ex.Message}",
                                                "OK"
                                            );
                                            var quitAnyway = MessageBox.Query(
                                                "Save Failed",
                                                "Could not save game state. Quit anyway?",
                                                "Yes",
                                                "No"
                                            );
                                            if (quitAnyway == 1)
                                                return;
                                        }
                                    }
                                    Application.RequestStop();
                                }
                            }
                        ),
                    }
                ),
                new MenuBarItem(
                    "_Actions",
                    new MenuItem[]
                    {
                        new MenuItem(
                            "_Shop",
                            "Purchase cyberdeck upgrades",
                            () =>
                            { /* ... Shop logic ... */
                                if (
                                    ApplicationState.CurrentGameState == null
                                    || ApplicationState.InGameViewInstance == null
                                )
                                {
                                    MessageBox.ErrorQuery(
                                        "Error",
                                        "You must be logged on to access the shop.",
                                        "OK"
                                    );
                                    return;
                                }
                                var shopDialog = new ShopDialog();
                                Application.Run(shopDialog);
                                if (shopDialog.PurchaseMade)
                                {
                                    ApplicationState.InGameViewInstance.UpdateState(
                                        ApplicationState.CurrentGameState
                                    );
                                }
                            }
                        ),
                    }
                ),
            }
        );

        Application.Top.Add(menu);
        Application.Top.Add(_mainWindowContent);
        AddLoggedOffStatus();
        return _mainWindowContent;
    }

    private static void AddLoggedOffStatus()
    {
        // ... (AddLoggedOffStatus remains the same) ...
        if (_mainWindowContent != null)
        {
            _mainWindowContent.RemoveAll();
            _mainWindowContent.Add(
                new Label("Status: Logged Off. Use System -> Logon.") { X = 1, Y = 1 }
            );
            _mainWindowContent.SetNeedsDisplay();
        }
    }

    private static void AddSetupStatus(string message)
    {
        // ... (AddSetupStatus remains the same) ...
        if (_mainWindowContent != null)
        {
            _mainWindowContent.RemoveAll();
            _mainWindowContent.Add(new Label(message) { X = 1, Y = 1 });
            _mainWindowContent.SetNeedsDisplay();
            Application.Refresh();
        }
    }

    private static string GetApplicationVersion()
    {
        // ... (GetApplicationVersion remains the same) ...
        try
        {
            string infoVersion = GitVersionInformation.InformationalVersion;
            if (
                string.IsNullOrEmpty(infoVersion)
                || infoVersion == "0.0.0"
                || infoVersion.StartsWith("0.1.0+0")
            )
            {
                Console.WriteLine(
                    "[DEBUG] GitVersionInformation.InformationalVersion was null, empty, or default. Using fallback."
                );
                return "vDev";
            }
            var versionParts = infoVersion.Split('+');
            var versionWithoutMetadata = versionParts[0];
            Console.WriteLine(
                $"[DEBUG] Using GitVersionInformation.InformationalVersion: {infoVersion}"
            );
            return $"v{versionWithoutMetadata}";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Error getting application version via GitVersionInformation: {ex.Message}"
            );
            return "v?.?.?";
        }
    }
}
