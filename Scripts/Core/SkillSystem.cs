using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

namespace ClickerTowerDefense
{
    public enum SkillType
    {
        None = 0,
        Freeze = 1,
        MegaStrike = 2
    }

    public class SkillSystem : MonoBehaviour
    {
        public static SkillSystem Instance { get; private set; }

        public const float SkillCooldownSeconds = 300f;
        public const float FreezeDurationSeconds = 5f;
        public const int FreezeCost = 2000;
        public const int MegaStrikeCost = 10000;

        [Header("Audio")]
        [SerializeField] private AudioClip freezeUseSound;
        [SerializeField, Range(0f, 1f)] private float freezeUseVolume = 1f;
        [SerializeField] private AudioClip megaStrikeUseSound;
        [SerializeField, Range(0f, 1f)] private float megaStrikeUseVolume = 1f;
        [Header("Runtime Fallback (Build)")]
        [SerializeField] private string freezeUseSoundResourcesPath = "Audio/SkillFreeze";
        [SerializeField] private string megaStrikeUseSoundResourcesPath = "Audio/SkillMegaStrike";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null || FindFirstObjectByType<SkillSystem>() != null)
            {
                return;
            }

            GameObject go = new GameObject("SkillSystem");
            go.AddComponent<SkillSystem>();
        }

        public SkillType OwnedSkill { get; private set; }
        public float NextUseTime { get; private set; }
        public float FreezeUseVolume => freezeUseVolume;
        public float MegaStrikeUseVolume => megaStrikeUseVolume;

        public event Action StateChanged;
        public event Action<SkillType> SkillUsed;

        private GameManager gameManager;
        private EnemySpawner enemySpawner;
        private BaseHealth baseHealth;
        private bool isGameOver;
        private AudioSource sfxAudioSource;
        private bool cooldownPausedByFocus;
        private bool cooldownPausedByMenu;
        private float remainingCooldownAtPause;
        private bool lastMenuOpenState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureAudioSource();
            EnsureSkillUseAudioLoaded();
            gameManager = GameManager.Instance;
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
            baseHealth = FindFirstObjectByType<BaseHealth>();
            if (baseHealth != null)
            {
                baseHealth.GameOverEvent += OnGameOver;
            }

            YG2.onHideWindowGame += OnWindowHidden;
            YG2.onShowWindowGame += OnWindowShown;
            lastMenuOpenState = GameMenuUI.IsMenuOpen;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }

            if (baseHealth != null)
            {
                baseHealth.GameOverEvent -= OnGameOver;
            }

            YG2.onHideWindowGame -= OnWindowHidden;
            YG2.onShowWindowGame -= OnWindowShown;
        }

        private void Update()
        {
            bool menuOpen = GameMenuUI.IsMenuOpen;
            if (menuOpen == lastMenuOpenState)
            {
                return;
            }

            lastMenuOpenState = menuOpen;
            if (menuOpen)
            {
                OnMenuOpened();
            }
            else
            {
                OnMenuClosed();
            }
        }

        public static int GetCost(SkillType skill)
        {
            switch (skill)
            {
                case SkillType.Freeze:
                    return FreezeCost;
                case SkillType.MegaStrike:
                    return MegaStrikeCost;
                default:
                    return 0;
            }
        }

        public static string GetDisplayName(SkillType skill)
        {
            switch (skill)
            {
                case SkillType.Freeze:
                    return "Freeze";
                case SkillType.MegaStrike:
                    return "Mega Strike";
                default:
                    return "None";
            }
        }

        public bool TryPurchase(SkillType skill)
        {
            if (skill == SkillType.None)
            {
                return false;
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (gameManager == null)
            {
                return false;
            }

            int cost = GetCost(skill);
            if (!gameManager.SpendCoins(cost))
            {
                return false;
            }

            OwnedSkill = skill;
            // Shared cooldown across all skills:
            // buying a different skill must not reset remaining cooldown.
            StateChanged?.Invoke();
            return true;
        }

        public bool CanUseNow()
        {
            return !isGameOver && !IsCooldownPaused() && OwnedSkill != SkillType.None && Time.unscaledTime >= NextUseTime;
        }

        public bool TryUseOwnedSkill()
        {
            if (!CanUseNow())
            {
                return false;
            }

            switch (OwnedSkill)
            {
                case SkillType.Freeze:
                    UseFreeze();
                    break;
                case SkillType.MegaStrike:
                    UseMegaStrike();
                    break;
                default:
                    return false;
            }

            SkillUsed?.Invoke(OwnedSkill);
            NextUseTime = Time.unscaledTime + SkillCooldownSeconds;
            StateChanged?.Invoke();
            return true;
        }

        public float GetRemainingCooldown()
        {
            if (IsCooldownPaused())
            {
                return Mathf.Max(0f, remainingCooldownAtPause);
            }

            return Mathf.Max(0f, NextUseTime - Time.unscaledTime);
        }

        public void SetFreezeUseVolume(float volume)
        {
            freezeUseVolume = Mathf.Clamp01(volume);
        }

        public void SetMegaStrikeUseVolume(float volume)
        {
            megaStrikeUseVolume = Mathf.Clamp01(volume);
        }

        private void UseFreeze()
        {
            var enemies = EnemyRegistry.All;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                enemy.ApplyFreeze(FreezeDurationSeconds);
            }

            if (enemySpawner == null)
            {
                enemySpawner = FindFirstObjectByType<EnemySpawner>();
            }

            if (enemySpawner != null)
            {
                enemySpawner.AddSpawnDelay(FreezeDurationSeconds);
            }

            PlayOneShot(freezeUseSound, freezeUseVolume);
        }

        private void UseMegaStrike()
        {
            var enemies = EnemyRegistry.All;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                int damage = Mathf.CeilToInt(enemy.CurrentHealth * 0.5f);
                enemy.TakeDamage(Mathf.Max(1, damage));
            }

            PlayOneShot(megaStrikeUseSound, megaStrikeUseVolume);
        }

        private void EnsureAudioSource()
        {
            if (sfxAudioSource != null)
            {
                return;
            }

            sfxAudioSource = GetComponent<AudioSource>();
            if (sfxAudioSource == null)
            {
                sfxAudioSource = gameObject.AddComponent<AudioSource>();
            }

            sfxAudioSource.playOnAwake = false;
            sfxAudioSource.loop = false;
            sfxAudioSource.spatialBlend = 0f;
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null)
            {
                Debug.LogWarning("[SkillSystem] Skill use sound is missing.");
                return;
            }

            EnsureAudioSource();
            if (sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(clip, volume * AudioSettings.SfxVolume);
            }
        }

        private void EnsureSkillUseAudioLoaded()
        {
            if (freezeUseSound == null && !string.IsNullOrWhiteSpace(freezeUseSoundResourcesPath))
            {
                freezeUseSound = Resources.Load<AudioClip>(freezeUseSoundResourcesPath);
            }

            if (megaStrikeUseSound == null && !string.IsNullOrWhiteSpace(megaStrikeUseSoundResourcesPath))
            {
                megaStrikeUseSound = Resources.Load<AudioClip>(megaStrikeUseSoundResourcesPath);
            }
        }

        private void OnGameOver()
        {
            isGameOver = true;
            StateChanged?.Invoke();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (baseHealth != null)
            {
                baseHealth.GameOverEvent -= OnGameOver;
            }

            gameManager = GameManager.Instance;
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
            baseHealth = FindFirstObjectByType<BaseHealth>();
            if (baseHealth != null)
            {
                baseHealth.GameOverEvent += OnGameOver;
            }

            OwnedSkill = SkillType.None;
            NextUseTime = 0f;
            isGameOver = false;
            cooldownPausedByFocus = false;
            cooldownPausedByMenu = false;
            remainingCooldownAtPause = 0f;
            lastMenuOpenState = GameMenuUI.IsMenuOpen;
            StateChanged?.Invoke();
        }

        private void OnWindowHidden()
        {
            if (cooldownPausedByFocus)
            {
                return;
            }

            PauseCooldown();
            cooldownPausedByFocus = true;
            StateChanged?.Invoke();
        }

        private void OnWindowShown()
        {
            if (!cooldownPausedByFocus)
            {
                return;
            }

            cooldownPausedByFocus = false;
            TryResumeCooldown();
            StateChanged?.Invoke();
        }

        private void OnMenuOpened()
        {
            if (cooldownPausedByMenu)
            {
                return;
            }

            PauseCooldown();
            cooldownPausedByMenu = true;
            StateChanged?.Invoke();
        }

        private void OnMenuClosed()
        {
            if (!cooldownPausedByMenu)
            {
                return;
            }

            cooldownPausedByMenu = false;
            TryResumeCooldown();
            StateChanged?.Invoke();
        }

        private void PauseCooldown()
        {
            if (IsCooldownPaused())
            {
                return;
            }

            float remaining = Mathf.Max(0f, NextUseTime - Time.unscaledTime);
            if (remaining <= 0f)
            {
                return;
            }

            remainingCooldownAtPause = remaining;
        }

        private void TryResumeCooldown()
        {
            if (IsCooldownPaused())
            {
                return;
            }

            if (remainingCooldownAtPause <= 0f)
            {
                return;
            }

            NextUseTime = Time.unscaledTime + remainingCooldownAtPause;
            remainingCooldownAtPause = 0f;
        }

        private bool IsCooldownPaused()
        {
            return cooldownPausedByFocus || cooldownPausedByMenu;
        }
    }
}
