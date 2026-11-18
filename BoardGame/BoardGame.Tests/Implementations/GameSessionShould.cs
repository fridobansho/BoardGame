namespace BoardGame.Tests.Implementations;

using System.Collections.Generic;
using System.Linq;
using BoardGame.Library.Implementations;
using BoardGame.Library.Implementations.Checkers;
using BoardGame.Library.Implementations.Scrabble;
using BoardGame.Library.Implementations.Sudoku;
using BoardGame.Library.Implementations.Wordle;
using BoardGame.Library.Interfaces;
using BoardGame.Web.Models;
using BoardGame.Web.Services;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class GameSessionShould
{
    [Test]
    public void InitializeConnectFourCorrectly()
    {
        var session = new GameSession("connect4", "pass-and-play", "smart", "Alice", "Bob");

        session.Board.Width.ShouldBe(7);
        session.Board.Height.ShouldBe(6);
        session.Status.ShouldBe("InProgress");
        session.CurrentTurnIndex.ShouldBe(0);
        session.Players[0].Name.ShouldBe("Alice");
        session.Players[1].Name.ShouldBe("Bob");
    }

    [Test]
    public void InitializeTicTacToeCorrectly()
    {
        var session = new GameSession("tictactoe", "pass-and-play", "smart", "Alice", "Bob");

        session.Board.Width.ShouldBe(3);
        session.Board.Height.ShouldBe(3);
        session.Status.ShouldBe("InProgress");
        session.CurrentTurnIndex.ShouldBe(0);
    }

    [Test]
    public void InitializeChompCorrectly()
    {
        var session = new GameSession("chomp", "pass-and-play", "smart", "Alice", "Bob");

        session.Board.Width.ShouldBe(5);
        session.Board.Height.ShouldBe(4);
        session.Board.PieceAt(0, 0).Value.ShouldBe("P"); // Poison
        session.Board.PieceAt(1, 1).Value.ShouldBe("C"); // Chocolate
        session.Status.ShouldBe("InProgress");
    }

    [Test]
    public void AlternateTurnsInPassAndPlay()
    {
        var session = new GameSession("tictactoe", "pass-and-play", "smart", "Alice", "Bob");

        var (s1, err1) = session.MakeMove(0, 0);
        s1.ShouldBeTrue();
        err1.ShouldBeNull();
        session.CurrentTurnIndex.ShouldBe(1);
        session.Board.PieceAt(0, 0).Value.ShouldBe("X");

        var (s2, err2) = session.MakeMove(1, 1);
        s2.ShouldBeTrue();
        err2.ShouldBeNull();
        session.CurrentTurnIndex.ShouldBe(0);
        session.Board.PieceAt(1, 1).Value.ShouldBe("O");
    }

    [Test]
    public void RejectInvalidMove()
    {
        var session = new GameSession("tictactoe", "pass-and-play", "smart", "Alice", "Bob");

        session.MakeMove(0, 0);
        var (success, error) = session.MakeMove(0, 0); // Already occupied

        success.ShouldBeFalse();
        error.ShouldNotBeNull();
        session.CurrentTurnIndex.ShouldBe(1); // Turn not changed
    }

    [Test]
    public void DetectWinInTicTacToe()
    {
        var session = new GameSession("tictactoe", "pass-and-play", "smart", "Alice", "Bob");

        // Alice: (0,0), (1,0), (2,0) -> top row win
        session.MakeMove(0, 0); // Alice
        session.MakeMove(0, 1); // Bob
        session.MakeMove(1, 0); // Alice
        session.MakeMove(1, 1); // Bob
        session.MakeMove(2, 0); // Alice wins!

        session.Status.ShouldBe("Finished");
        session.Winner.ShouldBe("Alice");
    }

    [Test]
    public void EndChompWhenPoisonEaten()
    {
        var session = new GameSession("chomp", "pass-and-play", "smart", "Alice", "Bob");

        // Alice eats poison block directly at (0, 0) -> Alice loses, Bob wins!
        var (s, _) = session.MakeMove(0, 0);
        s.ShouldBeTrue();
        session.Status.ShouldBe("Finished");
        session.Winner.ShouldBe("Bob");
    }

    [Test]
    public void TriggerAiMoveInVsAiMode()
    {
        var session = new GameSession("tictactoe", "vs-ai", "smart", "Human", null);

        session.MakeMove(0, 0); // Human moves

        // In vs-ai mode, AI moves immediately in the same call
        session.MoveCount.ShouldBe(2);
        session.CurrentTurnIndex.ShouldBe(0); // Returned to human
    }

    private static SessionManager CreateIsolatedSessionManager()
    {
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"boardgame_test_{System.Guid.NewGuid():N}.db");
        return new SessionManager(dbPath);
    }

    [Test]
    public void ManageRoomsCorrectly()
    {
        using var sm = CreateIsolatedSessionManager();
        var session = sm.CreateSession("connect4", "room", "smart", "Host", null);

        session.Status.ShouldBe("WaitingForPlayer");
        session.RoomCode.ShouldNotBeNullOrEmpty();

        var retrieved = sm.GetSessionByRoomCode(session.RoomCode);
        retrieved.ShouldBeSameAs(session);

        // Guest joins
        var joined = session.JoinRoomAsGuest("GuestFriend", out var guestToken);
        joined.ShouldBeTrue();
        guestToken.ShouldNotBeNullOrEmpty();
        session.Status.ShouldBe("InProgress");
        session.Players[1].Name.ShouldBe("GuestFriend");
    }

    [Test]
    public void ListAvailablePublicRooms()
    {
        using var sm = CreateIsolatedSessionManager();
        var publicRoom = sm.CreateSession("connect4", "room", "smart", "HostAlice", null, isPublic: true);
        var privateRoom = sm.CreateSession("tictactoe", "room", "smart", "HostBob", null, isPublic: false);
        var soloSession = sm.CreateSession("chomp", "pass-and-play", "smart", "Solo", null, isPublic: true);

        var rooms = sm.GetAvailableRooms().ToList();

        // Both publicRoom and soloSession are public
        rooms.Count.ShouldBe(2);
        
        var room1 = rooms.First(r => r.SessionId == publicRoom.Id);
        room1.RoomCode.ShouldBe(publicRoom.RoomCode);
        room1.GameType.ShouldBe("connect4");
        room1.HostName.ShouldBe("HostAlice");
        room1.Status.ShouldBe("WaitingForPlayer");
        room1.CanJoin.ShouldBeTrue();
        room1.CanSpectate.ShouldBeTrue();

        var room2 = rooms.First(r => r.SessionId == soloSession.Id);
        room2.GameType.ShouldBe("chomp");
        room2.CanJoin.ShouldBeFalse();
        room2.CanSpectate.ShouldBeTrue();

        // Once guest joins the room, it is in progress: CanJoin is false, CanSpectate remains true
        publicRoom.JoinRoomAsGuest("Guest", out _);
        var updatedRooms = sm.GetAvailableRooms().ToList();
        var updatedRoom1 = updatedRooms.First(r => r.SessionId == publicRoom.Id);
        updatedRoom1.CanJoin.ShouldBeFalse();
        updatedRoom1.CanSpectate.ShouldBeTrue();
    }

    [Test]
    public void RejectSpectatorMoves()
    {
        using var sm = CreateIsolatedSessionManager();
        var session = sm.CreateSession("connect4", "room", "smart", "Host", null, isPublic: true);
        session.JoinRoomAsGuest("Guest", out var guestToken);

        // Host moves first (valid)
        var (p1Success, _) = session.MakeMove(0, 0, session.Player1Token);
        p1Success.ShouldBeTrue();

        // Spectator tries to move (invalid)
        var (specSuccess, specErr) = session.MakeMove(1, 0, "spectator");
        specSuccess.ShouldBeFalse();
        specErr.ShouldBe("Spectators cannot make moves.");

        // Random non-player token tries to move in vs-ai
        var aiSession = sm.CreateSession("tictactoe", "vs-ai", "smart", "Human", null, isPublic: true);
        var (aiSpecSuccess, aiSpecErr) = aiSession.MakeMove(0, 0, "spectator");
        aiSpecSuccess.ShouldBeFalse();
        aiSpecErr.ShouldBe("Spectators cannot make moves.");
    }

    [Test]
    public void ResetGameProperly()
    {
        var session = new GameSession("tictactoe", "pass-and-play", "smart", "Alice", "Bob");
        session.MakeMove(0, 0);
        session.MoveCount.ShouldBe(1);

        session.Reset();

        session.MoveCount.ShouldBe(0);
        session.CurrentTurnIndex.ShouldBe(0);
        session.Board.PieceAt(0, 0).Value.ShouldBe(BlankPiece.BlankValue);
    }

    [Test]
    public void SupportUndoInPassAndPlay()
    {
        var session = new GameSession("tictactoe", "pass-and-play", "smart", "Alice", "Bob");
        session.MakeMove(0, 0);
        session.Board.PieceAt(0, 0).Value.ShouldBe("X");
        session.MoveCount.ShouldBe(1);
        session.CurrentTurnIndex.ShouldBe(1);

        var (success, err) = session.Undo();
        success.ShouldBeTrue();
        err.ShouldBeNull();
        session.MoveCount.ShouldBe(0);
        session.CurrentTurnIndex.ShouldBe(0);
        session.Board.PieceAt(0, 0).Value.ShouldBe(BlankPiece.BlankValue);
    }

    [Test]
    public void SupportUndoInVsAiMode()
    {
        var session = new GameSession("tictactoe", "vs-ai", "smart", "Human", null);
        session.MakeMove(0, 0); // Human moves, then AI moves
        session.MoveCount.ShouldBe(2);

        var (success, _) = session.Undo();
        success.ShouldBeTrue();
        session.MoveCount.ShouldBe(0);
        session.CurrentTurnIndex.ShouldBe(0);
        session.Board.PieceAt(0, 0).Value.ShouldBe(BlankPiece.BlankValue);
    }

    [Test]
    public void TrackScoreAndAlternateStartsInRematch()
    {
        var session = new GameSession("tictactoe", "pass-and-play", "smart", "Alice", "Bob");
        // Alice wins
        session.MakeMove(0, 0); // Alice
        session.MakeMove(0, 1); // Bob
        session.MakeMove(1, 0); // Alice
        session.MakeMove(1, 1); // Bob
        session.MakeMove(2, 0); // Alice wins!

        session.Status.ShouldBe("Finished");
        session.Player1Score.ShouldBe(1);
        session.Player2Score.ShouldBe(0);

        // Rematch (Game 2)
        session.Reset(isRematch: true);
        session.GameNumber.ShouldBe(2);
        session.CurrentTurnIndex.ShouldBe(1); // Bob goes first in Game 2!
        session.MoveCount.ShouldBe(0);
        session.Status.ShouldBe("InProgress");
    }

    [Test]
    public void TrackLastMoveCoordinatesCorrectly()
    {
        var session = new GameSession("connect4", "pass-and-play", "smart", "Alice", "Bob");
        session.MakeMove(3, 0); // Drop in column 3 -> lands at row 5 (bottom)

        session.LastMoveX.ShouldBe(3);
        session.LastMoveY.ShouldBe(5);
    }

    [Test]
    public void SupportCustomGridSizesAcrossAllGames()
    {
        // 4x4 Tic-Tac-Toe
        var ttt4 = new GameSession("tictactoe", "pass-and-play", "smart", "Alice", "Bob", customWidth: 4, customHeight: 4);
        ttt4.Board.Width.ShouldBe(4);
        ttt4.Board.Height.ShouldBe(4);

        // 5x5 Tic-Tac-Toe
        var ttt5 = new GameSession("tictactoe", "pass-and-play", "smart", "Alice", "Bob", customWidth: 5, customHeight: 5);
        ttt5.Board.Width.ShouldBe(5);
        ttt5.Board.Height.ShouldBe(5);

        // 8x7 Connect Four
        var c4 = new GameSession("connect4", "pass-and-play", "smart", "Alice", "Bob", customWidth: 8, customHeight: 7);
        c4.Board.Width.ShouldBe(8);
        c4.Board.Height.ShouldBe(7);

        // 6x5 Chomp
        var chomp = new GameSession("chomp", "pass-and-play", "smart", "Alice", "Bob", customWidth: 6, customHeight: 5);
        chomp.Board.Width.ShouldBe(6);
        chomp.Board.Height.ShouldBe(5);
    }

    [Test]
    public void RecordMoveHistoryStepByStep()
    {
        var session = new GameSession("tictactoe", "pass-and-play", "smart", "Alice", "Bob");
        session.MoveHistory.Count.ShouldBe(1); // Game start (move 0)
        session.MoveHistory[0].MoveNumber.ShouldBe(0);

        session.MakeMove(0, 0);
        session.MoveHistory.Count.ShouldBe(2);
        session.MoveHistory[1].MoveNumber.ShouldBe(1);
        session.MoveHistory[1].X.ShouldBe(0);
        session.MoveHistory[1].Y.ShouldBe(0);
        session.MoveHistory[1].PlayerName.ShouldBe("Alice");

        session.MakeMove(1, 1);
        session.MoveHistory.Count.ShouldBe(3);
        session.MoveHistory[2].MoveNumber.ShouldBe(2);
        session.MoveHistory[2].PlayerName.ShouldBe("Bob");
    }

    [Test]
    public void HandleTurnTimerAndPredictions()
    {
        var session = new GameSession("tictactoe", "pass-and-play", "smart", "Alice", "Bob", timerSeconds: 15);
        session.TimerSeconds.ShouldBe(15);
        session.TurnExpiresAt.ShouldNotBeNull();

        // Predictions
        session.AddPrediction(0);
        session.AddPrediction(0);
        session.AddPrediction(1);

        session.Player1Predictions.ShouldBe(2);
        session.Player2Predictions.ShouldBe(1);
    }

    [Test]
    public void AllowBothHostAndGuestToMakeMovesInRoom()
    {
        using var sm = CreateIsolatedSessionManager();
        var session = sm.CreateSession("tictactoe", "room", "smart", "Host", null);

        var joined = session.JoinRoomAsGuest("GuestFriend", out var guestToken);
        joined.ShouldBeTrue();

        var (p1Ok, p1Err) = session.MakeMove(0, 0, session.Player1Token);
        p1Ok.ShouldBeTrue();
        p1Err.ShouldBeNull();
        session.Board.PieceAt(0, 0).Value.ShouldBe("X");

        var (p2Ok, p2Err) = session.MakeMove(1, 1, guestToken);
        p2Ok.ShouldBeTrue();
        p2Err.ShouldBeNull();
        session.Board.PieceAt(1, 1).Value.ShouldBe("O");
    }

    [Test]
    public void DetectDrawInTicTacToe()
    {
        var session = new GameSession("tictactoe", "pass-and-play", "smart", "Alice", "Bob");

        // X | O | X
        // X | O | O
        // O | X | X
        session.MakeMove(0, 0); // X
        session.MakeMove(1, 0); // O
        session.MakeMove(2, 0); // X
        session.MakeMove(1, 1); // O
        session.MakeMove(0, 1); // X
        session.MakeMove(2, 1); // O
        session.MakeMove(1, 2); // X
        session.MakeMove(0, 2); // O
        session.MakeMove(2, 2); // X -> Draw!

        session.Status.ShouldBe("Draw");
        session.Winner.ShouldBeNull();
        session.DrawScore.ShouldBe(1);
        session.Player1Score.ShouldBe(0);
        session.Player2Score.ShouldBe(0);
    }

    [Test]
    public void ResetAndUndoChompAfterPoisonEaten()
    {
        var session = new GameSession("chomp", "pass-and-play", "smart", "Alice", "Bob");

        // Alice eats poison -> Bob wins
        session.MakeMove(0, 0);
        session.Status.ShouldBe("Finished");

        // Undo poison move and play non-poison move -> game stays InProgress
        var (undoOk, _) = session.Undo();
        undoOk.ShouldBeTrue();
        session.Status.ShouldBe("InProgress");
        var (afterUndoOk, _) = session.MakeMove(1, 1);
        afterUndoOk.ShouldBeTrue();
        session.Status.ShouldBe("InProgress");

        // Alice eats poison again -> game finishes
        session.MakeMove(0, 0);
        session.Status.ShouldBe("Finished");

        // Rematch reset -> non-poison move keeps game InProgress
        session.Reset(isRematch: true);
        session.Status.ShouldBe("InProgress");
        var (rematchMoveOk, _) = session.MakeMove(1, 1);
        rematchMoveOk.ShouldBeTrue();
        session.Status.ShouldBe("InProgress");
    }

    [Test]
    public void SupportCheckersPieceMoveAndVsAiMode()
    {
        var session = new GameSession("checkers", "pass-and-play", "smart", "Alice", "Bob");

        session.Board.Width.ShouldBe(8);
        session.Board.Height.ShouldBe(8);
        session.Board.PieceAt(0, 5).Value.ShouldBe(RedCheckerPiece.Red.Value);
        session.Board.PieceAt(1, 2).Value.ShouldBe(BlackCheckerPiece.Black.Value);

        var (ok, err) = session.MakeMove(0, 5, toX: 1, toY: 4);
        ok.ShouldBeTrue();
        err.ShouldBeNull();
        session.Board.PieceAt(0, 5).Value.ShouldBe(BlankPiece.BlankValue);
        session.Board.PieceAt(1, 4).Value.ShouldBe(RedCheckerPiece.Red.Value);
        session.CurrentTurnIndex.ShouldBe(1);

        var aiSession = new GameSession("checkers", "vs-ai", "smart", "Human", null);
        var (aiOk, aiErr) = aiSession.MakeMove(0, 5, toX: 1, toY: 4);
        aiOk.ShouldBeTrue();
        aiErr.ShouldBeNull();
        aiSession.MoveCount.ShouldBe(2);
        aiSession.CurrentTurnIndex.ShouldBe(0);
    }

    [Test]
    public void SupportScrabbleWordMoveRackScoresAndPass()
    {
        var session = new GameSession("scrabble", "pass-and-play", "smart", "Alice", "Bob");

        var initialDto = session.ToDto();
        initialDto.Width.ShouldBe(15);
        initialDto.Height.ShouldBe(15);
        initialDto.TileBagCount.ShouldBe(84);
        initialDto.Player1Rack.Count.ShouldBe(7);
        initialDto.Player2Rack.Count.ShouldBe(7);
        initialDto.BonusGrid![7][7].ShouldBe("DW");

        var logic = (ScrabbleLogic)session.GameLogic;
        logic.SetRack(session.Players[0], new[] { 'C', 'A', 'T', 'S', 'E', 'R', 'D' });

        var placements = new List<TilePlacementDto>
        {
            new(7, 7, "C"),
            new(8, 7, "A"),
            new(9, 7, "T")
        };

        var (ok, err) = session.MakeMove(7, 7, placements: placements);
        ok.ShouldBeTrue();
        err.ShouldBeNull();

        var afterWordDto = session.ToDto();
        afterWordDto.Player1GamePoints.ShouldBe(10);
        afterWordDto.CurrentTurnIndex.ShouldBe(1);
        session.Board.PieceAt(7, 7).Value.ShouldBe("C");

        var (p2PassOk, _) = session.MakeMove(0, 0, isPass: true);
        p2PassOk.ShouldBeTrue();
        var (p1PassOk, _) = session.MakeMove(0, 0, isPass: true);
        p1PassOk.ShouldBeTrue();

        session.Status.ShouldBe("Finished");
        session.Winner.ShouldBe("Alice");
    }

    [Test]
    public void TriggerScrabbleAiResponseInVsAiMode()
    {
        var session = new GameSession("scrabble", "vs-ai", "smart", "Human", null);
        var logic = (ScrabbleLogic)session.GameLogic;
        logic.SetRack(session.Players[0], new[] { 'C', 'A', 'T', 'S', 'E', 'R', 'D' });

        var (ok, err) = session.MakeMove(7, 7, placements: new List<TilePlacementDto>
        {
            new(7, 7, "C"),
            new(8, 7, "A"),
            new(9, 7, "T")
        });

        ok.ShouldBeTrue();
        err.ShouldBeNull();
        session.MoveCount.ShouldBe(2);
        session.CurrentTurnIndex.ShouldBe(0);
    }

    [Test]
    public void SupportSudokuDailyAndVsAiModes()
    {
        var dailySession = new GameSession("sudoku", "daily", "smart", "Alice", null);
        dailySession.Board.Width.ShouldBe(9);
        dailySession.Board.Height.ShouldBe(9);
        var dailyLogic = (SudokuLogic)dailySession.GameLogic;

        int emptyX = -1, emptyY = -1;
        for (int y = 0; y < 9 && emptyX == -1; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                if (SudokuLogic.GetDigitAt(dailySession.Board, x, y) == 0)
                {
                    emptyX = x;
                    emptyY = y;
                    break;
                }
            }
        }

        int digit = dailyLogic.GetSolutionAt(emptyX, emptyY);
        var (ok, err) = dailySession.MakeMove(emptyX, emptyY, digit: digit);
        ok.ShouldBeTrue();
        err.ShouldBeNull();
        dailySession.CurrentTurnIndex.ShouldBe(0); // Daily stays on Player 1
        dailySession.ToDto().Player1GamePoints.ShouldBeGreaterThanOrEqualTo(10);

        var aiSession = new GameSession("sudoku", "vs-ai", "smart", "Human", null);
        var aiLogic = (SudokuLogic)aiSession.GameLogic;
        int ex = -1, ey = -1;
        for (int y = 0; y < 9 && ex == -1; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                if (SudokuLogic.GetDigitAt(aiSession.Board, x, y) == 0)
                {
                    ex = x;
                    ey = y;
                    break;
                }
            }
        }
        var (aiOk, aiErr) = aiSession.MakeMove(ex, ey, digit: aiLogic.GetSolutionAt(ex, ey));
        aiOk.ShouldBeTrue();
        aiErr.ShouldBeNull();
        aiSession.MoveCount.ShouldBe(2);
        aiSession.CurrentTurnIndex.ShouldBe(0);
    }

    [Test]
    public void RecordAndQueryLeaderboardsAcrossMultipleGames()
    {
        using var sm = CreateIsolatedSessionManager();

        // 1. Finish a Tic-Tac-Toe game
        var ttt = sm.CreateSession("tictactoe", "pass-and-play", "smart", "Alice", "Bob");
        ttt.MakeMove(0, 0);
        ttt.MakeMove(0, 1);
        ttt.MakeMove(1, 0);
        ttt.MakeMove(1, 1);
        ttt.MakeMove(2, 0); // Alice wins
        ttt.Status.ShouldBe("Finished");

        // 2. Finish a Scrabble game
        var scrabble = sm.CreateSession("scrabble", "pass-and-play", "smart", "Alice", "Bob");
        var sl = (ScrabbleLogic)scrabble.GameLogic;
        sl.SetRack(scrabble.Players[0], new[] { 'C', 'A', 'T', 'S', 'E', 'R', 'D' });
        scrabble.MakeMove(7, 7, placements: new List<TilePlacementDto>
        {
            new(7, 7, "C"),
            new(8, 7, "A"),
            new(9, 7, "T")
        });
        scrabble.MakeMove(0, 0, isPass: true);
        scrabble.MakeMove(0, 0, isPass: true); // Alice wins with 10 points
        scrabble.Status.ShouldBe("Finished");

        // 3. Finish a Daily Sudoku puzzle
        var sudoku = sm.CreateSession("sudoku", "daily", "smart", "Charlie", null);
        var sdl = (SudokuLogic)sudoku.GameLogic;
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                if (SudokuLogic.GetDigitAt(sudoku.Board, x, y) == 0)
                {
                    sudoku.MakeMove(x, y, digit: sdl.GetSolutionAt(x, y));
                }
            }
        }
        sudoku.Status.ShouldBe("Finished");
        sudoku.Winner.ShouldBe("Charlie");

        // Query per-game and global leaderboards
        var allEntries = sm.GetLeaderboard("all", "all");
        allEntries.Count.ShouldBe(3);

        var scrabbleEntries = sm.GetLeaderboard("scrabble", "all");
        scrabbleEntries.Count.ShouldBe(1);
        scrabbleEntries[0].PlayerName.ShouldBe("Alice");
        scrabbleEntries[0].Score.ShouldBe(10);

        var dailySudoku = sm.GetLeaderboard("sudoku", "daily");
        dailySudoku.Count.ShouldBe(1);
        dailySudoku[0].PlayerName.ShouldBe("Charlie");
        dailySudoku[0].Score.ShouldBeGreaterThan(400);

        var standings = sm.GetPlayerStandings("all");
        standings.Count.ShouldBe(2);
        standings[0].PlayerName.ShouldBe("Alice");
        standings[0].Wins.ShouldBe(2);
    }

    [Test]
    public void SupportMisereTicTacToeVariant()
    {
        var session = new GameSession("tictactoe", "pass-and-play", "smart", "Alice", "Bob", variant: "misere");
        session.Variant.ShouldBe("misere");
        session.ToDto().Variant.ShouldBe("misere");

        // Alice completes row 0 -> loses under Misere rules, Bob wins
        session.MakeMove(0, 0).Success.ShouldBeTrue();
        session.MakeMove(0, 1).Success.ShouldBeTrue();
        session.MakeMove(1, 0).Success.ShouldBeTrue();
        session.MakeMove(1, 1).Success.ShouldBeTrue();
        session.MakeMove(2, 0).Success.ShouldBeTrue();

        session.Status.ShouldBe("Finished");
        session.Winner.ShouldBe("Bob");
    }

    [Test]
    public void SupportMisereNimVariant()
    {
        var session = new GameSession("nim", "pass-and-play", "smart", "Alice", "Bob", variant: "misere");
        session.Variant.ShouldBe("misere");

        // Standard 1-3-5-7 heaps: players clear rows 0..3; Bob takes last stone so Alice wins
        session.MakeMove(0, 0).Success.ShouldBeTrue();
        session.MakeMove(0, 1).Success.ShouldBeTrue();
        session.MakeMove(0, 2).Success.ShouldBeTrue();
        session.MakeMove(0, 3).Success.ShouldBeTrue();

        session.Status.ShouldBe("Finished");
        session.Winner.ShouldBe("Alice");
    }

    [Test]
    public void SupportCasualCheckersVariantAllowsOptionalCaptures()
    {
        var standard = new GameSession("checkers", "pass-and-play", "smart", "Alice", "Bob", variant: "standard");
        standard.MakeMove(2, 5, toX: 3, toY: 4).Success.ShouldBeTrue();
        standard.MakeMove(1, 2, toX: 2, toY: 3).Success.ShouldBeTrue();
        // Capture (3,4)->(1,2) is available, so non-capture move (4,5)->(5,4) fails in standard
        standard.MakeMove(4, 5, toX: 5, toY: 4).Success.ShouldBeFalse();

        var casual = new GameSession("checkers", "pass-and-play", "smart", "Alice", "Bob", variant: "casual");
        casual.Variant.ShouldBe("casual");
        casual.MakeMove(2, 5, toX: 3, toY: 4).Success.ShouldBeTrue();
        casual.MakeMove(1, 2, toX: 2, toY: 3).Success.ShouldBeTrue();
        casual.MakeMove(4, 5, toX: 5, toY: 4).Success.ShouldBeTrue();
    }

    [Test]
    public void SupportBlitzScrabbleVariantShortensTileBag()
    {
        var standard = new GameSession("scrabble", "pass-and-play", "smart", "Alice", "Bob", variant: "standard");
        var blitz = new GameSession("scrabble", "pass-and-play", "smart", "Alice", "Bob", variant: "blitz");

        standard.ToDto().TileBagCount.ShouldBe(84);
        blitz.ToDto().Variant.ShouldBe("blitz");
        blitz.ToDto().TileBagCount.ShouldBe(22);
    }

    [Test]
    public void SupportSoloSudokuModeAndPersistScoredCustomState()
    {
        var solo = new GameSession("sudoku", "solo", "smart", "Alice", null);
        solo.Mode.ShouldBe("solo");
        solo.Players[1].Name.ShouldBe("Solo");
        (solo.GameLogic is IScoredGameLogic).ShouldBeTrue();
        var scored = (IScoredGameLogic)solo.GameLogic;
        var sudokuLogic = (SudokuLogic)solo.GameLogic;

        int moveX = -1;
        int moveY = -1;
        for (int y = 0; y < 9 && moveX == -1; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                if (!SudokuLogic.IsGivenCell(solo.Board, x, y))
                {
                    moveX = x;
                    moveY = y;
                    break;
                }
            }
        }

        int digit = sudokuLogic.GetSolutionAt(moveX, moveY);
        solo.MakeMove(moveX, moveY, digit: digit).Success.ShouldBeTrue();
        solo.CurrentTurnIndex.ShouldBe(0);
        scored.GetScore(solo.Players[0]).ShouldBeGreaterThan(0);
        solo.ToDto().Player1GamePoints.ShouldBe(scored.GetScore(solo.Players[0]));

        // Scrabble custom state & undo/persistence round-trip
        var scrabble = new GameSession("scrabble", "pass-and-play", "smart", "Alice", "Bob");
        (scrabble.GameLogic is ICustomStateGameLogic).ShouldBeTrue();
        (scrabble.GameLogic is IScoredGameLogic).ShouldBeTrue();
        scrabble.MakeMove(0, 0, isPass: true).Success.ShouldBeTrue();
        var persisted = scrabble.ExportPersistedState();
        persisted.CustomState.ShouldNotBeNull();
        persisted.CustomState!["consecutivePasses"].ShouldBe("1");

        var restored = GameSession.FromPersistedState(persisted);
        ((ScrabbleLogic)restored.GameLogic).ConsecutivePasses.ShouldBe(1);
        scrabble.Undo().Success.ShouldBeTrue();
        ((ScrabbleLogic)scrabble.GameLogic).ConsecutivePasses.ShouldBe(0);

        // Checkers custom state & payload-shape PieceMove round-trip
        var checkers = new GameSession("checkers", "pass-and-play", "smart", "Alice", "Bob");
        (checkers.GameLogic is ICustomStateGameLogic).ShouldBeTrue();
        checkers.MakeMove(2, 5, toX: 3, toY: 4).Success.ShouldBeTrue();
        var checkersPersisted = checkers.ExportPersistedState();
        checkersPersisted.CustomState!["noProgressMoves"].ShouldBe("1");
        var checkersRestored = GameSession.FromPersistedState(checkersPersisted);
        ((CheckersLogic)checkersRestored.GameLogic).MovesWithoutCaptureOrPromotion.ShouldBe(1);
    }

    [Test]
    public void PlayBreakthroughAndAiVsAiExhibitionToCompletion()
    {
        var session = new GameSession("breakthrough", "pass-and-play", "smart", "Alice", "Bob");
        session.Board.Width.ShouldBe(8);
        session.Board.Height.ShouldBe(8);
        session.MakeMove(0, 6, toX: 0, toY: 5).Success.ShouldBeTrue();
        session.Board.PieceAt(0, 5).Value.ShouldBe("W");

        var aiMatch = new GameSession("breakthrough", "ai-vs-ai", "smart", "Bot", null);
        aiMatch.Mode.ShouldBe("ai-vs-ai");
        aiMatch.MakeMove(0, 6, toX: 0, toY: 5).Success.ShouldBeFalse();

        int steps = 0;
        while (aiMatch.Status == "InProgress" && steps++ < 400)
        {
            aiMatch.StepAiMove().Success.ShouldBeTrue();
        }
        aiMatch.Status.ShouldBe("Finished");
        aiMatch.Winner.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void PlayMancalaWithExtraTurnInStore()
    {
        var session = new GameSession("mancala", "pass-and-play", "smart", "Alice", "Bob");
        session.Board.Width.ShouldBe(6);
        session.Board.Height.ShouldBe(2);

        // Pit x=2 on P1 row (y=1) has 4 stones -> lands in P1 Store (index 6) for extra turn
        session.MakeMove(2, 1).Success.ShouldBeTrue();
        session.CurrentTurnIndex.ShouldBe(0);
        session.ToDto().Player1GamePoints.ShouldBe(1);

        // Next move from pit x=0 (4 stones -> lands at x=4, no extra turn) advances to Bob
        session.MakeMove(0, 1).Success.ShouldBeTrue();
        session.CurrentTurnIndex.ShouldBe(1);
    }

    [Test]
    public void PlayWordleAndMinesweeperInDailyAndSoloModes()
    {
        var wordle = new GameSession("wordle", "daily", "smart", "Alice", null);
        var wl = (WordleLogic)wordle.GameLogic;
        string secret = wl.SecretWord;
        var placements = Enumerable.Range(0, 5)
            .Select(i => new TilePlacementDto(i, 0, secret[i].ToString()))
            .ToList();

        wordle.MakeMove(0, 0, placements: placements).Success.ShouldBeTrue();
        wordle.Status.ShouldBe("Finished");
        wordle.Winner.ShouldBe("Alice");
        wordle.ToDto().Player1GamePoints.ShouldBe(1000);

        var minesweeper = new GameSession("minesweeper", "solo", "smart", "Alice", null);
        minesweeper.MakeMove(0, 0, digit: 1).Success.ShouldBeTrue();
        minesweeper.ToDto().Grid[0][0].ShouldBe("F");
        minesweeper.MakeMove(0, 0, digit: 1).Success.ShouldBeTrue();
        minesweeper.ToDto().Grid[0][0].ShouldBe("H");

        minesweeper.MakeMove(0, 0).Success.ShouldBeTrue();
        minesweeper.ToDto().Player1GamePoints.ShouldBeGreaterThanOrEqualTo(10);
    }
}
