using UnityEngine;

namespace ClickerTowerDefense
{
    public class SlowTower : TowerBase, ISellable, IUpgradeable
    {
        [Header("Base Stats")]
        [SerializeField] private float slowRange = 2.6f;
        [SerializeField] private float rangeScale = 0.5f;
        [SerializeField] private float slowMultiplier = 0.5f;
        [SerializeField] private float slowDuration = 0.5f;
        [SerializeField] private float tickInterval = 0.25f;

        [Header("Upgrades")]
        [SerializeField] private int maxLevel = 3;
        [SerializeField] private int[] upgradeCosts = new[] { 12, 22 };
        [SerializeField] private float rangePerLevel = 0.35f;
        [SerializeField] private float slowMultiplierPerLevel = -0.05f;
        [SerializeField] private float durationPerLevel = 0.1f;
        [Header("Visual by Level")]
        [SerializeField] private SpriteRenderer towerSpriteRenderer;
        [SerializeField] private Sprite[] levelSprites = new Sprite[3];

        [Header("Slow Radius Visual")]
        [SerializeField] private SpriteRenderer slowRadiusRenderer;
        [SerializeField] private float showRadiusForSeconds = 0.25f;
        [SerializeField] private Transform slowRadiusTransform;
        [SerializeField] private float circleScale = 1f;
        [Header("Audio")]
        [SerializeField] private AudioClip slowPulseSound;
        [SerializeField, Range(0f, 1f)] private float slowPulseVolume = 0.55f;

        private float nextTickTime;
        private float showTimer;
        private int level = 1;
        private GameManager gameManager;
        private static Sprite cachedCircleSprite;
        private AudioSource slowAudioSource;

        public override float Range => GetEffectiveRange();

        private void Awake()
        {
            gameManager = GameManager.Instance;
            if (towerSpriteRenderer == null)
            {
                towerSpriteRenderer = GetComponent<SpriteRenderer>();
            }

            EnsureRadiusVisual();
            EnsureAudioSource();
            EnsureSellComponent();
            EnsureUpgradeComponent();
            ApplyLevelSprite();
        }

        private void Update()
        {
            UpdateVisual();
            if (Time.time < nextTickTime)
            {
                return;
            }

            var enemies = EnemyRegistry.All;
            Vector3 position = transform.position;
            float effectiveRange = GetEffectiveRange();
            float rangeSqr = effectiveRange * effectiveRange;
            bool applied = false;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                float sqr = (enemy.transform.position - position).sqrMagnitude;
                if (sqr > rangeSqr)
                {
                    continue;
                }

                enemy.ApplySlow(slowMultiplier, slowDuration);
                applied = true;
            }

            nextTickTime = Time.time + tickInterval;
            if (applied)
            {
                showTimer = showRadiusForSeconds;
                if (slowRadiusRenderer != null)
                {
                    slowRadiusRenderer.enabled = true;
                }

                PlaySlowPulseSound();
            }
        }

        public void TryUpgrade()
        {
            if (level >= GetEffectiveMaxLevel() || gameManager == null)
            {
                return;
            }

            int cost = GetUpgradeCost();
            if (!gameManager.SpendCoins(cost))
            {
                return;
            }

            level++;
            ApplyUpgradeStats();
            gameManager.PlayTowerUpgradeSound();
        }

        private int GetUpgradeCost()
        {
            int index = level - 1;
            if (upgradeCosts != null && index >= 0 && index < upgradeCosts.Length)
            {
                return Mathf.Max(0, upgradeCosts[index]);
            }

            return 0;
        }

        private int GetEffectiveMaxLevel()
        {
            int byCosts = upgradeCosts != null ? (upgradeCosts.Length + 1) : 1;
            return Mathf.Max(1, Mathf.Max(maxLevel, byCosts));
        }

        private void ApplyUpgradeStats()
        {
            int levelOffset = Mathf.Max(0, level - 1);
            slowRange = slowRange + (rangePerLevel * levelOffset);
            slowMultiplier = Mathf.Clamp01(slowMultiplier + (slowMultiplierPerLevel * levelOffset));
            slowDuration = slowDuration + (durationPerLevel * levelOffset);
            ApplyLevelSprite();
        }

