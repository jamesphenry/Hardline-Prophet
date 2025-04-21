// src/HardlineProphet/UI/Views/UI.cs
using HardlineProphet.Core.Models; // GameState, Mission
using HardlineProphet.Services; // TickService
using HardlineProphet.UI.Dialogs; // LogonDialog
using Terminal.Gui;
using System; // MessageBox, Console, Exception, Func, Action
using System.Collections.Generic; // IReadOnlyDictionary
using System.IO; // InvalidDataException, IOException
using System.Reflection; // Added for Assembly version retrieval
using System.Threading.Tasks; // Task

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
        // --- Get Application Version ---
        string appVersion = GetApplicationVersion();
        // -----------------------------

        // Create the content window where different views (Logged Off, InGame) will be placed.
        _mainWindowContent = new Window($"Hardline Prophet - {appVersion}") // Add version to title
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
                        // ... (Logon logic remains the same) ...
                        ApplicationState.TickServiceInstance?.Stop(); ApplicationState.TickServiceInstance = null; ApplicationState.CurrentGameState = null; ApplicationState.InGameViewInstance = null; AddLoggedOffStatus(); var logonDialog = new LogonDialog(); Application.Run(logonDialog); if (!logonDialog.Canceled && _mainWindowContent != null) { var username = logonDialog.GetUsername(); try { GameState loadedState = await ApplicationState.GameStateRepository.LoadStateAsync(username); ApplicationState.CurrentGameState = loadedState; _mainWindowContent.RemoveAll(); ApplicationState.InGameViewInstance = new InGameView { Width = Dim.Fill(), Height = Dim.Fill() }; _mainWindowContent.Add(ApplicationState.InGameViewInstance); ApplicationState.InGameViewInstance.UpdateState(ApplicationState.CurrentGameState); Func<GameState?> getGameState = () => ApplicationState.CurrentGameState; Action<GameState> updateGameState = (newState) => { ApplicationState.CurrentGameState = newState; Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.UpdateState(newState)); }; Action<string> tickLog = (msg) => { Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.AddLogMessage(msg)); }; var missions = ApplicationState.LoadedMissions ?? new Dictionary<string, Mission>(); ApplicationState.TickServiceInstance = new TickService( getGameState, updateGameState, tickLog, missions, Application.MainLoop ); ApplicationState.TickServiceInstance.Start(); Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.AddLogMessage($"Logon successful for {username}.")); } catch (Exception ex) when (ex is InvalidDataException || ex is IOException) { MessageBox.ErrorQuery("Logon Failed", $"Failed to load save data: {ex.Message}", "OK"); ApplicationState.CurrentGameState = null; ApplicationState.InGameViewInstance = null; AddLoggedOffStatus(); } catch (Exception ex) { MessageBox.ErrorQuery("Logon Failed", $"An unexpected error occurred: {ex.Message}\n{ex.StackTrace}", "OK"); ApplicationState.CurrentGameState = null; ApplicationState.InGameViewInstance = null; AddLoggedOffStatus(); } }
                    }),
                    new MenuItem("_Shutdown", "Save and exit", async () => // Shutdown action
                    {
                        // ... (Shutdown logic remains the same) ...
                        var n = MessageBox.Query("Confirm", "Save and Shutdown?", "Yes", "No"); if (n == 0) { ApplicationState.TickServiceInstance?.Stop(); ApplicationState.TickServiceInstance = null; if (ApplicationState.CurrentGameState != null) { try { await ApplicationState.GameStateRepository.SaveStateAsync(ApplicationState.CurrentGameState); } catch (Exception ex) { MessageBox.ErrorQuery("Save Failed", $"Could not save game state: {ex.Message}", "OK"); var quitAnyway = MessageBox.Query("Save Failed", "Could not save game state. Quit anyway?", "Yes", "No"); if (quitAnyway == 1) return; } } Application.RequestStop(); }
                    }),
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
        // ... (AddLoggedOffStatus remains the same) ...
        if (_mainWindowContent != null) { _mainWindowContent.RemoveAll(); _mainWindowContent.Add(new Label("Status: Logged Off. Use System -> Logon.") { X = 1, Y = 1 }); _mainWindowContent.SetNeedsDisplay(); }
    }

    /// <summary>
    /// Gets the application version string, typically from assembly info populated by GitVersion.
    /// </summary>
    private static string GetApplicationVersion()
    {
        try
        {
            var assembly = Assembly.GetEntryAssembly();
            // GitVersion.MsBuild typically populates AssemblyInformationalVersionAttribute
            var infoVersion = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(infoVersion))
            {
                return $"v{infoVersion}"; // Prepend 'v'
            }

            // Fallback to assembly version if informational version isn't available
            var assemblyVersion = assembly?.GetName().Version?.ToString();
            return assemblyVersion ?? "v?.?.?"; // Fallback display
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting application version: {ex.Message}");
            return "v?.?.?"; // Fallback display on error
        }
    }
}
