namespace BoardGame.Web.Services;

using BoardGame.Library.Implementations;
using BoardGame.Library.Implementations.Checkers;
using BoardGame.Library.Implementations.Chomp;
using BoardGame.Library.Implementations.ConnectFour;
using BoardGame.Library.Implementations.Hex;
using BoardGame.Library.Implementations.Mancala;
using BoardGame.Library.Implementations.Minesweeper;
using BoardGame.Library.Implementations.Othello;
using BoardGame.Library.Implementations.Scrabble;
using BoardGame.Library.Implementations.Sudoku;
using BoardGame.Library.Implementations.TicTacToe;
using BoardGame.Library.Implementations.Wordle;
using BoardGame.Library.Interfaces;

public static class GameFactory
{
    public static bool IsSoloOnlyPuzzle(string gameType)
    {
        return gameType is "sudoku" or "wordle" or "minesweeper";
    }

    public static (string GameType, string Variant, IBoard Board, IGameLogic GameLogic) CreateGame(
        string? rawGameType,
        string mode,
        string aiDifficulty,
        string variant,
        int? customWidth = null,
        int? customHeight = null)
    {
        string rawKey = (!string.IsNullOrWhiteSpace(rawGameType) ? rawGameType : "connect4").Trim().ToLowerInvariant();
        string normVariant = (!string.IsNullOrWhiteSpace(variant) ? variant.Trim().ToLowerInvariant() : "standard");
        string gameType = rawKey;

        switch (rawKey)
        {
            case "gomoku":
            case "five-in-a-row":
                gameType = "tictactoe";
                if (normVariant == "standard") normVariant = "gomoku";
                customWidth ??= 15;
                customHeight ??= 15;
                break;
            case "nim":
                gameType = "chomp";
                normVariant = (normVariant == "misere" || normVariant == "nim_misere") ? "nim_misere" : "nim";
                break;
            case "breakthrough":
            case "bt":
                gameType = "checkers";
                normVariant = (normVariant == "misere" || normVariant == "breakthrough_misere") ? "breakthrough_misere" : "breakthrough";
                break;
            case "draughts":
            case "checker":
                gameType = "checkers";
                break;
            case "connectfour":
            case "connect-four":
            case "connect-4":
            case "c4":
                gameType = "connect4";
                break;
            case "ttt":
            case "tic-tac-toe":
            case "tic":
                gameType = "tictactoe";
                break;
            case "reversi":
                gameType = "othello";
                break;
            case "kalah":
                gameType = "mancala";
                break;
            case "wordguess":
                gameType = "wordle";
                break;
            case "mines":
                gameType = "minesweeper";
                break;
        }

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
                    TargetLength = normVariant == "connect5" ? 5 : 4,
                    IsMisere = normVariant == "misere"
                };
                break;
            case "chomp":
                bool isNim = normVariant == "nim" || normVariant == "nim_misere";
                if (isNim)
                {
                    board = new Board(7, 4);
                    gameLogic = new ChompLogic
                    {
                        IsRowOnlyNim = true,
                        IsMisere = normVariant == "nim_misere"
                    };
                }
                else
                {
                    int chompWidth = customWidth is >= 2 and <= 10 ? customWidth.Value : 5;
                    int chompHeight = customHeight is >= 2 and <= 10 ? customHeight.Value : 4;
                    board = new Board(chompWidth, chompHeight);
                    gameLogic = new ChompLogic
                    {
                        IsRowOnlyNim = false,
                        IsMisere = normVariant != "normal"
                    };
                }
                break;
            case "othello":
                board = new Board(8, 8);
                gameLogic = new OthelloLogic
                {
                    FewerDiscsWins = normVariant == "reversed"
                };
                break;
            case "hex":
                int hexSize = customWidth is >= 5 and <= 11 ? customWidth.Value : 7;
                board = new Board(hexSize, hexSize);
                gameLogic = new HexLogic
                {
                    IsMisere = normVariant == "misere"
                };
                break;
            case "checkers":
                bool isBreakthrough = normVariant == "breakthrough" || normVariant == "breakthrough_misere";
                int chkSize = isBreakthrough && customWidth is >= 6 and <= 10
                    ? customWidth.Value
                    : CheckersLogic.DefaultSize;
                board = new Board(chkSize, chkSize);
                gameLogic = new CheckersLogic
                {
                    MandatoryCapture = normVariant != "casual",
                    IsGiveaway = normVariant == "giveaway" || normVariant == "breakthrough_misere",
                    IsBreakthrough = isBreakthrough
                };
                break;
            case "scrabble":
                board = new Board(ScrabbleLogic.DefaultSize, ScrabbleLogic.DefaultSize);
                gameLogic = new ScrabbleLogic
                {
                    IsShortBag = normVariant == "blitz",
                    DoubleBonuses = normVariant == "double_bonus"
                };
                break;
            case "sudoku":
                board = new Board(SudokuLogic.DefaultSize, SudokuLogic.DefaultSize);
                bool isDaily = mode == "daily";
                int emptyCount = normVariant switch
                {
                    "easy_puzzle" => 28,
                    "hard_puzzle" => 50,
                    _ => isDaily ? 40 : (aiDifficulty == "easy" ? 32 : 42)
                };
                gameLogic = isDaily
                    ? SudokuLogic.CreateDaily()
                    : new SudokuLogic(seed: null, emptyCells: emptyCount);
                break;
            case "mancala":
                board = new Board(MancalaLogic.PitCountPerSide, 2);
                gameLogic = new MancalaLogic
                {
                    InitialStonesPerPit = normVariant == "quick" ? 3 : 4,
                    IsMisere = normVariant == "misere"
                };
                break;
            case "wordle":
                board = new Board(WordleLogic.WordLength, WordleLogic.MaxGuesses);
                gameLogic = mode == "daily"
                    ? WordleLogic.CreateDaily()
                    : new WordleLogic();
                if (gameLogic is WordleLogic wl)
                {
                    wl.AllowAnyLetters = normVariant == "any_word";
                }
                break;
            case "minesweeper":
                int msW = customWidth is >= 6 and <= 12 ? customWidth.Value : MinesweeperLogic.DefaultSize;
                int msH = customHeight is >= 6 and <= 12 ? customHeight.Value : msW;
                int defaultMines = msW <= 6 ? 5 : (msW >= 12 ? 22 : MinesweeperLogic.DefaultMines);
                int mines = normVariant == "hard_puzzle"
                    ? (int)Math.Round(defaultMines * 1.5)
                    : (normVariant == "easy_puzzle" ? Math.Max(3, defaultMines / 2) : defaultMines);
                board = new Board(msW, msH);
                gameLogic = mode == "daily"
                    ? new MinesweeperLogic(MinesweeperLogic.GetDailySeed(), mines)
                    : new MinesweeperLogic(null, mines);
                break;
            case "tictactoe":
            default:
                gameType = "tictactoe";
                int defaultSize = (normVariant == "gomoku" || normVariant == "exact5") ? 15 : 3;
                int tttSize = customWidth is >= 3 and <= 19 ? customWidth.Value : defaultSize;
                board = new Board(tttSize, tttSize);
                gameLogic = new TicTacToeLogic
                {
                    TargetLength = (normVariant == "gomoku" && tttSize >= 5) ? 5 : 0,
                    ExactTargetOnly = normVariant == "exact5",
                    IsMisere = normVariant == "misere"
                };
                break;
        }

        return (gameType, normVariant, board, gameLogic);
    }

    public static IPlayer CreateAiPlayer(string gameType, string difficulty, string name, IGameLogic? gameLogic = null)
    {
        if (difficulty != "smart")
        {
            return new RandomAiPlayer(name, gameLogic);
        }

        return gameType switch
        {
            "connect4" => new ConnectFourPlayer(name),
            "chomp" => new SmartChompPlayer(name),
            "othello" => new SmartOthelloPlayer(name),
            "hex" => new SmartHexPlayer(name),
            "checkers" => new SmartCheckersPlayer(name),
            "scrabble" => new SmartScrabblePlayer(name),
            "mancala" => new SmartMancalaPlayer(name),
            "tictactoe" => new MinimaxPlayer(name),
            _ => new RandomAiPlayer(name, gameLogic)
        };
    }

    public static IMove? SelectFallbackAiMove(
        string gameType,
        IBoard board,
        IGameLogic gameLogic,
        IPlayer aiPlayer,
        Func<IPlayer, IMove, bool> isMoveValid)
    {
        if (gameType == "checkers" && gameLogic is CheckersLogic cl)
        {
            string side = gameLogic.GetPiece(aiPlayer).Value;
            var validMoves = CheckersLogic.GetValidMovesForSide(board, side, cl.MandatoryCapture, cl.IsBreakthrough);
            return validMoves.Count > 0 ? validMoves[0] : null;
        }

        if (gameType == "scrabble")
        {
            return WordMove.Pass;
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

    public static string GetInvalidMoveMessage(string gameType, string variant = "standard") => gameType switch
    {
        "checkers" => variant is "breakthrough" or "breakthrough_misere"
            ? "Invalid move. Pawns move 1 square forward or diagonally forward."
            : "Invalid move. Captures are mandatory when available!",
        "scrabble" => "Invalid word placement. Check dictionary, center/connection, and rack tiles.",
        "wordle" => variant == "any_word"
            ? "Enter any 5 letters (A-Z)."
            : "Not in word list. Enter a valid 5-letter English word.",
        "sudoku" => "Invalid digit placement. Conflicts with row/column/box or breaks solvability.",
        "mancala" => "Select a non-empty pit on your own side.",
        _ => "Invalid move."
    };
}
