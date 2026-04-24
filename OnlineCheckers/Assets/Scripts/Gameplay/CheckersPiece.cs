using System;
using System.Collections;
using UnityEngine;
using Checkers.Core;
using Checkers.Data;
using Checkers.Utilities;

namespace Checkers.Gameplay
{
    /// <summary>
    /// Represents a visual checkers piece on the board.
    /// Handles initialization, movement animation, king promotion, highlighting, and pooling.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class CheckersPiece : MonoBehaviour
    {
        #region Fields

        [Header("State")]
        [SerializeField] private int ownerActorNumber;
        [SerializeField] private bool isKing;
        [SerializeField] private int row;
        [SerializeField] private int col;

        private SpriteRenderer _spriteRenderer;
        private PieceConfig _pieceConfig;
        private Color _baseColor;
        private bool _isHighlighted;
        private Coroutine _moveCoroutine;

        public int OwnerActorNumber => ownerActorNumber;
        public bool IsKing => isKing;
        public int Row => row;
        public int Col => col;

        #endregion

        #region Events

        /// <summary>Fired when this piece is clicked. Only fires if it's the local player's turn.</summary>
        public static event Action<CheckersPiece> OnPieceClicked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

            // Ensure collider exists for click detection
            BoxCollider2D collider = GetComponent<BoxCollider2D>();
            if (collider == null)
                collider = gameObject.AddComponent<BoxCollider2D>();

            collider.isTrigger = true;
            collider.size = Vector2.one * 0.8f;
        }

        private void OnMouseDown()
        {
            // Only allow click if it's the local player's turn
            if (GameManager.Instance == null || GameManager.Instance.TurnManager == null)
                return;

            if (!GameManager.Instance.TurnManager.IsLocalPlayerTurn())
                return;

            OnPieceClicked?.Invoke(this);
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes this piece with owner, position, and visual configuration.
        /// </summary>
        /// <param name="owner">Player owner index (1-based).</param>
        /// <param name="initRow">Board row.</param>
        /// <param name="initCol">Board column.</param>
        /// <param name="config">Piece configuration for visuals.</param>
        public void Initialize(int owner, int initRow, int initCol, PieceConfig config)
        {
            ownerActorNumber = owner;
            row = initRow;
            col = initCol;
            isKing = false;
            _pieceConfig = config;

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            // Set color based on player owner (0-indexed for config)
            _baseColor = config.GetPlayerColor(owner - 1);
            _spriteRenderer.color = _baseColor;

            // Set normal piece sprite
            if (config.normalPieceSprite != null)
            {
                _spriteRenderer.sprite = config.normalPieceSprite;
            }

            // Ensure sorting order is above cells
            _spriteRenderer.sortingOrder = 1;

            _isHighlighted = false;

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Piece initialized: Player {owner} at ({initRow},{initCol})");
        }

        #endregion

        #region Movement

        /// <summary>
        /// Animates the piece moving to a new board position.
        /// Updates internal row/col.
        /// </summary>
        public void MoveTo(int targetRow, int targetCol)
        {
            row = targetRow;
            col = targetCol;

            // Calculate target world position
            if (GameManager.Instance != null && GameManager.Instance.BoardManager != null)
            {
                Vector3 targetPos = GameManager.Instance.BoardManager.GetWorldPosition(targetRow, targetCol);
                targetPos.z = -0.1f; // Keep above cells

                if (_moveCoroutine != null)
                    StopCoroutine(_moveCoroutine);

                float duration = _pieceConfig != null ? _pieceConfig.moveAnimationDuration : 0.2f;
                _moveCoroutine = StartCoroutine(MoveAnimationCoroutine(targetPos, duration));
            }
        }

        /// <summary>
        /// Lerp-based move animation coroutine.
        /// </summary>
        private IEnumerator MoveAnimationCoroutine(Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = transform.localPosition;
            float elapsed = 0f;

            // Raise sorting order during movement
            _spriteRenderer.sortingOrder = 3;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            transform.localPosition = targetPosition;
            _spriteRenderer.sortingOrder = 1;
            _moveCoroutine = null;
        }

        #endregion

        #region King Promotion

        /// <summary>
        /// Promotes this piece to king. Swaps sprite and sets isKing flag.
        /// </summary>
        public void PromoteToKing()
        {
            if (isKing)
                return;

            isKing = true;

            if (_pieceConfig != null && _pieceConfig.kingPieceSprite != null)
            {
                _spriteRenderer.sprite = _pieceConfig.kingPieceSprite;
            }

            // Visual feedback — brief scale pulse
            StartCoroutine(KingPromotionAnimation());

            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Piece promoted to King: Player {ownerActorNumber} at ({row},{col})");
        }

        private IEnumerator KingPromotionAnimation()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 pulseScale = originalScale * 1.3f;
            float duration = 0.3f;
            float elapsed = 0f;

            // Scale up
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(originalScale, pulseScale, t);
                yield return null;
            }

            // Scale back down
            elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(pulseScale, originalScale, t);
                yield return null;
            }

            transform.localScale = originalScale;
        }

        #endregion

        #region Highlighting

        /// <summary>
        /// Sets the highlight state of this piece (selection indicator).
        /// </summary>
        public void SetHighlight(bool active)
        {
            _isHighlighted = active;

            if (_spriteRenderer == null)
                return;

            if (active && _pieceConfig != null)
            {
                _spriteRenderer.color = _pieceConfig.highlightTint;
            }
            else
            {
                _spriteRenderer.color = _baseColor;
            }
        }

        #endregion

        #region Object Pooling

        /// <summary>
        /// Resets the piece and returns it to a disabled state for pooling.
        /// </summary>
        public void ReturnToPool()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            ownerActorNumber = 0;
            isKing = false;
            row = -1;
            col = -1;
            _isHighlighted = false;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white;
                _spriteRenderer.sortingOrder = 1;
            }

            gameObject.SetActive(false);
        }

        #endregion
    }
}
