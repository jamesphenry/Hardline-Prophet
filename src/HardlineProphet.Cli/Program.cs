// 💾 Cyberpunk Color Scheme
using HardlineProphet.UI.Views;
using Terminal.Gui;

// --- Main Application Logic ---
Application.Init();

// Define Color Scheme (as before)
var cyberpunkScheme = new ColorScheme()
{
    Normal = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black),
    Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightGreen),
    HotNormal = Application.Driver.MakeAttribute(Color.BrightMagenta, Color.Black),
    HotFocus = Application.Driver.MakeAttribute(Color.Black, Color.BrightMagenta),
    Disabled = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black)
};

var mainWindow = UI.CreateMainWindow(cyberpunkScheme); // UI class needs access to ApplicationState

SplashView.Show(cyberpunkScheme, mainWindow); // SplashView doesn't need state access directly

Application.Run();
Application.Shutdown(); // Ensure clean shutdown