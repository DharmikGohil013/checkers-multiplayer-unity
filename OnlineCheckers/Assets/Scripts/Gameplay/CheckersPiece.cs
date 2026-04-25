using System;
using System.Collections;
using UnityEngine;
using Checkers.Core;
using Checkers.Data;
using Checkers.Utilities;
namespace Checkers.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
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
        #region Unity Lifecycle
        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            CircleCollider2D collider = GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<CircleCollider2D>();
                Debug.LogError("[COLLIDER] MISSING collider on: " + gameObject.name + " — Auto-added one.");
            }
            else
            {
                Debug.Log("[COLLIDER] Found collider on: " + gameObject.name + 
                          " | Size: " + collider.bounds.size);
            }
            collider.isTrigger = true;
            collider.radius = 0.4f;
        }
        public void OnPieceClicked()
        {
            Debug.Log($"[CheckersPiece] OnPieceClicked triggered for piece at ({row},{col})");
            if (GameManager.Instance != null && GameManager.Instance.BoardManager != null)
            {
                Debug.Log($"[CheckersPiece] Routing click to BoardManager.SelectPiece({row}, {col})");
                GameManager.Instance.BoardManager.SelectPiece(row, col);
            }
            else
            {
                Debug.LogError("[CheckersPiece] Cannot route click: GameManager or BoardManager is null!");
            }
        }
        #endregion
        #region Initialization
        public void Initialize(int owner, int initRow, int initCol, PieceConfig config)
        {
            ownerActorNumber = owner;
            row = initRow;
            col = initCol;
            isKing = false;
            _pieceConfig = config;
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            CircleCollider2D col2D = GetComponent<CircleCollider2D>();
            if (col2D == null)
            {
                col2D = gameObject.AddComponent<CircleCollider2D>();
                col2D.radius = 0.4f;
            }
            _baseColor = config.GetPlayerColor(owner - 1);
            _spriteRenderer.color = _baseColor;
            if (config.normalPieceSprite != null)
            {
                _spriteRenderer.sprite = config.normalPieceSprite;
            }
            _spriteRenderer.sortingOrder = 1;
            _isHighlighted = false;
            Debug.Log("[RENDER] " + gameObject.name +
                      " SortingLayer: " + _spriteRenderer.sortingLayerName +
                      " Order: " + _spriteRenderer.sortingOrder);
            Debug.Log("[POSITION] " + gameObject.name + " Z: " + transform.position.z);
            GameLogger.Log(GameLogger.LogLevel.INFO,
                $"Piece initialized: Player {owner} at ({initRow},{initCol})");
        }
        #endregion
        #region Movement
        public void MoveTo(int targetRow, int targetCol)
        {
            row = targetRow;
            col = targetCol;
            if (GameManager.Instance != null && GameManager.Instance.BoardManager != null)
            {
                Vector3 targetPos = GameManager.Instance.BoardManager.GetWorldPosition(targetRow, targetCol);
                targetPos.z = -0.1f; 
                if (_moveCoroutine != null)
                    StopCoroutine(_moveCoroutine);
                float duration = _pieceConfig != null ? _pieceConfig.moveAnimationDuration : 0.2f;
                _moveCoroutine = StartCoroutine(MoveAnimationCoroutine(targetPos, duration));
            }
        }
        private IEnumerator MoveAnimationCoroutine(Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = transform.localPosition;
            float elapsed = 0f;
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
        public void PromoteToKing()
        {
            if (isKing)
                return;
            isKing = true;
            if (_pieceConfig != null && _pieceConfig.kingPieceSprite != null)
            {
                _spriteRenderer.sprite = _pieceConfig.kingPieceSprite;
            }
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
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(originalScale, pulseScale, t);
                yield return null;
            }
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
