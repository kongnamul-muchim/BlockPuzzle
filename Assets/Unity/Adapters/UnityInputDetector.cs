using System;
using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;

namespace BlockPuzzle.Unity.Adapters
{
    /// <summary>
    /// 마우스/터치 입력을 감지하여 Core에 전달.
    /// Input System 패키지 기반. IInputProvider 구현체.
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

            if (_isMobile)
                EnhancedTouchSupport.Enable();

            // 상태 머신 구독
            if (GameManager.Container != null)
            {
                _stateMachine = GameManager.Container.Resolve<IGameStateMachine>();
                _stateMachine.OnStateChanged += OnGameStateChanged;

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
                HandleTouch();
            }
            else
            {
                HandleMouse();
            }
        }

        private void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 screenPos = mouse.position.ReadValue();
                HandleClick(screenPos);
            }
        }

        private void HandleTouch()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null) return;

            if (touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                Vector2 screenPos = touchscreen.primaryTouch.position.ReadValue();
                HandleClick(screenPos);
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
