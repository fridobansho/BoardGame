namespace BoardGame.Library.Implementations.Chomp
{
    public class StonePiece : Piece
    {
        public static readonly StonePiece Stone = new StonePiece();

        private StonePiece() : base("S")
        {
        }
    }
}
