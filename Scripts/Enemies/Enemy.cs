using System;
using UnityEngine;

namespace ClickerTowerDefense
{
    public enum EnemyType
    {
        Regular = 0,
        Elite = 1,
        Boss = 2
    }

    public class Enemy : MonoBehaviour
    {
        private const int BossBaseHealOnKill = 10;
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private int maxHealth = 5;
        [SerializeField] private int coinReward = 1;
        [SerializeField] private int baseDamage = 1;
        [SerializeField] private bool isBoss;
        [SerializeField] private bool isElite;
        [SerializeField] private float slowMultiplier = 1f;
        [SerializeField] private float slowTimer;
        [SerializeField] private float freezeTimer;
        [SerializeField] private float slowEffectiveness = 1f;
        [SerializeField] private float freezeEffectiveness = 1f;
        [Header("Rendering")]
        [SerializeField] private bool forceSorting = true;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 30;
        [Header("Status Visual")]
        [SerializeField] private Color slowBlueTint = new Color(0.35f, 0.65f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float slowTintStrength = 0.55f;
        [SerializeField] private Color freezeBlueTint = new Color(0.1f, 0.45f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float freezeTintStrength = 0.85f;

        private const int RegularKillScore = 10;
        private const int BossKillScore = 100;
        private const int EliteBaseDamage = 10;
        private const int BossBaseDamage = 15;

        public event Action<Enemy> ReachedGoal;
        public event Action<Enemy> Removed;
        public event Action<int, int> HealthChanged;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsAlive => CurrentHealth > 0;

        private Path path;
        private int waypointIndex;
        private BaseHealth baseHealth;
        private SpriteRenderer[] cachedRenderers;
        private Color[] baseRendererColors;
        private bool isStatusTintApplied;
        private EnemyType enemyType;

        public void Initialize(
            Path pathToFollow,
            float speed,
            int health,
            EnemyType type = EnemyType.Regular,
            float slowEff = 1f,
            float freezeEff = 1f)
        {
            path = pathToFollow;
            waypointIndex = 0;
            moveSpeed = speed;
            maxHealth = Mathf.Max(1, health);
            enemyType = type;
            isBoss = type == EnemyType.Boss;
            isElite = type == EnemyType.Elite;
            slowEffectiveness = Mathf.Clamp01(slowEff);
            freezeEffectiveness = Mathf.Clamp01(freezeEff);
            if (isBoss)
            {
                baseDamage = BossBaseDamage;
            }
            else if (isElite)
            {
                baseDamage = EliteBaseDamage;
            }
            CurrentHealth = maxHealth;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            baseHealth = FindFirstObjectByType<BaseHealth>();
            slowMultiplier = 1f;
            slowTimer = 0f;
            freezeTimer = 0f;
            ApplySorting();
        }

        private void OnEnable()
        {
            EnemyRegistry.Register(this);
            ApplySorting();
        }

        private void OnDisable()
        {
            EnemyRegistry.Unregister(this);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0)
            {
                return;
            }

            CurrentHealth -= amount;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                if (coinReward > 0)
                {
                    gameManager.AddCoins(coinReward);
                }

                gameManager.AddScore(isBoss ? BossKillScore : RegularKillScore);
                gameManager.PlayEnemyDeathSound(enemyType);
            }

            if (isBoss)
            {
                if (baseHealth == null)
                {
                    baseHealth = FindFirstObjectByType<BaseHealth>();
                }

                if (baseHealth != null)
                {
                    baseHealth.Heal(BossBaseHealOnKill);
                }
            }

            HandleRemoved();
        }

        private void Update()
        {
            if (path == null || path.Waypoints == null || path.Waypoints.Length == 0)
            {
                return;
            }

            UpdateSlow();
            UpdateFreeze();
            UpdateStatusTint();

            Transform target = path.GetWaypoint(waypointIndex);
            if (target == null)
            {
                return;
            }

            Vector3 current = transform.position;
            float speed = freezeTimer > 0f ? 0f : moveSpeed * slowMultiplier;
            Vector3 next = Vector3.MoveTowards(current, target.position, speed * Time.deltaTime);
            transform.position = next;

            if (Vector3.Distance(next, target.position) <= 0.01f)
            {
                waypointIndex++;
                if (waypointIndex >= path.Waypoints.Length)
                {
                    ReachedGoal?.Invoke(this);
                    if (baseHealth != null && baseDamage > 0)
                    {
                        baseHealth.TakeDamage(baseDamage);
                    }
                    HandleRemoved();
                }
            }
        }

