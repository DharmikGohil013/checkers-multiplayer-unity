using UnityEngine;
using Checkers.Data;
using Checkers.Core;
using Checkers.Gameplay;
using Checkers.Utilities;

/// <summary>
/// Checks win/draw conditions after every move.
/// Attach to the GameManagers GameObject.
/// </summary>
public class WinConditionChecker : MonoBehaviour
{
    // -1 = no winner yet
    public const int NO_WINNER = -1;

    /// <summary>
    /// Check if any player has won.
    /// Returns the winning playerOwner index (0-based), or NO_WINNER (-1).
    /// Call this after every move completes.
    /// </summary>
    public static int CheckWinCondition(PieceData[,] board, int[] activePlayers, GameSettings settings)
    {
        if (board == null || activePlayers == null || activePlayers.Length < 2)
            return NO_WINNER;

        foreach (int player in activePlayers)
        {
            if (HasLost(board, player, activePlayers, settings))
            {
                // Find the opponent who won
                foreach (int other in activePlayers)
                {
                    if (other != player)
                        return other;
                }
            }
        }

        return NO_WINNER;
    }

    /// <summary>
    /// Returns true if the given player has lost:
    /// — they have no pieces left, OR
    /// — they have no valid moves available.
    /// </summary>
    public static bool HasLost(PieceData[,] board, int playerOwner, int[] activePlayers, GameSettings settings)
    {
        if (HasNoPieces(board, playerOwner))
        {
            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"[WinCondition] Player {playerOwner} has no pieces left.");
            return true;
        }

        if (HasNoMoves(board, playerOwner, settings))
        {
            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"[WinCondition] Player {playerOwner} has no valid moves.");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the player has zero pieces remaining on the board.
    /// </summary>
    public static bool HasNoPieces(PieceData[,] board, int playerOwner)
    {
        int size = board.GetLength(0);
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
                if (!board[r, c].IsEmpty && board[r, c].playerOwner == playerOwner)
                    return false;
        return true;
    }

    /// <summary>
    /// Returns true if the player has no legal moves available.
    /// </summary>
    public static bool HasNoMoves(PieceData[,] board, int playerOwner, GameSettings settings)
    {
        int size = board.GetLength(0);
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                if (!board[r, c].IsEmpty && board[r, c].playerOwner == playerOwner)
                {
                    var moves = CheckersRules.GetValidMovesForPiece(board, r, c, settings);
                    if (moves != null && moves.Count > 0)
                        return false;
                }
            }
        }
        return true;
    }
}
