namespace BoardGame.Library.Implementations.Checkers
{
    public class RedCheckerPiece : Piece
    {
        public static readonly RedCheckerPiece Red = new RedCheckerPiece();

        private RedCheckerPiece() : base("R")
        {
        }
    }
}
