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

            // GameManager.Awake가 InputDetector.Awake보다 늦게 실행된 경우
            // 여기서 다시 시도 (Start는 모든 Awake 이후에 실행됨)
            if (_stateMachine == null && GameManager.Container != null)
            {
                _stateMachine = GameManager.Container.Resolve<IGameStateMachine>();
                _stateMachine.OnStateChanged += OnGameStateChanged;
                _enabled = (_stateMachine.CurrentState == GameState.Playing);
            }
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
            if (mouse == null)
            {
                Debug.Log("[Input] Mouse.current is NULL");
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 screenPos = mouse.position.ReadValue();
                Debug.Log($"[Input] Mouse clicked at screen ({screenPos.x:F0}, {screenPos.y:F0})");
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
            Debug.Log($"[Input] Screen→World: ({worldPos.x:F2}, {worldPos.y:F2})");

            if (_gridRenderer != null)
            {
                Vector2Int gridPos = _gridRenderer.WorldToGridPosition(worldPos);
                Debug.Log($"[Input] Grid coord: ({gridPos.x}, {gridPos.y})");

                if (gridPos.x >= 0 && gridPos.x < 10 && gridPos.y >= 0 && gridPos.y < 10)
                {
                    Debug.Log($"[Input] Firing OnBlockClicked(row={gridPos.y}, col={gridPos.x})");
                    OnBlockClicked?.Invoke(gridPos.y, gridPos.x);
                }
                else
                {
                    Debug.Log($"[Input] Grid coord out of bounds!");
                }
            }
            else
            {
                Debug.Log("[Input] GridRenderer is NULL!");
            }
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }
    }
}
