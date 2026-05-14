using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Interfaces;

namespace BlockPuzzle.Core.Game
{
    public class GameStateMachine : IGameStateMachine
    {
        private readonly IGrid _grid;
        private readonly IChainDetector _chainDetector;
        private readonly IScoreManager _scoreManager;
        private readonly IDifficultyConfig _config;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        private System.DateTime _gameStartTime;

        public event Action<GameState> OnStateChanged;
        public event Action<ScoreBreakdown> OnScoreChanged;
        public event Action<IReadOnlyList<IBlock>> OnBlocksRemoved;
        public event Action<RemovalResult> OnGravityApplied;
        public event Action OnColumnsShifted;
        public event Action OnRowAdded;
        public event Action<GameOverData> OnGameOver;

        public GameStateMachine(IGrid grid, IChainDetector chainDetector, IScoreManager scoreManager, IDifficultyConfig config)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _chainDetector = chainDetector ?? throw new ArgumentNullException(nameof(chainDetector));
            _scoreManager = scoreManager ?? throw new ArgumentNullException(nameof(scoreManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void StartGame(Difficulty difficulty)
        {
            _config.CurrentDifficulty = difficulty;
            _scoreManager.Reset();
            _grid.Initialize();

            // 안전장치: Initialize 후에도 overflow 상태면 즉시 게임오버
            if (_grid.HasOverflow())
            {
                TriggerGameOver();
                return;
            }

            _gameStartTime = System.DateTime.Now;
            SetState(GameState.Playing);
        }

        public void ProcessClick(int row, int column)
        {
            if (CurrentState != GameState.Playing)
                return;

            IBlock clickedBlock = _grid.GetBlockAt(row, column);
            if (clickedBlock == null || clickedBlock.State == BlockState.Removed)
                return;

            // 1. 인접 동색 블럭 찾기
            IReadOnlyList<IBlock> chain = _chainDetector.FindConnectedBlocks(row, column);

            // 2. 제거 가능한지 확인 (2개 이상)
            if (!_chainDetector.CanRemove(chain))
                return;

            // 3. 블럭 제거
            _grid.RemoveBlocks(chain);
            OnBlocksRemoved?.Invoke(chain);

            // 4. 중력 적용
            RemovalResult fallResult = _grid.ApplyGravity();
            OnGravityApplied?.Invoke(fallResult);

            // 5. 열 이동
            _grid.ShiftColumnsTowardCenter();
            OnColumnsShifted?.Invoke();

            // 6. 점수 계산
            int fallBonusTotal = 0;
            foreach (var kvp in fallResult.FallDistances)
            {
                fallBonusTotal += _scoreManager.CalculateFallBonus(kvp.Value);
            }
            ScoreBreakdown score = _scoreManager.CalculateScore(chain.Count, fallBonusTotal);
            OnScoreChanged?.Invoke(score);

            // 7. 턴 시스템: 일정 횟수 제거마다 새 행 추가
            bool turnAdvanced = false;
            if (_grid.RemovalCount >= Grid.TURN_RESET_THRESHOLD)
            {
                _grid.ResetRemovalCount();
                turnAdvanced = DoAddRowAtBottom();
                if (CurrentState != GameState.Playing) return;
            }

            // 8. 교착 상태(Deadlock) 감지: 제거 가능한 블럭이 없으면 강제 턴 진행
            if (CurrentState == GameState.Playing && !_chainDetector.HasAnyValidMove())
            {
                // 강제로 턴 카운트 초기화 후 새 행 추가
                _grid.ResetRemovalCount();
                DoAddRowAtBottom();
            }
        }

        private bool DoAddRowAtBottom()
        {
            bool isGameOver = _grid.AddRowAtBottom();
            OnRowAdded?.Invoke();
            if (isGameOver)
            {
                TriggerGameOver();
                return true;
            }
            return false;
        }

        public void GoToMainMenu()
        {
            SetState(GameState.MainMenu);
        }

        public void RestartGame()
        {
            StartGame(_config.CurrentDifficulty);
        }

        public GameOverData GetGameOverData()
        {
            return new GameOverData
            {
                FinalScore = _scoreManager.CurrentScore,
                MaxCombo = _scoreManager.MaxCombo,
                TotalClearedBlocks = _scoreManager.TotalClearedBlocks,
                Difficulty = _config.CurrentDifficulty,
                GameDurationSeconds = (int)(System.DateTime.Now - _gameStartTime).TotalSeconds
            };
        }

        private void TriggerGameOver()
        {
            SetState(GameState.GameOver);
            OnGameOver?.Invoke(GetGameOverData());
        }

        private void SetState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
