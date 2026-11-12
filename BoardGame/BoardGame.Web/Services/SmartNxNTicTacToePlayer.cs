namespace BoardGame.Web.Services;

using BoardGame.Library.Implementations;
using BoardGame.Library.Implementations.TicTacToe;
using BoardGame.Library.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

public class SmartNxNTicTacToePlayer : IPlayer
{
    public string Name { get; }

    public SmartNxNTicTacToePlayer(string name)
    {
        Name = name ?? nameof(SmartNxNTicTacToePlayer);
    }

    public ILocation GetMove(IBoard board)
    {
        if (board == null) return new Location(0, 0);

        int xCount = 0;
        int oCount = 0;
        var availableMoves = new List<ILocation>();

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                var p = board.PieceAt(x, y);
                if (p != null && p.Value == XPiece.X.Value) xCount++;
                else if (p != null && p.Value == OPiece.O.Value) oCount++;
                else if (p == null || p.Value == BlankPiece.BlankValue) availableMoves.Add(new Location(x, y));
            }
        }

        if (availableMoves.Count == 0) return new Location(0, 0);

        IPiece myPiece = (xCount <= oCount) ? (IPiece)XPiece.X : OPiece.O;
        IPiece oppPiece = (myPiece.Value == XPiece.X.Value) ? (IPiece)OPiece.O : XPiece.X;

        // 1. Can we win in 1 move?
        foreach (var move in availableMoves)
        {
            if (SimulateMoveWins(board, move, myPiece))
            {
                return move;
            }
        }

        // 2. Can opponent win in 1 move? Block them!
        foreach (var move in availableMoves)
        {
            if (SimulateMoveWins(board, move, oppPiece))
            {
                return move;
            }
        }

        // 3. Prefer center positions if available
        int centerX = board.Width / 2;
        int centerY = board.Height / 2;
        var centerMoves = availableMoves.Where(m => Math.Abs(m.X - centerX) <= 1 && Math.Abs(m.Y - centerY) <= 1).ToList();
        if (centerMoves.Count > 0)
        {
            return centerMoves.OrderByDescending(m => EvaluateMove(board, m, myPiece)).First();
        }

        // 4. Score moves based on potential
        return availableMoves.OrderByDescending(m => EvaluateMove(board, m, myPiece)).First();
    }

    private static bool SimulateMoveWins(IBoard board, ILocation move, IPiece piece)
    {
        int targetLength = board.Width; // square board
        // Check Row
        int count = 1;
        for (int x = 0; x < board.Width; x++)
        {
            if (x == move.X) continue;
            if (board.PieceAt(x, move.Y).Value == piece.Value) count++;
        }
        if (count == targetLength) return true;

        // Check Col
        count = 1;
        for (int y = 0; y < board.Height; y++)
        {
            if (y == move.Y) continue;
            if (board.PieceAt(move.X, y).Value == piece.Value) count++;
        }
        if (count == targetLength) return true;

        // Check Main Diagonal
        if (move.X == move.Y)
        {
            count = 1;
            for (int i = 0; i < board.Width; i++)
            {
                if (i == move.X) continue;
                if (board.PieceAt(i, i).Value == piece.Value) count++;
            }
            if (count == targetLength) return true;
        }

        // Check Anti Diagonal
        if (move.X + move.Y == targetLength - 1)
        {
            count = 1;
            for (int i = 0; i < board.Width; i++)
            {
                if (i == move.X) continue;
                if (board.PieceAt(i, targetLength - 1 - i).Value == piece.Value) count++;
            }
            if (count == targetLength) return true;
        }

        return false;
    }

    private static int EvaluateMove(IBoard board, ILocation move, IPiece myPiece)
    {
        int score = 0;
        for (int x = 0; x < board.Width; x++)
            if (board.PieceAt(x, move.Y).Value == myPiece.Value) score += 2;
        for (int y = 0; y < board.Height; y++)
            if (board.PieceAt(move.X, y).Value == myPiece.Value) score += 2;

        if (move.X == move.Y)
            for (int i = 0; i < board.Width; i++)
                if (board.PieceAt(i, i).Value == myPiece.Value) score += 3;

        if (move.X + move.Y == board.Width - 1)
            for (int i = 0; i < board.Width; i++)
                if (board.PieceAt(i, board.Width - 1 - i).Value == myPiece.Value) score += 3;

        return score;
    }
}
