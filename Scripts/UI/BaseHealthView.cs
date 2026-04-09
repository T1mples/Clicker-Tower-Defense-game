using UnityEngine;
using UnityEngine.UI;

namespace ClickerTowerDefense
{
    public class BaseHealthView : MonoBehaviour
    {
        [SerializeField] private Text healthText;
        [SerializeField] private BaseHealth baseHealth;
        [SerializeField] private string prefix = "Base HP: ";

        private void Awake()
        {
            if (healthText == null)
            {
                healthText = GetComponent<Text>();
            }

            if (baseHealth == null)
            {
                baseHealth = FindFirstObjectByType<BaseHealth>();
            }
        }

        private void OnEnable()
        {
            if (baseHealth != null)
            {
                baseHealth.HealthChanged += OnHealthChanged;
                OnHealthChanged(baseHealth.CurrentHealth, baseHealth.MaxHealth);
            }
        }

        private void OnDisable()
        {
            if (baseHealth != null)
            {
                baseHealth.HealthChanged -= OnHealthChanged;
            }
        }

        private void OnHealthChanged(int current, int max)
        {
            if (healthText == null)
            {
                return;
            }

            healthText.text = prefix + current + "/" + max;
        }
    }
}
