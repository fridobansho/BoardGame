namespace BoardGame.Library.Implementations.Gomoku
{
    public class BlackPiece : Piece
    {
        public static readonly BlackPiece Black = new BlackPiece();

        private BlackPiece() : base("B")
        {
        }
    }
}
