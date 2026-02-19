using UnityEngine;

namespace ClickerTowerDefense
{
    public class BaseHealthBar : MonoBehaviour
    {
        [SerializeField] private BaseHealth baseHealth;
        [SerializeField] private Transform fillTransform;

        private Vector3 initialScale;

        private void Awake()
        {
            if (baseHealth == null)
            {
                baseHealth = FindFirstObjectByType<BaseHealth>();
            }

            if (fillTransform == null)
            {
                fillTransform = transform;
            }

            initialScale = fillTransform.localScale;
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
            if (fillTransform == null)
            {
                return;
            }

            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            fillTransform.localScale = new Vector3(initialScale.x * ratio, initialScale.y, initialScale.z);
        }
    }
}
