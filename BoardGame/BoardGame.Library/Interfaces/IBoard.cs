namespace BoardGame.Library.Interfaces
{
    public interface IBoard
    {
        uint Height { get; }

        uint Width { get; }

        IPiece PieceAt(uint x, uint y);

        void PieceAt(uint x, uint y, IPiece piece);
    }
}
