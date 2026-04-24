using Checkers.Data;
using Checkers.Utilities;

namespace Checkers.Core
{
    /// <summary>
    /// Evaluates win/loss conditions for the Checkers game.
    /// Static class with pure functions — no state.
    /// </summary>
    public static class WinConditionChecker
    {
        /// <summary>
        /// Checks if any player has won the game.
        /// A player wins if all opponents have no pieces left or no valid moves.
        /// </summary>
        /// <param name="board">Current board state.</param>
        /// <param name="activePlayers">Array of active player actor numbers.</param>
        /// <param name="playerCount">Total player count (2 or 4).</param>
        /// <param name="settings">Game settings (for rule config).</param>
        /// <returns>Actor number of the winner, or -1 if no winner yet.</returns>
        public static int CheckWinCondition(PieceData[,] board, int[] activePlayers, int playerCount, GameSettings settings)
        {
            if (activePlayers == null || activePlayers.Length < 2)
                return -1;

            int size = board.GetLength(0);

            // For 2-player mode: check if one player has eliminated the other
            if (playerCount == 2)
            {
                for (int i = 0; i < activePlayers.Length; i++)
                {
                    int playerOwner = i + 1; // Player owner is 1-based
                    if (HasNoPieces(board, playerOwner) || HasNoMoves(board, playerOwner, settings))
                    {
                        // This player loses; the other player wins
                        int winnerIndex = (i == 0) ? 1 : 0;
                        if (winnerIndex < activePlayers.Length)
                        {
                            GameLogger.Log(GameLogger.LogLevel.INFO,
                                $"Win condition met: Player {playerOwner} eliminated. Winner: Actor {activePlayers[winnerIndex]}");
                            return activePlayers[winnerIndex];
                        }
                    }
                }
            }
            else // 4-player mode
            {
                // Count active players (those with pieces and moves)
                int activeCount = 0;
                int lastActiveActor = -1;

                for (int i = 0; i < activePlayers.Length; i++)
                {
                    int playerOwner = i + 1;
                    if (!HasNoPieces(board, playerOwner) && !HasNoMoves(board, playerOwner, settings))
                    {
                        activeCount++;
                        lastActiveActor = activePlayers[i];
                    }
                }

                // If only one player remains active, they win
                if (activeCount == 1)
                {
                    GameLogger.Log(GameLogger.LogLevel.INFO,
                        $"Win condition met in 4-player mode. Last player standing: Actor {lastActiveActor}");
                    return lastActiveActor;
                }

                // If no players are active (shouldn't happen normally), return draw
                if (activeCount == 0)
                {
                    GameLogger.Log(GameLogger.LogLevel.WARN, "No active players found — draw condition.");
                    return -1;
                }
            }

            return -1; // No winner yet
        }

        /// <summary>
        /// Returns true if the specified player has no pieces on the board.
        /// </summary>
        public static bool HasNoPieces(PieceData[,] board, int playerOwner)
        {
            int size = board.GetLength(0);

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (board[r, c].playerOwner == playerOwner)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if the specified player has no valid moves available.
        /// </summary>
        public static bool HasNoMoves(PieceData[,] board, int playerOwner, GameSettings settings)
        {
            int size = board.GetLength(0);

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (board[r, c].playerOwner == playerOwner)
                    {
                        var moves = CheckersRules.GetValidMovesForPiece(board, r, c, settings);
                        if (moves.Count > 0)
                            return false;
                    }
                }
            }

            return true;
        }
    }
}
