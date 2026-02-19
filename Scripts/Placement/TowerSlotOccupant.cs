using UnityEngine;

namespace ClickerTowerDefense
{
    public class TowerSlotOccupant : MonoBehaviour
    {
        private TowerSlot slot;
        private TowerPlacementManager placementManager;

        public void Initialize(TowerSlot towerSlot, TowerPlacementManager manager)
        {
            slot = towerSlot;
            placementManager = manager;
            if (slot != null)
            {
                slot.SetOccupied(true);
            }
        }

        private void OnDestroy()
        {
            if (slot != null)
            {
                slot.SetOccupied(false);
            }

            if (placementManager != null)
            {
                placementManager.NotifyTowerRemoved();
            }
        }
    }
}
