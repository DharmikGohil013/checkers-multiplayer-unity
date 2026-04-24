using System.Collections.Generic;
using Checkers.Data;
using Checkers.Utilities;

namespace Checkers.Core
{
    /// <summary>
    /// Static class containing all Checkers game rules as pure functions.
    /// No state — operates entirely on passed-in board data.
    /// </summary>
    public static class CheckersRules
    {
        // Pre-allocated list to avoid per-frame GC — used only in internal methods
        // Note: callers should copy results if needed beyond the current frame.
        private static readonly List<MoveData> s_tempMoves = new List<MoveData>(20);

        // Diagonal direction vectors: { rowDelta, colDelta }
        private static readonly int[,] s_forwardDirsP1 = { { 1, -1 }, { 1, 1 } };   // Player 1 moves upward
        private static readonly int[,] s_forwardDirsP2 = { { -1, -1 }, { -1, 1 } };  // Player 2 moves downward
        private static readonly int[,] s_allDirs = { { 1, -1 }, { 1, 1 }, { -1, -1 }, { -1, 1 } };

        /// <summary>
        /// Validates whether a specific move is legal on the given board.
        /// </summary>
        public static bool IsValidMove(PieceData[,] board, MoveData move, GameSettings settings)
        {
            int size = board.GetLength(0);

            // Bounds check
            if (!InBounds(move.fromRow, move.fromCol, size) || !InBounds(move.toRow, move.toCol, size))
                return false;

            PieceData piece = board[move.fromRow, move.fromCol];
            if (piece.IsEmpty)
                return false;

            // Target must be empty
            if (!board[move.toRow, move.toCol].IsEmpty)
                return false;

            // Target must be on a dark cell (playable)
            if ((move.toRow + move.toCol) % 2 == 0)
                return false;

            int rowDiff = move.toRow - move.fromRow;
            int colDiff = move.toCol - move.fromCol;
            int absRowDiff = rowDiff < 0 ? -rowDiff : rowDiff;
            int absColDiff = colDiff < 0 ? -colDiff : colDiff;

            // Must be diagonal
            if (absRowDiff != absColDiff)
                return false;

            // Normal move (1 step diagonal)
            if (absRowDiff == 1)
            {
                // Non-king pieces can only move forward
                if (!piece.isKing)
                {
                    int forwardDir = GetForwardDirection(piece.playerOwner);
                    if (rowDiff != forwardDir)
                        return false;
                }

                // If force capture and a capture is available, simple move is invalid
                if (settings.forceCapture && HasForcedCapture(board, piece.playerOwner, settings))
                    return false;

                return true;
            }

            // Capture move (2 steps diagonal)
            if (absRowDiff == 2)
            {
                // Non-king pieces can only capture forward
                if (!piece.isKing)
                {
                    int forwardDir = GetForwardDirection(piece.playerOwner);
                    if ((rowDiff > 0 ? 1 : -1) != forwardDir)
                        return false;
                }

                int midRow = move.fromRow + rowDiff / 2;
                int midCol = move.fromCol + colDiff / 2;

                // Must jump over an opponent's piece
                PieceData jumped = board[midRow, midCol];
                if (jumped.IsEmpty || jumped.playerOwner == piece.playerOwner)
                    return false;

                // Validate the capture coordinates match
                if (move.isCapture && (move.captureRow != midRow || move.captureCol != midCol))
                    return false;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all valid moves for a specific piece at (row, col).
        /// Returns a new list (caller may cache).
        /// </summary>
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

            // Check for capture moves first
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

            // If force capture is on and captures exist, only return captures
            if (settings.forceCapture && hasCaptures)
                return results;

            // If force capture is on, check if ANY piece of this player has a capture
            if (settings.forceCapture && HasForcedCapture(board, piece.playerOwner, settings))
            {
                // Only return capture moves for this piece (already added above, possibly empty)
                return results;
            }

            // Add simple moves
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

        /// <summary>
        /// Returns true if the specified player has any forced capture available on the board.
        /// </summary>
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

        /// <summary>
        /// Returns true if a piece at the specified position can capture.
        /// </summary>
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

        /// <summary>
        /// Returns true if a piece can be promoted to king.
        /// Player 1 promotes at the top row, Player 2 promotes at the bottom row.
        /// For 4-player mode, promotion depends on player index.
        /// </summary>
        public static bool CanPromoteToKing(int row, int playerOwner, int boardSize)
        {
            switch (playerOwner)
            {
                case 1: return row == boardSize - 1;   // Player 1 promotes at top
                case 2: return row == 0;                // Player 2 promotes at bottom
                case 3: return row == 0;                // Player 3 promotes at bottom
                case 4: return row == boardSize - 1;    // Player 4 promotes at top
                default: return false;
            }
        }

        /// <summary>
        /// Returns true if a multi-jump (chain capture) is available after a capture at (row, col).
        /// </summary>
        public static bool IsMultiJumpAvailable(PieceData[,] board, int row, int col, GameSettings settings)
        {
            PieceData piece = board[row, col];
            if (piece.IsEmpty)
                return false;

            return HasCaptureFromPosition(board, row, col, piece);
        }

        /// <summary>
        /// Gets the forward direction for a player.
        /// Player 1 and 4 move up (+1 row), Player 2 and 3 move down (-1 row).
        /// </summary>
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

        /// <summary>
        /// Bounds check helper.
        /// </summary>
        private static bool InBounds(int row, int col, int size)
        {
            return row >= 0 && row < size && col >= 0 && col < size;
        }
    }
}
