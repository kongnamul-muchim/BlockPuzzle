using System;
using System.Collections.Generic;
using System.Linq;
using BlockPuzzle.Core.Interfaces;

namespace BlockPuzzle.Core.Game
{
    public class Grid : IGrid
    {
        public const int GRID_ROWS = 10;
        public const int GRID_COLUMNS = 10;
        public const int LEFT_HALF_END = 4;      // 열 인덱스 0~4 (좌측 5열)
        public const int RIGHT_HALF_START = 5;    // 열 인덱스 5~9 (우측 5열)
        public const int TURN_RESET_THRESHOLD = 3;

        public int Rows => GRID_ROWS;
        public int Columns => GRID_COLUMNS;
        public int RemovalCount { get; private set; }

        private readonly IBlock[,] _grid = new IBlock[GRID_ROWS, GRID_COLUMNS];
        private readonly IDifficultyConfig _config;
        private readonly Random _rng = new();

        public Grid(IDifficultyConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public IBlock GetBlockAt(int row, int column)
        {
            if (row < 0 || row >= GRID_ROWS || column < 0 || column >= GRID_COLUMNS)
                return null;
            return _grid[row, column];
        }

        public IEnumerable<IBlock> GetAllBlocks()
        {
            for (int r = 0; r < GRID_ROWS; r++)
                for (int c = 0; c < GRID_COLUMNS; c++)
                    if (_grid[r, c] != null)
                        yield return _grid[r, c];
        }

        /// <summary>
        /// 게임 시작 시 격자를 초기 블럭으로 채움.
        /// 하단 3행을 랜덤 색상으로 채워서 시작.
        /// </summary>
        public void Initialize()
        {
            // 격자 초기화
            for (int r = 0; r < GRID_ROWS; r++)
                for (int c = 0; c < GRID_COLUMNS; c++)
                    _grid[r, c] = null;

            // 하단 3행 (row 7, 8, 9)을 랜덤 블럭으로 채움
            var colorPool = _config.GetColorPool();
            for (int r = GRID_ROWS - 3; r < GRID_ROWS; r++)
            {
                for (int c = 0; c < GRID_COLUMNS; c++)
                {
                    BlockColor color = colorPool[_rng.Next(colorPool.Count)];
                    _grid[r, c] = new Block(color, r, c);
                }
            }

            RemovalCount = 0;
        }

        /// <summary>
        /// 하단(10행)에 새 블럭 행을 추가하고 기존 블럭을 위로 1칸씩 밀어올림.
        /// </summary>
        /// <returns>1행 위로 블럭이 밀려나면 true (게임오버)</returns>
        public bool AddRowAtBottom()
        {
            // 이동 전 row 0에 블럭이 있는지 확인
            bool rowZeroHadBlocks = false;
            for (int c = 0; c < GRID_COLUMNS; c++)
            {
                if (_grid[0, c] != null)
                {
                    rowZeroHadBlocks = true;
                    break;
                }
            }

            var colorPool = _config.GetColorPool();

            // 모든 블럭을 1행씩 위로 이동 (아래→위로 scan하여 덮어쓰기 방지)
            for (int r = GRID_ROWS - 1; r >= 0; r--)
            {
                for (int c = 0; c < GRID_COLUMNS; c++)
                {
                    if (_grid[r, c] != null)
                    {
                        int newRow = r - 1;
                        if (newRow >= 0)
                        {
                            _grid[r, c].MoveTo(newRow, c);
                            _grid[newRow, c] = _grid[r, c];
                        }
                        _grid[r, c] = null;
                    }
                }
            }

            // 하단 행(row 9)에 새 블럭 생성
            for (int c = 0; c < GRID_COLUMNS; c++)
            {
                BlockColor color = colorPool[_rng.Next(colorPool.Count)];
                _grid[GRID_ROWS - 1, c] = new Block(color, GRID_ROWS - 1, c);
            }

            // 게임오버: 이동 전 row 0에 블럭이 있었다면 위로 밀려나서 게임오버
            return rowZeroHadBlocks;
        }

        /// <summary>
        /// 지정된 블럭들을 격자에서 제거.
        /// </summary>
        public void RemoveBlocks(IReadOnlyList<IBlock> blocks)
        {
            foreach (var block in blocks)
            {
                if (block.Row >= 0 && block.Row < GRID_ROWS &&
                    block.Column >= 0 && block.Column < GRID_COLUMNS)
                {
                    if (_grid[block.Row, block.Column] == block)
                    {
                        _grid[block.Row, block.Column] = null;
                    }
                }
                block.State = BlockState.Removed;
            }

            RemovalCount++;
        }

        /// <summary>
        /// 중력 적용: 제거된 블럭 위에 있던 블럭들을 아래로 낙하시킴.
        /// 각 열을 독립적으로 처리.
        /// </summary>
        public RemovalResult ApplyGravity()
        {
            var result = new RemovalResult();

            for (int c = 0; c < GRID_COLUMNS; c++)
            {
                // 이 열의 블럭들을 아래에서 위로 수집 (null 제외)
                var blocksInColumn = new List<IBlock>();
                for (int r = GRID_ROWS - 1; r >= 0; r--)
                {
                    if (_grid[r, c] != null)
                        blocksInColumn.Add(_grid[r, c]);
                }

                // 블럭이 없으면 skip
                if (blocksInColumn.Count == 0)
                    continue;

                // 블럭들을 아래에서부터 재배치
                int writeRow = GRID_ROWS - 1;
                foreach (var block in blocksInColumn)
                {
                    int fallDistance = block.Row - writeRow;

                    // 기존 위치 클리어
                    if (_grid[block.Row, block.Column] == block)
                        _grid[block.Row, block.Column] = null;

                    // 새 위치로 이동
                    block.MoveTo(writeRow, c);
                    _grid[writeRow, c] = block;

                    if (fallDistance > 0)
                    {
                        block.State = BlockState.Falling;
                        result.FallDistances[block] = fallDistance;
                    }

                    writeRow--;
                }
            }

            return result;
        }

        /// <summary>
        /// 빈 열을 중앙 방향으로 이동.
        /// 좌측 절반(col 0~4): 블럭을 오른쪽(중앙 방향)으로 밀착 → 빈 열은 왼쪽 끝으로
        /// 우측 절반(col 5~9): 블럭을 왼쪽(중앙 방향)으로 밀착 → 빈 열은 오른쪽 끝으로
        /// </summary>
        public void ShiftColumnsTowardCenter()
        {
            // --- 좌측 절반 (col 0~4): 오른쪽(중앙)으로 밀착 ---
            int leftTarget = LEFT_HALF_END; // col 4부터 채워나감
            for (int c = LEFT_HALF_END; c >= 0; c--)
            {
                if (!IsColumnEmpty(c))
                {
                    if (c != leftTarget)
                        MoveColumn(c, leftTarget);
                    leftTarget--;
                }
            }
            // 남은 좌측 끝 열들 비우기
            for (int c = leftTarget; c >= 0; c--)
                ClearColumn(c);

            // --- 우측 절반 (col 5~9): 왼쪽(중앙)으로 밀착 ---
            int rightTarget = RIGHT_HALF_START; // col 5부터 채워나감
            for (int c = RIGHT_HALF_START; c < GRID_COLUMNS; c++)
            {
                if (!IsColumnEmpty(c))
                {
                    if (c != rightTarget)
                        MoveColumn(c, rightTarget);
                    rightTarget++;
                }
            }
            // 남은 우측 끝 열들 비우기
            for (int c = rightTarget; c < GRID_COLUMNS; c++)
                ClearColumn(c);
        }

        /// <summary>
        /// 한 열의 모든 블럭을 다른 열로 이동.
        /// </summary>
        private void MoveColumn(int fromCol, int toCol)
        {
            if (fromCol == toCol) return;

            for (int r = 0; r < GRID_ROWS; r++)
            {
                if (_grid[r, fromCol] != null)
                {
                    _grid[r, fromCol].MoveTo(r, toCol);
                    _grid[r, toCol] = _grid[r, fromCol];
                    _grid[r, fromCol] = null;
                }
            }
        }

        /// <summary>
        /// 한 열의 모든 블럭을 제거.
        /// </summary>
        private void ClearColumn(int col)
        {
            for (int r = 0; r < GRID_ROWS; r++)
            {
                if (_grid[r, col] != null)
                {
                    _grid[r, col].State = BlockState.Removed;
                    _grid[r, col] = null;
                }
            }
        }

        public bool IsColumnEmpty(int column)
        {
            for (int r = 0; r < GRID_ROWS; r++)
            {
                if (_grid[r, column] != null)
                    return false;
            }
            return true;
        }

        public void ResetRemovalCount()
        {
            RemovalCount = 0;
        }

        /// <summary>
        /// 게임오버 상태 확인: row 0에 블럭이 있는 경우,
        /// 다음 AddRowAtBottom 호출 시 게임오버 위험.
        /// 또는 이미 row 0 위로 블럭이 밀려난 경우.
        /// </summary>
        public bool IsAtRiskOfGameOver()
        {
            for (int c = 0; c < GRID_COLUMNS; c++)
            {
                if (_grid[0, c] != null)
                    return true;
            }
            return false;
        }
    }
}
