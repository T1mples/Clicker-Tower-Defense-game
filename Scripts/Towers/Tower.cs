using UnityEngine;

namespace ClickerTowerDefense
{
    public class Tower : TowerBase, ISellable, IUpgradeable
    {
        [Header("Base Stats")]
        [SerializeField] private float attackRange = 2.5f;
        [SerializeField] private float rangeScale = 0.5f;
        [SerializeField] private float attacksPerSecond = 1f;
        [SerializeField] private int damage = 1;
        [SerializeField] private Color rangeGizmoColor = new Color(0f, 1f, 0f, 0.25f);

        [Header("Beam")]
        [SerializeField] private LineRenderer beamRenderer;
        [SerializeField] private float beamDuration = 0.08f;
        [Header("Audio")]
        [SerializeField] private AudioClip shootSound;
        [SerializeField, Range(0f, 1f)] private float shootVolume = 0.7f;

        [Header("Upgrades")]
        [SerializeField] private int maxLevel = 3;
        [SerializeField] private int[] upgradeCosts = new[] { 15, 25 };
        [SerializeField] private float rangePerLevel = 0.5f;
        [SerializeField] private float attacksPerSecondPerLevel = 0.3f;
        [SerializeField] private int damagePerLevel = 1;
        [Header("Visual by Level")]
        [SerializeField] private SpriteRenderer towerSpriteRenderer;
        [SerializeField] private Sprite[] levelSprites = new Sprite[3];

        private float nextAttackTime;
        private int level = 1;
        private GameManager gameManager;
        private Enemy currentTarget;
        private float beamTimer;
        private Vector3 lastBeamEnd;
        private AudioSource shootAudioSource;

        public int Level => level;
        public float AttackRange => GetEffectiveRange();
        public override float Range => GetEffectiveRange();

        private void Update()
        {
            UpdateBeam();

            if (Time.time < nextAttackTime)
            {
                return;
            }

            if (currentTarget == null || !currentTarget.IsAlive || !IsInRange(currentTarget))
            {
                SetTarget(FindTarget());
            }

            if (currentTarget == null)
            {
                return;
            }

            Enemy targetAtFire = currentTarget;
            targetAtFire.TakeDamage(damage);
            nextAttackTime = Time.time + (1f / Mathf.Max(0.01f, attacksPerSecond));
            FireBeam(targetAtFire);
            PlayShootSound();
        }

        private void Awake()
        {
            gameManager = GameManager.Instance;
            if (towerSpriteRenderer == null)
            {
                towerSpriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (beamRenderer == null)
            {
                beamRenderer = GetComponent<LineRenderer>();
            }

            if (beamRenderer != null)
            {
                beamRenderer.enabled = false;
            }

            EnsureAudioSource();
            EnsureSellComponent();
            EnsureUpgradeComponent();
            ApplyLevelSprite();
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

        private Enemy FindTarget()
        {
            Enemy closest = null;
            float closestSqr = float.MaxValue;
            Vector3 position = transform.position;

            var enemies = EnemyRegistry.All;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                float sqr = (enemy.transform.position - position).sqrMagnitude;
                float effectiveRange = GetEffectiveRange();
                if (sqr > effectiveRange * effectiveRange)
                {
                    continue;
                }

                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = enemy;
                }
            }

            return closest;
        }

        private bool IsInRange(Enemy enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
            float effectiveRange = GetEffectiveRange();
            return sqr <= effectiveRange * effectiveRange;
        }

        private void SetTarget(Enemy newTarget)
        {
            if (currentTarget == newTarget)
            {
                return;
            }

            if (currentTarget != null)
            {
                currentTarget.Removed -= OnTargetRemoved;
            }

            currentTarget = newTarget;
            if (currentTarget != null)
            {
                currentTarget.Removed += OnTargetRemoved;
            }
        }

        private void OnTargetRemoved(Enemy enemy)
        {
            if (enemy == currentTarget)
            {
                SetTarget(null);
            }
        }

        private void FireBeam(Enemy target)
        {
            if (beamRenderer == null || target == null)
            {
                return;
            }

            beamRenderer.enabled = true;
            beamRenderer.positionCount = 2;
            beamRenderer.SetPosition(0, transform.position);
            beamRenderer.SetPosition(1, target.transform.position);
            lastBeamEnd = target.transform.position;
            beamTimer = beamDuration;
        }

        private void UpdateBeam()
        {
            if (beamRenderer == null || !beamRenderer.enabled)
            {
                return;
            }

            beamTimer -= Time.deltaTime;
            if (beamTimer <= 0f)
            {
                beamRenderer.enabled = false;
                return;
            }

            if (currentTarget != null && currentTarget.IsAlive)
            {
                beamRenderer.SetPosition(0, transform.position);
                beamRenderer.SetPosition(1, currentTarget.transform.position);
                lastBeamEnd = currentTarget.transform.position;
            }
            else
            {
                beamRenderer.SetPosition(0, transform.position);
                beamRenderer.SetPosition(1, lastBeamEnd);
            }
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = rangeGizmoColor;
            Gizmos.DrawWireSphere(transform.position, GetEffectiveRange());
        }

        private float GetEffectiveRange()
        {
            return attackRange * Mathf.Max(0f, rangeScale);
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
