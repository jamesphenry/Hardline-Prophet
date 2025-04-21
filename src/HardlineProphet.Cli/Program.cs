// src/HardlineProphet/Program.cs
using HardlineProphet.Core.Interfaces;
using HardlineProphet.Core.Models;
using HardlineProphet.Infrastructure.Persistence;
using HardlineProphet.Services;
using HardlineProphet.UI.Views;
using Terminal.Gui;
using System;
using System.Collections.Generic; // Dictionary, List
using System.IO; // Path, File, InvalidDataException, IOException
using System.Linq; // ToDictionary
using System.Text.Json; // JsonSerializer, JsonException

// --- Application State ---
// --- Application State ---
public static class ApplicationState
{
    // Existing properties
    public static GameState? CurrentGameState { get; set; }
    public static ITickService? TickServiceInstance { get; set; }
    public static InGameView? InGameViewInstance { get; set; }
    public static IReadOnlyDictionary<string, Mission>? LoadedMissions { get; internal set; }
    public static string DefaultMissionId => LoadedMissions?.Keys.FirstOrDefault() ?? string.Empty;

    // New properties for Dev Mode and Service Initialization
    public static bool IsDevMode { get; private set; } = false; // Default to false
    public static IGameStateRepository? GameStateRepository { get; private set; } // Make nullable, init in method
    public static IReadOnlyDictionary<string, Item>? LoadedItems { get; internal set; }

    /// <summary>
    /// Initializes application-level services after configuration (like dev mode) is known.
    /// </summary>
    public static void InitializeServices(bool isDevMode)
    {
        IsDevMode = isDevMode;
        Console.WriteLine($"Initializing services... Dev Mode: {IsDevMode}");

        // Create repository instance here, passing dev mode status
        // NOTE: This will require updating the JsonGameStateRepository constructor later
        // For now, we use the existing constructor which ignores the flag.
        // TODO: Update JsonGameStateRepository constructor to accept isDevMode
        GameStateRepository = new JsonGameStateRepository(); // Pass isDevMode later: new JsonGameStateRepository(IsDevMode);

        // Initialize other services if needed...
        GameStateRepository = new JsonGameStateRepository(isDevMode: IsDevMode); // Pass isDevMode explicitly
    }
}

