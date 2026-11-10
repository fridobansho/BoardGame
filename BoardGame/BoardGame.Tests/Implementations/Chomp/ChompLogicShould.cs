namespace BoardGame.Tests.Implementations.Chomp
{
    using Shouldly;
    using NUnit.Framework;
    using Library.Implementations;
    using Library.Implementations.Chomp;
    using Library.Interfaces;
    using Moq;
    using System;
    using System.Linq;

    [TestFixture]
    public class ChompLogicShould
    {
        [Test]
        public void ConstructWithDefaults()
        {
            var sut = new ChompLogic();

            sut.PlayerPieces.ShouldBeEmpty();
            sut.LastPlayerWhoMoved.ShouldBeNull();
            sut.PoisonEaten.ShouldBeFalse();
        }

        [Test]
        public void InheritFromIGameLogic()
        {
            var sut = new ChompLogic();

            sut.ShouldBeAssignableTo<IGameLogic>();
        }

        [Test]
        public void ThrowArgumentNullExceptionWhenMappingNullPlayers()
        {
            var sut = new ChompLogic();

            Action action = () => sut.MapPieces(null);

            var exception = action.ShouldThrow<ArgumentNullException>();
            exception.ParamName.ShouldBe("players");
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void ThrowArgumentOutOfRangeExceptionWhenMappingWrongPlayerCount(int count)
        {
            var sut = new ChompLogic();
            var players = Enumerable.Range(0, count).Select(_ => new Mock<IPlayer>().Object).ToList();

            Action action = () => sut.MapPieces(players);

            var exception = action.ShouldThrow<ArgumentOutOfRangeException>();
            exception.ParamName.ShouldBe("players");
        }

        [Test]
        public void MapBothPlayersToChocolatePiece()
        {
            var sut = new ChompLogic();
            var p1 = new Mock<IPlayer>().Object;
            var p2 = new Mock<IPlayer>().Object;
            var players = new[] { p1, p2 };

            sut.MapPieces(players);

            sut.GetPiece(p1).ShouldBe(ChocolatePiece.Chocolate);
            sut.GetPiece(p2).ShouldBe(ChocolatePiece.Chocolate);
        }

        [Test]
        public void InitializeBoardWithPoisonAtOriginAndChocolateElsewhere()
        {
            var sut = new ChompLogic();
            var board = new Board(4, 3);

            sut.InitializeBoard(board);

            board.PieceAt(0, 0).ShouldBe(PoisonPiece.Poison);
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (x == 0 && y == 0) continue;
                    board.PieceAt(x, y).ShouldBe(ChocolatePiece.Chocolate);
                }
            }
        }

        [Test]
        public void ReturnTrueForUneatenBlockAndFalseForBlankOrOutOfBounds()
        {
            var sut = new ChompLogic();
            var board = new Board(3, 3);
            sut.InitializeBoard(board);

            sut.IsValidMove(board, new Location(0, 0)).ShouldBeTrue();
            sut.IsValidMove(board, new Location(2, 2)).ShouldBeTrue();

            board.PieceAt(2, 2, BlankPiece.Blank);
            sut.IsValidMove(board, new Location(2, 2)).ShouldBeFalse();

            sut.IsValidMove(board, new Location(-1, 0)).ShouldBeFalse();
            sut.IsValidMove(board, new Location(3, 0)).ShouldBeFalse();
            sut.IsValidMove(board, null).ShouldBeFalse();
        }

        [Test]
        public void ApplyMoveEatsAllBlocksDownAndToRight()
        {
            var sut = new ChompLogic();
            var board = new Board(4, 3);
            sut.InitializeBoard(board);
            var player1 = new Mock<IPlayer>().Object;
            sut.MapPieces(new[] { player1, new Mock<IPlayer>().Object });

            var result = sut.ApplyMove(board, player1, new Location(2, 1));

            result.ShouldBeTrue();
            sut.LastPlayerWhoMoved.ShouldBe(player1);

            // Uneaten area
            board.PieceAt(0, 0).ShouldBe(PoisonPiece.Poison);
            board.PieceAt(1, 1).ShouldBe(ChocolatePiece.Chocolate);
            board.PieceAt(2, 0).ShouldBe(ChocolatePiece.Chocolate);

            // Eaten area: x >= 2 and y >= 1
            board.PieceAt(2, 1).ShouldBe(BlankPiece.Blank);
            board.PieceAt(2, 2).ShouldBe(BlankPiece.Blank);
            board.PieceAt(3, 1).ShouldBe(BlankPiece.Blank);
            board.PieceAt(3, 2).ShouldBe(BlankPiece.Blank);
        }

        [Test]
        public void DeclareOtherPlayerWinnerWhenPoisonBlockEaten()
        {
            var sut = new ChompLogic();
            var board = new Board(3, 3);
            sut.InitializeBoard(board);
            var player1 = new Mock<IPlayer>().Object;
            var player2 = new Mock<IPlayer>().Object;
            var players = new[] { player1, player2 };
            sut.MapPieces(players);

            sut.GetWinners(board, players).ShouldBeEmpty();

            // Player 1 is forced to eat the poison block at (0, 0)
            sut.ApplyMove(board, player1, new Location(0, 0));

            sut.PoisonEaten.ShouldBeTrue();
            var winners = sut.GetWinners(board, players).ToList();
            winners.Count.ShouldBe(1);
            winners[0].ShouldBe(player2); // Player 2 wins because Player 1 ate the poison
        }
    }
}
