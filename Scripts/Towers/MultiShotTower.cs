using UnityEngine;

namespace ClickerTowerDefense
{
public class MultiShotTower : TowerBase, ISellable, IUpgradeable
    {
        [Header("Base Stats")]
        [SerializeField] private float attackRange = 2.2f;
        [SerializeField] private float rangeScale = 0.5f;
        [SerializeField] private float attacksPerSecond = 0.6f;
        [SerializeField] private int damage = 1;
        [SerializeField] private int maxTargets = 3;
        [SerializeField] private bool hitAllInRange = false;

        [Header("Upgrades")]
        [SerializeField] private int maxLevel = 3;
        [SerializeField] private int[] upgradeCosts = new[] { 18, 30 };
        [SerializeField] private float rangePerLevel = 0.3f;
        [SerializeField] private float attacksPerSecondPerLevel = 0.15f;
        [SerializeField] private int damagePerLevel = 1;
        [SerializeField] private int extraTargetsPerLevel = 1;
        [Header("Visual by Level")]
        [SerializeField] private SpriteRenderer towerSpriteRenderer;
        [SerializeField] private Sprite[] levelSprites = new Sprite[3];

        [Header("Beam")]
        [SerializeField] private LineRenderer[] beamRenderers;
        [SerializeField] private float beamDuration = 0.08f;
        [Header("Audio")]
        [SerializeField] private AudioClip shootSound;
        [SerializeField, Range(0f, 1f)] private float shootVolume = 0.65f;

        private float nextAttackTime;
        private int level = 1;
        private GameManager gameManager;
        private float beamTimer;
        private readonly Enemy[] lastTargets = new Enemy[8];
        private readonly System.Collections.Generic.List<Enemy> targetBuffer = new System.Collections.Generic.List<Enemy>(16);
        private readonly System.Collections.Generic.List<float> distanceBuffer = new System.Collections.Generic.List<float>(16);
        private AudioSource shootAudioSource;

        public override float Range => GetEffectiveRange();

        private void Awake()
        {
            gameManager = GameManager.Instance;
            if (towerSpriteRenderer == null)
            {
                towerSpriteRenderer = GetComponent<SpriteRenderer>();
            }

            EnsureBeams();
            DisableBeams();
            EnsureAudioSource();
            EnsureSellComponent();
            EnsureUpgradeComponent();
            ApplyLevelSprite();
        }

        private void Update()
        {
            UpdateBeams();
            if (Time.time < nextAttackTime)
            {
                return;
            }

            int hitCount = 0;
            var enemies = EnemyRegistry.All;
            Vector3 position = transform.position;
            float effectiveRange = GetEffectiveRange();
            float rangeSqr = effectiveRange * effectiveRange;

            int cap = hitAllInRange ? int.MaxValue : Mathf.Max(1, maxTargets);
            BuildTargetList(enemies, position, rangeSqr, cap);

            for (int i = 0; i < targetBuffer.Count && hitCount < cap; i++)
            {
                Enemy enemy = targetBuffer[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                enemy.TakeDamage(damage);
                lastTargets[hitCount] = enemy;
                hitCount++;
            }

            if (hitCount > 0)
            {
                nextAttackTime = Time.time + (1f / Mathf.Max(0.01f, attacksPerSecond));
                FireBeams(hitCount);
                PlayShootSound();
            }
        }

        private void BuildTargetList(
            System.Collections.Generic.IReadOnlyList<Enemy> enemies,
            Vector3 position,
            float rangeSqr,
            int cap)
        {
            targetBuffer.Clear();
            distanceBuffer.Clear();

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

                InsertByDistance(enemy, sqr, cap);
            }
        }

        private void InsertByDistance(Enemy enemy, float sqr, int cap)
        {
            int insertIndex = 0;
            while (insertIndex < distanceBuffer.Count && sqr >= distanceBuffer[insertIndex])
            {
                insertIndex++;
            }

            if (insertIndex > cap)
            {
                return;
            }

            targetBuffer.Insert(insertIndex, enemy);
            distanceBuffer.Insert(insertIndex, sqr);

            if (targetBuffer.Count > cap)
            {
                int last = targetBuffer.Count - 1;
                targetBuffer.RemoveAt(last);
                distanceBuffer.RemoveAt(last);
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
            attackRange = attackRange + (rangePerLevel * levelOffset);
            attacksPerSecond = attacksPerSecond + (attacksPerSecondPerLevel * levelOffset);
            damage = damage + (damagePerLevel * levelOffset);
            maxTargets = maxTargets + (extraTargetsPerLevel * levelOffset);
            ApplyLevelSprite();
        }

        private float GetEffectiveRange()
        {
            return attackRange * Mathf.Max(0f, rangeScale);
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

        private void FireBeams(int hitCount)
        {
            if (beamRenderers == null || beamRenderers.Length == 0)
            {
                return;
            }

            beamTimer = beamDuration;
            int count = Mathf.Min(hitCount, beamRenderers.Length);
            for (int i = 0; i < count; i++)
            {
                LineRenderer lr = beamRenderers[i];
                Enemy target = lastTargets[i];
                if (lr == null || target == null)
                {
                    continue;
                }

                lr.enabled = true;
                lr.positionCount = 2;
                lr.SetPosition(0, transform.position);
                lr.SetPosition(1, target.transform.position);
            }
        }

        private void UpdateBeams()
        {
            if (beamRenderers == null || beamRenderers.Length == 0)
            {
                return;
            }

            if (beamTimer <= 0f)
            {
                DisableBeams();
                return;
            }

            beamTimer -= Time.deltaTime;
            for (int i = 0; i < beamRenderers.Length; i++)
            {
                LineRenderer lr = beamRenderers[i];
                if (lr == null || !lr.enabled)
                {
                    continue;
                }

                Enemy target = lastTargets[i];
                if (target != null && target.IsAlive)
                {
                    lr.SetPosition(0, transform.position);
                    lr.SetPosition(1, target.transform.position);
                }
            }
        }

        private void DisableBeams()
        {
            if (beamRenderers == null)
            {
                return;
            }

            for (int i = 0; i < beamRenderers.Length; i++)
            {
                if (beamRenderers[i] != null)
                {
                    beamRenderers[i].enabled = false;
                }
            }
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

        private void EnsureBeams()
        {
            if (beamRenderers != null && beamRenderers.Length > 0)
            {
                return;
            }

            int count = Mathf.Max(1, maxTargets);
            beamRenderers = new LineRenderer[count];

            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject($"Beam_{i}");
                go.transform.SetParent(transform, false);
                LineRenderer lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.startWidth = 0.08f;
                lr.endWidth = 0.08f;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                Color beamPink = new Color(1f, 0.35f, 0.75f, 1f);
                lr.startColor = beamPink;
                lr.endColor = beamPink;
                lr.enabled = false;
                beamRenderers[i] = lr;
            }
        }

        private void EnsureAudioSource()
        {
            if (shootAudioSource != null)
            {
                return;
            }

            shootAudioSource = GetComponent<AudioSource>();
            if (shootAudioSource == null)
            {
                shootAudioSource = gameObject.AddComponent<AudioSource>();
            }

            shootAudioSource.playOnAwake = false;
            shootAudioSource.loop = false;
            shootAudioSource.spatialBlend = 0f;
        }

        private void PlayShootSound()
        {
            if (shootSound == null)
            {
                return;
            }

            EnsureAudioSource();
            if (shootAudioSource != null)
            {
                shootAudioSource.PlayOneShot(shootSound, shootVolume * AudioSettings.SfxVolume);
            }
        }
    }
}
