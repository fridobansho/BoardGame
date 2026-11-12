namespace BoardGame.Web.Services;

using BoardGame.Library.Implementations;
using BoardGame.Library.Implementations.Chomp;
using BoardGame.Library.Implementations.ConnectFour;
using BoardGame.Library.Implementations.TicTacToe;
using BoardGame.Library.Interfaces;
using BoardGame.Web.Models;

public class GameSession
{
    private readonly object _lock = new();

    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string RoomCode { get; }
    public string GameType { get; }
    public string Mode { get; }
    public string AiDifficulty { get; }

    public IBoard Board { get; }
    public IGameLogic GameLogic { get; }
    public List<IPlayer> Players { get; } = new();

    public string Player1Token { get; } = Guid.NewGuid().ToString("N");
    public string Player2Token { get; } = Guid.NewGuid().ToString("N");

    public int CurrentTurnIndex { get; private set; } = 0;
    public string Status { get; private set; } = "InProgress";
    public string? Winner { get; private set; }
    public string? LastMoveText { get; private set; }
    public int MoveCount { get; private set; } = 0;

    public event Action? OnStateChanged;

    public GameSession(string? gameType, string? mode, string? aiDifficulty, string? p1Name, string? p2Name, string? customRoomCode = null)
    {
        GameType = (!string.IsNullOrWhiteSpace(gameType) ? gameType : "connect4").ToLowerInvariant();
        Mode = (!string.IsNullOrWhiteSpace(mode) ? mode : "pass-and-play").ToLowerInvariant();
        AiDifficulty = (!string.IsNullOrWhiteSpace(aiDifficulty) ? aiDifficulty : "smart").ToLowerInvariant();
        RoomCode = customRoomCode ?? GenerateRoomCode();

        string player1Name = !string.IsNullOrWhiteSpace(p1Name) ? p1Name.Trim() : "Player 1";
        string player2Name = !string.IsNullOrWhiteSpace(p2Name) ? p2Name.Trim() : 
            (Mode == "vs-ai" ? (AiDifficulty == "smart" ? "AI (Smart)" : "AI (Easy)") : "Player 2");

        var p1 = new WebHumanPlayer(player1Name, Player1Token);
        IPlayer p2 = Mode == "vs-ai" ? CreateAiPlayer(GameType, AiDifficulty, player2Name) : new WebHumanPlayer(player2Name, Player2Token);

        Players.Add(p1);
        Players.Add(p2);

        switch (GameType)
        {
            case "connect4":
                Board = new Board(ConnectFourLogic.DefaultWidth, ConnectFourLogic.DefaultHeight);
                GameLogic = new ConnectFourLogic();
                break;
            case "chomp":
                Board = new Board(5, 4);
                GameLogic = new ChompLogic();
                break;
            case "tictactoe":
            default:
                GameType = "tictactoe";
                Board = new Board(3, 3);
                GameLogic = new TicTacToeLogic();
                break;
        }

        GameLogic.MapPieces(Players);
        GameLogic.InitializeBoard(Board);

        if (Mode == "room")
        {
            Status = "WaitingForPlayer";
        }
    }

