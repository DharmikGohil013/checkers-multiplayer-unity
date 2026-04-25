using System;
using UnityEngine;
using Checkers.Core;
using Checkers.Utilities;

namespace Checkers.Gameplay
{
    /// <summary>
    /// Represents a single cell on the board.
    /// Handles visual color, highlight state, and click events.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class BoardCell : MonoBehaviour
    {
        #region Fields

        [Header("Cell State")]
        [SerializeField] private int row;
        [SerializeField] private int col;
        [SerializeField] private bool isPlayable;

        private SpriteRenderer _spriteRenderer;
        private SpriteRenderer _highlightRenderer;
        private Color _baseColor;
        private bool _isHighlighted;

        public int Row => row;
        public int Col => col;
        public bool IsPlayable => isPlayable;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

            BoxCollider2D collider = GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider2D>();
                Debug.LogError("[COLLIDER] MISSING collider on: " + gameObject.name + " — Auto-added one.");
            }
            else
            {
                Debug.Log("[COLLIDER] Found collider on: " + gameObject.name + 
                          " | Size: " + collider.bounds.size);
            }

            collider.isTrigger = true;
            collider.size = Vector2.one;

            // Create a child object for highlight overlay
            CreateHighlightRenderer();
        }

        // InputHandler calls this when the cell is clicked/tapped
        public void OnCellClicked()
        {
            Debug.Log($"[BoardCell] OnCellClicked triggered for cell at ({row},{col})");
            if (GameManager.Instance != null && GameManager.Instance.BoardManager != null)
            {
                Debug.Log($"[BoardCell] Routing click to BoardManager.TryMove({row}, {col})");
                GameManager.Instance.BoardManager.TryMove(row, col);
            }
            else
            {
                Debug.LogError("[BoardCell] Cannot route click: GameManager or BoardManager is null!");
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the cell with position and base color.
        /// </summary>
        public void Initialize(int cellRow, int cellCol, Color color)
        {
            row = cellRow;
            col = cellCol;
            _baseColor = color;
            isPlayable = (cellRow + cellCol) % 2 == 1; // Dark cells are playable

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            // Auto-add BoxCollider2D if it's missing from the prefab
            BoxCollider2D col2D = GetComponent<BoxCollider2D>();
            if (col2D == null)
            {
                col2D = gameObject.AddComponent<BoxCollider2D>();
                col2D.size = new Vector2(1f, 1f);
                col2D.isTrigger = true;
            }

            _spriteRenderer.color = color;
            _spriteRenderer.sortingOrder = 0;

            // Hide highlight by default
            SetHighlight(false, Color.clear);
        }

        #endregion

        #region Highlighting

        /// <summary>
        /// Sets the highlight state of this cell.
        /// Used to show valid move destinations.
        /// </summary>
        public void SetHighlight(bool active, Color highlightColor)
        {
            _isHighlighted = active;

            if (_highlightRenderer != null)
            {
                _highlightRenderer.enabled = active;
                if (active)
                {
                    _highlightRenderer.color = highlightColor;
                }
            }
        }

        /// <summary>
        /// Sets highlight with a default green color.
        /// </summary>
        public void SetHighlight(bool active)
        {
            Color defaultHighlight = new Color(0.2f, 0.8f, 0.2f, 0.5f);
            SetHighlight(active, defaultHighlight);
        }

        /// <summary>
        /// Returns whether this cell is currently highlighted.
        /// </summary>
        public bool IsHighlighted()
        {
            return _isHighlighted;
        }

        #endregion

        #region Helpers

        private void CreateHighlightRenderer()
        {
            // Check if highlight child already exists
            Transform existing = transform.Find("Highlight");
            if (existing != null)
            {
                _highlightRenderer = existing.GetComponent<SpriteRenderer>();
                return;
            }

            GameObject highlightGO = new GameObject("Highlight");
            highlightGO.transform.SetParent(transform);
            highlightGO.transform.localPosition = Vector3.zero;
            highlightGO.transform.localScale = Vector3.one * 0.9f;

            _highlightRenderer = highlightGO.AddComponent<SpriteRenderer>();
            _highlightRenderer.sortingOrder = 0;
            _highlightRenderer.enabled = false;

            // Use a simple white sprite (will be tinted by color)
            _highlightRenderer.sprite = CreateDefaultSprite();
        }

        /// <summary>
        /// Creates a simple 1x1 white sprite for use as highlight overlay.
        /// </summary>
        private static Sprite CreateDefaultSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
        }

        #endregion

        #region Object Pooling

        /// <summary>
        /// Resets the cell for pooling.
        /// </summary>
        public void ResetForPool()
        {
            SetHighlight(false, Color.clear);
            row = -1;
            col = -1;
            isPlayable = false;
            gameObject.SetActive(false);
        }

        #endregion
    }
}