        private void HandleRemoved()
        {
            Removed?.Invoke(this);
            Destroy(gameObject);
        }

        public void ApplySlow(float multiplier, float duration)
        {
            if (multiplier <= 0f || duration <= 0f)
            {
                return;
            }

            float effectiveMultiplier = 1f - ((1f - multiplier) * slowEffectiveness);
            effectiveMultiplier = Mathf.Clamp(effectiveMultiplier, 0.05f, 1f);
            slowMultiplier = Mathf.Min(slowMultiplier, effectiveMultiplier);
            slowTimer = Mathf.Max(slowTimer, duration);
            UpdateStatusTint();
        }

        public void ApplyFreeze(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            float effectiveDuration = duration * freezeEffectiveness;
            if (effectiveDuration <= 0f)
            {
                return;
            }

            freezeTimer = Mathf.Max(freezeTimer, effectiveDuration);
            UpdateStatusTint();
        }

        private void UpdateSlow()
        {
            if (slowTimer <= 0f)
            {
                slowMultiplier = 1f;
                return;
            }

            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
            {
                slowMultiplier = 1f;
            }
        }

        private void UpdateFreeze()
        {
            if (freezeTimer <= 0f)
            {
                return;
            }

            freezeTimer -= Time.deltaTime;
            if (freezeTimer < 0f)
            {
                freezeTimer = 0f;
            }
        }

        private void UpdateStatusTint()
        {
            bool shouldTint = slowTimer > 0f || freezeTimer > 0f;
            if (!shouldTint && !isStatusTintApplied)
            {
                return;
            }

            EnsureRendererColorCache();
            if (cachedRenderers == null || baseRendererColors == null)
            {
                return;
            }

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                SpriteRenderer sr = cachedRenderers[i];
                if (sr == null || IsStatusUiRenderer(sr))
                {
                    continue;
                }

                Color baseColor = i < baseRendererColors.Length ? baseRendererColors[i] : Color.white;
                if (!shouldTint)
                {
                    sr.color = baseColor;
                    continue;
                }

                bool isFrozen = freezeTimer > 0f;
                Color targetTint = isFrozen ? freezeBlueTint : slowBlueTint;
                float targetStrength = isFrozen ? freezeTintStrength : slowTintStrength;
                float tintAmount = Mathf.Clamp01(targetStrength * targetTint.a);
                Color tinted = baseColor;
                tinted.r = Mathf.Lerp(baseColor.r, targetTint.r, tintAmount);
                tinted.g = Mathf.Lerp(baseColor.g, targetTint.g, tintAmount);
                tinted.b = Mathf.Lerp(baseColor.b, targetTint.b, tintAmount);
                tinted.a = baseColor.a; // keep original opacity, apply only blue filter
                sr.color = tinted;
            }

            isStatusTintApplied = shouldTint;
        }

        private bool IsStatusUiRenderer(SpriteRenderer renderer)
        {
            return renderer.GetComponentInParent<EnemyHealthBar>() != null;
        }

        private void ApplySorting()
        {
            if (!forceSorting)
            {
                return;
            }

            if (cachedRenderers == null || cachedRenderers.Length == 0)
            {
                cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }

            EnsureRendererColorCache();

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                SpriteRenderer sr = cachedRenderers[i];
                if (sr == null)
                {
                    continue;
                }

                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = sr.gameObject == gameObject ? sortingOrder : sortingOrder + 1;
            }
        }

        private void EnsureRendererColorCache()
        {
            if (cachedRenderers == null || cachedRenderers.Length == 0)
            {
                return;
            }

            if (baseRendererColors != null && baseRendererColors.Length == cachedRenderers.Length)
            {
                return;
            }

            baseRendererColors = new Color[cachedRenderers.Length];
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                SpriteRenderer sr = cachedRenderers[i];
                baseRendererColors[i] = sr != null ? sr.color : Color.white;
            }
        }
    }
}
