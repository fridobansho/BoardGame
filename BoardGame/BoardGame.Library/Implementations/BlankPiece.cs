namespace BoardGame.Library.Implementations
{
    using Interfaces;
    public class BlankPiece : Piece
    {
        public const string BlankValue = " ";

        public static IPiece Blank = new BlankPiece();

        private BlankPiece() : base(BlankValue)
        {
        }
    }
}
