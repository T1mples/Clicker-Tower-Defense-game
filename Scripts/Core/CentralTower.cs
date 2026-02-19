using UnityEngine;

namespace ClickerTowerDefense
{
    public class CentralTower : MonoBehaviour
    {
        private static readonly int[] DefaultClickUpgradeCosts = { 500, 2500, 10000, 20000 };
        [SerializeField] private int baseCoinsPerClick = 1;
        [Header("Click Upgrades")]
        [SerializeField] private int[] clickUpgradeCosts = new[] { 500, 2500, 10000, 20000 };
        [SerializeField] private GameManager gameManager;
        [Header("Rendering")]
        [SerializeField] private bool forceSorting = true;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 40;

        private SpriteRenderer[] cachedRenderers;
        private int clickUpgradeLevel;

        public int ClickUpgradeLevel => clickUpgradeLevel;
        public int MaxClickUpgradeLevel => clickUpgradeCosts != null ? clickUpgradeCosts.Length : 0;
        public bool IsClickUpgradeMaxed => ClickUpgradeLevel >= MaxClickUpgradeLevel;
        public int CoinsPerClick => Mathf.Max(1, baseCoinsPerClick) * (1 << clickUpgradeLevel);

        private void Awake()
        {
            EnsureClickUpgradeCostsConfigured();
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            clickUpgradeLevel = 0;
            ApplySorting();
        }

        private void OnEnable()
        {
            ApplySorting();
        }

        private void OnMouseDown()
        {
            if (GameMenuUI.IsMenuOpen || StartScreenUI.IsOpen)
            {
                return;
            }

            if (gameManager != null)
            {
                gameManager.AddCoins(CoinsPerClick);
                gameManager.PlayTowerClickSound();
            }
        }

        public int GetNextClickUpgradeCost()
        {
            if (IsClickUpgradeMaxed || clickUpgradeCosts == null)
            {
                return -1;
            }

            return Mathf.Max(0, clickUpgradeCosts[clickUpgradeLevel]);
        }

        public bool TryUpgradeClick()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (gameManager == null || IsClickUpgradeMaxed)
            {
                return false;
            }

            int cost = GetNextClickUpgradeCost();
            if (cost < 0 || !gameManager.SpendCoins(cost))
            {
                return false;
            }

            clickUpgradeLevel++;
            return true;
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

        private void EnsureClickUpgradeCostsConfigured()
        {
            if (clickUpgradeCosts == null || clickUpgradeCosts.Length != DefaultClickUpgradeCosts.Length)
            {
                clickUpgradeCosts = (int[])DefaultClickUpgradeCosts.Clone();
                return;
            }

            bool outOfRange = false;
            for (int i = 0; i < clickUpgradeCosts.Length; i++)
            {
                if (clickUpgradeCosts[i] > 20000 || clickUpgradeCosts[i] < 0)
                {
                    outOfRange = true;
                    break;
                }
            }

            if (outOfRange)
            {
                clickUpgradeCosts = (int[])DefaultClickUpgradeCosts.Clone();
            }
        }
    }
}
