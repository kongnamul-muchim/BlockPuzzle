using System.Collections.Generic;
using BlockPuzzle.Core.Game;
using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using UnityEngine;

namespace BlockPuzzle.Unity.Adapters
{
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
            TryResolveDependencies();
        }

        private void Start()
        {
            if (_stateMachine == null || _grid == null)
                TryResolveDependencies();
        }

        private void TryResolveDependencies()
        {
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

            if (_blockRenderers == null)
                _blockRenderers = new UnityBlockRenderer[_grid.Rows, _grid.Columns];

            SubscribeToEvents();

            if (_stateMachine.CurrentState == GameState.Playing)
                RebuildAllBlocks();
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
                    break;

                case GameState.MainMenu:
                    ClearAllBlocks();
                    break;
            }
        }

        private void RebuildAllBlocks()
        {
            ClearAllBlocks();

            foreach (IBlock block in _grid.GetAllBlocks())
            {
                if (block.State == BlockState.Removed) continue;

                CreateBlockRenderer(block);
            }
        }

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
            renderer.Initialize(block, _blockSprite, tint, _gridOrigin, _cellSize);
            renderer.transform.localScale = Vector3.one;

            _blockRenderers[block.Row, block.Column] = renderer;
            return renderer;
        }

        private Vector3 GridToWorldPosition(int row, int column)
        {
            float x = _gridOrigin.x + column * _cellSize;
            float y = _gridOrigin.y - row * _cellSize;
            return new Vector3(x, y, 0);
        }

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

        private void OnGravityApplied(RemovalResult result)
        {
            // 중력 후 위치는 OnColumnsShifted에서 최종 재구축
            // 여기서 rebuild하면 불필요한 2중 호출 발생
        }

        private void OnColumnsShifted()
        {
            RebuildAllBlocks();
        }

        private void OnRowAdded()
        {
            RebuildAllBlocks();
        }

        private void SyncAllBlockPositions()
        {
            ClearAllBlocks();
            RebuildAllBlocks();
        }

        private UnityBlockRenderer GetRendererAt(int row, int col)
        {
            if (row < 0 || row >= _blockRenderers.GetLength(0)) return null;
            if (col < 0 || col >= _blockRenderers.GetLength(1)) return null;
            return _blockRenderers[row, col];
        }

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
