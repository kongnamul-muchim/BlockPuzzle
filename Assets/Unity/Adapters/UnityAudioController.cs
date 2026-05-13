using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using UnityEngine;

namespace BlockPuzzle.Unity.Adapters
{
    /// <summary>
    /// 오디오 재생 담당. IAudioService 구현체.
    /// </summary>
    public class UnityAudioController : MonoBehaviour, IAudioService
    {
        [Header("Audio Clips")]
        [SerializeField] private AudioClip _blockClearClip;
        [SerializeField] private AudioClip _blockFallClip;
        [SerializeField] private AudioClip _comboClip;
        [SerializeField] private AudioClip _gameOverClip;
        [SerializeField] private AudioClip _buttonClickClip;

        [Header("Settings")]
        [SerializeField] private float _masterVolume = 1f;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.volume = _masterVolume;

            // GameManager에 IAudioService로 등록
            GameManager.RegisterAudioService(this);
        }

        public void PlayBlockClear()
        {
            PlayClip(_blockClearClip, 1f);
        }

        public void PlayBlockFall()
        {
            PlayClip(_blockFallClip, 0.7f);
        }

        public void PlayChainCombo(int comboCount)
        {
            if (_comboClip != null)
            {
                // 연계 수에 따라 피치 변화
                float pitch = 1f + (comboCount - 2) * 0.1f;
                _audioSource.pitch = Mathf.Clamp(pitch, 0.8f, 2f);
                _audioSource.PlayOneShot(_comboClip, _masterVolume);
                _audioSource.pitch = 1f;
            }
        }

        public void PlayGameOver()
        {
            PlayClip(_gameOverClip, 1f);
        }

        public void PlayButtonClick()
        {
            PlayClip(_buttonClickClip, 0.5f);
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            _audioSource.volume = _masterVolume;
        }

        private void PlayClip(AudioClip clip, float volumeMultiplier)
        {
            if (clip == null) return;
            _audioSource.PlayOneShot(clip, _masterVolume * volumeMultiplier);
        }
    }
}
