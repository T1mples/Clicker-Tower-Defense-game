using UnityEngine;
using UnityEngine.UI;

namespace ClickerTowerDefense
{
    public class CoinsView : MonoBehaviour
    {
        [SerializeField] private Text coinsText;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private string prefix = "Coins: ";

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (coinsText == null)
            {
                coinsText = GetComponent<Text>();
            }
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.CoinsChanged += OnCoinsChanged;
                OnCoinsChanged(gameManager.Coins);
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.CoinsChanged -= OnCoinsChanged;
            }
        }

        private void OnCoinsChanged(int coins)
        {
            if (coinsText != null)
            {
                coinsText.text = prefix + coins;
            }
        }
    }
}
