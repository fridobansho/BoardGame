namespace BoardGame.Library.Implementations.Checkers
{
    public class RedKingPiece : Piece
    {
        public static readonly RedKingPiece RedKing = new RedKingPiece();

        private RedKingPiece() : base("RK")
        {
        }
    }
}
