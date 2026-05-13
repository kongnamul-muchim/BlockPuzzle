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
        [SerializeField] private float _fallDuration = 0.15f;
        [SerializeField] private float _removeDuration = 0.2f;
        [SerializeField] private float _selectedScale = 1.1f;

        private SpriteRenderer _spriteRenderer;
        private IBlock _blockData;
        private Vector3 _targetPosition;
        private bool _isAnimating;

        public IBlock BlockData => _blockData;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Core 블럭 데이터 기준으로 초기화.
        /// </summary>
        public void Initialize(IBlock blockData, Sprite blockSprite, Color tint)
        {
            _blockData = blockData;
            _spriteRenderer.sprite = blockSprite;
            _spriteRenderer.color = tint;
            _spriteRenderer.sortingOrder = 1;

            UpdateWorldPosition();
        }

        /// <summary>
        /// 현재 격자 위치를 월드 좌표로 변환하여 배치.
        /// </summary>
        public void UpdateWorldPosition()
        {
            if (_blockData == null) return;

            // GridRenderer가 설정한 오프셋 기반 위치 계산
            // 실제 좌표는 UnityGridRenderer가 GridOrigin을 통해 관리
            float x = _blockData.Column;
            float y = -_blockData.Row; // Y축 반전 (위가 +, 아래가 -)
            transform.localPosition = new Vector3(x, y, 0);
        }

        /// <summary>
        /// 블럭 데이터 동기화 (Grid가 블럭을 이동시킨 후 호출).
        /// </summary>
        public void SyncPosition(int row, int column)
        {
            _blockData?.MoveTo(row, column);
            UpdateWorldPosition();
        }

        /// <summary>
        /// 제거 애니메이션 재생.
        /// </summary>
        public void PlayRemoveAnimation()
        {
            if (_isAnimating) return;
            _isAnimating = true;

            // 간단한 페이드 아웃 + 스케일 다운
            LeanTweenOrFallback();

            // 상태 변경
            _blockData.State = BlockState.Removed;
        }

        private void LeanTweenOrFallback()
        {
            // DOTween/LeanTween 없으면 기본 코루틴 처리
            // 여기서는 간단히 바로 비활성화 (추후 애니메이션 교체)
            _spriteRenderer.color = new Color(1, 1, 1, 0.3f);
            transform.localScale = Vector3.one * 1.2f;

            // 실제 프로젝트에서는 LeanTween/DOTween 또는 Unity Animation으로 교체
            Destroy(gameObject, _removeDuration);
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
