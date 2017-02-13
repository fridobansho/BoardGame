﻿namespace BoardGame.Library.Implementations.TicTacToe
{
    using Interfaces;

    public class XPiece : Piece
    {
        public const string XValue = "X";

        public static IPiece X = new XPiece();

        private XPiece() : base(XValue)
        {

        }
    }
}
