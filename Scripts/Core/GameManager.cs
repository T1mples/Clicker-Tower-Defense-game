using System;
using System.Collections;
using UnityEngine;
using YG;

namespace ClickerTowerDefense
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private int startingCoins = 0;
        [Header("Audio")]
        [SerializeField] private AudioClip towerClickSound;
        [SerializeField, Range(0f, 1f)] private float towerClickVolume = 1f;
        [SerializeField] private AudioClip towerPlaceSound;
        [SerializeField, Range(0f, 1f)] private float towerPlaceVolume = 1f;
        [SerializeField] private AudioClip towerUpgradeSound;
        [SerializeField, Range(0f, 1f)] private float towerUpgradeVolume = 1f;
        [SerializeField] private AudioClip towerSellSound;
        [SerializeField, Range(0f, 1f)] private float towerSellVolume = 1f;
        [SerializeField] private AudioClip shopUpgradePurchaseSound;
        [SerializeField, Range(0f, 1f)] private float shopUpgradePurchaseVolume = 1f;
        [SerializeField] private AudioClip skillPurchaseSound;
        [SerializeField, Range(0f, 1f)] private float skillPurchaseVolume = 1f;
        [SerializeField] private AudioClip regularEnemyDeathSound;
        [SerializeField, Range(0f, 1f)] private float regularEnemyDeathVolume = 1f;
        [SerializeField] private AudioClip eliteEnemyDeathSound;
        [SerializeField, Range(0f, 1f)] private float eliteEnemyDeathVolume = 1f;
        [SerializeField] private AudioClip bossEnemyDeathSound;
        [SerializeField, Range(0f, 1f)] private float bossEnemyDeathVolume = 1f;
        [SerializeField] private AudioClip gameStartSound;
        [SerializeField, Range(0f, 1f)] private float gameStartVolume = 1f;
        [Header("Music")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.5f;
        [SerializeField] private float backgroundMusicStartDelay = 5f;
        [SerializeField] private bool debugMusicLogs = true;

        public int Coins { get; private set; }
        public int Score { get; private set; }
        public event Action<int> CoinsChanged;
        public event Action<int> ScoreChanged;
        private AudioSource sfxAudioSource;
        private AudioSource musicAudioSource;
        private AudioSource gameplayStartAudioSource;
        private BaseHealth baseHealth;
        private bool musicPausedByStartScreen;
        private Coroutine delayedMusicCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureAudioSources();
            Coins = startingCoins;
            Score = 0;
            CoinsChanged?.Invoke(Coins);
            ScoreChanged?.Invoke(Score);
            ResolveBaseHealth();
        }

        private void OnDestroy()
        {
            if (baseHealth != null)
            {
                baseHealth.GameOverEvent -= OnGameOver;
            }

            if (delayedMusicCoroutine != null)
            {
                StopCoroutine(delayedMusicCoroutine);
                delayedMusicCoroutine = null;
            }
        }

        private void Update()
        {
            SyncMusicPauseWithStartScreen();
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Coins += amount;
            CoinsChanged?.Invoke(Coins);
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (Coins < amount)
            {
                return false;
            }

            Coins -= amount;
            CoinsChanged?.Invoke(Coins);
            return true;
        }

        public void AddScore(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Score += amount;
            ScoreChanged?.Invoke(Score);
        }

        public void PlayTowerClickSound()
        {
            PlayOneShot(towerClickSound, towerClickVolume);
        }

        public void PlayTowerPlaceSound()
        {
            PlayOneShot(towerPlaceSound, towerPlaceVolume);
        }

        public void PlayTowerUpgradeSound()
        {
            PlayOneShot(towerUpgradeSound, towerUpgradeVolume);
        }

        public void PlayTowerSellSound()
        {
            PlayOneShot(towerSellSound, towerSellVolume);
        }

        public void PlayShopUpgradePurchaseSound()
        {
            PlayOneShot(shopUpgradePurchaseSound, shopUpgradePurchaseVolume);
        }

        public void PlaySkillPurchaseSound()
        {
            PlayOneShot(skillPurchaseSound, skillPurchaseVolume);
        }

        public void PlayEnemyDeathSound(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.Boss:
                    PlayOneShot(bossEnemyDeathSound, bossEnemyDeathVolume);
                    break;
                case EnemyType.Elite:
                    PlayOneShot(eliteEnemyDeathSound, eliteEnemyDeathVolume);
                    break;
                default:
                    PlayOneShot(regularEnemyDeathSound, regularEnemyDeathVolume);
                    break;
            }
        }

        public void SetSfxVolume(float value)
        {
            AudioSettings.SetSfxVolume(value);
        }

        public void SetMusicVolume(float value)
        {
            AudioSettings.SetMusicVolume(value);
            UpdateMusicVolume();
        }

        public float GetSfxVolume()
        {
            return AudioSettings.SfxVolume;
        }

        public float GetMusicVolume()
        {
            return AudioSettings.MusicVolume;
        }

        public void StopMusicAndCountdown()
        {
            if (delayedMusicCoroutine != null)
            {
                LogMusic("StopMusicAndCountdown: stop pending music countdown.");
                StopCoroutine(delayedMusicCoroutine);
                delayedMusicCoroutine = null;
            }

            LogMusic("StopMusicAndCountdown: stop current music.");
            StopMusic();
        }

        public void BeginGameplayMusicCountdown()
        {
            EnsureAudioSources();
            if (musicAudioSource == null)
            {
                LogMusic("BeginGameplayMusicCountdown skipped: missing music audio source.");
                return;
            }

            bool gameOver = baseHealth != null && baseHealth.IsGameOver;
            if (gameOver || StartScreenUI.IsOpen)
            {
                LogMusic("BeginGameplayMusicCountdown skipped: game over or start screen is open.");
                return;
            }

            if (musicAudioSource.isPlaying || delayedMusicCoroutine != null)
            {
                LogMusic("BeginGameplayMusicCountdown skipped: already playing or countdown is active.");
                return;
            }

            LogMusic("BeginGameplayMusicCountdown: scheduling music countdown for gameplay start.");
            StartMusicWithDelay();
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null)
            {
                return;
            }

            EnsureAudioSources();
            if (sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(clip, volume * AudioSettings.SfxVolume);
            }
        }

        private void EnsureAudioSources()
        {
            if (sfxAudioSource != null)
            {
                ConfigureSfxSource(sfxAudioSource);
            }
            if (sfxAudioSource == null)
            {
                sfxAudioSource = gameObject.AddComponent<AudioSource>();
                ConfigureSfxSource(sfxAudioSource);
            }

            if (musicAudioSource == null)
            {
                musicAudioSource = gameObject.AddComponent<AudioSource>();
            }
            ConfigureMusicSource(musicAudioSource);

            if (gameplayStartAudioSource == null)
            {
                gameplayStartAudioSource = gameObject.AddComponent<AudioSource>();
            }
            ConfigureSfxSource(gameplayStartAudioSource);
        }

        private void ConfigureSfxSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        private void ConfigureMusicSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.clip = backgroundMusic;
            UpdateMusicVolume();
        }

        private void StartMusic()
        {
            EnsureAudioSources();
            if (musicAudioSource == null || backgroundMusic == null)
            {
                LogMusic("StartMusic skipped: audio source or clip is missing.");
                return;
            }

            musicAudioSource.clip = backgroundMusic;
            UpdateMusicVolume();
            musicAudioSource.Play();
            LogMusic("Music started.");
        }

        private void StartMusicWithDelay()
        {
            if (delayedMusicCoroutine != null)
            {
                LogMusic("StartMusicWithDelay: replacing existing countdown.");
                StopCoroutine(delayedMusicCoroutine);
            }

            LogMusic($"StartMusicWithDelay: countdown started ({Mathf.Max(0f, backgroundMusicStartDelay):0.##}s).");
            delayedMusicCoroutine = StartCoroutine(PlayMusicDelayed());
        }

        private IEnumerator PlayMusicDelayed()
        {
            float remaining = Mathf.Max(0f, backgroundMusicStartDelay);
            bool wasBlockedByExternalState = false;

            while (remaining > 0f)
            {
                bool blocked = IsCountdownBlocked();
                if (blocked)
                {
                    if (!wasBlockedByExternalState)
                    {
                        LogMusic($"PlayMusicDelayed: paused (ads/focus), remaining {remaining:0.##}s.");
                    }

                    wasBlockedByExternalState = true;
                    yield return null;
                    continue;
                }

                if (wasBlockedByExternalState)
                {
                    LogMusic($"PlayMusicDelayed: resumed, remaining {remaining:0.##}s.");
                    wasBlockedByExternalState = false;
                }

                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            delayedMusicCoroutine = null;
            LogMusic("PlayMusicDelayed: countdown finished.");
            StartMusic();
        }

        private void StopMusic()
        {
            if (musicAudioSource == null)
            {
                return;
            }

            musicAudioSource.Stop();
            musicPausedByStartScreen = false;
            LogMusic("Music stopped.");
        }

        private void UpdateMusicVolume()
        {
            if (musicAudioSource == null)
            {
                return;
            }

            musicAudioSource.volume = backgroundMusicVolume * AudioSettings.MusicVolume;
        }

        private void ResolveBaseHealth()
        {
            if (baseHealth != null)
            {
                baseHealth.GameOverEvent -= OnGameOver;
            }

            baseHealth = FindFirstObjectByType<BaseHealth>();
            if (baseHealth != null)
            {
                baseHealth.GameOverEvent += OnGameOver;
            }
        }

        private void OnGameOver()
        {
            StopMusicAndCountdown();
        }

        public void PlayGameplayStartSound()
        {
            if (gameStartSound == null)
            {
                return;
            }

            EnsureAudioSources();
            if (gameplayStartAudioSource == null)
            {
                return;
            }

            // Force restart from beginning even if this method fires more than once.
            gameplayStartAudioSource.Stop();
            gameplayStartAudioSource.clip = gameStartSound;
            gameplayStartAudioSource.time = 0f;
            gameplayStartAudioSource.volume = gameStartVolume * AudioSettings.SfxVolume;
            gameplayStartAudioSource.Play();
        }

        private void SyncMusicPauseWithStartScreen()
        {
            if (musicAudioSource == null)
            {
                return;
            }

            bool shouldPause = StartScreenUI.IsOpen;
            if (shouldPause)
            {
                if (!musicPausedByStartScreen && musicAudioSource.isPlaying)
                {
                    musicAudioSource.Pause();
                    musicPausedByStartScreen = true;
                }

                return;
            }

            if (musicPausedByStartScreen)
            {
                bool gameOver = baseHealth != null && baseHealth.IsGameOver;
                if (!gameOver)
                {
                    musicAudioSource.UnPause();
                }

                musicPausedByStartScreen = false;
            }
        }

        private void LogMusic(string message)
        {
            if (!debugMusicLogs)
            {
                return;
            }

            Debug.Log("[Music] " + message);
        }

        private static bool IsCountdownBlocked()
        {
            return YG2.nowAdsShow || !YG2.isFocusWindowGame || GameMenuUI.IsMenuOpen;
        }
    }
}
