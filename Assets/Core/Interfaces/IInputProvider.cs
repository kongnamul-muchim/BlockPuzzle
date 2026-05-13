using System;

namespace BlockPuzzle.Core.Interfaces
{
    /// <summary>
    /// 입력 추상화 인터페이스.
    /// Unity Adapter(UnityInputDetector)가 구현하여 Core에 입력 전달.
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>블럭 클릭 이벤트 (격자 좌표)</summary>
        event Action<int, int> OnBlockClicked;

        /// <summary>입력 처리 활성화/비활성화</summary>
        void SetEnabled(bool enabled);
    }
}
