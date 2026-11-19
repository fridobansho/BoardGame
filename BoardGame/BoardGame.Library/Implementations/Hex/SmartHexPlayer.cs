namespace BoardGame.Library.Implementations.Hex
{
    using Interfaces;
    using System;

    public class SmartHexPlayer : IPlayer
    {
        private static readonly (int dx, int dy)[] HexNeighbors =
        {
            (-1, 0), (1, 0), (0, -1), (0, 1), (1, -1), (-1, 1)
        };

        private readonly Random _random = new Random();

        internal string AssignedSymbol { get; set; }
        internal bool IsMisere { get; set; } = false;

        public string Name { get; }

        public SmartHexPlayer(string name = "Hex Bot (Smart)")
        {
            Name = name;
        }

        public ILocation GetMove(IBoard board)
        {
            string mySymbol = AssignedSymbol ?? InferSymbol(board);
            string oppSymbol = mySymbol == BlackPiece.Black.Value ? WhitePiece.White.Value : BlackPiece.Black.Value;
            bool isTopToBottom = mySymbol == BlackPiece.Black.Value;
            IPiece myPiece = mySymbol == BlackPiece.Black.Value ? BlackPiece.Black : WhitePiece.White;
            IBoard simBoard = IsMisere ? CloneBoard(board) : null;

            int centerX = board.Width / 2;
            int centerY = board.Height / 2;
            ILocation bestMove = null;
            int bestScore = int.MinValue;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.PieceAt(x, y)?.Value != BlankPiece.BlankValue) continue;

                    int ownNeighbors = 0;
                    int oppNeighbors = 0;
                    bool bridgesNewRowOrCol = false;

                    foreach (var (dx, dy) in HexNeighbors)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (board.CheckBounds(nx, ny))
                        {
                            var val = board.PieceAt(nx, ny)?.Value;
                            if (val == mySymbol)
                            {
                                ownNeighbors++;
                                if ((isTopToBottom && ny != y) || (!isTopToBottom && nx != x))
                                {
                                    bridgesNewRowOrCol = true;
                                }
                            }
                            else if (val == oppSymbol)
                            {
                                oppNeighbors++;
                            }
                        }
                    }

                    // Reward own directional chains, blocking opponent clusters, and touching goal edges
                    int neighborScore = ownNeighbors * 15 + oppNeighbors * 6 + (bridgesNewRowOrCol ? 12 : 0);
                    if (isTopToBottom && (y == 0 || y == board.Height - 1)) neighborScore += 8;
                    if (!isTopToBottom && (x == 0 || x == board.Width - 1)) neighborScore += 8;

                    int distFromCenter = Math.Abs(x - centerX) + Math.Abs(y - centerY);
                    int score;
                    if (IsMisere)
                    {
                        simBoard.PieceAt(x, y, myPiece);
                        bool completesPath = HexLogic.HasConnectedPath(simBoard, mySymbol, isTopToBottom);
                        simBoard.PieceAt(x, y, BlankPiece.Blank);
                        score = completesPath ? -1_000_000 : (-neighborScore - distFromCenter);
                    }
                    else
                    {
                        score = neighborScore - distFromCenter;
                    }

                    if (score > bestScore || (score == bestScore && _random.Next(2) == 0))
                    {
                        bestScore = score;
                        bestMove = new Location(x, y);
                    }
                }
            }

            return bestMove;
        }

        private static IBoard CloneBoard(IBoard board)
        {
            var clone = new Board(board.Width, board.Height);
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    clone.PieceAt(x, y, board.PieceAt(x, y));
                }
            }
            return clone;
        }

        private static string InferSymbol(IBoard board)
        {
            int b = 0, w = 0;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var val = board.PieceAt(x, y)?.Value;
                    if (val == BlackPiece.Black.Value) b++;
                    else if (val == WhitePiece.White.Value) w++;
                }
            }
            return b <= w ? BlackPiece.Black.Value : WhitePiece.White.Value;
        }
    }
}
