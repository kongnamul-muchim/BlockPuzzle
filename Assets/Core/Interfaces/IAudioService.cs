namespace BlockPuzzle.Core.Interfaces
{
    /// <summary>
    /// 오디오 추상화 인터페이스.
    /// Unity Adapter(UnityAudioController)가 구현.
    /// </summary>
    public interface IAudioService
    {
        void PlayBlockClear();
        void PlayBlockFall();
        void PlayChainCombo(int comboCount);
        void PlayGameOver();
        void PlayButtonClick();
        void SetMasterVolume(float volume);
    }
}