    private static IPlayer CreateAiPlayer(string gameType, string difficulty, string name)
    {
        return gameType switch
        {
            "connect4" => difficulty == "smart" ? new ConnectFourPlayer(name) : new RandomConnectFourPlayer(name),
            "chomp" => difficulty == "smart" ? new SmartChompPlayer(name) : new ChompPlayer(name),
            "tictactoe" => difficulty == "smart" ? new MinimaxPlayer(name) : new SimplePlayer(name),
            _ => new SimplePlayer(name)
        };
    }

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rnd = new Random();
        return new string(Enumerable.Repeat(chars, 4).Select(s => s[rnd.Next(s.Length)]).ToArray());
    }

    public bool JoinRoomAsGuest(string guestName, out string token)
    {
        lock (_lock)
        {
            if (Mode != "room")
            {
                token = Player2Token;
                return true;
            }

            var guestPlayer = new WebHumanPlayer(!string.IsNullOrWhiteSpace(guestName) ? guestName.Trim() : "Guest", Player2Token);
            Players[1] = guestPlayer;
            GameLogic.MapPieces(Players);
            Status = "InProgress";
            token = Player2Token;
            NotifyChanged();
            return true;
        }
    }

    public (bool Success, string? Error) MakeMove(int x, int y, string? playerToken = null)
    {
        lock (_lock)
        {
            if (Status == "WaitingForPlayer")
            {
                return (false, "Waiting for Player 2 to join the room.");
            }

            if (Status != "InProgress")
            {
                return (false, "Game is already over.");
            }

            var currentPlayer = Players[CurrentTurnIndex];

            // In room mode or vs-ai mode, enforce player token if provided
            if (Mode == "room" && !string.IsNullOrEmpty(playerToken))
            {
                var expectedToken = CurrentTurnIndex == 0 ? Player1Token : Player2Token;
                if (!string.Equals(playerToken, expectedToken, StringComparison.Ordinal))
                {
                    return (false, "It is not your turn!");
                }
            }
            else if (Mode == "vs-ai" && CurrentTurnIndex == 1)
            {
                return (false, "It is the AI's turn.");
            }

            var moveLocation = new Location(x, y);
            if (!GameLogic.IsValidMove(Board, moveLocation))
            {
                return (false, "Invalid move.");
            }

            GameLogic.ApplyMove(Board, currentPlayer, moveLocation);
            MoveCount++;
            LastMoveText = $"{currentPlayer.Name} played at ({x}, {y})";

            CheckGameState(currentPlayer);

            if (Status == "InProgress")
            {
                CurrentTurnIndex = 1 - CurrentTurnIndex;

                // If next player is AI, trigger AI move
                if (Mode == "vs-ai" && CurrentTurnIndex == 1)
                {
                    ExecuteAiMove();
                }
            }

            NotifyChanged();
            return (true, null);
        }
    }

    private void ExecuteAiMove()
    {
        var aiPlayer = Players[1];
        var aiMove = aiPlayer.GetMove(Board);

        if (GameLogic.IsValidMove(Board, aiMove))
        {
            GameLogic.ApplyMove(Board, aiPlayer, aiMove);
            MoveCount++;
            LastMoveText = $"{aiPlayer.Name} played at ({aiMove.X}, {aiMove.Y})";
            CheckGameState(aiPlayer);

            if (Status == "InProgress")
            {
                CurrentTurnIndex = 0;
            }
        }
        else
        {
            // Fallback: search for first valid move if any
            for (int x = 0; x < Board.Width; x++)
            {
                for (int y = 0; y < Board.Height; y++)
                {
                    var loc = new Location(x, y);
                    if (GameLogic.IsValidMove(Board, loc))
                    {
                        GameLogic.ApplyMove(Board, aiPlayer, loc);
                        MoveCount++;
                        LastMoveText = $"{aiPlayer.Name} played at ({x}, {y})";
                        CheckGameState(aiPlayer);
                        if (Status == "InProgress") CurrentTurnIndex = 0;
                        return;
                    }
                }
            }
        }
    }

    private void CheckGameState(IPlayer lastPlayer)
    {
        var winners = GameLogic.GetWinners(Board, Players).ToList();
        if (winners.Count > 0)
        {
            Winner = winners[0].Name;
            Status = "Finished";
            return;
        }

        // Draw detection
        if (IsDraw())
        {
            Status = "Draw";
        }
    }

    private bool IsDraw()
    {
        if (GameType == "connect4")
        {
            // Draw if top row is completely filled
            for (int x = 0; x < Board.Width; x++)
            {
                if (Board.PieceAt(x, 0).Value == BlankPiece.BlankValue) return false;
            }
            return true;
        }

        if (GameType == "tictactoe")
        {
            for (int x = 0; x < Board.Width; x++)
            {
                for (int y = 0; y < Board.Height; y++)
                {
                    if (Board.PieceAt(x, y).Value == BlankPiece.BlankValue) return false;
                }
            }
            return true;
        }

        return false;
    }

    public void Reset()
    {
        lock (_lock)
        {
            for (int x = 0; x < Board.Width; x++)
            {
                for (int y = 0; y < Board.Height; y++)
                {
                    Board.PieceAt(x, y, BlankPiece.Blank);
                }
            }
            GameLogic.InitializeBoard(Board);
            CurrentTurnIndex = 0;
            MoveCount = 0;
            Winner = null;
            LastMoveText = null;
            Status = Mode == "room" && Players.Count < 2 ? "WaitingForPlayer" : "InProgress";
            NotifyChanged();
        }
    }

    public SessionStateDto ToDto()
    {
        lock (_lock)
        {
            string[][] grid = new string[Board.Height][];
            for (int y = 0; y < Board.Height; y++)
            {
                grid[y] = new string[Board.Width];
                for (int x = 0; x < Board.Width; x++)
                {
                    grid[y][x] = Board.PieceAt(x, y)?.Value ?? BlankPiece.BlankValue;
                }
            }

            var currentTurnPlayer = Players[CurrentTurnIndex].Name;
            bool isAiTurn = Mode == "vs-ai" && CurrentTurnIndex == 1;

            return new SessionStateDto(
                Id: Id,
                RoomCode: RoomCode,
                GameType: GameType,
                Mode: Mode,
                Width: Board.Width,
                Height: Board.Height,
                Grid: grid,
                Status: Status,
                CurrentTurnPlayer: currentTurnPlayer,
                CurrentTurnIndex: CurrentTurnIndex,
                Winner: Winner,
                LastMoveText: LastMoveText,
                MoveCount: MoveCount,
                Player1Name: Players[0].Name,
                Player2Name: Players[1].Name,
                IsAiTurn: isAiTurn
            );
        }
    }

    private void NotifyChanged()
    {
        OnStateChanged?.Invoke();
    }
}

public class WebHumanPlayer : IPlayer
{
    public string Name { get; }
    public string Token { get; }

    public WebHumanPlayer(string name, string? token = null)
    {
        Name = name;
        Token = token ?? Guid.NewGuid().ToString("N");
    }

    public ILocation GetMove(IBoard board) => new Location(0, 0);
}

public class RandomConnectFourPlayer : IPlayer
{
    private static readonly Random Rnd = new();
    public string Name { get; }

    public RandomConnectFourPlayer(string name = "Bot (Easy)")
    {
        Name = name;
    }

    public ILocation GetMove(IBoard board)
    {
        if (board == null) return new Location(0, 0);
        var valid = new List<int>();
        for (int x = 0; x < board.Width; x++)
        {
            if (board.PieceAt(x, 0).Value == BlankPiece.BlankValue) valid.Add(x);
        }
        if (valid.Count == 0) return new Location(0, 0);
        return new Location(valid[Rnd.Next(valid.Count)], 0);
    }
}
