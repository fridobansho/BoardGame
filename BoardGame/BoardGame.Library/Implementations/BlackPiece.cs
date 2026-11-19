namespace BoardGame.Library.Implementations
{
    public class BlackPiece : Piece
    {
        public static readonly BlackPiece Black = new BlackPiece();

        private BlackPiece() : base("B")
        {
        }
    }
}
