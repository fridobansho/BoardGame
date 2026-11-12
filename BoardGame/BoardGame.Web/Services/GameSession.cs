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
    public int? LastMoveX { get; private set; }
    public int? LastMoveY { get; private set; }
    public int MoveCount { get; private set; } = 0;

    public int SpectatorCount { get; private set; } = 0;
    public int Player1Score { get; private set; } = 0;
    public int Player2Score { get; private set; } = 0;
    public int DrawScore { get; private set; } = 0;
    public int GameNumber { get; private set; } = 1;

    public event Action? OnStateChanged;
    public event Action<string>? OnReaction;

    public bool IsPublic { get; set; } = true;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    private readonly Stack<GameSnapshot> _history = new();

    private record GameSnapshot(
        string[][] Grid,
        int CurrentTurnIndex,
        int MoveCount,
        string Status,
        string? Winner,
        string? LastMoveText,
        int? LastMoveX,
        int? LastMoveY
    );

    public GameSession(string? gameType, string? mode, string? aiDifficulty, string? p1Name, string? p2Name, string? customRoomCode = null, bool isPublic = true)
    {
        GameType = (!string.IsNullOrWhiteSpace(gameType) ? gameType : "connect4").ToLowerInvariant();
        Mode = (!string.IsNullOrWhiteSpace(mode) ? mode : "pass-and-play").ToLowerInvariant();
        AiDifficulty = (!string.IsNullOrWhiteSpace(aiDifficulty) ? aiDifficulty : "smart").ToLowerInvariant();
        RoomCode = customRoomCode ?? GenerateRoomCode();
        IsPublic = isPublic;

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

            // Reject moves from spectators
            if (string.Equals(playerToken, "spectator", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Spectators cannot make moves.");
            }

            // In room mode or vs-ai mode, enforce player token if provided
            if (Mode == "room" && !string.IsNullOrEmpty(playerToken))
            {
                var expectedToken = CurrentTurnIndex == 0 ? Player1Token : Player2Token;
                if (!string.Equals(playerToken, expectedToken, StringComparison.Ordinal))
                {
                    return (false, "It is not your turn!");
                }
            }
            else if (Mode == "vs-ai")
            {
                if (CurrentTurnIndex == 1) return (false, "It is the AI's turn.");
                if (!string.IsNullOrEmpty(playerToken) && !string.Equals(playerToken, Player1Token, StringComparison.Ordinal))
                {
                    return (false, "Spectators cannot make moves.");
                }
            }
            else if (Mode == "pass-and-play")
            {
                if (!string.IsNullOrEmpty(playerToken) &&
                    !string.Equals(playerToken, Player1Token, StringComparison.Ordinal) &&
                    !string.Equals(playerToken, Player2Token, StringComparison.Ordinal))
                {
                    return (false, "Spectators cannot make moves.");
                }
            }

            var moveLocation = new Location(x, y);
            if (!GameLogic.IsValidMove(Board, moveLocation))
            {
                return (false, "Invalid move.");
            }

            int actualY = y;
            if (GameType == "connect4")
            {
                for (int r = Board.Height - 1; r >= 0; r--)
                {
                    if (Board.PieceAt(x, r).Value == BlankPiece.BlankValue)
                    {
                        actualY = r;
                        break;
                    }
                }
            }

            SaveSnapshot();

            GameLogic.ApplyMove(Board, currentPlayer, moveLocation);
            MoveCount++;
            LastMoveText = $"{currentPlayer.Name} played at ({x}, {y})";
            LastMoveX = x;
            LastMoveY = actualY;

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
            int aiX = aiMove.X;
            int aiY = aiMove.Y;
            if (GameType == "connect4")
            {
                for (int r = Board.Height - 1; r >= 0; r--)
                {
                    if (Board.PieceAt(aiX, r).Value == BlankPiece.BlankValue)
                    {
                        aiY = r;
                        break;
                    }
                }
            }

            SaveSnapshot();

            GameLogic.ApplyMove(Board, aiPlayer, aiMove);
            MoveCount++;
            LastMoveText = $"{aiPlayer.Name} played at ({aiMove.X}, {aiMove.Y})";
            LastMoveX = aiX;
            LastMoveY = aiY;
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
                        int fallbackY = y;
                        if (GameType == "connect4")
                        {
                            for (int r = Board.Height - 1; r >= 0; r--)
                            {
                                if (Board.PieceAt(x, r).Value == BlankPiece.BlankValue)
                                {
                                    fallbackY = r;
                                    break;
                                }
                            }
                        }

                        SaveSnapshot();

                        GameLogic.ApplyMove(Board, aiPlayer, loc);
                        MoveCount++;
                        LastMoveText = $"{aiPlayer.Name} played at ({x}, {y})";
                        LastMoveX = x;
                        LastMoveY = fallbackY;
                        CheckGameState(aiPlayer);
                        if (Status == "InProgress") CurrentTurnIndex = 0;
                        return;
                    }
                }
            }
        }
    }

    private void SaveSnapshot()
    {
        string[][] gridCopy = new string[Board.Height][];
        for (int y = 0; y < Board.Height; y++)
        {
            gridCopy[y] = new string[Board.Width];
            for (int x = 0; x < Board.Width; x++)
            {
                gridCopy[y][x] = Board.PieceAt(x, y)?.Value ?? BlankPiece.BlankValue;
            }
        }

        _history.Push(new GameSnapshot(
            Grid: gridCopy,
            CurrentTurnIndex: CurrentTurnIndex,
            MoveCount: MoveCount,
            Status: Status,
            Winner: Winner,
            LastMoveText: LastMoveText,
            LastMoveX: LastMoveX,
            LastMoveY: LastMoveY
        ));
    }

    private void RestoreSnapshot(GameSnapshot snapshot)
    {
        CurrentTurnIndex = snapshot.CurrentTurnIndex;
        MoveCount = snapshot.MoveCount;
        Status = snapshot.Status;
        Winner = snapshot.Winner;
        LastMoveText = snapshot.LastMoveText;
        LastMoveX = snapshot.LastMoveX;
        LastMoveY = snapshot.LastMoveY;

        for (int y = 0; y < Board.Height; y++)
        {
            for (int x = 0; x < Board.Width; x++)
            {
                string val = snapshot.Grid[y][x];
                IPiece piece = val switch
                {
                    "R" => RedPiece.Red,
                    "Y" => YellowPiece.Yellow,
                    "X" => XPiece.X,
                    "O" => OPiece.O,
                    "P" => PoisonPiece.Poison,
                    "C" => ChocolatePiece.Chocolate,
                    _ => BlankPiece.Blank
                };
                Board.PieceAt(x, y, piece);
            }
        }
    }

    public (bool Success, string? Error) Undo(string? playerToken = null)
    {
        lock (_lock)
        {
            if (Mode != "pass-and-play" && Mode != "vs-ai")
            {
                return (false, "Undo is only supported in Pass & Play and vs AI modes.");
            }

            if (Status == "WaitingForPlayer")
            {
                return (false, "Game has not started yet.");
            }

            if (_history.Count == 0)
            {
                return (false, "No moves to undo.");
            }

            // Spectators cannot undo
            if (string.Equals(playerToken, "spectator", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Spectators cannot undo moves.");
            }

            if (Mode == "vs-ai")
            {
                // In vs-ai, human and AI each made a move. Revert both so human player gets turn back.
                GameSnapshot? target = null;
                if (_history.Count >= 2)
                {
                    _history.Pop(); // Pop AI snapshot
                    target = _history.Pop(); // Pop Human snapshot
                }
                else
                {
                    target = _history.Pop();
                }
                RestoreSnapshot(target);
            }
            else
            {
                var snapshot = _history.Pop();
                RestoreSnapshot(snapshot);
            }

            NotifyChanged();
            return (true, null);
        }
    }

    public void SendReaction(string emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji)) return;
        OnReaction?.Invoke(emoji.Trim());
    }

    public void IncrementSpectators()
    {
        lock (_lock)
        {
            SpectatorCount++;
        }
        NotifyChanged();
    }

    public void DecrementSpectators()
    {
        lock (_lock)
        {
            if (SpectatorCount > 0) SpectatorCount--;
        }
        NotifyChanged();
    }

    private void CheckGameState(IPlayer lastPlayer)
    {
        var winners = GameLogic.GetWinners(Board, Players).ToList();
        if (winners.Count > 0)
        {
            Winner = winners[0].Name;
            Status = "Finished";
            if (Winner == Players[0].Name) Player1Score++;
            else if (Winner == Players[1].Name) Player2Score++;
            return;
        }

        // Draw detection
        if (IsDraw())
        {
            Status = "Draw";
            DrawScore++;
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

    public int StartingPlayerIndex { get; private set; } = 0;

    public void Reset(bool isRematch = false)
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
            if (isRematch)
            {
                GameNumber++;
                StartingPlayerIndex = (GameNumber - 1) % 2;
            }
            CurrentTurnIndex = StartingPlayerIndex;
            MoveCount = 0;
            Winner = null;
            LastMoveText = null;
            LastMoveX = null;
            LastMoveY = null;
            _history.Clear();
            Status = Mode == "room" && Players.Count < 2 ? "WaitingForPlayer" : "InProgress";

            if (Mode == "vs-ai" && CurrentTurnIndex == 1)
            {
                ExecuteAiMove();
            }

            NotifyChanged();
        }
    }

    public void ResetSeries()
    {
        lock (_lock)
        {
            Player1Score = 0;
            Player2Score = 0;
            DrawScore = 0;
            GameNumber = 1;
            StartingPlayerIndex = 0;
            Reset(isRematch: false);
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
                IsAiTurn: isAiTurn,
                LastMoveX: LastMoveX,
                LastMoveY: LastMoveY,
                SpectatorCount: SpectatorCount,
                Player1Score: Player1Score,
                Player2Score: Player2Score,
                DrawScore: DrawScore,
                GameNumber: GameNumber,
                CanUndo: (Mode == "pass-and-play" || Mode == "vs-ai") && _history.Count > 0 && Status == "InProgress"
            );
        }
    }

    public RoomSummaryDto ToSummaryDto()
    {
        lock (_lock)
        {
            var span = DateTime.UtcNow - CreatedAt;
            string timeAgo = span.TotalSeconds < 60 ? "just now" :
                             span.TotalMinutes < 60 ? $"{(int)span.TotalMinutes}m ago" :
                             $"{(int)span.TotalHours}h ago";

            bool canJoin = Mode == "room" && Status == "WaitingForPlayer";
            bool canSpectate = Status == "InProgress" || Status == "WaitingForPlayer" || Status == "Finished" || Status == "Draw";

            return new RoomSummaryDto(
                RoomCode: RoomCode,
                SessionId: Id,
                GameType: GameType,
                Mode: Mode,
                HostName: Players.Count > 0 ? Players[0].Name : "Host",
                Status: Status,
                CanJoin: canJoin,
                CanSpectate: canSpectate,
                CreatedAt: CreatedAt,
                TimeAgo: timeAgo
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
