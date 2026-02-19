using System.Collections;
using System;
using UnityEngine;

namespace ClickerTowerDefense
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private Enemy enemyPrefab;
        [SerializeField] private Enemy elitePrefab;
        [SerializeField] private Enemy bossPrefab;
        [Header("Elite Balance")]
        [SerializeField] private float eliteHealthMultiplier = 3.5f;
        [SerializeField] private float eliteSpeedMultiplier = 0.72f;
        [SerializeField] private float eliteSlowEffectiveness = 0.45f;
        [SerializeField] private float eliteFreezeEffectiveness = 0.55f;
        [Header("Boss Balance")]
        [SerializeField] private float bossSlowEffectiveness = 0.35f;
        [SerializeField] private float bossFreezeEffectiveness = 0.4f;
        [SerializeField] private Path path;
        private float pendingSpawnDelay;
        private bool skipCurrentWaveRequested;

        private void Awake()
        {
            if (path == null)
            {
                path = FindFirstObjectByType<Path>();
            }
        }

        public IEnumerator SpawnWave(WaveSettings wave, Action<Enemy> onSpawned)
        {
            skipCurrentWaveRequested = false;
            if (wave == null || enemyPrefab == null || path == null)
            {
                yield break;
            }

            if (wave.isBossWave)
            {
                int bossCount = Mathf.Max(1, wave.enemyCount);
                Enemy prefab = bossPrefab != null ? bossPrefab : enemyPrefab;
                for (int i = 0; i < bossCount; i++)
                {
                    if (skipCurrentWaveRequested)
                    {
                        yield break;
                    }

                    yield return WaitWithSpawnDelay(0f);
                    if (skipCurrentWaveRequested)
                    {
                        yield break;
                    }
                    Enemy boss = Instantiate(prefab, transform.position, Quaternion.identity);
                    boss.Initialize(
                        path,
                        wave.enemySpeed,
                        wave.enemyHealth,
                        EnemyType.Boss,
                        bossSlowEffectiveness,
                        bossFreezeEffectiveness);
                    onSpawned?.Invoke(boss);
                    yield return WaitWithSpawnDelay(wave.spawnInterval);
                }

                yield break;
            }

            int totalCount = Mathf.Max(0, wave.enemyCount);
            int elitesToSpawn = Mathf.Clamp(wave.eliteCount, 0, totalCount);
            int regularToSpawn = Mathf.Max(0, totalCount - elitesToSpawn);

            for (int i = 0; i < totalCount; i++)
            {
                if (skipCurrentWaveRequested)
                {
                    yield break;
                }

                yield return WaitWithSpawnDelay(0f);
                if (skipCurrentWaveRequested)
                {
                    yield break;
                }

                int remaining = elitesToSpawn + regularToSpawn;
                bool spawnElite = false;
                if (remaining > 0 && elitesToSpawn > 0)
                {
                    float eliteChance = elitesToSpawn / (float)remaining;
                    spawnElite = UnityEngine.Random.value <= eliteChance;
                }

                Enemy prefab = enemyPrefab;
                float speed = wave.enemySpeed;
                int health = wave.enemyHealth;
                EnemyType type = EnemyType.Regular;
                float slowEff = 1f;
                float freezeEff = 1f;

                if (spawnElite)
                {
                    type = EnemyType.Elite;
                    prefab = elitePrefab != null ? elitePrefab : enemyPrefab;
                    speed = Mathf.Max(0.1f, wave.enemySpeed * eliteSpeedMultiplier);
                    health = Mathf.Max(1, Mathf.RoundToInt(wave.enemyHealth * eliteHealthMultiplier));
                    slowEff = eliteSlowEffectiveness;
                    freezeEff = eliteFreezeEffectiveness;
                    elitesToSpawn--;
                }
                else
                {
                    regularToSpawn--;
                }

                Enemy enemy = Instantiate(prefab, transform.position, Quaternion.identity);
                enemy.Initialize(path, speed, health, type, slowEff, freezeEff);
                onSpawned?.Invoke(enemy);
                yield return WaitWithSpawnDelay(wave.spawnInterval);
            }
        }

        public void AddSpawnDelay(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            pendingSpawnDelay += seconds;
        }

        public void RequestSkipCurrentWave()
        {
            skipCurrentWaveRequested = true;
        }

        private IEnumerator WaitWithSpawnDelay(float baseSeconds)
        {
            float remainingBase = Mathf.Max(0f, baseSeconds);
            while ((remainingBase > 0f || pendingSpawnDelay > 0f) && !skipCurrentWaveRequested)
            {
                float dt = Time.deltaTime;
                if (pendingSpawnDelay > 0f)
                {
                    pendingSpawnDelay = Mathf.Max(0f, pendingSpawnDelay - dt);
                }
                else
                {
                    remainingBase = Mathf.Max(0f, remainingBase - dt);
                }

                yield return null;
            }
        }
    }
}
