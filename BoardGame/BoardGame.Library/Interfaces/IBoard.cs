namespace BoardGame.Library.Interfaces
{
   public interface IBoard
    {
        int Height { get; }

        int Width { get; }

        IPiece PieceAt(int x, int y);

        void PieceAt(int x, int y, IPiece piece);

        bool CheckBounds(int x, int y);
    }
}
