using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClickerTowerDefense
{
    public enum AutoClickerType
    {
        None = 0,
        Noob = 1,
        Pro = 2,
        Hacker = 3
    }

    public class AutoClickerSystem : MonoBehaviour
    {
        public static AutoClickerSystem Instance { get; private set; }

        private static readonly int[] DefaultUpgradeCosts = { 500, 1200, 2500, 5000, 9000, 13000, 17000, 20000 };
        [SerializeField] private int[] upgradeCosts = { 500, 1200, 2500, 5000, 9000, 13000, 17000, 20000 };
        [SerializeField] private int[] incomePerSecondByLevel = { 5, 12, 25, 50, 100, 220, 450, 1000 };

        public const int NoobCost = 1000;
        public const int ProCost = 10000;
        public const int HackerCost = 1000000;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null || FindFirstObjectByType<AutoClickerSystem>() != null)
            {
                return;
            }

            GameObject go = new GameObject("AutoClickerSystem");
            go.AddComponent<AutoClickerSystem>();
        }

        public AutoClickerType ActiveType { get; private set; }
        public int UpgradeLevel { get; private set; }
        public int MaxUpgradeLevel => Mathf.Min(
            upgradeCosts != null ? upgradeCosts.Length : 0,
            incomePerSecondByLevel != null ? incomePerSecondByLevel.Length : 0);
        public bool IsUpgradeMaxed => UpgradeLevel >= MaxUpgradeLevel;
        public int IncomePerSecond => UpgradeLevel <= 0 || incomePerSecondByLevel == null
            ? 0
            : incomePerSecondByLevel[Mathf.Clamp(UpgradeLevel - 1, 0, incomePerSecondByLevel.Length - 1)];

        private GameManager gameManager;
        private float incomeAccumulator;

        private void Awake()
        {
            EnsureUpgradeCostsConfigured();
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            gameManager = GameManager.Instance;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private void Update()
        {
            int incomePerSecond = IncomePerSecond;
            if (incomePerSecond <= 0)
            {
                return;
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
                if (gameManager == null)
                {
                    return;
                }
            }

            incomeAccumulator += incomePerSecond * Time.deltaTime;
            int wholeCoins = Mathf.FloorToInt(incomeAccumulator);
            if (wholeCoins > 0)
            {
                incomeAccumulator -= wholeCoins;
                gameManager.AddCoins(wholeCoins);
            }
        }

        public int GetNextUpgradeCost()
        {
            if (IsUpgradeMaxed || upgradeCosts == null)
            {
                return -1;
            }

            return Mathf.Max(0, upgradeCosts[UpgradeLevel]);
        }

        public bool TryUpgrade()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (gameManager == null || IsUpgradeMaxed)
            {
                return false;
            }

            int cost = GetNextUpgradeCost();
            if (cost < 0 || !gameManager.SpendCoins(cost))
            {
                return false;
            }

            UpgradeLevel++;
            return true;
        }

        public bool TryPurchase(AutoClickerType type)
        {
            if (type == AutoClickerType.None)
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

            int cost = GetCost(type);
            if (!gameManager.SpendCoins(cost))
            {
                return false;
            }

            ActiveType = type;
            return true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            gameManager = GameManager.Instance;
            ActiveType = AutoClickerType.None;
            UpgradeLevel = 0;
            incomeAccumulator = 0f;
        }

        public static int GetCost(AutoClickerType type)
        {
            switch (type)
            {
                case AutoClickerType.Noob:
                    return NoobCost;
                case AutoClickerType.Pro:
                    return ProCost;
                case AutoClickerType.Hacker:
                    return HackerCost;
                default:
                    return 0;
            }
        }

        public static int GetIncomePerSecond(AutoClickerType type)
        {
            switch (type)
            {
                case AutoClickerType.Noob:
                    return 10;
                case AutoClickerType.Pro:
                    return 100;
                case AutoClickerType.Hacker:
                    return 1000;
                default:
                    return 0;
            }
        }

        private void EnsureUpgradeCostsConfigured()
        {
            if (upgradeCosts == null || upgradeCosts.Length != DefaultUpgradeCosts.Length)
            {
                upgradeCosts = (int[])DefaultUpgradeCosts.Clone();
                return;
            }

            bool outOfRange = false;
            for (int i = 0; i < upgradeCosts.Length; i++)
            {
                if (upgradeCosts[i] > 20000 || upgradeCosts[i] < 0)
                {
                    outOfRange = true;
                    break;
                }
            }

            if (outOfRange)
            {
                upgradeCosts = (int[])DefaultUpgradeCosts.Clone();
            }
        }
    }
}
