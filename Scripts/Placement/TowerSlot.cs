using UnityEngine;

namespace ClickerTowerDefense
{
    public class TowerSlot : MonoBehaviour
    {
        [SerializeField] private TowerPlacementManager placementManager;
        [SerializeField] private bool isOccupied;
        [SerializeField] private SpriteRenderer slotRenderer;
        [SerializeField] private bool hideWhenOccupied = true;

        private void Awake()
        {
            if (placementManager == null)
            {
                placementManager = FindFirstObjectByType<TowerPlacementManager>();
            }

            if (slotRenderer == null)
            {
                slotRenderer = GetComponent<SpriteRenderer>();
            }

            UpdateVisual();
        }

        private void OnMouseDown()
        {
            if (GameMenuUI.IsMenuOpen || StartScreenUI.IsOpen)
            {
                return;
            }

            if (isOccupied || placementManager == null)
            {
                return;
            }

            if (placementManager.TryPlaceTower(transform.position, this))
            {
                SetOccupied(true);
            }
        }

        public void SetOccupied(bool value)
        {
            isOccupied = value;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (slotRenderer == null)
            {
                return;
            }

            if (hideWhenOccupied)
            {
                slotRenderer.enabled = !isOccupied;
            }
        }
    }
}
