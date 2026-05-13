using System.Collections.Generic;
using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Unity.UI
{
    /// <summary>
    /// 게임 플레이 중 HUD (점수, 연계, 남은 제거 횟수, 난이도).
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _comboText;
        [SerializeField] private Text _removalCountText;
        [SerializeField] private Text _difficultyText;

        [Header("Format Strings")]
        [SerializeField] private string _scoreFormat = "Score: {0}";
        [SerializeField] private string _comboFormat = "Combo: {0}";
        [SerializeField] private string _removalFormat = "Remaining: {0}/{1}";
        [SerializeField] private string _difficultyFormat = "{0}";

        private IGameStateMachine _stateMachine;
        private IScoreManager _scoreManager;
        private IDifficultyConfig _config;
        private IGrid _grid;

        private const int MAX_REMOVALS = 3;

        private void Awake()
        {
            if (GameManager.Container != null)
            {
                _stateMachine = GameManager.Container.Resolve<IGameStateMachine>();
                _scoreManager = GameManager.Container.Resolve<IScoreManager>();
                _config = GameManager.Container.Resolve<IDifficultyConfig>();
                _grid = GameManager.Container.Resolve<IGrid>();
            }

            if (_stateMachine != null)
            {
                _stateMachine.OnStateChanged += OnStateChanged;
                _stateMachine.OnScoreChanged += OnScoreChanged;
                _stateMachine.OnBlocksRemoved += OnBlocksRemoved;
                _stateMachine.OnRowAdded += OnRowAdded;
            }

            SetActive(false);
        }

        private void OnDestroy()
        {
            if (_stateMachine != null)
            {
                _stateMachine.OnStateChanged -= OnStateChanged;
                _stateMachine.OnScoreChanged -= OnScoreChanged;
                _stateMachine.OnBlocksRemoved -= OnBlocksRemoved;
                _stateMachine.OnRowAdded -= OnRowAdded;
            }
        }

        private void OnBlocksRemoved(IReadOnlyList<IBlock> _)
        {
            UpdateRemovalCount();
        }

        private void OnStateChanged(GameState state)
        {
            SetActive(state == GameState.Playing);

            if (state == GameState.Playing)
            {
                UpdateAll();
            }
        }

        private void OnScoreChanged(ScoreBreakdown score)
        {
            UpdateScore();
            UpdateCombo();
        }

        private void OnRowAdded()
        {
            UpdateRemovalCount();
        }

        private void UpdateAll()
        {
            UpdateScore();
            UpdateCombo();
            UpdateRemovalCount();
            UpdateDifficulty();
        }

        private void UpdateScore()
        {
            if (_scoreText != null && _scoreManager != null)
            {
                _scoreText.text = string.Format(_scoreFormat, _scoreManager.CurrentScore);
            }
        }

        private void UpdateCombo()
        {
            if (_comboText != null && _scoreManager != null)
            {
                _comboText.text = string.Format(_comboFormat, _scoreManager.MaxCombo);
            }
        }

        private void UpdateRemovalCount()
        {
            if (_removalCountText != null && _grid != null)
            {
                int current = _grid.RemovalCount;
                _removalCountText.text = string.Format(_removalFormat, MAX_REMOVALS - current, MAX_REMOVALS);
            }
        }

        private void UpdateDifficulty()
        {
            if (_difficultyText != null && _config != null)
            {
                _difficultyText.text = string.Format(_difficultyFormat, _config.CurrentDifficulty.ToString());
            }
        }

        private void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}
