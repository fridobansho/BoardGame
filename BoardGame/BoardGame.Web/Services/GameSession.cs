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
    public int StartingPlayerIndex { get; private set; } = 0;

    public int TimerSeconds { get; } = 0;
    public DateTime? TurnExpiresAt { get; private set; }
    public int Player1Predictions { get; private set; } = 0;
    public int Player2Predictions { get; private set; } = 0;
    public List<MoveStepDto> MoveHistory { get; } = new();

    public event Action? OnStateChanged;
    public event Action<string>? OnReaction;
    public event Action<string, string>? OnChat;
    public event Action<int, int>? OnPrediction;

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

    public GameSession(
        string? gameType,
        string? mode,
        string? aiDifficulty,
        string? p1Name,
        string? p2Name,
        string? customRoomCode = null,
        bool isPublic = true,
        int? customWidth = null,
        int? customHeight = null,
        int? timerSeconds = null
    )
    {
        GameType = (!string.IsNullOrWhiteSpace(gameType) ? gameType : "connect4").ToLowerInvariant();
        Mode = (!string.IsNullOrWhiteSpace(mode) ? mode : "pass-and-play").ToLowerInvariant();
        AiDifficulty = (!string.IsNullOrWhiteSpace(aiDifficulty) ? aiDifficulty : "smart").ToLowerInvariant();
        RoomCode = customRoomCode ?? GenerateRoomCode();
        IsPublic = isPublic;
        TimerSeconds = timerSeconds.HasValue && timerSeconds.Value > 0 ? timerSeconds.Value : 0;

        string player1Name = !string.IsNullOrWhiteSpace(p1Name) ? p1Name.Trim() : "Player 1";
        string player2Name = !string.IsNullOrWhiteSpace(p2Name) ? p2Name.Trim() : 
            (Mode == "vs-ai" ? (AiDifficulty == "smart" ? "AI (Smart)" : "AI (Easy)") : "Player 2");

        switch (GameType)
        {
            case "connect4":
                int c4Width = customWidth.HasValue && customWidth.Value >= 4 && customWidth.Value <= 10 ? customWidth.Value : 7;
                int c4Height = customHeight.HasValue && customHeight.Value >= 4 && customHeight.Value <= 10 ? customHeight.Value : 6;
                Board = new Board(c4Width, c4Height);
                GameLogic = new ConnectFourLogic();
                break;
            case "chomp":
                int chompWidth = customWidth.HasValue && customWidth.Value >= 2 && customWidth.Value <= 10 ? customWidth.Value : 5;
                int chompHeight = customHeight.HasValue && customHeight.Value >= 2 && customHeight.Value <= 10 ? customHeight.Value : 4;
                Board = new Board(chompWidth, chompHeight);
                GameLogic = new ChompLogic();
                break;
            case "tictactoe":
            default:
                GameType = "tictactoe";
                int tttSize = customWidth.HasValue && customWidth.Value >= 3 && customWidth.Value <= 5 ? customWidth.Value : 3;
                Board = new Board(tttSize, tttSize);
                GameLogic = new TicTacToeLogic();
                break;
        }

        var p1 = new WebHumanPlayer(player1Name, Player1Token);
        IPlayer p2 = Mode == "vs-ai" ? CreateAiPlayer(GameType, AiDifficulty, player2Name, Board.Width) : new WebHumanPlayer(player2Name, Player2Token);

        Players.Add(p1);
        Players.Add(p2);

        GameLogic.MapPieces(Players);
        GameLogic.InitializeBoard(Board);

        RecordInitialHistory();

        if (Mode == "room")
        {
            Status = "WaitingForPlayer";
        }
        else
        {
            ResetTurnTimer();
        }
    }

    private void RecordInitialHistory()
    {
        MoveHistory.Add(new MoveStepDto(
            MoveNumber: 0,
            PlayerName: "Game Start",
            X: null,
            Y: null,
            Grid: CloneGrid(Board),
            LastMoveText: "Game started"
        ));
    }

    private static IPlayer CreateAiPlayer(string gameType, string difficulty, string name, int boardWidth = 3)
    {
        return gameType switch
        {
            "connect4" => difficulty == "smart" ? new ConnectFourPlayer(name) : new RandomConnectFourPlayer(name),
            "chomp" => difficulty == "smart" ? new SmartChompPlayer(name) : new ChompPlayer(name),
            "tictactoe" => difficulty == "smart"
                ? (boardWidth == 3 ? new MinimaxPlayer(name) : new SmartNxNTicTacToePlayer(name))
                : new SimplePlayer(name),
            _ => new SimplePlayer(name)
        };
    }

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rnd = new Random();
        return new string(Enumerable.Repeat(chars, 4).Select(s => s[rnd.Next(s.Length)]).ToArray());
    }

    private void ResetTurnTimer()
    {
        if (TimerSeconds > 0 && Status == "InProgress")
        {
            TurnExpiresAt = DateTime.UtcNow.AddSeconds(TimerSeconds);
        }
        else
        {
            TurnExpiresAt = null;
        }
    }

    public bool CheckTimerExpired()
    {
        lock (_lock)
        {
            if (TimerSeconds > 0 && Status == "InProgress" && TurnExpiresAt.HasValue && DateTime.UtcNow > TurnExpiresAt.Value)
            {
                var timedOutPlayer = Players[CurrentTurnIndex];
                var otherPlayer = Players[1 - CurrentTurnIndex];
                Status = "Finished";
                Winner = otherPlayer.Name;
                LastMoveText = $"{timedOutPlayer.Name} ran out of time! {otherPlayer.Name} wins.";
                if (1 - CurrentTurnIndex == 0) Player1Score++; else Player2Score++;
                TurnExpiresAt = null;
                NotifyChanged();
                return true;
            }
            return false;
        }
    }

    public bool JoinRoomAsGuest(string guestName, out string playerToken)
    {
        lock (_lock)
        {
            if (Mode != "room")
            {
                playerToken = string.Empty;
                return false;
            }

            if (Players.Count >= 2 && Status != "WaitingForPlayer")
            {
                playerToken = string.Empty;
                return false;
            }

            string cleanName = !string.IsNullOrWhiteSpace(guestName) ? guestName.Trim() : "Player 2";
            Players[1] = new WebHumanPlayer(cleanName, Player2Token);
            Status = "InProgress";
            playerToken = Player2Token;
            ResetTurnTimer();
            NotifyChanged();
            return true;
        }
    }

    public (bool Success, string? Error) MakeMove(int x, int y, string? playerToken = null)
    {
        lock (_lock)
        {
            CheckTimerExpired();

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

            // Enforce player tokens
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
            LastMoveText = $"{currentPlayer.Name} played at ({x}, {actualY})";
            LastMoveX = x;
            LastMoveY = actualY;

            MoveHistory.Add(new MoveStepDto(
                MoveNumber: MoveCount,
                PlayerName: currentPlayer.Name,
                X: x,
                Y: actualY,
                Grid: CloneGrid(Board),
                LastMoveText: LastMoveText
            ));

            CheckGameState(currentPlayer);

            if (Status == "InProgress")
            {
                CurrentTurnIndex = 1 - CurrentTurnIndex;
                ResetTurnTimer();

                if (Mode == "vs-ai" && CurrentTurnIndex == 1)
                {
                    ExecuteAiMove();
                }
            }
            else
            {
                TurnExpiresAt = null;
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
            LastMoveText = $"{aiPlayer.Name} played at ({aiMove.X}, {aiY})";
            LastMoveX = aiX;
            LastMoveY = aiY;

            MoveHistory.Add(new MoveStepDto(
                MoveNumber: MoveCount,
                PlayerName: aiPlayer.Name,
                X: aiX,
                Y: aiY,
                Grid: CloneGrid(Board),
                LastMoveText: LastMoveText
            ));

            CheckGameState(aiPlayer);

            if (Status == "InProgress")
            {
                CurrentTurnIndex = 0;
                ResetTurnTimer();
            }
            else
            {
                TurnExpiresAt = null;
            }
        }
        else
        {
            // Fallback: search for first valid move
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
                        LastMoveText = $"{aiPlayer.Name} played at ({x}, {fallbackY})";
                        LastMoveX = x;
                        LastMoveY = fallbackY;

                        MoveHistory.Add(new MoveStepDto(
                            MoveNumber: MoveCount,
                            PlayerName: aiPlayer.Name,
                            X: x,
                            Y: fallbackY,
                            Grid: CloneGrid(Board),
                            LastMoveText: LastMoveText
                        ));

                        CheckGameState(aiPlayer);
                        if (Status == "InProgress")
                        {
                            CurrentTurnIndex = 0;
                            ResetTurnTimer();
                        }
                        else
                        {
                            TurnExpiresAt = null;
                        }
                        return;
                    }
                }
            }
        }
    }

    private void SaveSnapshot()
    {
        string[][] gridCopy = CloneGrid(Board);

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

    private static string[][] CloneGrid(IBoard board)
    {
        string[][] gridCopy = new string[board.Height][];
        for (int y = 0; y < board.Height; y++)
        {
            gridCopy[y] = new string[board.Width];
            for (int x = 0; x < board.Width; x++)
            {
                gridCopy[y][x] = board.PieceAt(x, y)?.Value ?? BlankPiece.BlankValue;
            }
        }
        return gridCopy;
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

            if (string.Equals(playerToken, "spectator", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Spectators cannot undo moves.");
            }

            if (Mode == "vs-ai")
            {
                GameSnapshot? target = null;
                if (_history.Count >= 2)
                {
                    _history.Pop();
                    target = _history.Pop();
                    if (MoveHistory.Count > 1) MoveHistory.RemoveAt(MoveHistory.Count - 1);
                    if (MoveHistory.Count > 1) MoveHistory.RemoveAt(MoveHistory.Count - 1);
                }
                else
                {
                    target = _history.Pop();
                    if (MoveHistory.Count > 1) MoveHistory.RemoveAt(MoveHistory.Count - 1);
                }
                RestoreSnapshot(target);
            }
            else
            {
                var snapshot = _history.Pop();
                if (MoveHistory.Count > 1) MoveHistory.RemoveAt(MoveHistory.Count - 1);
                RestoreSnapshot(snapshot);
            }

            ResetTurnTimer();
            NotifyChanged();
            return (true, null);
        }
    }

    public void SendReaction(string emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji)) return;
        OnReaction?.Invoke(emoji.Trim());
    }

    public void SendChat(string sender, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        string cleanSender = string.IsNullOrWhiteSpace(sender) ? "Player" : sender.Trim();
        string cleanText = text.Trim();
        if (cleanText.Length > 80) cleanText = cleanText[..80];
        OnChat?.Invoke(cleanSender, cleanText);
    }

    public void AddPrediction(int playerIndex)
    {
        lock (_lock)
        {
            if (MoveCount > 6) return;
            if (playerIndex == 0) Player1Predictions++;
            else if (playerIndex == 1) Player2Predictions++;
            OnPrediction?.Invoke(Player1Predictions, Player2Predictions);
            NotifyChanged();
        }
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
            TurnExpiresAt = null;
            return;
        }

        if (IsDraw())
        {
            Status = "Draw";
            DrawScore++;
            TurnExpiresAt = null;
        }
    }

    private bool IsDraw()
    {
        if (GameType == "connect4")
        {
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
            Player1Predictions = 0;
            Player2Predictions = 0;
            _history.Clear();
            MoveHistory.Clear();
            RecordInitialHistory();

            Status = Mode == "room" && Players.Count < 2 ? "WaitingForPlayer" : "InProgress";

            ResetTurnTimer();

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
            CheckTimerExpired();

            string[][] grid = CloneGrid(Board);
            var currentTurnPlayer = Players[CurrentTurnIndex].Name;
            bool isAiTurn = Mode == "vs-ai" && CurrentTurnIndex == 1;

            int? timeRemaining = null;
            if (TimerSeconds > 0 && Status == "InProgress" && TurnExpiresAt.HasValue)
            {
                timeRemaining = Math.Max(0, (int)Math.Ceiling((TurnExpiresAt.Value - DateTime.UtcNow).TotalSeconds));
            }

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
                CanUndo: (Mode == "pass-and-play" || Mode == "vs-ai") && _history.Count > 0 && Status == "InProgress",
                TimerSeconds: TimerSeconds,
                TurnTimeRemaining: timeRemaining,
                Player1Predictions: Player1Predictions,
                Player2Predictions: Player2Predictions,
                MoveHistory: new List<MoveStepDto>(MoveHistory)
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
