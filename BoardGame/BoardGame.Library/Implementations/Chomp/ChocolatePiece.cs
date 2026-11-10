namespace BoardGame.Library.Implementations.Chomp
{
    using Interfaces;

    public class ChocolatePiece : Piece
    {
        public const string ChocolateValue = "C";

        public static IPiece Chocolate = new ChocolatePiece();

        public ChocolatePiece() : base(ChocolateValue)
        {
        }
    }
}
