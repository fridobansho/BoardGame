namespace BoardGame.Library.Implementations.Checkers
{
    public class BlackCheckerPiece : Piece
    {
        public static readonly BlackCheckerPiece Black = new BlackCheckerPiece();

        private BlackCheckerPiece() : base("B")
        {
        }
    }
}
