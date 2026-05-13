using System.Collections.Generic;
using BlockPuzzle.Core.Game;
using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using UnityEngine;

namespace BlockPuzzle.Unity.Adapters
{
    /// <summary>
    /// Core Grid의 시각적 표현을 관리.
    /// 블럭 GameObject 풀링, 위치 동기화, 애니메이션 트리거.
    /// </summary>
    public class UnityGridRenderer : MonoBehaviour
    {
        [Header("Grid Layout")]
        [SerializeField] private Vector2 _gridOrigin = new(-4.5f, 4.5f);
        [SerializeField] private float _cellSize = 1f;

        [Header("Block Prefab")]
        [SerializeField] private UnityBlockRenderer _blockPrefab;

        [Header("Sprites")]
        [SerializeField] private Sprite _blockSprite;

        private IGrid _grid;
        private IGameStateMachine _stateMachine;
        private UnityBlockRenderer[,] _blockRenderers;

        private void Awake()
        {
            // DI 컨테이너에서 서비스 해결
            if (GameManager.Container != null)
            {
                _grid = GameManager.Container.Resolve<IGrid>();
                _stateMachine = GameManager.Container.Resolve<IGameStateMachine>();
            }

            if (_grid == null || _stateMachine == null)
            {
                Debug.LogError("[UnityGridRenderer] Failed to resolve core services.");
                return;
            }

            _blockRenderers = new UnityBlockRenderer[_grid.Rows, _grid.Columns];

            // 이벤트 구독
            SubscribeToEvents();

            // 씬 전환 후 로드: 이미 Playing 상태면 바로 렌더링
            if (_stateMachine.CurrentState == GameState.Playing)
            {
                RebuildAllBlocks();
            }
        }

        private void SubscribeToEvents()
        {
            _stateMachine.OnStateChanged += OnGameStateChanged;
            _stateMachine.OnBlocksRemoved += OnBlocksRemoved;
            _stateMachine.OnGravityApplied += OnGravityApplied;
            _stateMachine.OnColumnsShifted += OnColumnsShifted;
            _stateMachine.OnRowAdded += OnRowAdded;
        }

        private void OnDestroy()
        {
            if (_stateMachine != null)
            {
                _stateMachine.OnStateChanged -= OnGameStateChanged;
                _stateMachine.OnBlocksRemoved -= OnBlocksRemoved;
                _stateMachine.OnGravityApplied -= OnGravityApplied;
                _stateMachine.OnColumnsShifted -= OnColumnsShifted;
                _stateMachine.OnRowAdded -= OnRowAdded;
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    RebuildAllBlocks();
                    break;

                case GameState.GameOver:
                    // 게임오버 연출 (추후 구현)
                    break;

                case GameState.MainMenu:
                    ClearAllBlocks();
                    break;
            }
        }

        /// <summary>
        /// Core Grid의 현재 상태를 읽어 모든 블럭 시각화 재구축.
        /// </summary>
        private void RebuildAllBlocks()
        {
            ClearAllBlocks();

            foreach (IBlock block in _grid.GetAllBlocks())
            {
                if (block.State == BlockState.Removed) continue;

                CreateBlockRenderer(block);
            }
        }

        /// <summary>
        /// Core Block 데이터로 새로운 블럭 GameObject 생성.
        /// </summary>
        private UnityBlockRenderer CreateBlockRenderer(IBlock block)
        {
            if (_blockPrefab == null)
            {
                Debug.LogError("[UnityGridRenderer] Block prefab is not assigned.");
                return null;
            }

            UnityBlockRenderer renderer = Instantiate(_blockPrefab, transform);
            renderer.name = $"Block_{block.Row}_{block.Column}_{block.Color}";

            Color tint = UnityBlockRenderer.GetColorForBlockColor(block.Color);
            renderer.Initialize(block, _blockSprite, tint);

            // 월드 좌표 계산
            Vector3 worldPos = GridToWorldPosition(block.Row, block.Column);
            renderer.transform.localPosition = worldPos;
            renderer.transform.localScale = Vector3.one;

            _blockRenderers[block.Row, block.Column] = renderer;
            return renderer;
        }

        /// <summary>
        /// 격자 좌표 → 월드 좌표 변환.
        /// </summary>
        private Vector3 GridToWorldPosition(int row, int column)
        {
            float x = _gridOrigin.x + column * _cellSize;
            float y = _gridOrigin.y - row * _cellSize;
            return new Vector3(x, y, 0);
        }

        /// <summary>
        /// 월드 좌표 → 격자 좌표 변환.
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            float col = (worldPos.x - _gridOrigin.x) / _cellSize;
            float row = (_gridOrigin.y - worldPos.y) / _cellSize;
            return new Vector2Int(Mathf.RoundToInt(col), Mathf.RoundToInt(row));
        }

        private void ClearAllBlocks()
        {
            if (_blockRenderers == null) return;

            for (int r = 0; r < _blockRenderers.GetLength(0); r++)
            {
                for (int c = 0; c < _blockRenderers.GetLength(1); c++)
                {
                    if (_blockRenderers[r, c] != null)
                    {
                        Destroy(_blockRenderers[r, c].gameObject);
                        _blockRenderers[r, c] = null;
                    }
                }
            }
        }

        /// <summary>
        /// 블럭 제거 시 시각 업데이트.
        /// </summary>
        private void OnBlocksRemoved(IReadOnlyList<IBlock> removedBlocks)
        {
            foreach (IBlock block in removedBlocks)
            {
                UnityBlockRenderer renderer = GetRendererAt(block.Row, block.Column);
                if (renderer != null)
                {
                    renderer.PlayRemoveAnimation();
                    _blockRenderers[block.Row, block.Column] = null;
                }
            }
        }

        /// <summary>
        /// 중력 적용 후 모든 블럭 위치 재동기화.
        /// </summary>
        private void OnGravityApplied(RemovalResult result)
        {
            // 전체 블럭 위치 재동기화
            SyncAllBlockPositions();
        }

        /// <summary>
        /// 열 이동 후 전체 블럭 위치 재동기화.
        /// </summary>
        private void OnColumnsShifted()
        {
            // Grid가 변경됐으므로 전체 재구축
            RebuildAllBlocks();
        }

        /// <summary>
        /// 새 행 추가 후 전체 블럭 위치 재동기화.
        /// </summary>
        private void OnRowAdded()
        {
            RebuildAllBlocks();
        }

        /// <summary>
        /// Core Grid의 모든 블럭 위치를 Renderer에 동기화.
        /// </summary>
        private void SyncAllBlockPositions()
        {
            // 기존 렌더러 모두 제거
            ClearAllBlocks();

            // Grid 데이터 기반으로 재생성
            RebuildAllBlocks();
        }

        private UnityBlockRenderer GetRendererAt(int row, int col)
        {
            if (row < 0 || row >= _blockRenderers.GetLength(0)) return null;
            if (col < 0 || col >= _blockRenderers.GetLength(1)) return null;
            return _blockRenderers[row, col];
        }

        /// <summary>
        /// 디버그용: Grid 시작점 기즈모
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                new Vector3(_gridOrigin.x + 4.5f, _gridOrigin.y - 4.5f, 0),
                new Vector3(10, 10, 0)
            );
        }
    }
}
