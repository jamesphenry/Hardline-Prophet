using HardlineProphet.Core.Interfaces;
using HardlineProphet.Core.Models;
using HardlineProphet.Infrastructure.Persistence;
using HardlineProphet.UI.Views;

public static class ApplicationState
{
    public static GameState? CurrentGameState { get; set; }
    // Create a single repository instance for the application lifetime
    public static IGameStateRepository GameStateRepository { get; } = new JsonGameStateRepository();
    public static ITickService? TickServiceInstance { get; set; }
    public static InGameView? InGameViewInstance { get; set; }
}