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
                fallBonusTotal += ScoreManager.CalculateFallBonus(kvp.Value);
            }
            ScoreBreakdown score = _scoreManager.CalculateScore(chain.Count, fallBonusTotal);
            OnScoreChanged?.Invoke(score);

            // 7. 턴 시스템: 3회 제거마다 새 행 추가
            if (_grid.RemovalCount >= 3)
            {
                _grid.ResetRemovalCount();
                bool isGameOver = _grid.AddRowAtBottom();
                OnRowAdded?.Invoke();

                if (isGameOver)
                {
                    TriggerGameOver();
                    return;
                }
            }
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
                Difficulty = _config.CurrentDifficulty
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
