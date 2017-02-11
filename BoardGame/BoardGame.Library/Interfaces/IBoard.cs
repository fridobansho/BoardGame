namespace BoardGame.Library.Interfaces
{
   public interface IBoard
    {
        int Height { get; }

        int Width { get; }

        IPiece PieceAt(ILocation location);

        void PieceAt(ILocation location, IPiece piece);

        bool CheckBounds(ILocation location);
    }
}
