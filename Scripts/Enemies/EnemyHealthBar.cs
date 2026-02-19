using UnityEngine;

namespace ClickerTowerDefense
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Enemy enemy;
        [SerializeField] private Transform fillTransform;

        private Vector3 initialScale;

        private void Awake()
        {
            if (enemy == null)
            {
                enemy = GetComponentInParent<Enemy>();
            }

            if (fillTransform == null)
            {
                fillTransform = transform;
            }

            initialScale = fillTransform.localScale;
        }

        private void OnEnable()
        {
            if (enemy != null)
            {
                enemy.HealthChanged += OnHealthChanged;
                OnHealthChanged(enemy.CurrentHealth, enemy.MaxHealth);
            }
        }

        private void OnDisable()
        {
            if (enemy != null)
            {
                enemy.HealthChanged -= OnHealthChanged;
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
