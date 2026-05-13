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

        [Header("Settings")]
        [SerializeField] private LayerMask _blockLayer = ~0;

        private Camera _mainCamera;
        private bool _enabled;

        public event Action<int, int> OnBlockClicked;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _enabled = false;

            if (_gridRenderer == null)
                _gridRenderer = FindAnyObjectByType<UnityGridRenderer>();

            // GameManager에 IInputProvider로 등록
            GameManager.RegisterInputProvider(this);
        }

        private void Update()
        {
            if (!_enabled) return;

            if (Input.GetMouseButtonDown(0))
            {
                HandleClick(Input.mousePosition);
            }

            // 터치 지원
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                HandleClick(Input.GetTouch(0).position);
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
