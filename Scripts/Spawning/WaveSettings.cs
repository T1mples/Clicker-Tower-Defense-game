using System;
using UnityEngine;

namespace ClickerTowerDefense
{
    [Serializable]
    public class WaveSettings
    {
        public int enemyCount = 5;
        public int eliteCount = 0;
        public float spawnInterval = 1.5f;
        public float enemySpeed = 1.5f;
        public int enemyHealth = 5;
        public bool isBossWave;
    }
}
