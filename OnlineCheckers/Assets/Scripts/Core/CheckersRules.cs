using System.Collections.Generic;
using Checkers.Data;
using Checkers.Utilities;
namespace Checkers.Core
{
    public static class CheckersRules
    {
        private static readonly List<MoveData> s_tempMoves = new List<MoveData>(20);
        private static readonly int[,] s_forwardDirsP1 = { { 1, -1 }, { 1, 1 } };   
        private static readonly int[,] s_forwardDirsP2 = { { -1, -1 }, { -1, 1 } };  
        private static readonly int[,] s_allDirs = { { 1, -1 }, { 1, 1 }, { -1, -1 }, { -1, 1 } };
        public static bool IsValidMove(PieceData[,] board, MoveData move, GameSettings settings)
        {
            int size = board.GetLength(0);
            if (!InBounds(move.fromRow, move.fromCol, size) || !InBounds(move.toRow, move.toCol, size))
                return false;
            PieceData piece = board[move.fromRow, move.fromCol];
            if (piece.IsEmpty)
                return false;
            if (!board[move.toRow, move.toCol].IsEmpty)
                return false;
            if ((move.toRow + move.toCol) % 2 == 0)
                return false;
            int rowDiff = move.toRow - move.fromRow;
            int colDiff = move.toCol - move.fromCol;
            int absRowDiff = rowDiff < 0 ? -rowDiff : rowDiff;
            int absColDiff = colDiff < 0 ? -colDiff : colDiff;
            if (absRowDiff != absColDiff)
                return false;
            if (absRowDiff == 1)
            {
                if (!piece.isKing)
                {
                    int forwardDir = GetForwardDirection(piece.playerOwner);
                    if (rowDiff != forwardDir)
                        return false;
                }
                if (settings.forceCapture && HasForcedCapture(board, piece.playerOwner, settings))
                    return false;
                return true;
            }
            if (absRowDiff == 2)
            {
                if (!piece.isKing)
                {
                    int forwardDir = GetForwardDirection(piece.playerOwner);
                    if ((rowDiff > 0 ? 1 : -1) != forwardDir)
                        return false;
                }
                int midRow = move.fromRow + rowDiff / 2;
                int midCol = move.fromCol + colDiff / 2;
                PieceData jumped = board[midRow, midCol];
                if (jumped.IsEmpty || jumped.playerOwner == piece.playerOwner)
                    return false;
                if (move.isCapture && (move.captureRow != midRow || move.captureCol != midCol))
                    return false;
                return true;
            }
            return false;
        }
        public static List<MoveData> GetValidMovesForPiece(PieceData[,] board, int row, int col, GameSettings settings)
        {
            List<MoveData> results = new List<MoveData>(8);
            int size = board.GetLength(0);
            PieceData piece = board[row, col];
            if (piece.IsEmpty)
                return results;
            int[,] directions;
            if (piece.isKing)
            {
                directions = s_allDirs;
            }
            else
            {
                int forward = GetForwardDirection(piece.playerOwner);
                if (forward == 1)
                    directions = s_forwardDirsP1;
                else
                    directions = s_forwardDirsP2;
            }
            bool hasCaptures = false;
            int dirCount = directions.GetLength(0);
            for (int d = 0; d < dirCount; d++)
            {
                int dr = directions[d, 0];
                int dc = directions[d, 1];
                int jumpRow = row + dr * 2;
                int jumpCol = col + dc * 2;
                int midRow = row + dr;
                int midCol = col + dc;
                if (InBounds(jumpRow, jumpCol, size) && board[jumpRow, jumpCol].IsEmpty)
                {
                    PieceData midPiece = board[midRow, midCol];
                    if (!midPiece.IsEmpty && midPiece.playerOwner != piece.playerOwner)
                    {
                        results.Add(new MoveData(row, col, jumpRow, jumpCol, true, midRow, midCol));
                        hasCaptures = true;
                    }
                }
            }
            if (settings.forceCapture && hasCaptures)
                return results;
            if (settings.forceCapture && HasForcedCapture(board, piece.playerOwner, settings))
            {
                return results;
            }
            for (int d = 0; d < dirCount; d++)
            {
                int dr = directions[d, 0];
                int dc = directions[d, 1];
                int newRow = row + dr;
                int newCol = col + dc;
                if (InBounds(newRow, newCol, size) && board[newRow, newCol].IsEmpty)
                {
                    results.Add(new MoveData(row, col, newRow, newCol, false, -1, -1));
                }
            }
            return results;
        }
        public static bool HasForcedCapture(PieceData[,] board, int playerOwner, GameSettings settings)
        {
            if (!settings.forceCapture)
                return false;
            int size = board.GetLength(0);
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (board[r, c].playerOwner != playerOwner)
                        continue;
                    if (HasCaptureFromPosition(board, r, c, board[r, c]))
                        return true;
                }
            }
            return false;
        }
        private static bool HasCaptureFromPosition(PieceData[,] board, int row, int col, PieceData piece)
        {
            int size = board.GetLength(0);
            int[,] directions;
            if (piece.isKing)
            {
                directions = s_allDirs;
            }
            else
            {
                int forward = GetForwardDirection(piece.playerOwner);
                directions = forward == 1 ? s_forwardDirsP1 : s_forwardDirsP2;
            }
            int dirCount = directions.GetLength(0);
            for (int d = 0; d < dirCount; d++)
            {
                int dr = directions[d, 0];
                int dc = directions[d, 1];
                int jumpRow = row + dr * 2;
                int jumpCol = col + dc * 2;
                int midRow = row + dr;
                int midCol = col + dc;
                if (InBounds(jumpRow, jumpCol, size) && board[jumpRow, jumpCol].IsEmpty)
                {
                    PieceData midPiece = board[midRow, midCol];
                    if (!midPiece.IsEmpty && midPiece.playerOwner != piece.playerOwner)
                        return true;
                }
            }
            return false;
        }
        public static bool CanPromoteToKing(int row, int playerOwner, int boardSize)
        {
            switch (playerOwner)
            {
                case 1: return row == boardSize - 1;   
                case 2: return row == 0;                
                case 3: return row == 0;                
                case 4: return row == boardSize - 1;    
                default: return false;
            }
        }
        public static bool IsMultiJumpAvailable(PieceData[,] board, int row, int col, GameSettings settings)
        {
            PieceData piece = board[row, col];
            if (piece.IsEmpty)
                return false;
            return HasCaptureFromPosition(board, row, col, piece);
        }
        private static int GetForwardDirection(int playerOwner)
        {
            switch (playerOwner)
            {
                case 1: return 1;
                case 2: return -1;
                case 3: return -1;
                case 4: return 1;
                default: return 1;
            }
        }
        private static bool InBounds(int row, int col, int size)
        {
            return row >= 0 && row < size && col >= 0 && col < size;
        }
    }
}
