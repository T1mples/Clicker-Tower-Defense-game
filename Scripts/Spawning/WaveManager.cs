using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClickerTowerDefense
{
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private bool endlessMode = true;
        [SerializeField] private List<WaveSettings> waves = new List<WaveSettings>();
        [SerializeField] private float firstWaveStartDelay = 10f;
        [SerializeField] private float timeBetweenWaves = 2f;
        [SerializeField] private float delayBetweenBatches = 0.5f;

        [Header("Finite Mode")]
        [SerializeField] private int totalWaves = 10;

        [Header("Wave Cost Rules")]
        [SerializeField] private int waveCostStart = 10;
        [SerializeField] private int waveCostPerWave = 1;
        [SerializeField] private int regularEnemyCost = 1;
        [SerializeField] private int eliteEnemyCost = 3;
        [SerializeField] private int eliteStartWave = 5;
        [SerializeField] private int eliteOnlyFromWave = 1000;

        [Header("Elite Growth")]
        [SerializeField, Range(0f, 1f)] private float eliteShareAtStart = 0.1f;
        [SerializeField, Range(0f, 1f)] private float eliteShareGrowthPerWave = 0.0015f;
        [SerializeField, Range(0f, 1f)] private float eliteShareCapBeforeEliteOnly = 0.85f;

        [Header("Enemy Scaling")]
        [SerializeField] private int baseHealth = 5;
        [SerializeField] private int healthIncrement = 3;
        [SerializeField] private float baseSpeed = 1.5f;
        [SerializeField] private float speedIncrement = 0.1f;
        [SerializeField] private float baseSpawnInterval = 1.5f;
        [SerializeField] private float spawnIntervalDecreasePerWave = 0.1f;
        [SerializeField] private float minSpawnInterval = 0.3f;
        [SerializeField] private float batchSpawnInterval = 0.5f;

        [Header("Boss Rules")]
        [SerializeField] private int bossEveryWaves = 10;
        [SerializeField] private float bossHealthMultiplier = 10f;
        [SerializeField] private float bossHealthGrowthPerWave = 0.08f;
        [SerializeField, Range(0.1f, 1f)] private float bossHealthScale = 0.75f;
        [SerializeField] private float bossSpeedMultiplier = 0.6f;
        [SerializeField] private float bossSpeedGrowthPerWave = 0.015f;

        private int aliveEnemies;
        private int currentWaveIndex = -1;
        private int killedThisWave;
        private int currentWaveEnemyCount;
        private bool waveInProgress;
        private bool skipRequested;

        public event System.Action<int, int, int, int> WaveChanged;
        public int CurrentWaveIndex => currentWaveIndex;
        public int TotalWaves => endlessMode ? -1 : (waves != null ? waves.Count : 0);
        public int CurrentWaveEnemyCount => currentWaveEnemyCount;
        public int AliveEnemies => aliveEnemies;
        public int KilledThisWave => killedThisWave;

        private void Awake()
        {
            if (spawner == null)
            {
                spawner = FindFirstObjectByType<EnemySpawner>();
            }

            if (!endlessMode && (waves == null || waves.Count == 0))
            {
                BuildDefaultWaves();
            }
        }

        private void Start()
        {
            StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            if (firstWaveStartDelay > 0f)
            {
                yield return new WaitForSeconds(firstWaveStartDelay);
            }

            int waveIndex = 0;
            while (endlessMode || waveIndex < waves.Count)
            {
                currentWaveIndex = waveIndex;
                killedThisWave = 0;
                skipRequested = false;
                waveInProgress = true;

                int waveNumber = waveIndex + 1;
                WaveSettings wave = GetWaveSettings(waveIndex);
                currentWaveEnemyCount = Mathf.Max(0, wave.enemyCount);
                WaveChanged?.Invoke(currentWaveIndex, TotalWaves, currentWaveEnemyCount, killedThisWave);

                if (wave.isBossWave)
                {
                    yield return StartCoroutine(spawner.SpawnWave(wave, OnEnemySpawned));
                    while (aliveEnemies > 0 && !skipRequested)
                    {
                        yield return null;
                    }
                }
                else
                {
                    List<WaveSettings> packs = BuildThreePacks(wave, waveNumber);
                    for (int i = 0; i < packs.Count && !skipRequested; i++)
                    {
                        WaveSettings pack = packs[i];
                        if (pack.enemyCount <= 0)
                        {
                            continue;
                        }

                        yield return StartCoroutine(spawner.SpawnWave(pack, OnEnemySpawned));
                        while (aliveEnemies > 0 && !skipRequested)
                        {
                            yield return null;
                        }

                        if (i < packs.Count - 1 && delayBetweenBatches > 0f && !skipRequested)
                        {
                            yield return new WaitForSeconds(delayBetweenBatches);
                        }
                    }
                }

                waveInProgress = false;
                if (timeBetweenWaves > 0f)
                {
                    yield return new WaitForSeconds(timeBetweenWaves);
                }

                waveIndex++;
            }
        }

        public bool SkipCurrentWave()
        {
            if (!waveInProgress)
            {
                return false;
            }

            if (spawner == null)
            {
                spawner = FindFirstObjectByType<EnemySpawner>();
            }

            if (spawner != null)
            {
                spawner.RequestSkipCurrentWave();
            }

            skipRequested = true;

            var enemies = EnemyRegistry.All;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                enemy.TakeDamage(int.MaxValue);
            }

            killedThisWave = currentWaveEnemyCount;
            WaveChanged?.Invoke(currentWaveIndex, TotalWaves, currentWaveEnemyCount, killedThisWave);
            return true;
        }

        private WaveSettings GetWaveSettings(int index)
        {
            if (endlessMode)
            {
                return GenerateEndlessWave(index);
            }

            if (waves == null || index < 0 || index >= waves.Count || waves[index] == null)
            {
                return new WaveSettings();
            }

            return waves[index];
        }

        private void OnEnemySpawned(Enemy enemy)
        {
            aliveEnemies++;
            enemy.Removed += OnEnemyRemoved;
            WaveChanged?.Invoke(currentWaveIndex, TotalWaves, currentWaveEnemyCount, killedThisWave);
        }

        private void OnEnemyRemoved(Enemy enemy)
        {
            enemy.Removed -= OnEnemyRemoved;
            aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
            if (enemy != null && enemy.CurrentHealth <= 0)
            {
                killedThisWave = Mathf.Min(currentWaveEnemyCount, killedThisWave + 1);
            }

            WaveChanged?.Invoke(currentWaveIndex, TotalWaves, currentWaveEnemyCount, killedThisWave);
        }

        private WaveSettings GenerateEndlessWave(int index)
        {
            int waveNumber = Mathf.Max(1, index + 1);
            bool isBoss = bossEveryWaves > 0 && waveNumber % bossEveryWaves == 0;
            int growth = waveNumber - 1;

            int health = Mathf.Max(1, baseHealth + (healthIncrement * growth));
            float speed = Mathf.Max(0.2f, baseSpeed + (speedIncrement * growth));
            float interval = Mathf.Max(minSpawnInterval, baseSpawnInterval - (spawnIntervalDecreasePerWave * growth));

            if (isBoss)
            {
                float bossGrowth = 1f + (bossHealthGrowthPerWave * growth);
                health = Mathf.Max(1, Mathf.RoundToInt(health * bossHealthMultiplier * bossGrowth * bossHealthScale));

                float bossSpeedGrowth = 1f + (bossSpeedGrowthPerWave * growth);
                speed = Mathf.Max(0.2f, (baseSpeed * bossSpeedMultiplier) * bossSpeedGrowth);

                return new WaveSettings
                {
                    enemyCount = 1,
                    eliteCount = 0,
                    enemyHealth = health,
                    enemySpeed = speed,
                    spawnInterval = Mathf.Max(0.05f, interval),
                    isBossWave = true
                };
            }

            int waveCost = CalculateWaveCost(waveNumber);
            BuildWaveCompositionByCost(waveNumber, waveCost, out int regularCount, out int eliteCount);

            return new WaveSettings
            {
                enemyCount = Mathf.Max(1, regularCount + eliteCount),
                eliteCount = Mathf.Max(0, eliteCount),
                enemyHealth = health,
                enemySpeed = speed,
                spawnInterval = Mathf.Max(0.05f, interval),
                isBossWave = false
            };
        }

        private void BuildDefaultWaves()
        {
            waves = new List<WaveSettings>(totalWaves);
            for (int i = 0; i < totalWaves; i++)
            {
                waves.Add(GenerateEndlessWave(i));
            }
        }

        private int CalculateWaveCost(int waveNumber)
        {
            return Mathf.Max(1, waveCostStart + ((waveNumber - 1) * waveCostPerWave));
        }

        private void BuildWaveCompositionByCost(int waveNumber, int waveCost, out int regularCount, out int eliteCount)
        {
            int regularCost = Mathf.Max(1, regularEnemyCost);
            int eliteCost = Mathf.Max(1, eliteEnemyCost);
            int totalCost = Mathf.Max(regularCost, waveCost);

            if (waveNumber >= eliteOnlyFromWave)
            {
                eliteCount = Mathf.Max(1, totalCost / eliteCost);
                regularCount = 0;
                return;
            }

            if (waveNumber < eliteStartWave)
            {
                eliteCount = 0;
                regularCount = totalCost / regularCost;
                return;
            }

            int eliteGrowthWaves = waveNumber - eliteStartWave;
            float eliteShare = eliteShareAtStart + (eliteGrowthWaves * eliteShareGrowthPerWave);
            eliteShare = Mathf.Clamp(eliteShare, 0f, eliteShareCapBeforeEliteOnly);

            int eliteBudget = Mathf.FloorToInt(totalCost * eliteShare);
            int maxEliteCountByBudget = Mathf.Max(0, (totalCost - regularCost) / eliteCost);
            eliteCount = Mathf.Clamp(eliteBudget / eliteCost, 0, maxEliteCountByBudget);

            if (eliteCount <= 0 && maxEliteCountByBudget > 0)
            {
                eliteCount = 1;
            }

            int remainingCost = Mathf.Max(0, totalCost - (eliteCount * eliteCost));
            regularCount = remainingCost / regularCost;
        }

        private List<WaveSettings> BuildThreePacks(WaveSettings wave, int waveNumber)
        {
            List<WaveSettings> packs = new List<WaveSettings>(3);
            int regularCost = Mathf.Max(1, regularEnemyCost);
            int eliteCost = Mathf.Max(1, eliteEnemyCost);

            int totalElite = Mathf.Clamp(wave.eliteCount, 0, Mathf.Max(0, wave.enemyCount));
            int totalRegular = Mathf.Max(0, wave.enemyCount - totalElite);
            int waveCost = (totalRegular * regularCost) + (totalElite * eliteCost);

            int pack1Cost = Mathf.FloorToInt(waveCost * 0.2f);
            int pack2Cost = Mathf.FloorToInt(waveCost * 0.3f);
            int pack3Cost = waveCost - pack1Cost - pack2Cost;

            int[] packCosts = { pack1Cost, pack2Cost, pack3Cost };
            int[] packElite = { 0, 0, 0 };
            int remainingElites = totalElite;

            // Deterministic: elites are pushed to later packs first.
            for (int i = 2; i >= 0; i--)
            {
                int maxEliteInPack = packCosts[i] / eliteCost;
                int take = Mathf.Min(remainingElites, maxEliteInPack);
                packElite[i] = take;
                remainingElites -= take;
            }

            // If any elites still remain because early packs had too low budget,
            // force them into the last pack.
            if (remainingElites > 0)
            {
                packElite[2] += remainingElites;
                remainingElites = 0;
            }

            bool eliteOnly = waveNumber >= eliteOnlyFromWave;

            for (int i = 0; i < 3; i++)
            {
                int eliteCount = Mathf.Max(0, packElite[i]);
                int regularCount = 0;
                if (!eliteOnly)
                {
                    int regularBudget = Mathf.Max(0, packCosts[i] - (eliteCount * eliteCost));
                    regularCount = regularBudget / regularCost;
                }

                int count = Mathf.Max(0, eliteCount + regularCount);
                packs.Add(new WaveSettings
                {
                    enemyCount = count,
                    eliteCount = eliteCount,
                    enemyHealth = wave.enemyHealth,
                    enemySpeed = wave.enemySpeed,
                    spawnInterval = Mathf.Max(0.05f, batchSpawnInterval),
                    isBossWave = false
                });
            }

            return packs;
        }
    }
}
