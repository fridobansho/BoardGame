namespace BoardGame.Library.Implementations.Chomp
{
    using Interfaces;

    public class PoisonPiece : Piece
    {
        public const string PoisonValue = "P";

        public static IPiece Poison = new PoisonPiece();

        public PoisonPiece() : base(PoisonValue)
        {
        }
    }
}
