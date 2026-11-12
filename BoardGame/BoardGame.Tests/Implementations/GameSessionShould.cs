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
}
