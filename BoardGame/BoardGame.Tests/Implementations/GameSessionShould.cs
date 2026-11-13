namespace BoardGame.Tests.Implementations;

using System.Linq;
using BoardGame.Library.Implementations;
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

    [Test]
    public void ManageRoomsCorrectly()
    {
        var sm = new SessionManager();
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
        var sm = new SessionManager();
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
        var sm = new SessionManager();
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
        var sm = new SessionManager();
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
}
