namespace BoardGame.Library.Implementations.Checkers
{
    using System;
    using Interfaces;

    public class SmartCheckersPlayer : IPlayer
    {
        private readonly Random _random = new Random();
        private readonly string _symbol;

        internal string AssignedSymbol { get; set; }

        public string Name { get; }

        public SmartCheckersPlayer(string name = "Checkers Bot (Smart)", string symbol = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _symbol = symbol;
        }

        public ILocation GetMove(IBoard board)
        {
            if (board == null) return new Location(0, 0);

            string side = _symbol ?? AssignedSymbol ?? RandomCheckersPlayer.InferSide(board);
            string oppSide = side == RedCheckerPiece.Red.Value
                ? BlackCheckerPiece.Black.Value
                : RedCheckerPiece.Red.Value;

            var validMoves = CheckersLogic.GetValidMovesForSide(board, side);
            if (validMoves.Count == 0)
            {
                return new Location(-1, -1);
            }

            PieceMove bestMove = validMoves[0];
            int bestScore = int.MinValue;

            foreach (var move in validMoves)
            {
                int score = EvaluateMove(board, move, side, oppSide);
                if (score > bestScore || (score == bestScore && _random.Next(2) == 0))
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }

        private static int EvaluateMove(IBoard board, PieceMove move, string side, string oppSide)
        {
            int score = 0;

            bool isCapture = Math.Abs(move.Path[1].X - move.Path[0].X) == 2;
            if (isCapture)
            {
                int jumpCount = move.Path.Count - 1;
                score += jumpCount * 150;
            }

            string pieceVal = board.PieceAt(move.FromX, move.FromY)?.Value;
            bool wasKing = CheckersLogic.IsKing(pieceVal);
            bool promotes = !wasKing &&
                ((side == RedCheckerPiece.Red.Value && move.ToY == 0) ||
                 (side == BlackCheckerPiece.Black.Value && move.ToY == board.Height - 1));

            if (promotes)
            {
                score += 120;
            }

            // Simulate move to evaluate opponent's immediate capture reply and positional value
            var simBoard = CheckersLogic.CloneBoard(board);
            SimulateMove(simBoard, move, side);

            var oppCaptures = CheckersLogic.GetCaptureMovesForSide(simBoard, oppSide);
            if (oppCaptures.Count > 0)
            {
                int maxOppLoss = 0;
                foreach (var cap in oppCaptures)
                {
                    int loss = (cap.Path.Count - 1) * 110;
                    if (loss > maxOppLoss) maxOppLoss = loss;
                }
                score -= maxOppLoss;
            }

            // Prefer winning immediately if opponent has no legal moves left
            if (CheckersLogic.GetValidMovesForSide(simBoard, oppSide).Count == 0)
            {
                score += 10000;
            }

            // Center control and advancing non-kings forward
            int centerDist = Math.Abs(move.ToX - board.Width / 2);
            score += (4 - centerDist) * 5;

            if (!wasKing)
            {
                int advancement = side == RedCheckerPiece.Red.Value
                    ? (board.Height - 1 - move.ToY)
                    : move.ToY;
                score += advancement * 8;
            }

            // Slight edge safety bonus
            if (move.ToX == 0 || move.ToX == board.Width - 1)
            {
                score += 12;
            }

            return score;
        }

        private static void SimulateMove(IBoard board, PieceMove move, string side)
        {
            string currentVal = board.PieceAt(move.FromX, move.FromY)?.Value;
            bool isCapture = Math.Abs(move.Path[1].X - move.Path[0].X) == 2;

            if (isCapture)
            {
                for (int i = 0; i < move.Path.Count - 1; i++)
                {
                    var cur = move.Path[i];
                    var next = move.Path[i + 1];
                    int midX = (cur.X + next.X) / 2;
                    int midY = (cur.Y + next.Y) / 2;

                    board.PieceAt(cur.X, cur.Y, BlankPiece.Blank);
                    board.PieceAt(midX, midY, BlankPiece.Blank);
                    IPiece landed = ResolvePiece(currentVal, side, next.Y, board.Height);
                    board.PieceAt(next.X, next.Y, landed);
                    currentVal = landed.Value;
                }
            }
            else
            {
                board.PieceAt(move.FromX, move.FromY, BlankPiece.Blank);
                board.PieceAt(move.ToX, move.ToY, ResolvePiece(currentVal, side, move.ToY, board.Height));
            }
        }

        private static IPiece ResolvePiece(string pieceVal, string side, int toY, int height)
        {
            if (side == RedCheckerPiece.Red.Value)
            {
                return (pieceVal == RedKingPiece.RedKing.Value || toY == 0)
                    ? (IPiece)RedKingPiece.RedKing
                    : RedCheckerPiece.Red;
            }
            return (pieceVal == BlackKingPiece.BlackKing.Value || toY == height - 1)
                ? (IPiece)BlackKingPiece.BlackKing
                : BlackCheckerPiece.Black;
        }
    }
}
