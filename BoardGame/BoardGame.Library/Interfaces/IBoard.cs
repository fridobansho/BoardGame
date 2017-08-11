namespace BoardGame.Library.Interfaces
{
   public interface IBoard
    {
        int Height { get; }

        int Width { get; }

        IPiece PieceAt(ILocation location);

        IPiece PieceAt(int x, int y);

        void PieceAt(int x, int y, IPiece piece);

        void PieceAt(ILocation location, IPiece piece);

        bool CheckBounds(int x, int y);
    }
}
