using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClickerTowerDefense
{
    public class BaseHealth : MonoBehaviour
    {
        private static readonly int[] DefaultBaseHealthUpgradeCosts = { 100, 800, 5000, 20000 };
        [SerializeField] private int maxHealth = 20;
        [Header("Base Upgrades")]
        [SerializeField] private int hpPerUpgrade = 5;
        [SerializeField] private int[] baseHealthUpgradeCosts = new[] { 100, 800, 5000, 20000 };
        [Header("Audio")]
        [SerializeField] private AudioClip baseDamageSound;
        [SerializeField, Range(0f, 1f)] private float baseDamageVolume = 1f;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsGameOver => isGameOver;
        public int BaseUpgradeLevel { get; private set; }
        public int MaxBaseUpgradeLevel => baseHealthUpgradeCosts != null ? baseHealthUpgradeCosts.Length : 0;
        public bool IsBaseUpgradeMaxed => BaseUpgradeLevel >= MaxBaseUpgradeLevel;

        public event Action<int, int> HealthChanged;
        public event Action GameOverEvent;

        private bool isGameOver;
        private AudioSource sfxAudioSource;

        private void Awake()
        {
            EnsureUpgradeCostsConfigured();
            BaseUpgradeLevel = 0;
            CurrentHealth = Mathf.Max(1, maxHealth);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            EnsureAudioSource();
        }

        public void TakeDamage(int amount)
        {
            if (isGameOver || amount <= 0)
            {
                return;
            }

            int previous = CurrentHealth;
            CurrentHealth -= amount;
            if (CurrentHealth != previous)
            {
                PlayDamageSound();
            }

            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                HealthChanged?.Invoke(CurrentHealth, maxHealth);
                GameOver();
                return;
            }

            if (CurrentHealth != previous)
            {
                HealthChanged?.Invoke(CurrentHealth, maxHealth);
            }
        }

        public void ForceGameOverWithoutDamageSound()
        {
            if (isGameOver)
            {
                return;
            }

            CurrentHealth = 0;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            GameOver();
        }

        public int Heal(int amount)
        {
            if (isGameOver || amount <= 0 || CurrentHealth >= maxHealth)
            {
                return 0;
            }

            int previous = CurrentHealth;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            if (CurrentHealth != previous)
            {
                HealthChanged?.Invoke(CurrentHealth, maxHealth);
            }

            return CurrentHealth - previous;
        }

        private void GameOver()
        {
            isGameOver = true;
            GameOverEvent?.Invoke();
        }

        public void RestartScene()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.StopMusicAndCountdown();
            }

            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        public int GetNextBaseUpgradeCost()
        {
            if (IsBaseUpgradeMaxed || baseHealthUpgradeCosts == null)
            {
                return 0;
            }

            return Mathf.Max(0, baseHealthUpgradeCosts[BaseUpgradeLevel]);
        }

        public bool TryUpgradeBaseHealth()
        {
            if (IsBaseUpgradeMaxed)
            {
                return false;
            }

            GameManager gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                return false;
            }

            int cost = GetNextBaseUpgradeCost();
            if (!gameManager.SpendCoins(cost))
            {
                return false;
            }

            BaseUpgradeLevel++;
            maxHealth += Mathf.Max(1, hpPerUpgrade);
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + Mathf.Max(1, hpPerUpgrade));
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            return true;
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

        private void PlayDamageSound()
        {
            if (baseDamageSound == null)
            {
                return;
            }

            EnsureAudioSource();
            if (sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(baseDamageSound, baseDamageVolume * AudioSettings.SfxVolume);
            }
        }

        private void EnsureUpgradeCostsConfigured()
        {
            if (baseHealthUpgradeCosts == null || baseHealthUpgradeCosts.Length != DefaultBaseHealthUpgradeCosts.Length)
            {
                baseHealthUpgradeCosts = (int[])DefaultBaseHealthUpgradeCosts.Clone();
                return;
            }

            bool outOfRange = false;
            for (int i = 0; i < baseHealthUpgradeCosts.Length; i++)
            {
                if (baseHealthUpgradeCosts[i] > 20000 || baseHealthUpgradeCosts[i] < 0)
                {
                    outOfRange = true;
                    break;
                }
            }

            if (outOfRange)
            {
                baseHealthUpgradeCosts = (int[])DefaultBaseHealthUpgradeCosts.Clone();
            }
        }
    }
}
