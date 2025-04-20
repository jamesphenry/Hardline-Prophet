// 💾 Cyberpunk Color Scheme
using HardlineProphet.Cli;
using Terminal.Gui;

Application.Init();

var cyberpunkScheme = new ColorScheme()
{
    Normal = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black),
    Focus = Application.Driver.MakeAttribute(Color.Black, Color.BrightGreen),
    HotNormal = Application.Driver.MakeAttribute(Color.BrightMagenta, Color.Black),
    HotFocus = Application.Driver.MakeAttribute(Color.Black, Color.BrightMagenta),
    Disabled = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black)
};

var mainWindow = UI.CreateMainWindow(cyberpunkScheme);
SplashView.Show(cyberpunkScheme, mainWindow);
Application.Run();