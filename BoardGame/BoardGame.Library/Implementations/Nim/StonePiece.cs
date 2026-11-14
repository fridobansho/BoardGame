namespace BoardGame.Library.Implementations.Nim
{
    public class StonePiece : Piece
    {
        public static readonly StonePiece Stone = new StonePiece();

        private StonePiece() : base("S")
        {
        }
    }
}
