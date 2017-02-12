﻿namespace BoardGame.Tests
{
    using Shouldly;
    using NUnit.Framework;
    using Library.Implementations;
    using Library.Interfaces;

    [TestFixture]
    public class PieceShould
    {
        [Test]
        public void ConstructWithDefault()
        {
            var sut = new Piece();

            sut.ShouldBeOfType<Piece>();
            sut.Value.ShouldBe(Piece.BlankValue);
        }

        [Test]
        [TestCase("X")]
        [TestCase("O")]
        [TestCase(" ")]
        public void ConstructWithValueGiven(string value)
        {
            var sut = new Piece(value);

            sut.Value.ShouldBe(value);
        }

        [Test]
        public void InheritFromIPiece()
        {
            var sut = new Piece();

            sut.ShouldBeAssignableTo<IPiece>();
        }
    }
}
