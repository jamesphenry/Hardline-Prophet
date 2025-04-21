// src/HardlineProphet/UI/Views/UI.cs
using HardlineProphet.Core.Models; // GameState, Mission
using HardlineProphet.Services; // TickService
using HardlineProphet.UI.Dialogs; // LogonDialog
using Terminal.Gui;
using System; // MessageBox, Console, Exception, Func, Action
using System.Collections.Generic; // IReadOnlyDictionary
using System.IO; // InvalidDataException, IOException
// using System.Reflection; // No longer needed for version retrieval
using System.Threading.Tasks; // Task

// Assuming GitVersionInformation is generated in the root namespace
// If not, add the correct using statement here.
// using RootNamespaceForGitVersionInformation;

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
        string appVersion = GetApplicationVersion(); // Use updated helper
        Console.WriteLine($"[DEBUG] Retrieved App Version: {appVersion}");
        // -----------------------------

        // Create the content window where different views (Logged Off, InGame) will be placed.
        // Add version to title
        _mainWindowContent = new Window($"Hardline Prophet - {appVersion}")
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            ColorScheme = scheme
        };

        // Create the menu bar
        var menu = new MenuBar(new MenuBarItem[] { /* ... Menu Items ... */
            new MenuBarItem("_System", new MenuItem[] { new MenuItem("_Logon", "Log on to the system", async () => { /* ... Logon logic ... */ ApplicationState.TickServiceInstance?.Stop(); ApplicationState.TickServiceInstance = null; ApplicationState.CurrentGameState = null; ApplicationState.InGameViewInstance = null; AddLoggedOffStatus(); var logonDialog = new LogonDialog(); Application.Run(logonDialog); if (!logonDialog.Canceled && _mainWindowContent != null) { var username = logonDialog.GetUsername(); try { GameState loadedState = await ApplicationState.GameStateRepository!.LoadStateAsync(username); ApplicationState.CurrentGameState = loadedState; _mainWindowContent.RemoveAll(); ApplicationState.InGameViewInstance = new InGameView { Width = Dim.Fill(), Height = Dim.Fill() }; _mainWindowContent.Add(ApplicationState.InGameViewInstance); ApplicationState.InGameViewInstance.UpdateState(ApplicationState.CurrentGameState); Func<GameState?> getGameState = () => ApplicationState.CurrentGameState; Action<GameState> updateGameState = (newState) => { ApplicationState.CurrentGameState = newState; Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.UpdateState(newState)); }; Action<string> tickLog = (msg) => { Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.AddLogMessage(msg)); }; var missions = ApplicationState.LoadedMissions ?? new Dictionary<string, Mission>(); ApplicationState.TickServiceInstance = new TickService( getGameState, updateGameState, tickLog, missions, Application.MainLoop ); ApplicationState.TickServiceInstance.Start(); Application.MainLoop.Invoke(() => ApplicationState.InGameViewInstance?.AddLogMessage($"Logon successful for {username}.")); } catch (Exception ex) when (ex is InvalidDataException || ex is IOException) { MessageBox.ErrorQuery("Logon Failed", $"Failed to load save data: {ex.Message}", "OK"); ApplicationState.CurrentGameState = null; ApplicationState.InGameViewInstance = null; AddLoggedOffStatus(); } catch (Exception ex) { MessageBox.ErrorQuery("Logon Failed", $"An unexpected error occurred: {ex.Message}\n{ex.StackTrace}", "OK"); ApplicationState.CurrentGameState = null; ApplicationState.InGameViewInstance = null; AddLoggedOffStatus(); } } }), new MenuItem("_Shutdown", "Save and exit", async () => { /* ... Shutdown logic ... */ var n = MessageBox.Query("Confirm", "Save and Shutdown?", "Yes", "No"); if (n == 0) { ApplicationState.TickServiceInstance?.Stop(); ApplicationState.TickServiceInstance = null; if (ApplicationState.CurrentGameState != null) { try { await ApplicationState.GameStateRepository!.SaveStateAsync(ApplicationState.CurrentGameState); } catch (Exception ex) { MessageBox.ErrorQuery("Save Failed", $"Could not save game state: {ex.Message}", "OK"); var quitAnyway = MessageBox.Query("Save Failed", "Could not save game state. Quit anyway?", "Yes", "No"); if (quitAnyway == 1) return; } } Application.RequestStop(); } }), })
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

    /// <summary>
    /// Gets the application version string using the GitVersionInformation class.
    /// </summary>
    private static string GetApplicationVersion()
    {
        try
        {
            // Access the generated class directly.
            // Assumes GitVersionInformation is in the root namespace or accessible via using.
            // Use InformationalVersion as it includes SemVer + metadata
            string infoVersion = GitVersionInformation.InformationalVersion;

            // Handle default/missing value case (optional, depends on GitVersion behavior)
            if (string.IsNullOrEmpty(infoVersion) || infoVersion == "0.0.0" || infoVersion.StartsWith("0.1.0+0")) // Check for default/uninitialized states
            {
                // Fallback or default display if GitVersion didn't provide a meaningful version
                Console.WriteLine("[DEBUG] GitVersionInformation.InformationalVersion was null, empty, or default. Using fallback.");
                return "vDev"; // Or other placeholder
            }

            // Clean up potential build metadata (+) if desired
            var versionParts = infoVersion.Split('+');
            var versionWithoutMetadata = versionParts[0];

            Console.WriteLine($"[DEBUG] Using GitVersionInformation.InformationalVersion: {infoVersion}");
            return $"v{versionWithoutMetadata}"; // Prepend 'v'
        }
        catch (Exception ex) // Catch potential errors finding/accessing GitVersionInformation
        {
            Console.Error.WriteLine($"Error getting application version via GitVersionInformation: {ex.Message}");
            return "v?.?.?"; // Fallback display on error
        }
    }
}
