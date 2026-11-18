namespace BoardGame.Library.Implementations.Checkers
{
    public class BlackKingPiece : Piece
    {
        public static readonly BlackKingPiece BlackKing = new BlackKingPiece();

        private BlackKingPiece() : base("BK")
        {
        }
    }
}
