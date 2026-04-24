using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EndlessRunner
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("BGM")]
        [SerializeField] private AudioClip bgmMenu;
        [SerializeField] private AudioClip bgmGameplay;
        [SerializeField] private AudioClip bgmGameOver;
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.5f;
        [SerializeField] private float bgmFadeDuration = 0.6f;

        [Header("SFX")]
        [SerializeField] private AudioClip sfxButtonClick;
        [SerializeField] private AudioClip sfxStomp;
        [SerializeField] private AudioClip sfxDamage;
        [SerializeField] private AudioClip sfxDeath;
        [SerializeField] private AudioClip sfxCollectible;
        [SerializeField] private AudioClip sfxAbilityActivate;
        [SerializeField] private AudioClip sfxAbilityAcquired;
        [SerializeField] private AudioClip sfxAchievement;
        [SerializeField] private AudioClip sfxPause;
        [SerializeField] private AudioClip sfxResume;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

        private AudioSource bgmSource;
        private AudioSource sfxSource;
        private Coroutine fadeRoutine;

        private const string MasterVolumeKey = "settings.master_volume";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.priority = 0;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.priority = 128;

            LoadVolumeSettings();
            GeneratePlaceholderClips();
            EnsureAudioListener();
        }

        private void Start()
        {
            BindToGameManager();
            PlayBGMForCurrentState();
        }

        private void OnEnable()
        {
            BindToGameManager();
            AchievementManager.AchievementUnlocked += OnAchievementUnlocked;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
            }

            AchievementManager.AchievementUnlocked -= OnAchievementUnlocked;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }

        public void PlayButtonClick() => PlaySFX(sfxButtonClick);
        public void PlayStomp() => PlaySFX(sfxStomp);
        public void PlayDamage() => PlaySFX(sfxDamage);
        public void PlayDeath() => PlaySFX(sfxDeath);
        public void PlayCollectible() => PlaySFX(sfxCollectible);
        public void PlayAbilityActivate() => PlaySFX(sfxAbilityActivate);
        public void PlayAbilityAcquired() => PlaySFX(sfxAbilityAcquired);
        public void PlayAchievement() => PlaySFX(sfxAchievement);
        public void PlayPause() => PlaySFX(sfxPause);
        public void PlayResume() => PlaySFX(sfxResume);

        public void PlayBGM(AudioClip clip)
        {
            if (bgmSource == null)
            {
                return;
            }

            if (bgmSource.clip == clip && bgmSource.isPlaying)
            {
                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            fadeRoutine = StartCoroutine(CrossFadeBGM(clip));
        }

        public void StopBGM()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            fadeRoutine = StartCoroutine(FadeOutBGM());
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyBGMVolume();
            AudioListener.volume = masterVolume;
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        }

        public float GetMasterVolume() => masterVolume;

        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            ApplyBGMVolume();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        private GameState lastState = GameState.Boot;

        private void OnGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Menu:
                    PlayBGM(bgmMenu);
                    break;
                case GameState.Running:
                    if (lastState == GameState.Paused)
                    {
                        PlayResume();
                    }
                    PlayBGM(bgmGameplay);
                    break;
                case GameState.Paused:
                    PlayPause();
                    break;
                case GameState.GameOver:
                    PlayDeath();
                    PlayBGM(bgmGameOver);
                    break;
            }

            lastState = state;
        }

        private void OnAchievementUnlocked(AchievementManager.AchievementDefinition def)
        {
            PlayAchievement();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BindToGameManager();
            EnsureAudioListener();
            PlayBGMForCurrentState();
        }

        private void BindToGameManager()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
                GameManager.Instance.StateChanged += OnGameStateChanged;
            }
        }

        private void PlayBGMForCurrentState()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            OnGameStateChanged(GameManager.Instance.State);
        }

        private void EnsureAudioListener()
        {
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
        }

        private void LoadVolumeSettings()
        {
            if (PlayerPrefs.HasKey(MasterVolumeKey))
            {
                float saved = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
                masterVolume = Mathf.Clamp01(saved);
                if (masterVolume < 0.01f)
                {
                    masterVolume = 1f;
                }
            }

            AudioListener.volume = masterVolume;
            ApplyBGMVolume();
        }

        private void ApplyBGMVolume()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = bgmVolume * masterVolume;
            }
        }

        private IEnumerator CrossFadeBGM(AudioClip newClip)
        {
            float halfDuration = bgmFadeDuration * 0.5f;

            if (bgmSource.isPlaying && halfDuration > 0f)
            {
                float startVol = bgmSource.volume;
                float elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / halfDuration);
                    yield return null;
                }
            }

            bgmSource.Stop();
            bgmSource.clip = newClip;

            if (newClip != null)
            {
                bgmSource.volume = 0f;
                bgmSource.Play();

                float targetVol = bgmVolume * masterVolume;
                float elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    bgmSource.volume = Mathf.Lerp(0f, targetVol, elapsed / halfDuration);
                    yield return null;
                }

                bgmSource.volume = targetVol;
            }

            fadeRoutine = null;
        }

        private IEnumerator FadeOutBGM()
        {
            if (!bgmSource.isPlaying)
            {
                fadeRoutine = null;
                yield break;
            }

            float startVol = bgmSource.volume;
            float elapsed = 0f;
            while (elapsed < bgmFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / bgmFadeDuration);
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.clip = null;
            fadeRoutine = null;
        }

        private void GeneratePlaceholderClips()
        {
            if (bgmMenu == null) bgmMenu = CreatePlaceholderTone("BGM_Menu", 220f, 4f);
            if (bgmGameplay == null) bgmGameplay = CreatePlaceholderTone("BGM_Gameplay", 330f, 4f);
            if (bgmGameOver == null) bgmGameOver = CreatePlaceholderTone("BGM_GameOver", 165f, 3f);
            if (sfxButtonClick == null) sfxButtonClick = CreatePlaceholderTone("SFX_Click", 880f, 0.08f);
            if (sfxStomp == null) sfxStomp = CreatePlaceholderTone("SFX_Stomp", 200f, 0.15f);
            if (sfxDamage == null) sfxDamage = CreatePlaceholderTone("SFX_Damage", 150f, 0.2f);
            if (sfxDeath == null) sfxDeath = CreatePlaceholderTone("SFX_Death", 100f, 0.5f);
            if (sfxCollectible == null) sfxCollectible = CreatePlaceholderTone("SFX_Collect", 660f, 0.12f);
            if (sfxAbilityActivate == null) sfxAbilityActivate = CreatePlaceholderTone("SFX_AbilityActivate", 440f, 0.18f);
            if (sfxAbilityAcquired == null) sfxAbilityAcquired = CreatePlaceholderTone("SFX_AbilityAcquired", 523f, 0.3f);
            if (sfxAchievement == null) sfxAchievement = CreatePlaceholderTone("SFX_Achievement", 784f, 0.4f);
            if (sfxPause == null) sfxPause = CreatePlaceholderTone("SFX_Pause", 500f, 0.1f);
            if (sfxResume == null) sfxResume = CreatePlaceholderTone("SFX_Resume", 600f, 0.1f);
        }

        private static AudioClip CreatePlaceholderTone(string name, float frequency, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (float)i / sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.5f * envelope;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
