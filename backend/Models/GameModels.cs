namespace backend.Models;

public enum GameMode { TwoPlayer, Computer }
public enum GameStatus { InProgress, Won, Draw }

public sealed class Move
{
    public int Number { get; init; }
    public char Player { get; init; }
    public int Row { get; init; }
    public int Column { get; init; }
}

public sealed class Scoreboard
{
    public int XWins { get; set; }
    public int OWins { get; set; }
    public int Draws { get; set; }
}

public sealed class Game
{
    public Guid Id { get; init; }
    public GameMode Mode { get; init; }
    public char?[] Board { get; set; } = new char?[9];
    public char CurrentPlayer { get; set; } = 'X';
    public GameStatus Status { get; set; } = GameStatus.InProgress;
    public char? Winner { get; set; }
    public List<int> WinningCells { get; set; } = [];
    public List<Move> Moves { get; set; } = [];
    public bool ScoreRecorded { get; set; }
}

public sealed record CreateGameRequest(GameMode Mode);
public sealed record MoveRequest(char Player, int Row, int Column);

public sealed class GameResponse
{
    public Guid GameId { get; init; }
    public char?[] Board { get; init; } = [];
    public char CurrentPlayer { get; init; }
    public GameMode Mode { get; init; }
    public GameStatus Status { get; init; }
    public char? Winner { get; init; }
    public List<int> WinningCells { get; init; } = [];
    public List<Move> MoveHistory { get; init; } = [];
    public Scoreboard Scoreboard { get; init; } = new();
}