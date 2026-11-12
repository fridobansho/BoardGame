namespace BoardGame.Library.Interfaces
{
    public interface IBoardRenderer
    {
        void Render(IBoard board);

        string RenderToString(IBoard board);
    }
}
