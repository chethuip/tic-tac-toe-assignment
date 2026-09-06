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

    [Fact]
    public void Detects_column_win()
    {
        var service = new GameService(); var game = service.Create(GameMode.TwoPlayer);
        service.Move(game.GameId, new('X', 0, 0)); service.Move(game.GameId, new('O', 0, 1));
        service.Move(game.GameId, new('X', 1, 0)); service.Move(game.GameId, new('O', 1, 1));
        var result = service.Move(game.GameId, new('X', 2, 0));

        Assert.Equal(GameStatus.Won, result.Response!.Status);
        Assert.Equal([0, 3, 6], result.Response.WinningCells);
    }

    [Fact]
    public void Detects_diagonal_win()
    {
        var service = new GameService(); var game = service.Create(GameMode.TwoPlayer);
        service.Move(game.GameId, new('X', 0, 0)); service.Move(game.GameId, new('O', 0, 1));
        service.Move(game.GameId, new('X', 1, 1)); service.Move(game.GameId, new('O', 0, 2));
        var result = service.Move(game.GameId, new('X', 2, 2));

        Assert.Equal(GameStatus.Won, result.Response!.Status);
        Assert.Equal([0, 4, 8], result.Response.WinningCells);
    }

    [Fact]
    public void Detects_draw_without_recording_a_winner()
    {
        var service = new GameService(); var game = service.Create(GameMode.TwoPlayer);
        var moves = new[]
        {
            new MoveRequest('X', 0, 0), new MoveRequest('O', 0, 1),
            new MoveRequest('X', 0, 2), new MoveRequest('O', 1, 1),
            new MoveRequest('X', 1, 0), new MoveRequest('O', 2, 0),
            new MoveRequest('X', 1, 2), new MoveRequest('O', 2, 2),
            new MoveRequest('X', 2, 1)
        };

        GameResponse? result = null;
        foreach (var move in moves) result = service.Move(game.GameId, move).Response;

        Assert.Equal(GameStatus.Draw, result!.Status);
        Assert.Null(result.Winner);
        Assert.Equal(1, service.GetScoreboard().Draws);
    }

    [Fact]
    public void Rejects_coordinates_outside_the_board()
    {
        var service = new GameService(); var game = service.Create(GameMode.TwoPlayer);

        var result = service.Move(game.GameId, new('X', 3, 0));

        Assert.NotNull(result.Error);
        Assert.Empty(result.Response!.MoveHistory);
    }

    [Fact]
    public void Rejects_moves_and_undo_after_game_completion()
    {
        var service = new GameService(); var game = service.Create(GameMode.TwoPlayer);
        service.Move(game.GameId, new('X', 0, 0)); service.Move(game.GameId, new('O', 1, 0));
        service.Move(game.GameId, new('X', 0, 1)); service.Move(game.GameId, new('O', 1, 1));
        var completed = service.Move(game.GameId, new('X', 0, 2)).Response!;

        var moveAfterCompletion = service.Move(game.GameId, new('O', 2, 2));
        var undoAfterCompletion = service.Undo(game.GameId);

        Assert.Equal(GameStatus.Won, completed.Status);
        Assert.NotNull(moveAfterCompletion.Error);
        Assert.NotNull(undoAfterCompletion.Error);
        Assert.Equal(1, service.GetScoreboard().XWins);
    }

    [Fact]
    public void Resetting_scoreboard_preserves_the_current_game()
    {
        var service = new GameService(); var game = service.Create(GameMode.TwoPlayer);
        service.Move(game.GameId, new('X', 0, 0)); service.Move(game.GameId, new('O', 1, 1));
        service.Move(game.GameId, new('X', 0, 1)); service.Move(game.GameId, new('O', 2, 2));
        service.Move(game.GameId, new('X', 0, 2));

        service.ResetScoreboard();
        var current = service.Get(game.GameId)!;

        Assert.Equal(GameStatus.Won, current.Status);
        Assert.Equal('X', current.Board[0]);
        Assert.Equal(5, current.MoveHistory.Count);
        Assert.Equal(0, current.Scoreboard.XWins);
    }

    [Fact]
    public void Computer_blocks_an_immediate_human_win()
    {
        var service = new GameService(); var game = service.Create(GameMode.Computer);
        service.Move(game.GameId, new('X', 0, 0));
        var result = service.Move(game.GameId, new('X', 0, 2)).Response!;

        Assert.Equal('O', result.Board[1]);
        Assert.Equal(4, result.MoveHistory.Count);
    }

    [Fact]
    public void Computer_takes_an_immediate_winning_move()
    {
        var service = new GameService(); var game = service.Create(GameMode.Computer);
        service.Move(game.GameId, new('X', 0, 0));
        service.Move(game.GameId, new('X', 0, 1));
        var result = service.Move(game.GameId, new('X', 1, 0)).Response!;

        Assert.Equal(GameStatus.Won, result.Status);
        Assert.Equal('O', result.Winner);
        Assert.Contains(6, result.WinningCells);
    }
}