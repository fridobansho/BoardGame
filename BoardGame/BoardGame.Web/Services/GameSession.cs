namespace BoardGame.Web.Services;

using BoardGame.Library.Implementations;
using BoardGame.Library.Implementations.Breakthrough;
using BoardGame.Library.Implementations.Checkers;
using BoardGame.Library.Implementations.Chomp;
using BoardGame.Library.Implementations.ConnectFour;
using BoardGame.Library.Implementations.Gomoku;
using BoardGame.Library.Implementations.Hex;
using BoardGame.Library.Implementations.Mancala;
using BoardGame.Library.Implementations.Minesweeper;
using BoardGame.Library.Implementations.Nim;
using BoardGame.Library.Implementations.Othello;
using BoardGame.Library.Implementations.Scrabble;
using BoardGame.Library.Implementations.Sudoku;
using BoardGame.Library.Implementations.TicTacToe;
using BoardGame.Library.Implementations.Wordle;
using BoardGame.Library.Interfaces;
using BoardGame.Web.Models;

public class GameSession
{
    private readonly object _lock = new();

    public string Id { get; private set; } = Guid.NewGuid().ToString("N");
    public string RoomCode { get; }
    public string GameType { get; }
    public string Mode { get; }
    public string AiDifficulty { get; }
    public string Variant { get; }

    public IBoard Board { get; }
    public IGameLogic GameLogic { get; }
    public List<IPlayer> Players { get; } = new();

    public string Player1Token { get; private set; } = Guid.NewGuid().ToString("N");
    public string Player2Token { get; private set; } = Guid.NewGuid().ToString("N");

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
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private readonly Stack<GameSnapshot> _history = new();

    private record GameSnapshot(
        string[][] Grid,
        int CurrentTurnIndex,
        int MoveCount,
        string Status,
        string? Winner,
        string? LastMoveText,
        int? LastMoveX,
        int? LastMoveY,
        int P1GamePoints = 0,
        int P2GamePoints = 0,
        Dictionary<string, string>? CustomState = null,
        string Variant = "standard"
    );

    public record PersistedSession(
        string Id,
        string RoomCode,
        string GameType,
        string Mode,
        string AiDifficulty,
        string Player1Name,
        string Player2Name,
        string Player1Token,
        string Player2Token,
        int Width,
        int Height,
        string[][] Grid,
        int CurrentTurnIndex,
        string Status,
        string? Winner,
        string? LastMoveText,
        int? LastMoveX,
        int? LastMoveY,
        int MoveCount,
        int Player1Score,
        int Player2Score,
        int DrawScore,
        int GameNumber,
        int StartingPlayerIndex,
        int TimerSeconds,
        DateTime? TurnExpiresAt,
        int Player1Predictions,
        int Player2Predictions,
        bool IsPublic,
        DateTime CreatedAt,
        List<MoveStepDto> MoveHistory,
        int P1GamePoints = 0,
        int P2GamePoints = 0,
        List<string>? P1Rack = null,
        List<string>? P2Rack = null,
        List<string>? TileBag = null,
        int ConsecutivePasses = 0,
        int CheckersNoProgressMoves = 0,
        string Variant = "standard",
        Dictionary<string, string>? CustomState = null
    );

    public PersistedSession ExportPersistedState()
    {
        lock (_lock)
        {
            var grid = new string[Board.Height][];
            for (int y = 0; y < Board.Height; y++)
            {
                grid[y] = new string[Board.Width];
                for (int x = 0; x < Board.Width; x++)
                {
                    grid[y][x] = Board.PieceAt(x, y).Value;
                }
            }

            int p1GamePts = 0;
            int p2GamePts = 0;
            if (GameLogic is IScoredGameLogic scored)
            {
                p1GamePts = scored.GetScore(Players[0]);
                p2GamePts = scored.GetScore(Players[1]);
            }

            Dictionary<string, string>? customState = (GameLogic as ICustomStateGameLogic)?.ExportCustomState(Players);

            return new PersistedSession(
                Id,
                RoomCode,
                GameType,
                Mode,
                AiDifficulty,
                Players[0].Name,
                Players[1].Name,
                Player1Token,
                Player2Token,
                Board.Width,
                Board.Height,
                grid,
                CurrentTurnIndex,
                Status,
                Winner,
                LastMoveText,
                LastMoveX,
                LastMoveY,
                MoveCount,
                Player1Score,
                Player2Score,
                DrawScore,
                GameNumber,
                StartingPlayerIndex,
                TimerSeconds,
                TurnExpiresAt,
                Player1Predictions,
                Player2Predictions,
                IsPublic,
                CreatedAt,
                MoveHistory.ToList(),
                p1GamePts,
                p2GamePts,
                null,
                null,
                null,
                0,
                0,
                Variant,
                customState
            );
        }
    }

