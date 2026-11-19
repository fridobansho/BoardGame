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

public static class GameFactory
{
    public static (string GameType, IBoard Board, IGameLogic GameLogic) CreateGame(
        string? rawGameType,
        string mode,
        string aiDifficulty,
        string variant,
        int? customWidth = null,
        int? customHeight = null)
    {
        string gameType = (!string.IsNullOrWhiteSpace(rawGameType) ? rawGameType : "connect4").ToLowerInvariant();
        IBoard board;
        IGameLogic gameLogic;

        switch (gameType)
        {
            case "connect4":
                int c4Width = customWidth is >= 4 and <= 10 ? customWidth.Value : 7;
                int c4Height = customHeight is >= 4 and <= 10 ? customHeight.Value : 6;
                board = new Board(c4Width, c4Height);
                gameLogic = new ConnectFourLogic
                {
                    TargetLength = variant == "connect5" ? 5 : 4
                };
                break;
            case "chomp":
                int chompWidth = customWidth is >= 2 and <= 10 ? customWidth.Value : 5;
                int chompHeight = customHeight is >= 2 and <= 10 ? customHeight.Value : 4;
                board = new Board(chompWidth, chompHeight);
                gameLogic = new ChompLogic();
                break;
            case "gomoku":
                int gomokuSize = customWidth is >= 9 and <= 19 ? customWidth.Value : 15;
                board = new Board(gomokuSize, gomokuSize);
                gameLogic = new GomokuLogic
                {
                    ExactFiveOnly = variant == "exact5"
                };
                break;
            case "othello":
                board = new Board(8, 8);
                gameLogic = new OthelloLogic
                {
                    FewerDiscsWins = variant == "reversed"
                };
                break;
            case "nim":
                board = new Board(7, 4);
                gameLogic = new NimLogic
                {
                    IsMisere = variant == "misere"
                };
                break;
            case "hex":
                int hexSize = customWidth is >= 5 and <= 11 ? customWidth.Value : 7;
                board = new Board(hexSize, hexSize);
                gameLogic = new HexLogic();
                break;
            case "checkers":
                board = new Board(CheckersLogic.DefaultSize, CheckersLogic.DefaultSize);
                gameLogic = new CheckersLogic
                {
                    MandatoryCapture = variant != "casual",
                    IsGiveaway = variant == "giveaway"
                };
                break;
            case "scrabble":
                board = new Board(ScrabbleLogic.DefaultSize, ScrabbleLogic.DefaultSize);
                gameLogic = new ScrabbleLogic
                {
                    IsShortBag = variant == "blitz",
                    DoubleBonuses = variant == "double_bonus"
                };
                break;
            case "sudoku":
                board = new Board(SudokuLogic.DefaultSize, SudokuLogic.DefaultSize);
                bool isDaily = mode == "daily";
                int emptyCount = variant switch
                {
                    "easy_puzzle" => 28,
                    "hard_puzzle" => 50,
                    _ => isDaily ? 40 : (aiDifficulty == "easy" ? 32 : 42)
                };
                gameLogic = isDaily
                    ? SudokuLogic.CreateDaily()
                    : new SudokuLogic(seed: null, emptyCells: emptyCount);
                break;
            case "breakthrough":
                int btSize = customWidth is >= 6 and <= 10 ? customWidth.Value : BreakthroughLogic.DefaultSize;
                board = new Board(btSize, btSize);
                gameLogic = new BreakthroughLogic
                {
                    IsMisere = variant == "misere"
                };
                break;
            case "mancala":
                board = new Board(MancalaLogic.PitCountPerSide, 2);
                gameLogic = new MancalaLogic
                {
                    InitialStonesPerPit = variant == "quick" ? 3 : 4,
                    IsMisere = variant == "misere"
                };
                break;
            case "wordle":
                board = new Board(WordleLogic.WordLength, WordleLogic.MaxGuesses);
                gameLogic = mode == "daily"
                    ? WordleLogic.CreateDaily()
                    : new WordleLogic();
                if (gameLogic is WordleLogic wl)
                {
                    wl.AllowAnyLetters = variant == "any_word";
                }
                break;
            case "minesweeper":
                int msW = customWidth is >= 6 and <= 12 ? customWidth.Value : MinesweeperLogic.DefaultSize;
                int msH = customHeight is >= 6 and <= 12 ? customHeight.Value : msW;
                int defaultMines = msW <= 6 ? 5 : (msW >= 12 ? 22 : MinesweeperLogic.DefaultMines);
                int mines = variant == "hard_puzzle"
                    ? (int)Math.Round(defaultMines * 1.5)
                    : (variant == "easy_puzzle" ? Math.Max(3, defaultMines / 2) : defaultMines);
                board = new Board(msW, msH);
                gameLogic = mode == "daily"
                    ? new MinesweeperLogic(MinesweeperLogic.GetDailySeed(), mines)
                    : new MinesweeperLogic(null, mines);
                break;
            case "tictactoe":
            default:
                gameType = "tictactoe";
                int tttSize = customWidth is >= 3 and <= 5 ? customWidth.Value : 3;
                board = new Board(tttSize, tttSize);
                gameLogic = new TicTacToeLogic();
                break;
        }

        return (gameType, board, gameLogic);
    }

    public static IPlayer CreateAiPlayer(string gameType, string difficulty, string name, int boardWidth = 3)
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

    public static IMove? SelectFallbackAiMove(
        string gameType,
        IBoard board,
        IGameLogic gameLogic,
        IPlayer aiPlayer,
        Func<IPlayer, IMove, bool> isMoveValid)
    {
        if (gameType == "checkers")
        {
            string side = gameLogic.GetPiece(aiPlayer).Value;
            var validMoves = CheckersLogic.GetValidMovesForSide(board, side);
            return validMoves.Count > 0 ? validMoves[0] : null;
        }

        if (gameType == "breakthrough")
        {
            string side = gameLogic.GetPiece(aiPlayer).Value;
            var validMoves = BreakthroughLogic.GetValidMovesForSide(board, side);
            return validMoves.Count > 0 ? validMoves[0] : null;
        }

        if (gameType == "scrabble")
        {
            return WordMove.Pass;
        }

        if (gameType == "sudoku")
        {
            var fallback = new SmartSudokuPlayer(aiPlayer.Name);
            fallback.Bind((SudokuLogic)gameLogic);
            var fallbackMove = fallback.GetMove(board);
            return fallbackMove is SudokuMove && isMoveValid(aiPlayer, fallbackMove) ? fallbackMove : null;
        }

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                var loc = new Location(x, y);
                if (isMoveValid(aiPlayer, loc))
                {
                    return loc;
                }
            }
        }

        return null;
    }

    public static string GetInvalidMoveMessage(string gameType) => gameType switch
    {
        "checkers" => "Invalid move. Captures are mandatory when available!",
        "breakthrough" => "Invalid move. Pieces move 1 square forward or diagonally forward.",
        "scrabble" => "Invalid word placement. Check dictionary, center/connection, and rack tiles.",
        "wordle" => "Enter a valid 5-letter dictionary word.",
        "sudoku" => "Invalid digit placement. Conflicts with row/column/box or breaks solvability.",
        "mancala" => "Select a non-empty pit on your own side.",
        _ => "Invalid move."
    };
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
