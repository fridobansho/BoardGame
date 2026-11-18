namespace BoardGame.Library.Interfaces
{
    public interface IMoveGameLogic : IGameLogic
    {
        bool IsValidMove(IBoard board, IPlayer player, IMove move);

        bool ApplyMove(IBoard board, IPlayer player, IMove move);
    }
}
