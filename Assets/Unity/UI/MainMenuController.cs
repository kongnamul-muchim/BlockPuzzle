using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlockPuzzle.Unity.UI
{
    /// <summary>
    /// 메인 메뉴 — 난이도 선택, 리더보드 열기, 게임 시작.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _easyBtn;
        [SerializeField] private Button _normalBtn;
        [SerializeField] private Button _hardBtn;
        [SerializeField] private Button _leaderboardBtn;

        [Header("Leaderboard")]
        [SerializeField] private LeaderboardUI _leaderboardUI;

        [Header("Scene")]
        [SerializeField] private string _gameSceneName = "GameScene";

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _clickClip;

        private void Awake()
        {
            if (_easyBtn != null)
                _easyBtn.onClick.AddListener(() => StartGame(Difficulty.Easy));
            if (_normalBtn != null)
                _normalBtn.onClick.AddListener(() => StartGame(Difficulty.Normal));
            if (_hardBtn != null)
                _hardBtn.onClick.AddListener(() => StartGame(Difficulty.Hard));
            if (_leaderboardBtn != null)
                _leaderboardBtn.onClick.AddListener(OpenLeaderboard);
        }

        private void StartGame(Difficulty difficulty)
        {
            PlayClickSound();

            var stateMachine = GameManager.StateMachine;
            if (stateMachine != null)
            {
                stateMachine.StartGame(difficulty);
                SceneManager.LoadScene(_gameSceneName);
            }
            else
            {
                Debug.LogError("[MainMenu] GameManager.StateMachine is null. Is GameManager in the scene?");
            }
        }

        private void OpenLeaderboard()
        {
            PlayClickSound();
            _leaderboardUI?.Open();
        }

        private void PlayClickSound()
        {
            if (_audioSource != null && _clickClip != null)
                _audioSource.PlayOneShot(_clickClip);
        }
    }
}
