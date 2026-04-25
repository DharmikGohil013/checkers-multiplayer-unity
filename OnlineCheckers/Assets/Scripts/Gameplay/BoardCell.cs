using System;
using UnityEngine;
using Checkers.Core;
using Checkers.Utilities;
namespace Checkers.Gameplay
{
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
            CreateHighlightRenderer();
        }
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
        public void Initialize(int cellRow, int cellCol, Color color)
        {
            row = cellRow;
            col = cellCol;
            _baseColor = color;
            isPlayable = (cellRow + cellCol) % 2 == 1; 
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            BoxCollider2D col2D = GetComponent<BoxCollider2D>();
            if (col2D == null)
            {
                col2D = gameObject.AddComponent<BoxCollider2D>();
                col2D.size = new Vector2(1f, 1f);
                col2D.isTrigger = true;
            }
            _spriteRenderer.color = color;
            _spriteRenderer.sortingOrder = 0;
            SetHighlight(false, Color.clear);
        }
        #endregion
        #region Highlighting
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
        public void SetHighlight(bool active)
        {
            Color defaultHighlight = new Color(0.2f, 0.8f, 0.2f, 0.5f);
            SetHighlight(active, defaultHighlight);
        }
        public bool IsHighlighted()
        {
            return _isHighlighted;
        }
        #endregion
        #region Helpers
        private void CreateHighlightRenderer()
        {
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
            _highlightRenderer.sprite = CreateDefaultSprite();
        }
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
