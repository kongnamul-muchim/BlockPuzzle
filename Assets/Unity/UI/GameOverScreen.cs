using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlockPuzzle.Unity.UI
{
    /// <summary>
    /// 게임 오버 화면 — 최종 점수, 통계, 이름 입력, 저장/재시작/메뉴.
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text _finalScoreText;
        [SerializeField] private Text _maxComboText;
        [SerializeField] private Text _totalClearedText;
        [SerializeField] private Text _difficultyText;
        [SerializeField] private InputField _nameInput;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Text _saveStatusText;

        [Header("Format Strings")]
        [SerializeField] private string _scoreFormat = "Final Score: {0}";
        [SerializeField] private string _comboFormat = "Max Combo: {0}";
        [SerializeField] private string _clearedFormat = "Blocks Cleared: {0}";
        [SerializeField] private string _difficultyFormat = "Difficulty: {0}";

        private IGameStateMachine _stateMachine;
        private ILeaderboardService _leaderboardService;

        private void Awake()
        {
            if (GameManager.Container != null)
            {
                _stateMachine = GameManager.Container.Resolve<IGameStateMachine>();

                if (GameManager.Container.IsRegistered<ILeaderboardService>())
                    _leaderboardService = GameManager.Container.Resolve<ILeaderboardService>();
            }

            Debug.Log($"[GameOverScreen] Awake: _stateMachine={(_stateMachine != null ? "OK" : "NULL")}, state={_stateMachine?.CurrentState}");

            if (_stateMachine != null)
            {
                _stateMachine.OnStateChanged += OnStateChanged;
                _stateMachine.OnGameOver += OnGameOver;
            }

            // 씬 전환 후 로드: 이미 GameOver 상태면 바로 표시
            if (_stateMachine != null && _stateMachine.CurrentState == GameState.GameOver)
            {
                SetActive(true);
                var data = _stateMachine.GetGameOverData();
                if (data != null) UpdateStats(data);
            }
            else
            {
                SetActive(false);
            }

            // 버튼 이벤트
            if (_saveButton != null)
                _saveButton.onClick.AddListener(OnSaveClicked);
            if (_retryButton != null)
                _retryButton.onClick.AddListener(OnRetryClicked);
            if (_mainMenuButton != null)
                _mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            // 저장된 이름 불러오기
            if (_nameInput != null)
                _nameInput.text = PlayerPrefs.GetString("PlayerName", "");
        }

        private void OnDestroy()
        {
            if (_stateMachine != null)
            {
                _stateMachine.OnStateChanged -= OnStateChanged;
                _stateMachine.OnGameOver -= OnGameOver;
            }
        }

        private void OnStateChanged(GameState state)
        {
            Debug.Log($"[GameOverScreen] OnStateChanged: {state}");
            SetActive(state == GameState.GameOver);
        }

        private void OnGameOver(GameOverData data)
        {
            Debug.Log($"[GameOverScreen] OnGameOver called. Score={data.FinalScore}");
            UpdateStats(data);

            if (_saveStatusText != null)
                _saveStatusText.text = "";

            if (_saveButton != null)
                _saveButton.interactable = true;
        }

        private void UpdateStats(GameOverData data)
        {
            if (_finalScoreText != null)
                _finalScoreText.text = string.Format(_scoreFormat, data.FinalScore);

            if (_maxComboText != null)
                _maxComboText.text = string.Format(_comboFormat, data.MaxCombo);

            if (_totalClearedText != null)
                _totalClearedText.text = string.Format(_clearedFormat, data.TotalClearedBlocks);

            if (_difficultyText != null)
                _difficultyText.text = string.Format(_difficultyFormat, data.Difficulty.ToString());
        }

        private void OnSaveClicked()
        {
            string playerName = string.IsNullOrWhiteSpace(_nameInput?.text)
                ? "Anonymous"
                : _nameInput.text.Trim();

            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();

            if (_leaderboardService == null)
            {
                if (_saveStatusText != null)
                    _saveStatusText.text = "Leaderboard not available.";
                return;
            }

            GameOverData data = _stateMachine?.GetGameOverData();
            if (data == null) return;

            var entry = new LeaderboardEntry
            {
                PlayerName = playerName,
                Score = data.FinalScore,
                MaxCombo = data.MaxCombo,
                TotalCleared = data.TotalClearedBlocks,
                Difficulty = data.Difficulty.ToString(),
                GameDuration = data.GameDurationSeconds
            };

            _saveButton.interactable = false;
            if (_saveStatusText != null)
                _saveStatusText.text = "Saving...";

            _leaderboardService.SaveScoreAsync(entry).ContinueWith(task =>
            {
                MainThreadDispatcher.ExecuteOnMainThread(() =>
                {
                    if (task.IsCompletedSuccessfully && task.Result)
                    {
                        if (_saveStatusText != null)
                            _saveStatusText.text = "Score saved!";
                    }
                    else
                    {
                        if (_saveStatusText != null)
                            _saveStatusText.text = "Failed to save.";
                        _saveButton.interactable = true;
                    }
                });
            });
        }

        [Header("Scene Names")]
        [SerializeField] private string _gameSceneName = "GameScene";
        [SerializeField] private string _mainMenuSceneName = "MainMenuScene";

        private void OnRetryClicked()
        {
            _stateMachine?.RestartGame();
            SceneManager.LoadScene(_gameSceneName);
        }

        private void OnMainMenuClicked()
        {
            _stateMachine?.GoToMainMenu();
            SceneManager.LoadScene(_mainMenuSceneName);
        }

        private void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}
