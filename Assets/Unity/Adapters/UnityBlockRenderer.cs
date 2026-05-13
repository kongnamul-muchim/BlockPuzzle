using System.Collections;
using BlockPuzzle.Core.Game;
using BlockPuzzle.Core.Interfaces;
using UnityEngine;

namespace BlockPuzzle.Unity.Adapters
{
    /// <summary>
    /// 개별 블럭의 시각적 표현 담당.
    /// Core의 IBlock 데이터를 받아 SpriteRenderer로 렌더링.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class UnityBlockRenderer : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _removeDuration = 0.2f;
        [SerializeField] private float _fallSpeed = 8f; // 칸/초 단위 낙하 속도
        [SerializeField] private float _selectedScale = 1.1f;

        private SpriteRenderer _spriteRenderer;
        private IBlock _blockData;
        private Vector2 _gridOrigin;
        private float _cellSize;
        private bool _isAnimating;

        public IBlock BlockData => _blockData;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Core 블럭 데이터 기준으로 초기화.
        /// </summary>
        public void Initialize(IBlock blockData, Sprite blockSprite, Color tint, Vector2 gridOrigin, float cellSize)
        {
            _blockData = blockData;
            _gridOrigin = gridOrigin;
            _cellSize = cellSize;
            _spriteRenderer.sprite = blockSprite;
            _spriteRenderer.color = tint;
            _spriteRenderer.sortingOrder = 1;

            UpdateWorldPosition(gridOrigin, cellSize);
        }

        /// <summary>
        /// 현재 격자 위치를 월드 좌표로 변환하여 배치.
        /// </summary>
        public void UpdateWorldPosition(Vector2 gridOrigin, float cellSize)
        {
            if (_blockData == null) return;

            float x = gridOrigin.x + _blockData.Column * cellSize;
            float y = gridOrigin.y - _blockData.Row * cellSize;
            transform.localPosition = new Vector3(x, y, 0);
        }

        /// <summary>
        /// 블럭 데이터 동기화 (Grid가 블럭을 이동시킨 후 호출).
        /// </summary>
        public void SyncPosition(int row, int column, Vector2 gridOrigin, float cellSize)
        {
            _blockData?.MoveTo(row, column);
            UpdateWorldPosition(gridOrigin, cellSize);
        }

        /// <summary>
        /// 제거 애니메이션 재생 (코루틴 기반 페이드 아웃 + 확대).
        /// </summary>
        public void PlayRemoveAnimation()
        {
            if (_isAnimating) return;
            _isAnimating = true;

            _blockData.State = BlockState.Removed;
            StartCoroutine(AnimateRemove());
        }

        private System.Collections.IEnumerator AnimateRemove()
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Color startColor = _spriteRenderer.color;

            while (elapsed < _removeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _removeDuration;
                // 부드러운 페이드 아웃 + 확대
                transform.localScale = Vector3.Lerp(startScale, startScale * 1.3f, t);
                _spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, t));
                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// 낙하 애니메이션 재생.
        /// RemovalResult.FallDistances 정보를 기반으로 시작 위치부터 목표 위치까지 애니메이션.
        /// Grid는 이미 블럭을 이동시켰으므로, 목표 위치는 _blockData의 논리적 위치에서 계산.
        /// </summary>
        public void PlayFallAnimation(int fallDistance, System.Action onComplete)
        {
            if (_isAnimating || fallDistance <= 0) return;
            _isAnimating = true;

            // 목표 위치 (= Grid에서 이동된 최종 논리적 위치, renderer는 아직 미반영)
            float targetX = _gridOrigin.x + _blockData.Column * _cellSize;
            float targetY = _gridOrigin.y - _blockData.Row * _cellSize;
            Vector3 targetPos = new Vector3(targetX, targetY, 0);

            // 시작 위치 (= 낙하 전 위치, 목표 위치보다 fallDistance만큼 위)
            Vector3 startPos = new Vector3(targetPos.x, targetPos.y + fallDistance * _cellSize, 0);

            transform.localPosition = startPos;
            _blockData.State = BlockState.Falling;

            StartCoroutine(AnimateFall(startPos, targetPos, fallDistance, onComplete));
        }

        private System.Collections.IEnumerator AnimateFall(Vector3 startPos, Vector3 targetPos, int fallDistance, System.Action onComplete)
        {
            // 낙하 거리에 비례한 시간 계산 (칸당 _fallSpeed)
            float duration = fallDistance / _fallSpeed;
            duration = Mathf.Max(duration, 0.05f); // 최소 0.05초

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // 감속 효과 (Quadratic Ease-In)
                t = t * t;
                transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            transform.localPosition = targetPos;
            _blockData.State = BlockState.Normal;
            _isAnimating = false;

            onComplete?.Invoke();
        }

        /// <summary>
        /// 선택됨 표시 (스케일 업).
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (selected)
            {
                transform.localScale = Vector3.one * _selectedScale;
                _spriteRenderer.sortingOrder = 2;
            }
            else
            {
                transform.localScale = Vector3.one;
                _spriteRenderer.sortingOrder = 1;
            }
        }

        /// <summary>
        /// 블럭 색상에 따른 Sprite tint 반환.
        /// </summary>
        public static Color GetColorForBlockColor(BlockColor color)
        {
            return color switch
            {
                BlockColor.Red    => new Color(1f, 0.2f, 0.2f),
                BlockColor.Orange => new Color(1f, 0.6f, 0f),
                BlockColor.Yellow => new Color(1f, 0.9f, 0.1f),
                BlockColor.Green  => new Color(0.2f, 0.9f, 0.3f),
                BlockColor.Blue   => new Color(0.2f, 0.4f, 1f),
                BlockColor.Purple => new Color(0.7f, 0.2f, 0.9f),
                _                 => Color.white
            };
        }
    }
}
