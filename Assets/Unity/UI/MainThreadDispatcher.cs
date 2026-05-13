using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace BlockPuzzle.Unity.UI
{
    /// <summary>
    /// 메인 스레드 디스패처.
    /// 백그라운드 스레드에서 UI 업데이트가 필요할 때 사용.
    /// 빈 GameObject에 붙여서 사용.
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private readonly ConcurrentQueue<Action> _actions = new();

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            while (_actions.TryDequeue(out Action action))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// 메인 스레드에서 액션 실행을 예약.
        /// </summary>
        public static void ExecuteOnMainThread(Action action)
        {
            if (_instance != null)
            {
                _instance._actions.Enqueue(action);
            }
            else
            {
                // 디스패처가 없으면 동기 폴백 (에디터에서만 안전)
                action?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
