using UnityEngine;

namespace Checkers.Data
{
    /// <summary>
    /// ScriptableObject that defines board configuration for the Checkers game.
    /// Supports 4x4, 6x6, and 8x8 board sizes with 2 or 4 players.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBoardConfig", menuName = "Checkers/Board Config", order = 0)]
    public class BoardConfig : ScriptableObject
    {
        [Header("Board Dimensions")]
        [Tooltip("Size of the board (4, 6, or 8).")]
        [Range(4, 8)]
        public int boardSize = 8;

        [Header("Player Configuration")]
        [Tooltip("Number of players (2 or 4).")]
        [Range(2, 4)]
        public int playerCount = 2;

        [Header("Cell Colors")]
        [Tooltip("Color of light (non-playable) cells.")]
        public Color lightCellColor = new Color(0.93f, 0.86f, 0.72f, 1f);

        [Tooltip("Color of dark (playable) cells.")]
        public Color darkCellColor = new Color(0.55f, 0.27f, 0.07f, 1f);

        [Header("Cell Sizing")]
        [Tooltip("World-space size of each cell.")]
        public float cellSize = 1.0f;

        [Header("Pieces Per Player")]
        [Tooltip("Number of pieces per player. Auto-calculated if set to 0.")]
        public int piecesPerPlayer = 0;

        /// <summary>
        /// Returns the effective pieces per player count.
        /// If piecesPerPlayer is 0, it is auto-calculated based on board size.
        /// 8x8 = 12, 6x6 = 6, 4x4 = 2 (for 2-player mode).
        /// For 4-player mode, pieces are halved.
        /// </summary>
        public int GetPiecesPerPlayer()
        {
            if (piecesPerPlayer > 0)
                return piecesPerPlayer;

            int rowsForPieces = (boardSize / 2) - 1;
            int piecesPerRow = boardSize / 2;
            int totalPerPlayer = rowsForPieces * piecesPerRow;

            if (playerCount == 4)
            {
                totalPerPlayer = Mathf.Max(1, totalPerPlayer / 2);
            }

            return totalPerPlayer;
        }

        private void OnValidate()
        {
            // Enforce even board sizes
            if (boardSize % 2 != 0)
                boardSize = Mathf.Clamp(boardSize + 1, 4, 8);

            // Enforce valid player counts
            if (playerCount != 2 && playerCount != 4)
                playerCount = 2;
        }
    }
}