        private void ApplyLevelSprite()
        {
            if (towerSpriteRenderer == null || levelSprites == null || levelSprites.Length == 0)
            {
                return;
            }

            int spriteIndex = Mathf.Clamp(level - 1, 0, levelSprites.Length - 1);
            Sprite sprite = levelSprites[spriteIndex];
            if (sprite != null)
            {
                towerSpriteRenderer.sprite = sprite;
            }
        }

        private void UpdateVisual()
        {
            if (slowRadiusRenderer == null)
            {
                return;
            }

            UpdateRadiusScale();

            if (showTimer > 0f)
            {
                showTimer -= Time.deltaTime;
                if (showTimer <= 0f)
                {
                    slowRadiusRenderer.enabled = false;
                }
            }
        }

        private void UpdateRadiusScale()
        {
            if (slowRadiusTransform == null)
            {
                return;
            }

            float diameter = GetEffectiveRange() * 2f;
            float spriteSize = 1f;
            if (slowRadiusRenderer.sprite != null)
            {
                spriteSize = slowRadiusRenderer.sprite.bounds.size.x;
                if (spriteSize <= 0f)
                {
                    spriteSize = 1f;
                }
            }

            float scale = (diameter * circleScale) / spriteSize;
            slowRadiusTransform.localScale = new Vector3(scale, scale, 1f);
        }

        private float GetEffectiveRange()
        {
            return slowRange * Mathf.Max(0f, rangeScale);
        }

        public int GetSellValue()
        {
            return CalculateSellValue(level, upgradeCosts);
        }

        private void EnsureSellComponent()
        {
            if (GetComponent<TowerSellOnRightClick>() == null)
            {
                gameObject.AddComponent<TowerSellOnRightClick>();
            }
        }

        private void EnsureUpgradeComponent()
        {
            if (GetComponent<TowerUpgradeOnLeftClick>() == null)
            {
                gameObject.AddComponent<TowerUpgradeOnLeftClick>();
            }
        }

        private void EnsureRadiusVisual()
        {
            if (slowRadiusRenderer != null)
            {
                slowRadiusTransform = slowRadiusRenderer.transform;
                EnsureCircleSprite();
                slowRadiusRenderer.enabled = false;
                return;
            }

            GameObject go = new GameObject("SlowRadius");
            go.transform.SetParent(transform, false);
            slowRadiusTransform = go.transform;
            slowRadiusTransform.localPosition = Vector3.zero;

            slowRadiusRenderer = go.AddComponent<SpriteRenderer>();
            slowRadiusRenderer.color = new Color(1f, 1f, 1f, 0.2f);
            slowRadiusRenderer.sortingOrder = -1;
            EnsureCircleSprite();
            slowRadiusRenderer.enabled = false;
        }

        private void EnsureCircleSprite()
        {
            if (slowRadiusRenderer == null)
            {
                return;
            }

            if (slowRadiusRenderer.sprite != null)
            {
                return;
            }

            if (cachedCircleSprite == null)
            {
                cachedCircleSprite = CreateCircleSprite(256);
            }

            slowRadiusRenderer.sprite = cachedCircleSprite;
        }

        private static Sprite CreateCircleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 4;

            float radius = (size - 2) * 0.5f;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float edge = radius - 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center.x;
                    float dy = y - center.y;
                    float dist = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float alpha = dist <= edge ? 1f : Mathf.Clamp01(1f - (dist - edge));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f);
        }

        private void EnsureAudioSource()
        {
            if (slowAudioSource != null)
            {
                return;
            }

            slowAudioSource = GetComponent<AudioSource>();
            if (slowAudioSource == null)
            {
                slowAudioSource = gameObject.AddComponent<AudioSource>();
            }

            slowAudioSource.playOnAwake = false;
            slowAudioSource.loop = false;
            slowAudioSource.spatialBlend = 0f;
        }

        private void PlaySlowPulseSound()
        {
            if (slowPulseSound == null)
            {
                return;
            }

            EnsureAudioSource();
            if (slowAudioSource != null)
            {
                slowAudioSource.PlayOneShot(slowPulseSound, slowPulseVolume * AudioSettings.SfxVolume);
            }
        }
    }
}
