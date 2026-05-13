using BlockPuzzle.Core.Game;
using BlockPuzzle.Core.Interfaces;
using UnityEngine;

namespace BlockPuzzle.Core.Managers
{
    /// <summary>
    /// 게임 부트스트래핑 담당 MonoBehaviour.
    /// Core에서 유일하게 UnityEngine을 참조하는 파일.
    /// DI 컨테이너 생성 및 모든 서비스 등록.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private Difficulty _startDifficulty = Difficulty.Easy;

        private static IDIContainer _container;
        private static IGameStateMachine _stateMachine;

        /// <summary>전역 DI 컨테이너</summary>
        public static IDIContainer Container => _container;

        /// <summary>전역 게임 상태 머신</summary>
        public static IGameStateMachine StateMachine => _stateMachine;

        private void Awake()
        {
            if (_container != null)
            {
                Debug.LogWarning("GameManager already initialized. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            InitializeContainer();
        }

        private void InitializeContainer()
        {
            _container = new DIContainer();

            // --- Singleton 서비스 등록 ---
            _container.Register<IDifficultyConfig, DifficultyConfig>(ServiceLifetime.Singleton);
            _container.Register<IScoreManager, ScoreManager>(ServiceLifetime.Singleton);
            _container.Register<IGrid, global::BlockPuzzle.Core.Game.Grid>(ServiceLifetime.Singleton);
            _container.Register<IChainDetector, ChainDetector>(ServiceLifetime.Singleton);
            _container.Register<IGameStateMachine, GameStateMachine>(ServiceLifetime.Singleton);

            // --- 빈 인터페이스 등록 (Unity Adapter가 나중에 대체) ---
            // IInputProvider, IAudioService, ILeaderboardService는
            // Unity Adapter에서 RegisterInstance로 등록할 것

            // --- 서비스 초기화 ---
            _stateMachine = _container.Resolve<IGameStateMachine>();

            Debug.Log("[GameManager] Core services initialized.");
        }

        private void Start()
        {
            // 기본 시작: 메인 메뉴 상태에서 시작
            // 실제 게임 시작은 UI → StartGame(difficulty) 호출
        }

        /// <summary>
        /// Unity Adapter에서 IInputProvider 주입용.
        /// GameManager Awake 이후에 호출됨.
        /// </summary>
        public static void RegisterInputProvider(IInputProvider inputProvider)
        {
            if (_container == null) return;

            if (_container.IsRegistered<IInputProvider>())
                return;

            _container.RegisterInstance(inputProvider);

            // 입력 이벤트를 StateMachine에 연결
            inputProvider.OnBlockClicked += (row, col) =>
            {
                _stateMachine?.ProcessClick(row, col);
            };
        }

        /// <summary>
        /// Unity Adapter에서 IAudioService 주입용.
        /// </summary>
        public static void RegisterAudioService(IAudioService audioService)
        {
            if (_container == null) return;

            if (_container.IsRegistered<IAudioService>())
                return;

            _container.RegisterInstance(audioService);
        }

        /// <summary>
        /// Unity Adapter에서 ILeaderboardService 주입용.
        /// </summary>
        public static void RegisterLeaderboardService(ILeaderboardService leaderboardService)
        {
            if (_container == null) return;

            if (_container.IsRegistered<ILeaderboardService>())
                return;

            _container.RegisterInstance(leaderboardService);
        }

        private void OnDestroy()
        {
            if (_container != null)
            {
                _container = null;
                _stateMachine = null;
            }
        }
    }
}
