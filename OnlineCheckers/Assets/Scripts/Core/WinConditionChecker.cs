using UnityEngine;
using Checkers.Data;
using Checkers.Core;
using Checkers.Gameplay;
using Checkers.Utilities;
public class WinConditionChecker : MonoBehaviour
{
    public const int NO_WINNER = -1;
    public static int CheckWinCondition(PieceData[,] board, int[] activePlayers, GameSettings settings)
    {
        if (board == null || activePlayers == null || activePlayers.Length < 2)
            return NO_WINNER;
        foreach (int player in activePlayers)
        {
            if (HasLost(board, player, activePlayers, settings))
            {
                foreach (int other in activePlayers)
                {
                    if (other != player)
                        return other;
                }
            }
        }
        return NO_WINNER;
    }
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
    public static bool HasNoPieces(PieceData[,] board, int playerOwner)
    {
        int size = board.GetLength(0);
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
                if (!board[r, c].IsEmpty && board[r, c].playerOwner == playerOwner)
                    return false;
        return true;
    }
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