    public static GameSession FromPersistedState(PersistedSession state)
    {
        var session = new GameSession(
            state.GameType,
            state.Mode,
            state.AiDifficulty,
            state.Player1Name,
            state.Player2Name,
            customRoomCode: state.RoomCode,
            isPublic: state.IsPublic,
            customWidth: state.Width,
            customHeight: state.Height,
            timerSeconds: state.TimerSeconds,
            variant: state.Variant
        );

        session.Id = state.Id;
        session.Player1Token = state.Player1Token;
        session.Player2Token = state.Player2Token;
        session.CurrentTurnIndex = state.CurrentTurnIndex;
        session.Status = state.Status;
        session.Winner = state.Winner;
        session.LastMoveText = state.LastMoveText;
        session.LastMoveX = state.LastMoveX;
        session.LastMoveY = state.LastMoveY;
        session.MoveCount = state.MoveCount;
        session.Player1Score = state.Player1Score;
        session.Player2Score = state.Player2Score;
        session.DrawScore = state.DrawScore;
        session.GameNumber = state.GameNumber;
        session.StartingPlayerIndex = state.StartingPlayerIndex;
        session.TurnExpiresAt = state.TurnExpiresAt;
        session.Player1Predictions = state.Player1Predictions;
        session.Player2Predictions = state.Player2Predictions;
        session.CreatedAt = state.CreatedAt;
        if (state.MoveHistory != null)
        {
            session.MoveHistory.AddRange(state.MoveHistory);
        }

        for (int y = 0; y < state.Height && y < session.Board.Height; y++)
        {
            for (int x = 0; x < state.Width && x < session.Board.Width; x++)
            {
                var val = state.Grid[y][x];
                session.Board.PieceAt(x, y, val == BlankPiece.BlankValue ? BlankPiece.Blank : new Piece(val));
            }
        }

        if (session.GameLogic is IScoredGameLogic scored)
        {
            scored.SetScore(session.Players[0], state.P1GamePoints);
            scored.SetScore(session.Players[1], state.P2GamePoints);
        }

        if (session.GameLogic is ICustomStateGameLogic customLogic)
        {
            if (state.CustomState is { Count: > 0 })
            {
                customLogic.ImportCustomState(session.Players, state.CustomState);
            }
            else if (session.GameLogic is ScrabbleLogic)
            {
                var legacy = new Dictionary<string, string>
                {
                    ["consecutivePasses"] = state.ConsecutivePasses.ToString()
                };
                if (state.P1Rack != null) legacy["p1Rack"] = string.Concat(state.P1Rack);
                if (state.P2Rack != null) legacy["p2Rack"] = string.Concat(state.P2Rack);
                if (state.TileBag != null) legacy["tileBag"] = string.Concat(state.TileBag);
                customLogic.ImportCustomState(session.Players, legacy);
            }
            else if (session.GameLogic is CheckersLogic && state.CheckersNoProgressMoves > 0)
            {
                customLogic.ImportCustomState(session.Players, new Dictionary<string, string>
                {
                    ["noProgressMoves"] = state.CheckersNoProgressMoves.ToString()
                });
            }
        }

        return session;
    }

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
        int? timerSeconds = null,
        string? variant = "standard"
    )
    {
        GameType = (!string.IsNullOrWhiteSpace(gameType) ? gameType : "connect4").ToLowerInvariant();
        Mode = (!string.IsNullOrWhiteSpace(mode) ? mode : "pass-and-play").ToLowerInvariant();
        AiDifficulty = (!string.IsNullOrWhiteSpace(aiDifficulty) ? aiDifficulty : "smart").ToLowerInvariant();
        Variant = (!string.IsNullOrWhiteSpace(variant) ? variant.Trim().ToLowerInvariant() : "standard");
        RoomCode = customRoomCode ?? GenerateRoomCode();
        IsPublic = isPublic;
        TimerSeconds = timerSeconds.HasValue && timerSeconds.Value > 0 ? timerSeconds.Value : 0;

        string player1Name = !string.IsNullOrWhiteSpace(p1Name) ? p1Name.Trim() : "Player 1";
        string player2Name = !string.IsNullOrWhiteSpace(p2Name) ? p2Name.Trim() : 
            (Mode == "vs-ai" ? (AiDifficulty == "smart" ? "AI (Smart)" : "AI (Easy)") :
            (Mode == "ai-vs-ai" ? "AI Bot 2" :
            (Mode == "daily" ? "Daily Challenge" :
            (Mode == "solo" ? "Solo" : "Player 2"))));

        switch (GameType)
        {
            case "connect4":
                int c4Width = customWidth.HasValue && customWidth.Value >= 4 && customWidth.Value <= 10 ? customWidth.Value : 7;
                int c4Height = customHeight.HasValue && customHeight.Value >= 4 && customHeight.Value <= 10 ? customHeight.Value : 6;
                Board = new Board(c4Width, c4Height);
                GameLogic = new ConnectFourLogic
                {
                    TargetLength = Variant == "connect5" ? 5 : 4
                };
                break;
            case "chomp":
                int chompWidth = customWidth.HasValue && customWidth.Value >= 2 && customWidth.Value <= 10 ? customWidth.Value : 5;
                int chompHeight = customHeight.HasValue && customHeight.Value >= 2 && customHeight.Value <= 10 ? customHeight.Value : 4;
                Board = new Board(chompWidth, chompHeight);
                GameLogic = new ChompLogic();
                break;
            case "gomoku":
                int gomokuSize = customWidth.HasValue && customWidth.Value >= 9 && customWidth.Value <= 19 ? customWidth.Value : 15;
                Board = new Board(gomokuSize, gomokuSize);
                GameLogic = new GomokuLogic
                {
                    ExactFiveOnly = Variant == "exact5"
                };
                break;
            case "othello":
                Board = new Board(8, 8);
                GameLogic = new OthelloLogic
                {
                    FewerDiscsWins = Variant == "reversed"
                };
                break;
            case "nim":
                Board = new Board(7, 4);
                GameLogic = new NimLogic
                {
                    IsMisere = Variant == "misere"
                };
                break;
            case "hex":
                int hexSize = customWidth.HasValue && customWidth.Value >= 5 && customWidth.Value <= 11 ? customWidth.Value : 7;
                Board = new Board(hexSize, hexSize);
                GameLogic = new HexLogic();
                break;
            case "checkers":
                Board = new Board(CheckersLogic.DefaultSize, CheckersLogic.DefaultSize);
                GameLogic = new CheckersLogic
                {
                    MandatoryCapture = Variant != "casual",
                    IsGiveaway = Variant == "giveaway"
                };
                break;
            case "scrabble":
                Board = new Board(ScrabbleLogic.DefaultSize, ScrabbleLogic.DefaultSize);
                GameLogic = new ScrabbleLogic
                {
                    IsShortBag = Variant == "blitz",
                    DoubleBonuses = Variant == "double_bonus"
                };
                break;
            case "sudoku":
                Board = new Board(SudokuLogic.DefaultSize, SudokuLogic.DefaultSize);
                bool isDaily = Mode == "daily";
                int emptyCount = Variant switch
                {
                    "easy_puzzle" => 28,
                    "hard_puzzle" => 50,
                    _ => isDaily ? 40 : (AiDifficulty == "easy" ? 32 : 42)
                };
                GameLogic = isDaily
                    ? SudokuLogic.CreateDaily()
                    : new SudokuLogic(seed: null, emptyCells: emptyCount);
                break;
            case "breakthrough":
                int btSize = customWidth.HasValue && customWidth.Value >= 6 && customWidth.Value <= 10 ? customWidth.Value : BreakthroughLogic.DefaultSize;
                Board = new Board(btSize, btSize);
                GameLogic = new BreakthroughLogic
                {
                    IsMisere = Variant == "misere"
                };
                break;
            case "mancala":
                Board = new Board(MancalaLogic.PitCountPerSide, 2);
                GameLogic = new MancalaLogic
                {
                    InitialStonesPerPit = Variant == "quick" ? 3 : 4,
                    IsMisere = Variant == "misere"
                };
                break;
            case "wordle":
                Board = new Board(WordleLogic.WordLength, WordleLogic.MaxGuesses);
                GameLogic = Mode == "daily"
                    ? WordleLogic.CreateDaily()
                    : new WordleLogic();
                if (GameLogic is WordleLogic wl)
                {
                    wl.AllowAnyLetters = Variant == "any_word";
                }
                break;
            case "minesweeper":
                int msW = customWidth.HasValue && customWidth.Value >= 6 && customWidth.Value <= 12 ? customWidth.Value : MinesweeperLogic.DefaultSize;
                int msH = customHeight.HasValue && customHeight.Value >= 6 && customHeight.Value <= 12 ? customHeight.Value : msW;
                int defaultMines = msW <= 6 ? 5 : (msW >= 12 ? 22 : MinesweeperLogic.DefaultMines);
                int mines = Variant == "hard_puzzle"
                    ? (int)Math.Round(defaultMines * 1.5)
                    : (Variant == "easy_puzzle" ? Math.Max(3, defaultMines / 2) : defaultMines);
                Board = new Board(msW, msH);
                GameLogic = Mode == "daily"
                    ? new MinesweeperLogic(MinesweeperLogic.GetDailySeed(), mines)
                    : new MinesweeperLogic(null, mines);
                break;
            case "tictactoe":
            default:
                GameType = "tictactoe";
                int tttSize = customWidth.HasValue && customWidth.Value >= 3 && customWidth.Value <= 5 ? customWidth.Value : 3;
                Board = new Board(tttSize, tttSize);
                GameLogic = new TicTacToeLogic();
                break;
        }

        IPlayer p1 = Mode == "ai-vs-ai"
            ? CreateAiPlayer(GameType, AiDifficulty, $"{player1Name} (Bot 1)", Board.Width)
            : new WebHumanPlayer(player1Name, Player1Token);
        IPlayer p2 = (Mode == "vs-ai" || Mode == "ai-vs-ai")
            ? CreateAiPlayer(GameType, AiDifficulty, Mode == "ai-vs-ai" ? "AI Bot 2" : player2Name, Board.Width)
            : new WebHumanPlayer(player2Name, Player2Token);

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
            "gomoku" => difficulty == "smart" ? new SmartGomokuPlayer(name) : new RandomGomokuPlayer(name),
            "othello" => difficulty == "smart" ? new SmartOthelloPlayer(name) : new RandomOthelloPlayer(name),
            "nim" => difficulty == "smart" ? new SmartNimPlayer(name) : new RandomNimPlayer(name),
            "hex" => difficulty == "smart" ? new SmartHexPlayer(name) : new RandomHexPlayer(name),
            "checkers" => difficulty == "smart" ? new SmartCheckersPlayer(name) : new RandomCheckersPlayer(name),
            "scrabble" => difficulty == "smart" ? new SmartScrabblePlayer(name) : new RandomScrabblePlayer(name),
            "sudoku" => difficulty == "smart" ? new SmartSudokuPlayer(name) : new RandomSudokuPlayer(name),
            "breakthrough" => difficulty == "smart" ? new SmartBreakthroughPlayer(name) : new RandomBreakthroughPlayer(name),
            "mancala" => difficulty == "smart" ? new SmartMancalaPlayer(name) : new RandomMancalaPlayer(name),
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
            if (Players[1] is WebHumanPlayer existingGuest)
            {
                existingGuest.Name = cleanName;
            }
            else
            {
                Players[1] = new WebHumanPlayer(cleanName, Player2Token);
                GameLogic.PlayerPieces.Clear();
                GameLogic.MapPieces(Players);
                if (GameLogic is ScrabbleLogic && MoveCount == 0)
                {
                    GameLogic.InitializeBoard(Board);
                }
            }
            Status = "InProgress";
            playerToken = Player2Token;
            ResetTurnTimer();
            NotifyChanged();
            return true;
        }
    }

    private bool IsMoveValidForLogic(IPlayer player, IMove move)
    {
        return GameLogic is IMoveGameLogic moveLogic
            ? moveLogic.IsValidMove(Board, player, move)
            : GameLogic.IsValidMove(Board, (ILocation)move);
    }

    private bool ApplyMoveToLogic(IPlayer player, IMove move)
    {
        return GameLogic is IMoveGameLogic moveLogic
            ? moveLogic.ApplyMove(Board, player, move)
            : GameLogic.ApplyMove(Board, player, (ILocation)move);
    }

    public (bool Success, string? Error) MakeMove(
        int x,
        int y,
        string? playerToken = null,
        int? toX = null,
        int? toY = null,
        List<TilePlacementDto>? placements = null,
        bool isPass = false,
        int? digit = null)
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
            else if (Mode == "ai-vs-ai")
            {
                return (false, "AI vs AI exhibition is running automatically.");
            }
            else if (Mode == "pass-and-play" || Mode == "daily" || Mode == "solo")
            {
                if (!string.IsNullOrEmpty(playerToken) &&
                    !string.Equals(playerToken, Player1Token, StringComparison.Ordinal) &&
                    !string.Equals(playerToken, Player2Token, StringComparison.Ordinal))
                {
                    return (false, "Spectators cannot make moves.");
                }
            }

            IMove move;
            int actualX = x;
            int actualY = y;
            string moveText;

            if (toX.HasValue || toY.HasValue)
            {
                if (!toX.HasValue || !toY.HasValue)
                {
                    return (false, "Select a piece and a destination square.");
                }
                var pm = new PieceMove(x, y, toX.Value, toY.Value);
                move = pm;
                actualX = pm.ToX;
                actualY = pm.ToY;
                moveText = $"{currentPlayer.Name} moved ({pm.FromX}, {pm.FromY}) to ({pm.ToX}, {pm.ToY})";
            }
            else if (isPass || (placements != null && placements.Count > 0))
            {
                if (isPass)
                {
                    move = WordMove.Pass;
                    moveText = $"{currentPlayer.Name} passed their turn";
                }
                else
                {
                    var tilePlacements = new List<TilePlacement>();
                    foreach (var p in placements!)
                    {
                        if (string.IsNullOrWhiteSpace(p.Letter))
                        {
                            return (false, "Invalid tile letter.");
                        }
                        tilePlacements.Add(new TilePlacement(p.X, p.Y, char.ToUpperInvariant(p.Letter.Trim()[0])));
                    }
                    var wm = new WordMove(tilePlacements);
                    move = wm;
                    actualX = wm.Placements[0].X;
                    actualY = wm.Placements[0].Y;
                    moveText = $"{currentPlayer.Name} played {wm.Placements.Count} tile(s)";
                }
            }
            else if (digit.HasValue)
            {
                if (digit.Value < 1 || digit.Value > 9)
                {
                    return (false, "Select a number from 1 to 9.");
                }
                move = new SudokuMove(x, y, digit.Value);
                moveText = $"{currentPlayer.Name} placed {digit.Value} at ({x}, {y})";
            }
            else
            {
                if (GameType == "checkers")
                {
                    return (false, "Select a piece and a destination square.");
                }
                if (GameType == "scrabble")
                {
                    return (false, "Place at least one tile to form a valid word.");
                }
                if (GameType == "sudoku")
                {
                    return (false, "Select a number from 1 to 9.");
                }

                move = new Location(x, y);
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
                moveText = $"{currentPlayer.Name} played at ({x}, {actualY})";
            }

            if (!IsMoveValidForLogic(currentPlayer, move))
            {
                string invalidMsg = GameType switch
                {
                    "checkers" => "Invalid move. Captures are mandatory when available!",
                    "breakthrough" => "Invalid move. Pieces move 1 square forward or diagonally forward.",
                    "scrabble" => "Invalid word placement. Check dictionary, center/connection, and rack tiles.",
                    "wordle" => "Enter a valid 5-letter dictionary word.",
                    "sudoku" => "Invalid digit placement. Conflicts with row/column/box or breaks solvability.",
                    "mancala" => "Select a non-empty pit on your own side.",
                    _ => "Invalid move."
                };
                return (false, invalidMsg);
            }

            SaveSnapshot();

            ApplyMoveToLogic(currentPlayer, move);
            MoveCount++;
            LastMoveText = moveText;
            bool isWordPass = move is WordMove { IsPass: true };
            LastMoveX = isWordPass ? null : actualX;
            LastMoveY = isWordPass ? null : actualY;

            MoveHistory.Add(new MoveStepDto(
                MoveNumber: MoveCount,
                PlayerName: currentPlayer.Name,
                X: LastMoveX,
                Y: LastMoveY,
                Grid: CloneGrid(Board),
                LastMoveText: LastMoveText
            ));

            CheckGameState(currentPlayer);

            if (Status == "InProgress")
            {
                bool keepTurn = (Mode == "daily" || Mode == "solo") || (GameLogic is MancalaLogic { LastMoveEarnedExtraTurn: true });
                if (!keepTurn)
                {
                    CurrentTurnIndex = 1 - CurrentTurnIndex;
                }
                ResetTurnTimer();

                if (Mode == "vs-ai" && CurrentTurnIndex == 1)
                {
                    int guard = 0;
                    while (Status == "InProgress" && CurrentTurnIndex == 1 && guard++ < 20)
                    {
                        ExecuteAiMove(1);
                    }
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

    public (bool Success, string? Error) StepAiMove()
    {
        lock (_lock)
        {
            if (Mode != "ai-vs-ai")
            {
                return (false, "AI step is only available in AI vs AI mode.");
            }
            if (Status != "InProgress")
            {
                return (false, "Game is already over.");
            }

            ExecuteAiMove(CurrentTurnIndex);
            NotifyChanged();
            return (true, null);
        }
    }

    private void ExecuteAiMove(int playerIndex = 1)
    {
        var aiPlayer = Players[playerIndex];
        IMove? aiMove = aiPlayer.GetMove(Board);

        if (aiMove != null && IsMoveValidForLogic(aiPlayer, aiMove))
        {
            RecordAndApplyAiMove(aiPlayer, aiMove);
            return;
        }

        // Game-specific fallbacks if AI returned an invalid move
        if (GameType == "checkers")
        {
            string side = GameLogic.GetPiece(aiPlayer).Value;
            var validMoves = CheckersLogic.GetValidMovesForSide(Board, side);
            if (validMoves.Count > 0)
            {
                RecordAndApplyAiMove(aiPlayer, validMoves[0]);
            }
            return;
        }

        if (GameType == "breakthrough")
        {
            string side = GameLogic.GetPiece(aiPlayer).Value;
            var validMoves = BreakthroughLogic.GetValidMovesForSide(Board, side);
            if (validMoves.Count > 0)
            {
                RecordAndApplyAiMove(aiPlayer, validMoves[0]);
            }
            return;
        }

        if (GameType == "scrabble")
        {
            RecordAndApplyAiMove(aiPlayer, WordMove.Pass);
            return;
        }

        if (GameType == "sudoku")
        {
            var fallback = new SmartSudokuPlayer(aiPlayer.Name);
            fallback.Bind((SudokuLogic)GameLogic);
            var fallbackMove = fallback.GetMove(Board);
            if (fallbackMove is SudokuMove && IsMoveValidForLogic(aiPlayer, fallbackMove))
            {
                RecordAndApplyAiMove(aiPlayer, fallbackMove);
            }
            return;
        }

        // Fallback: search for first valid single-coordinate move
        for (int x = 0; x < Board.Width; x++)
        {
            for (int y = 0; y < Board.Height; y++)
            {
                var loc = new Location(x, y);
                if (IsMoveValidForLogic(aiPlayer, loc))
                {
                    RecordAndApplyAiMove(aiPlayer, loc);
                    return;
                }
            }
        }
    }

    private void RecordAndApplyAiMove(IPlayer aiPlayer, IMove move)
    {
        int? aiX = null;
        int? aiY = null;
        string moveText;

        if (move is PieceMove pm)
        {
            aiX = pm.ToX;
            aiY = pm.ToY;
            moveText = $"{aiPlayer.Name} moved ({pm.FromX}, {pm.FromY}) to ({pm.ToX}, {pm.ToY})";
        }
        else if (move is WordMove wm)
        {
            if (wm.IsPass)
            {
                moveText = $"{aiPlayer.Name} passed their turn";
            }
            else
            {
                aiX = wm.Placements.Count > 0 ? wm.Placements[0].X : null;
                aiY = wm.Placements.Count > 0 ? wm.Placements[0].Y : null;
                moveText = $"{aiPlayer.Name} played {wm.Placements.Count} tile(s)";
            }
        }
        else if (move is SudokuMove sm)
        {
            aiX = sm.X;
            aiY = sm.Y;
            moveText = $"{aiPlayer.Name} placed {sm.Digit} at ({sm.X}, {sm.Y})";
        }
        else if (move is ILocation loc)
        {
            int resolvedY = loc.Y;
            if (GameType == "connect4")
            {
                for (int r = Board.Height - 1; r >= 0; r--)
                {
                    if (Board.PieceAt(loc.X, r).Value == BlankPiece.BlankValue)
                    {
                        resolvedY = r;
                        break;
                    }
                }
            }
            aiX = loc.X;
            aiY = resolvedY;
            moveText = $"{aiPlayer.Name} played at ({loc.X}, {resolvedY})";
        }
        else
        {
            moveText = $"{aiPlayer.Name} moved";
        }

        SaveSnapshot();

        ApplyMoveToLogic(aiPlayer, move);
        MoveCount++;
        LastMoveText = moveText;
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
            bool keepTurn = GameLogic is MancalaLogic { LastMoveEarnedExtraTurn: true };
            if (!keepTurn)
            {
                CurrentTurnIndex = 1 - CurrentTurnIndex;
            }
            ResetTurnTimer();
        }
        else
        {
            TurnExpiresAt = null;
        }
    }

    private void SaveSnapshot()
    {
        string[][] gridCopy = CloneGrid(Board);

        int p1Pts = 0;
        int p2Pts = 0;
        if (GameLogic is IScoredGameLogic scored)
        {
            p1Pts = scored.GetScore(Players[0]);
            p2Pts = scored.GetScore(Players[1]);
        }
        Dictionary<string, string>? customState = (GameLogic as ICustomStateGameLogic)?.ExportCustomState(Players);

        _history.Push(new GameSnapshot(
            Grid: gridCopy,
            CurrentTurnIndex: CurrentTurnIndex,
            MoveCount: MoveCount,
            Status: Status,
            Winner: Winner,
            LastMoveText: LastMoveText,
            LastMoveX: LastMoveX,
            LastMoveY: LastMoveY,
            P1GamePoints: p1Pts,
            P2GamePoints: p2Pts,
            CustomState: customState,
            Variant: Variant
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
                    "R" => GameType == "checkers" ? (IPiece)RedCheckerPiece.Red : RedPiece.Red,
                    "Y" => YellowPiece.Yellow,
                    "X" => XPiece.X,
                    "O" => OPiece.O,
                    "P" => PoisonPiece.Poison,
                    "C" => ChocolatePiece.Chocolate,
                    " " or "" => BlankPiece.Blank,
                    _ => new Piece(val)
                };
                Board.PieceAt(x, y, piece);
            }
        }

        if (GameLogic is IScoredGameLogic scored)
        {
            scored.SetScore(Players[0], snapshot.P1GamePoints);
            scored.SetScore(Players[1], snapshot.P2GamePoints);
        }

        if (GameLogic is ICustomStateGameLogic customLogic && snapshot.CustomState is { Count: > 0 })
        {
            customLogic.ImportCustomState(Players, snapshot.CustomState);
        }
    }

    public (bool Success, string? Error) Undo(string? playerToken = null)
    {
        lock (_lock)
        {
            if (Mode != "pass-and-play" && Mode != "vs-ai" && Mode != "daily" && Mode != "solo")
            {
                return (false, "Undo is only supported in Pass & Play, vs AI, and Solo/Daily modes.");
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
        if (winners.Count >= 1 && (Mode == "daily" || Mode == "solo"))
        {
            Winner = Players[0].Name;
            Status = "Finished";
            Player1Score++;
            TurnExpiresAt = null;
            return;
        }

        if (winners.Count == 1)
        {
            var winnerPlayer = winners[0];
            bool invertWinner = (Variant == "misere" && (GameType == "tictactoe" || GameType == "connect4" || GameType == "gomoku" || GameType == "hex"))
                || (GameType == "chomp" && Variant == "normal");
            if (invertWinner)
            {
                winnerPlayer = Players.First(p => !ReferenceEquals(p, winnerPlayer));
            }
            Winner = winnerPlayer.Name;
            Status = "Finished";
            if (Winner == Players[0].Name) Player1Score++;
            else if (Winner == Players[1].Name) Player2Score++;
            TurnExpiresAt = null;
            return;
        }

        if (winners.Count > 1 || IsDraw())
        {
            Winner = null;
            Status = "Draw";
            if (GameLogic is WordleLogic wl && !wl.IsSolved && !wl.HasValidMoves(Board))
            {
                LastMoveText = $"Out of guesses! The word was {wl.SecretWord}.";
            }
            else if (GameLogic is MinesweeperLogic ml && ml.IsExploded)
            {
                LastMoveText = "💥 Boom! Hit a mine.";
            }
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
                StartingPlayerIndex = (Mode == "daily" || Mode == "solo") ? 0 : (GameNumber - 1) % 2;
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
            bool isAiTurn = (Mode == "vs-ai" && CurrentTurnIndex == 1) || Mode == "ai-vs-ai";

            int? timeRemaining = null;
            if (TimerSeconds > 0 && Status == "InProgress" && TurnExpiresAt.HasValue)
            {
                timeRemaining = Math.Max(0, (int)Math.Ceiling((TurnExpiresAt.Value - DateTime.UtcNow).TotalSeconds));
            }

            int p1GamePoints = 0;
            int p2GamePoints = 0;
            List<string>? currentPlayerRack = null;
            List<string>? p1Rack = null;
            List<string>? p2Rack = null;
            int tileBagCount = 0;
            string[][]? bonusGrid = null;

            if (GameLogic is IScoredGameLogic scored)
            {
                p1GamePoints = scored.GetScore(Players[0]);
                p2GamePoints = scored.GetScore(Players[1]);
            }

            if (GameLogic is ScrabbleLogic scrabble)
            {
                p1Rack = scrabble.GetRack(Players[0]).Select(c => c.ToString()).ToList();
                p2Rack = scrabble.GetRack(Players[1]).Select(c => c.ToString()).ToList();
                currentPlayerRack = CurrentTurnIndex == 0 ? p1Rack : p2Rack;
                tileBagCount = scrabble.TileBagCount;
                bonusGrid = new string[Board.Height][];
                for (int y = 0; y < Board.Height; y++)
                {
                    bonusGrid[y] = new string[Board.Width];
                    for (int x = 0; x < Board.Width; x++)
                    {
                        bonusGrid[y][x] = ScrabbleLogic.GetBonusAt(x, y, Board.Width, Board.Height);
                    }
                }
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
                CanUndo: (Mode == "pass-and-play" || Mode == "vs-ai" || Mode == "daily" || Mode == "solo") && _history.Count > 0 && Status == "InProgress",
                TimerSeconds: TimerSeconds,
                TurnTimeRemaining: timeRemaining,
                Player1Predictions: Player1Predictions,
                Player2Predictions: Player2Predictions,
                MoveHistory: new List<MoveStepDto>(MoveHistory),
                Player1GamePoints: p1GamePoints,
                Player2GamePoints: p2GamePoints,
                CurrentPlayerRack: currentPlayerRack,
                Player1Rack: p1Rack,
                Player2Rack: p2Rack,
                TileBagCount: tileBagCount,
                BonusGrid: bonusGrid,
                Variant: Variant
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
    public string Name { get; set; }
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
