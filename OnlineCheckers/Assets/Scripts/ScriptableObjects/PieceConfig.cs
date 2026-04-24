using UnityEngine;

namespace Checkers.Data
{
    /// <summary>
    /// ScriptableObject that defines piece visual and animation configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPieceConfig", menuName = "Checkers/Piece Config", order = 1)]
    public class PieceConfig : ScriptableObject
    {
        [Header("Player Colors")]
        [Tooltip("Colors for players 1–4. Index 0 = Player 1, etc.")]
        public Color[] playerColors = new Color[4]
        {
            new Color(0.85f, 0.15f, 0.15f, 1f),  // P1 - Red
            new Color(0.15f, 0.15f, 0.15f, 1f),  // P2 - Black
            new Color(0.15f, 0.45f, 0.85f, 1f),  // P3 - Blue
            new Color(0.15f, 0.75f, 0.25f, 1f)   // P4 - Green
        };

        [Header("Piece Sprites")]
        [Tooltip("Sprite for normal (non-king) pieces.")]
        public Sprite normalPieceSprite;

        [Tooltip("Sprite for king pieces.")]
        public Sprite kingPieceSprite;

        [Header("Animation")]
        [Tooltip("Duration of piece move animation in seconds.")]
        [Range(0.05f, 1.0f)]
        public float moveAnimationDuration = 0.2f;

        [Header("Highlight")]
        [Tooltip("Color tint applied when a piece is selected/highlighted.")]
        public Color highlightTint = new Color(1f, 1f, 0.5f, 1f);

        /// <summary>
        /// Returns the color for a given player index (0-based).
        /// Clamps to valid range.
        /// </summary>
        public Color GetPlayerColor(int playerIndex)
        {
            if (playerColors == null || playerColors.Length == 0)
                return Color.white;

            int clampedIndex = Mathf.Clamp(playerIndex, 0, playerColors.Length - 1);
            return playerColors[clampedIndex];
        }
    }
}
