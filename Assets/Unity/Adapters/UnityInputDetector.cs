using System;
using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using UnityEngine;

namespace BlockPuzzle.Unity.Adapters
{
    /// <summary>
    /// 마우스/터치 입력을 감지하여 Core에 전달.
    /// IInputProvider 구현체.
    /// </summary>
    public class UnityInputDetector : MonoBehaviour, IInputProvider
    {
        [Header("References")]
        [SerializeField] private UnityGridRenderer _gridRenderer;

        private Camera _mainCamera;
        private bool _enabled;
        private bool _isMobile;

        public event Action<int, int> OnBlockClicked;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _enabled = false;
            _isMobile = Application.isMobilePlatform;

            if (_gridRenderer == null)
                _gridRenderer = FindAnyObjectByType<UnityGridRenderer>();
        }

        private void Start()
        {
            // Start()에서 등록 (Awake 순서 문제 방지)
            GameManager.RegisterInputProvider(this);
        }

        private void Update()
        {
            if (!_enabled) return;

            if (_isMobile)
            {
                // 모바일: 터치만 처리
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    HandleClick(Input.GetTouch(0).position);
                }
            }
            else
            {
                // PC/에디터: 마우스만 처리
                if (Input.GetMouseButtonDown(0))
                {
                    HandleClick(Input.mousePosition);
                }
            }
        }

        private void HandleClick(Vector2 screenPos)
        {
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));

            // GridRenderer를 통해 격자 좌표 변환
            if (_gridRenderer != null)
            {
                Vector2Int gridPos = _gridRenderer.WorldToGridPosition(worldPos);

                // 유효 범위 확인
                if (gridPos.x >= 0 && gridPos.x < 10 && gridPos.y >= 0 && gridPos.y < 10)
                {
                    OnBlockClicked?.Invoke(gridPos.y, gridPos.x); // (row, column)
                }
            }
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }
    }
}
