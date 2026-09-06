using backend.Models;
using backend.Services;
using Xunit;

namespace backend.Tests;

public sealed class GameServiceTests
{
    [Fact]
    public void Detects_row_win_and_updates_scoreboard_once()
    {
        var service = new GameService(); var game = service.Create(GameMode.TwoPlayer);
        service.Move(game.GameId, new('X', 0, 0)); service.Move(game.GameId, new('O', 1, 0)); service.Move(game.GameId, new('X', 0, 1)); service.Move(game.GameId, new('O', 1, 1));
        var result = service.Move(game.GameId, new('X', 0, 2));
        Assert.Equal(GameStatus.Won, result.Response!.Status); Assert.Equal('X', result.Response.Winner); Assert.Equal(1, service.GetScoreboard().XWins);
    }

    [Fact]
    public void Undo_two_player_removes_one_move_and_restores_turn()
    {
        var service = new GameService(); var game = service.Create(GameMode.TwoPlayer);
        service.Move(game.GameId, new('X', 0, 0)); service.Move(game.GameId, new('O', 1, 1));
        var undone = service.Undo(game.GameId).Response!;
        Assert.Single(undone.MoveHistory); Assert.Equal('O', undone.CurrentPlayer);
    }

    [Fact]
    public void Computer_move_is_added_and_undo_removes_the_pair()
    {
        var service = new GameService(); var game = service.Create(GameMode.Computer);
        var afterMove = service.Move(game.GameId, new('X', 0, 0)).Response!;
        var undone = service.Undo(game.GameId).Response!;
        Assert.Equal(2, afterMove.MoveHistory.Count); Assert.Empty(undone.MoveHistory); Assert.Equal('X', undone.CurrentPlayer);
    }

    [Fact]
    public void Rejects_occupied_cell_and_wrong_player()
    {
        var service = new GameService(); var game = service.Create(GameMode.TwoPlayer);
        service.Move(game.GameId, new('X', 0, 0));
        Assert.NotNull(service.Move(game.GameId, new('X', 0, 0)).Error);
        Assert.NotNull(service.Move(game.GameId, new('X', 0, 1)).Error);
    }
}