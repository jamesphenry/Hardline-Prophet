// ╔═══════════════════════════════════════════════════════════════════════════
// ║ [SYSTEM ID]   HARDLINE-PROPHET
// ║ [STATUS]      OPERATIONAL
// ║ [PRIORITY]    MAXIMUM
// ║
// ║ ▒▒▒ When Progress Is Your Only Religion ▒▒▒
// ║
// ║ 🧠  Project Lead: jamesphenry
// ║ 🔢  GitVersion: 0.2.0-alpha.12
// ║ 📄  File: SplashView.cs
// ║ 🕒  Timestamp: 2025-04-21 22:52:51 -0500
// // [CyberHeader] Injected by Hardline-Prophet
namespace HardlineProphet.UI.Views;

using System;
using System.Collections.Generic;
using Terminal.Gui;

public static class SplashView
{
    // 10‑line ASCII template; the progress bar is row 7
    private static readonly string[] FrameTemplate = new[]
    {
        "~~~(              )~~~",
        "  ~~(            )~~  ",
        "    ~(          )~    ",
        "                      ",
        "                      ",
        "           /\\         ",
        "          /  \\        ",
        "[                    ]",
        "                      ",
        "                      ",
    };

    /// <summary>
    /// Adds a splash window to Top, animates the bar-fill, pulses arcs, fades in the tagline with glitch, then reveals mainWindow.
    /// </summary>
    public static void Show(ColorScheme scheme, Window mainWindow)
    {
        var top = Application.Top;

        // Create the splash window
        var splash = new Window()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = scheme,
        };

        // Schemes for special elements
        var magentaScheme = new ColorScheme()
        {
            Normal = Application.Driver.MakeAttribute(Color.BrightMagenta, Color.Black),
        };

        // Generate Labels for each template line, center them
        var labels = new List<Label>();
        int barRow = 7;
        int barSlots = FrameTemplate[barRow].Length - 2; // exclude the [ and ]

        for (int i = 0; i < FrameTemplate.Length; i++)
        {
            var lbl = new Label(FrameTemplate[i])
            {
                X = Pos.Center(),
                Y = Pos.Center() + (i - FrameTemplate.Length / 2),
                ColorScheme = scheme,
                Visible = i > 2, // hide arcs initially for rows 0–2
            };
            splash.Add(lbl);
            labels.Add(lbl);
        }

        // Create the tagline label, hidden initially
        var tagline = new Label("When Progress Is Your Only Religion.")
        {
            X = Pos.Center(),
            Y = Pos.Center() + (FrameTemplate.Length + 2 - FrameTemplate.Length / 2),
            ColorScheme = scheme,
            Visible = false,
        };
        splash.Add(tagline);

        // Initialize the bar empty
        labels[barRow].Text = "[" + new string(' ', barSlots) + "]";

        // Prepare mainWindow but keep hidden
        mainWindow.Visible = false;
        top.Add(mainWindow);

        // Add the splash on top
        top.Add(splash);

        // Schedule the bar-fill animation over 2 seconds
        int filled = 0;
        double fillInterval = 2.0 / barSlots;

        Application.MainLoop.AddTimeout(
            TimeSpan.FromSeconds(fillInterval),
            _ =>
            {
                filled++;
                labels[barRow].Text =
                    "[" + new string('█', filled) + new string(' ', barSlots - filled) + "]";
                splash.SetNeedsDisplay();

                if (filled < barSlots)
                {
                    return true; // continue filling
                }

                // Once full, wait 0.5s then start pulse sequence
                Application.MainLoop.AddTimeout(
                    TimeSpan.FromSeconds(0.5),
                    __ =>
                    {
                        // First pulse: show arcs
                        for (int j = 0; j <= 2; j++)
                        {
                            labels[j].ColorScheme = magentaScheme;
                            labels[j].Visible = true;
                        }
                        splash.SetNeedsDisplay();

                        // Hide arcs after 0.3s
                        Application.MainLoop.AddTimeout(
                            TimeSpan.FromSeconds(0.3),
                            ___ =>
                            {
                                for (int j = 0; j <= 2; j++)
                                    labels[j].Visible = false;
                                splash.SetNeedsDisplay();
                                return false;
                            }
                        );

                        // Second pulse at 0.8s
                        Application.MainLoop.AddTimeout(
                            TimeSpan.FromSeconds(0.8),
                            ___ =>
                            {
                                for (int j = 0; j <= 2; j++)
                                {
                                    labels[j].ColorScheme = magentaScheme;
                                    labels[j].Visible = true;
                                }
                                splash.SetNeedsDisplay();

                                // Hide arcs after 0.3s
                                Application.MainLoop.AddTimeout(
                                    TimeSpan.FromSeconds(0.3),
                                    ____ =>
                                    {
                                        for (int j = 0; j <= 2; j++)
                                            labels[j].Visible = false;
                                        splash.SetNeedsDisplay();
                                        return false;
                                    }
                                );

                                return false;
                            }
                        );

                        // After pulses, schedule tagline fade & smooth janky glitch
                        Application.MainLoop.AddTimeout(
                            TimeSpan.FromSeconds(1.2),
                            ___ =>
                            {
                                tagline.Visible = true;
                                splash.SetNeedsDisplay();

                                var rand = new Random();
                                int jitterCount = 0;
                                int maxJitter = 10; // more janky shakes
                                double jitterInterval = 0.05; // smoother 50ms intervals

                                // Jitter loop
                                Application.MainLoop.AddTimeout(
                                    TimeSpan.FromSeconds(jitterInterval),
                                    ____ =>
                                    {
                                        jitterCount++;
                                        // Random -2 to +2 column shift
                                        int offset = rand.Next(-2, 3);
                                        tagline.X = Pos.Center() + offset;
                                        splash.SetNeedsDisplay();

                                        if (jitterCount < maxJitter)
                                        {
                                            return true; // continue jitter
                                        }

                                        // Reset position
                                        tagline.X = Pos.Center();
                                        splash.SetNeedsDisplay();

                                        // Delay 2 seconds before removing splash
                                        Application.MainLoop.AddTimeout(
                                            TimeSpan.FromSeconds(2.0),
                                            _____ =>
                                            {
                                                top.Remove(splash);
                                                mainWindow.Visible = true;
                                                return false;
                                            }
                                        );

                                        return false;
                                    }
                                );

                                return false; // end tagline scheduling
                            }
                        );

                        return false; // end first fill timeout
                    }
                );

                return false; // stop fill scheduling
            }
        );
    }
}
