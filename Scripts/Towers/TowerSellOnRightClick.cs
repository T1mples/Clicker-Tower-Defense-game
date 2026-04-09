using UnityEngine;

namespace ClickerTowerDefense
{
    public class TowerSellOnRightClick : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        private ISellable sellable;
        private Collider2D towerCollider;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            sellable = GetComponent<ISellable>();
            towerCollider = GetComponent<Collider2D>();
        }

        private void Update()
        {
            if (GameMenuUI.IsMenuOpen || StartScreenUI.IsOpen)
            {
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                TrySellUnderMouse();
            }
        }

        private void TrySellUnderMouse()
        {
            if (towerCollider == null || Camera.main == null)
            {
                return;
            }

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (towerCollider.OverlapPoint(mouseWorld))
            {
                Sell();
            }
        }

        private void Sell()
        {
            if (sellable == null)
            {
                sellable = GetComponent<ISellable>();
                if (sellable == null)
                {
                    return;
                }
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (gameManager != null)
            {
                int value = Mathf.Max(0, sellable.GetSellValue());
                if (value > 0)
                {
                    gameManager.AddCoins(value);
                }

                gameManager.PlayTowerSellSound();
            }

            Destroy(gameObject);
        }
    }
}
