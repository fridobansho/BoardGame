namespace BoardGame.Library.Implementations.TicTacToe
{
    using Interfaces;

    public class OPiece : Piece
    {
        public const string OValue = "O";

        public static IPiece O = new OPiece();

        public OPiece() : base(OValue)
        {

        }
    }
}
