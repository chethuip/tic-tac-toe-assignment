using backend.Models;

namespace backend.Services;

public sealed class GameService
{
    private readonly Dictionary<Guid, Game> games = [];
    private readonly Scoreboard scoreboard = new();
    private readonly object gate = new();
    private static readonly int[][] winningLines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],
        [0, 3, 6], [1, 4, 7], [2, 5, 8],
        [0, 4, 8], [2, 4, 6]
    ];

    public GameResponse Create(GameMode mode)
    {
        lock (gate)
        {
            var game = new Game { Id = Guid.NewGuid(), Mode = mode };
            games[game.Id] = game;
            return ToResponse(game);
        }
    }

    public GameResponse? Get(Guid id)
    {
        lock (gate) return games.TryGetValue(id, out var game) ? ToResponse(game) : null;
    }

    public (GameResponse? Response, string? Error) Move(Guid id, MoveRequest request)
    {
        lock (gate)
        {
            if (!games.TryGetValue(id, out var game)) return (null, "Game not found.");
            if (game.Status != GameStatus.InProgress) return (ToResponse(game), "The game is already complete.");
            if (request.Player != game.CurrentPlayer) return (ToResponse(game), "It is not this player's turn.");
            if (request.Row is < 0 or > 2 || request.Column is < 0 or > 2) return (ToResponse(game), "The cell is outside the board.");

            var index = request.Row * 3 + request.Column;
            if (game.Board[index] is not null) return (ToResponse(game), "That cell is already occupied.");
            ApplyMove(game, request.Player, index);
            if (game.Mode == GameMode.Computer && game.Status == GameStatus.InProgress)
            {
                ApplyMove(game, 'O', SelectComputerMove(game));
            }
            return (ToResponse(game), null);
        }
    }

    public (GameResponse? Response, string? Error) Undo(Guid id)
    {
        lock (gate)
        {
            if (!games.TryGetValue(id, out var game)) return (null, "Game not found.");
            if (game.Status != GameStatus.InProgress) return (ToResponse(game), "Undo is disabled after a completed game.");
            if (game.Moves.Count == 0) return (ToResponse(game), "There are no moves to undo.");

            var count = game.Mode == GameMode.Computer ? Math.Min(2, game.Moves.Count) : 1;
            game.Moves.RemoveRange(game.Moves.Count - count, count);
            game.Board = new char?[9];
            foreach (var move in game.Moves) game.Board[move.Row * 3 + move.Column] = move.Player;
            game.CurrentPlayer = 'X';
            foreach (var move in game.Moves) game.CurrentPlayer = move.Player == 'X' ? 'O' : 'X';
            return (ToResponse(game), null);
        }
    }

    public GameResponse? Reset(Guid id)
    {
        lock (gate)
        {
            if (!games.TryGetValue(id, out var game)) return null;
            game.Board = new char?[9]; game.CurrentPlayer = 'X'; game.Status = GameStatus.InProgress;
            game.Winner = null; game.WinningCells = []; game.Moves = []; game.ScoreRecorded = false;
            return ToResponse(game);
        }
    }

    public Scoreboard GetScoreboard()
    {
        lock (gate) return new Scoreboard { XWins = scoreboard.XWins, OWins = scoreboard.OWins, Draws = scoreboard.Draws };
    }

    public Scoreboard ResetScoreboard()
    {
        lock (gate) { scoreboard.XWins = 0; scoreboard.OWins = 0; scoreboard.Draws = 0; return GetScoreboard(); }
    }

    private void ApplyMove(Game game, char player, int index)
    {
        game.Board[index] = player;
        game.Moves.Add(new Move { Number = game.Moves.Count + 1, Player = player, Row = index / 3, Column = index % 3 });
        var line = winningLines.FirstOrDefault(candidate => candidate.All(cell => game.Board[cell] == player));
        if (line is not null)
        {
            game.Status = GameStatus.Won; game.Winner = player; game.WinningCells = [.. line]; RecordScore(game); return;
        }
        if (game.Board.All(cell => cell is not null)) { game.Status = GameStatus.Draw; RecordScore(game); return; }
        game.CurrentPlayer = player == 'X' ? 'O' : 'X';
    }

    private void RecordScore(Game game)
    {
        if (game.ScoreRecorded) return;
        game.ScoreRecorded = true;
        if (game.Status == GameStatus.Draw) scoreboard.Draws++;
        else if (game.Winner == 'X') scoreboard.XWins++;
        else scoreboard.OWins++;
    }

    private static int SelectComputerMove(Game game)
    {
        var available = Enumerable.Range(0, 9).Where(i => game.Board[i] is null).ToList();
        foreach (var player in new[] { 'O', 'X' })
        {
            foreach (var index in available)
            {
                game.Board[index] = player;
                var wins = winningLines.Any(line => line.All(cell => game.Board[cell] == player));
                game.Board[index] = null;
                if (wins) return index;
            }
        }
        if (game.Board[4] is null) return 4;
        var corner = available.FirstOrDefault(i => i is 0 or 2 or 6 or 8, -1);
        return corner >= 0 ? corner : available[0];
    }

    private GameResponse ToResponse(Game game) => new()
    {
        GameId = game.Id, Board = [.. game.Board], CurrentPlayer = game.CurrentPlayer, Mode = game.Mode,
        Status = game.Status, Winner = game.Winner, WinningCells = [.. game.WinningCells], MoveHistory = [.. game.Moves], Scoreboard = GetScoreboard()
    };
}