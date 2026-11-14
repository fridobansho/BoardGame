namespace BoardGame.Library.Implementations.Gomoku
{
    public class WhitePiece : Piece
    {
        public static readonly WhitePiece White = new WhitePiece();

        private WhitePiece() : base("W")
        {
        }
    }
}
