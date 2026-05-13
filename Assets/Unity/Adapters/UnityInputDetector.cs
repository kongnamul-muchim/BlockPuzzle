using System;
using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using UnityEngine;

namespace BlockPuzzle.Unity.Adapters
{
    /// <summary>
    /// 마우스/터치 입력을 감지하여 Core에 전달.
    /// IInputProvider 구현체.
    /// 게임 상태에 따라 자동 활성화/비활성화.
    /// </summary>
    public class UnityInputDetector : MonoBehaviour, IInputProvider
    {
        [Header("References")]
        [SerializeField] private UnityGridRenderer _gridRenderer;

        private Camera _mainCamera;
        private bool _enabled;
        private bool _isMobile;
        private IGameStateMachine _stateMachine;

        public event Action<int, int> OnBlockClicked;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _enabled = false;
            _isMobile = Application.isMobilePlatform;

            if (_gridRenderer == null)
                _gridRenderer = FindAnyObjectByType<UnityGridRenderer>();

            // 상태 머신 구독 (Playing일 때만 입력 활성화)
            if (GameManager.Container != null)
            {
                _stateMachine = GameManager.Container.Resolve<IGameStateMachine>();
                _stateMachine.OnStateChanged += OnGameStateChanged;

                // 씬 전환 후 로드: 이미 Playing이면 바로 활성화
                if (_stateMachine.CurrentState == GameState.Playing)
                    _enabled = true;
            }
        }

        private void Start()
        {
            GameManager.RegisterInputProvider(this);
        }

        private void OnDestroy()
        {
            if (_stateMachine != null)
                _stateMachine.OnStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState state)
        {
            _enabled = (state == GameState.Playing);
        }

        private void Update()
        {
            if (!_enabled) return;

            if (_isMobile)
            {
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    HandleClick(Input.GetTouch(0).position);
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    HandleClick(Input.mousePosition);
                }
            }
        }

        private void HandleClick(Vector2 screenPos)
        {
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));

            if (_gridRenderer != null)
            {
                Vector2Int gridPos = _gridRenderer.WorldToGridPosition(worldPos);

                if (gridPos.x >= 0 && gridPos.x < 10 && gridPos.y >= 0 && gridPos.y < 10)
                {
                    OnBlockClicked?.Invoke(gridPos.y, gridPos.x);
                }
            }
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }
    }
}