// --- Main Application Logic ---
public static class Program
{
    // Using explicit Main method structure to ensure loading happens before GUI init
    public static void Main(string[] args)
    {
        // --- Check for Dev Mode ---
        bool isDevMode = args.Contains("--dev", StringComparer.OrdinalIgnoreCase);
        if (isDevMode)
        {
            Console.WriteLine("--- DEVELOPER MODE ACTIVE ---");
        }

        // --- Initialize Services ---
        // Pass dev mode status for services to use
        ApplicationState.InitializeServices(isDevMode);

        // --- Load Definitions ---
        // Load missions first, as other parts might depend on them
        LoadMissionDefinitions();
        // TODO: Load items, perks etc. later in a similar fashion
        LoadItemDefinitions();

        // --- Init Terminal.Gui ---
        Application.Init();

        // Define Color Scheme
        var cyberpunkScheme = new ColorScheme()
        { /* ... colors ... */
            Normal = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black),
            Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightGreen),
            HotNormal = Application.Driver.MakeAttribute(Color.BrightMagenta, Color.Black),
            HotFocus = Application.Driver.MakeAttribute(Color.Black, Color.BrightMagenta),
            Disabled = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black)
        };

        // Create Main Window (returns the content window)
        var mainWindow = UI.CreateMainWindow(cyberpunkScheme);

        // Show Splash Screen (runs on the main loop)
        SplashView.Show(cyberpunkScheme, mainWindow);

        // Run Application's main loop
        Application.Run();

        // Cleanup after main loop exits
        Application.Shutdown();
    }

    /// <summary>
    /// Loads mission definitions from the Data/missions.json file.
    /// Populates ApplicationState.LoadedMissions.
    /// </summary>
    private static void LoadMissionDefinitions()
    {
        // Construct the path relative to the application's base directory
        string filePath = Path.Combine(AppContext.BaseDirectory, "Data", "missions.json");
        Console.WriteLine($"Attempting to load missions from: {filePath}"); // Debug output

        // Ensure the file exists before trying to read
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"ERROR: Mission definition file not found at {filePath}. Check Copy to Output Directory setting.");
            // Set to empty dictionary to avoid null references later
            ApplicationState.LoadedMissions = new Dictionary<string, Mission>();
            return;
        }

        try
        {
            // Read the entire file content
            string json = File.ReadAllText(filePath);

            // Deserialize the JSON array into a list of Mission objects
            var missions = JsonSerializer.Deserialize<List<Mission>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Check if deserialization was successful and yielded any missions
            if (missions == null || !missions.Any())
            {
                Console.WriteLine($"WARNING: No missions loaded from {filePath}. File might be empty or contain only 'null'.");
                ApplicationState.LoadedMissions = new Dictionary<string, Mission>();
            }
            else
            {
                // Convert the list to a dictionary keyed by mission ID for efficient lookup
                // Handle potential duplicate IDs gracefully (e.g., take the first one found)
                ApplicationState.LoadedMissions = missions
                    .GroupBy(m => m.Id) // Group by ID
                    .ToDictionary(g => g.Key, g => g.First()); // Take the first mission for each ID
                Console.WriteLine($"Successfully loaded {ApplicationState.LoadedMissions.Count} mission(s). Default ID: {ApplicationState.DefaultMissionId}");

                // Log warning if duplicates were found
                if (missions.Count != ApplicationState.LoadedMissions.Count)
                {
                    Console.WriteLine($"WARNING: Duplicate mission IDs found in {filePath}. Only the first occurrence of each ID was loaded.");
                }
            }
        }
        // Handle errors during JSON parsing
        catch (JsonException ex)
        {
            Console.WriteLine($"ERROR: Failed to parse missions file {filePath}. Invalid JSON. Details: {ex.Message}");
            ApplicationState.LoadedMissions = new Dictionary<string, Mission>(); // Use empty on error
        }
        // Handle errors during file reading
        catch (IOException ex)
        {
            Console.WriteLine($"ERROR: Failed to read missions file {filePath}. Details: {ex.Message}");
            ApplicationState.LoadedMissions = new Dictionary<string, Mission>(); // Use empty on error
        }
        // Handle any other unexpected errors during loading
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: An unexpected error occurred loading missions from {filePath}. Details: {ex.Message}");
            ApplicationState.LoadedMissions = new Dictionary<string, Mission>(); // Use empty on error
        }
    }

    /// <summary>
    /// Loads item definitions from the Data/items.json file.
    /// </summary>
    private static void LoadItemDefinitions()
    {
        // Similar logic to LoadMissionDefinitions
        string filePath = Path.Combine(AppContext.BaseDirectory, "Data", "items.json");
        Console.WriteLine($"Attempting to load items from: {filePath}");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"ERROR: Item definition file not found at {filePath}. Check Copy to Output Directory setting.");
            ApplicationState.LoadedItems = new Dictionary<string, Item>();
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var items = JsonSerializer.Deserialize<List<Item>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (items == null || !items.Any())
            {
                Console.WriteLine($"WARNING: No items loaded from {filePath}.");
                ApplicationState.LoadedItems = new Dictionary<string, Item>();
            }
            else
            {
                ApplicationState.LoadedItems = items
                    .GroupBy(i => i.Id)
                    .ToDictionary(g => g.Key, g => g.First());
                Console.WriteLine($"Successfully loaded {ApplicationState.LoadedItems.Count} item(s).");
                if (items.Count != ApplicationState.LoadedItems.Count)
                {
                    Console.WriteLine($"WARNING: Duplicate item IDs found in {filePath}.");
                }
            }
        }
        catch (Exception ex) when (ex is JsonException || ex is NotSupportedException) { Console.WriteLine($"ERROR: Failed to parse items file {filePath}. Invalid JSON. Details: {ex.Message}"); ApplicationState.LoadedItems = new Dictionary<string, Item>(); }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { Console.WriteLine($"ERROR: Failed to read items file {filePath}. Details: {ex.Message}"); ApplicationState.LoadedItems = new Dictionary<string, Item>(); }
        catch (Exception ex) { Console.WriteLine($"ERROR: An unexpected error occurred loading items from {filePath}. Details: {ex.Message}"); ApplicationState.LoadedItems = new Dictionary<string, Item>(); }
    }
}
