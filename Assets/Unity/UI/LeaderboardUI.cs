using System.Collections.Generic;
using System.Linq;
using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Unity.UI
{
    /// <summary>
    /// 랭킹 리스트 표시 UI.
    /// 메인 메뉴 또는 별도 화면에서 사용.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform _entryContainer;
        [SerializeField] private Text _entryPrefab;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Text _statusText;
        [SerializeField] private ToggleGroup _difficultyFilter;

        [Header("Settings")]
        [SerializeField] private string _entryFormat = "{0}. {1} - {2}";

        private ILeaderboardService _leaderboardService;
        private string _currentFilter = null; // null = all, "Easy", "Normal", "Hard"

        private void Awake()
        {
            if (GameManager.Container != null && GameManager.Container.IsRegistered<ILeaderboardService>())
            {
                _leaderboardService = GameManager.Container.Resolve<ILeaderboardService>();
            }

            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(RefreshLeaderboard);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => gameObject.SetActive(false));

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 리더보드 열기 (외부에서 호출).
        /// </summary>
        public void Open(string difficultyFilter = null)
        {
            _currentFilter = difficultyFilter;
            gameObject.SetActive(true);
            RefreshLeaderboard();
        }

        /// <summary>
        /// 리더보드 닫기.
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void RefreshLeaderboard()
        {
            if (_leaderboardService == null)
            {
                SetStatus("Leaderboard not available.");
                return;
            }

            SetStatus("Loading...");

            _leaderboardService.GetLeaderboardAsync(_currentFilter).ContinueWith(task =>
            {
                MainThreadDispatcher.ExecuteOnMainThread(() =>
                {
                    if (task.IsCompletedSuccessfully && task.Result != null)
                    {
                        PopulateList(task.Result);
                        SetStatus($"Showing {task.Result.Count} entries.");
                    }
                    else
                    {
                        SetStatus("Failed to load leaderboard.");
                    }
                });
            });
        }

        private void PopulateList(List<LeaderboardEntry> entries)
        {
            // 기존 엔트리 제거
            foreach (Transform child in _entryContainer)
            {
                Destroy(child.gameObject);
            }

            if (entries == null || entries.Count == 0)
            {
                Text emptyEntry = Instantiate(_entryPrefab, _entryContainer);
                emptyEntry.text = "No entries yet.";
                return;
            }

            for (int i = 0; i < entries.Count && i < 100; i++)
            {
                var entry = entries[i];
                Text entryText = Instantiate(_entryPrefab, _entryContainer);
                entryText.text = string.Format(_entryFormat, i + 1, entry.PlayerName, entry.Score);
                entryText.gameObject.SetActive(true);
            }
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }
    }
}
