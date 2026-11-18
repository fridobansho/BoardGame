namespace BoardGame.Library.Interfaces
{
    public interface IScoredGameLogic : IGameLogic
    {
        int GetScore(IPlayer player);

        void SetScore(IPlayer player, int score);
    }
}
