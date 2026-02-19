using UnityEngine;
using UnityEngine.UI;

namespace ClickerTowerDefense
{
    [System.Serializable]
    public class TowerOption
    {
        public string displayName;
        public TowerBase towerPrefab;
        public int cost = 10;
    }

    [RequireComponent(typeof(Collider2D))]
    public abstract class TowerBase : MonoBehaviour, IRangeProvider
    {
        public abstract float Range { get; }

        protected int BaseCost { get; private set; }

        public void SetBaseCost(int cost)
        {
            BaseCost = Mathf.Max(0, cost);
        }

        protected int CalculateSellValue(int level, int[] upgradeCosts)
        {
            int value = BaseCost / 2;
            int upgradesPurchased = Mathf.Max(0, level - 1);
            if (upgradeCosts != null)
            {
                for (int i = 0; i < upgradesPurchased && i < upgradeCosts.Length; i++)
                {
                    value += Mathf.Max(0, upgradeCosts[i]) / 2;
                }
            }

            return value;
        }
    }

    public class TowerPlacementManager : MonoBehaviour
    {
        private static readonly int[] DefaultTowerLimitUpgradeCosts = { 100, 400, 1200, 3500, 9000, 20000 };

        [SerializeField] private TowerOption[] towerOptions;
        [SerializeField] private int selectedIndex;
        [SerializeField] private GameManager gameManager;
        [Header("Tower Limit")]
        [SerializeField] private int startingTowerLimit = 5;
        [SerializeField] private int towerLimitPerUpgrade = 5;
        [SerializeField] private int[] towerLimitUpgradeCosts = new[] { 100, 400, 1200, 3500, 9000, 20000 };
        [SerializeField] private Text towersLimitText;

        private int towerLimitUpgradeLevel;
        private int placedTowerCount;

        public TowerOption[] TowerOptions => towerOptions;
        public int SelectedIndex => selectedIndex;
        public int TowerLimitUpgradeLevel => towerLimitUpgradeLevel;
        public int MaxTowerLimitUpgradeLevel => towerLimitUpgradeCosts != null ? towerLimitUpgradeCosts.Length : 0;
        public bool IsTowerLimitUpgradeMaxed => TowerLimitUpgradeLevel >= MaxTowerLimitUpgradeLevel;
        public int PlacedTowerCount => placedTowerCount;
        public int MaxTowerCapacity => Mathf.Max(1, startingTowerLimit) + (Mathf.Max(1, towerLimitPerUpgrade) * towerLimitUpgradeLevel);
        public int AvailableTowerSlots => Mathf.Max(0, MaxTowerCapacity - placedTowerCount);

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            EnsureTowerLimitCostsConfigured();
            towerLimitUpgradeLevel = 0;
            placedTowerCount = FindObjectsByType<TowerBase>(FindObjectsSortMode.None).Length;
            ResolveTowersLimitText();
            RefreshTowerLimitText();
        }

        private void Start()
        {
            RefreshTowerLimitText();
        }

        public bool TryPlaceTower(Vector3 position, TowerSlot slot = null)
        {
            if (towerOptions == null || towerOptions.Length == 0 || gameManager == null)
            {
                return false;
            }

            if (AvailableTowerSlots <= 0)
            {
                return false;
            }

            TowerOption option = towerOptions[Mathf.Clamp(selectedIndex, 0, towerOptions.Length - 1)];
            if (option == null || option.towerPrefab == null)
            {
                return false;
            }

            if (!gameManager.SpendCoins(option.cost))
            {
                return false;
            }

            TowerBase tower = Instantiate(option.towerPrefab, position, Quaternion.identity);
            if (tower != null)
            {
                tower.SetBaseCost(option.cost);
                placedTowerCount++;
                gameManager.PlayTowerPlaceSound();
                if (slot != null)
                {
                    TowerSlotOccupant occupant = tower.gameObject.AddComponent<TowerSlotOccupant>();
                    occupant.Initialize(slot, this);
                }

                RefreshTowerLimitText();
            }
            else
            {
                gameManager.AddCoins(option.cost);
                return false;
            }

            return true;
        }

        public void SelectTower(int index)
        {
            if (towerOptions == null || towerOptions.Length == 0)
            {
                return;
            }

            selectedIndex = Mathf.Clamp(index, 0, towerOptions.Length - 1);
        }

        public int GetNextTowerLimitUpgradeCost()
        {
            if (IsTowerLimitUpgradeMaxed || towerLimitUpgradeCosts == null)
            {
                return -1;
            }

            return Mathf.Max(0, towerLimitUpgradeCosts[towerLimitUpgradeLevel]);
        }

        public bool TryUpgradeTowerLimit()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (gameManager == null || IsTowerLimitUpgradeMaxed)
            {
                return false;
            }

            int cost = GetNextTowerLimitUpgradeCost();
            if (cost < 0 || !gameManager.SpendCoins(cost))
            {
                return false;
            }

            towerLimitUpgradeLevel++;
            RefreshTowerLimitText();
            return true;
        }

        public void NotifyTowerRemoved()
        {
            if (placedTowerCount > 0)
            {
                placedTowerCount--;
            }

            RefreshTowerLimitText();
        }

        private void ResolveTowersLimitText()
        {
            if (towersLimitText != null)
            {
                return;
            }

            GameObject towersTextObject = GameObject.Find("TowersText");
            if (towersTextObject != null)
            {
                towersLimitText = towersTextObject.GetComponent<Text>();
            }
        }

        private void RefreshTowerLimitText()
        {
            ResolveTowersLimitText();
            if (towersLimitText == null)
            {
                return;
            }

            towersLimitText.text = "Towers Limit: " + AvailableTowerSlots + "/" + MaxTowerCapacity;
        }

        private void EnsureTowerLimitCostsConfigured()
        {
            if (towerLimitUpgradeCosts == null || towerLimitUpgradeCosts.Length != DefaultTowerLimitUpgradeCosts.Length)
            {
                towerLimitUpgradeCosts = (int[])DefaultTowerLimitUpgradeCosts.Clone();
                return;
            }

            bool outOfRange = false;
            for (int i = 0; i < towerLimitUpgradeCosts.Length; i++)
            {
                if (towerLimitUpgradeCosts[i] > 20000 || towerLimitUpgradeCosts[i] < 0)
                {
                    outOfRange = true;
                    break;
                }
            }

            if (outOfRange)
            {
                towerLimitUpgradeCosts = (int[])DefaultTowerLimitUpgradeCosts.Clone();
            }
        }
    }
}
