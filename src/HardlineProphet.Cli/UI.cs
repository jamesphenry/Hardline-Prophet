namespace HardlineProphet.Cli;

using Terminal.Gui;

public static class UI
{
    public static Window CreateMainWindow(ColorScheme scheme)
    {
        // Create the menu bar
        var menu = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_Actions", new MenuItem[]
            {
                new MenuItem("_Logon", "Log on to the system", () =>
                {
                    // TODO: Implement logon logic
                }),
                new MenuItem("_Logoff", "Log off the system", () =>
                {
                    // TODO: Implement logoff logic
                }),
                new MenuItem("_Shutdown", "Exit the application", () =>
                {
                    // Confirm shutdown
                    var n = MessageBox.Query("Confirm", "Are you sure you want to shutdown?", "Yes", "No");
                    if (n == 0)
                    {
                        Application.RequestStop();
                        Environment.Exit(0);  // ensure process exits
                    }
                }),
            })
        });

        // Main window content area (leaving space for menu at the top)
        var win = new Window("Hardline Prophet")
        {
            X = 0,
            Y = 1,                // start below the menu bar
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1,
            ColorScheme = scheme
        };

        // Example label in the center
        win.Add(
            new Label("Welcome to Hardline Prophet!")
            {
                X = Pos.Center(),
                Y = Pos.Center(),
                ColorScheme = scheme
            }
        );

        // Add the menu to the top-level
        Application.Top.Add(menu);

        return win;
    }
}
