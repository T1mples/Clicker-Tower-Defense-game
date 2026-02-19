using UnityEngine;
using UnityEngine.UI;

namespace ClickerTowerDefense
{
    public class WaveView : MonoBehaviour
    {
        [SerializeField] private Text waveText;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private string prefix = "Wave: ";
        [SerializeField] private string enemiesSuffix = " Remaining: ";
        [SerializeField] private string prepareMessage = "Prepare to Fight!";

        private void Awake()
        {
            if (waveText == null)
            {
                waveText = GetComponent<Text>();
            }

            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
            }
        }

        private void OnEnable()
        {
            if (waveManager != null)
            {
                waveManager.WaveChanged += OnWaveChanged;
                OnWaveChanged(
                    waveManager.CurrentWaveIndex,
                    waveManager.TotalWaves,
                    waveManager.CurrentWaveEnemyCount,
                    waveManager.KilledThisWave);
            }
        }

        private void OnDisable()
        {
            if (waveManager != null)
            {
                waveManager.WaveChanged -= OnWaveChanged;
            }
        }

        private void OnWaveChanged(int currentIndex, int total, int enemyCount, int killed)
        {
            if (waveText == null)
            {
                return;
            }

            if (currentIndex < 0)
            {
                waveText.text = prepareMessage;
                return;
            }

            int displayIndex = Mathf.Max(1, currentIndex + 1);
            int remaining = Mathf.Max(0, enemyCount - killed);

            if (total > 0)
            {
                int clampedDisplay = Mathf.Clamp(displayIndex, 1, total);
                waveText.text = prefix + clampedDisplay + "/" + total + enemiesSuffix + remaining + "/" + enemyCount;
                return;
            }

            waveText.text = prefix + displayIndex + enemiesSuffix + remaining + "/" + enemyCount;
        }
    }
}
