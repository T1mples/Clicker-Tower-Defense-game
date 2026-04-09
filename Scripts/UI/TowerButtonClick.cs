using UnityEngine;
using UnityEngine.EventSystems;

namespace ClickerTowerDefense
{
    public class TowerButtonClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private int index;
        [SerializeField] private TowerSelectionUI selectionUI;

        private void Awake()
        {
            if (selectionUI == null)
            {
                selectionUI = FindFirstObjectByType<TowerSelectionUI>();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (selectionUI == null)
            {
                Debug.Log("TowerButtonClick: selectionUI is null");
                return;
            }

            selectionUI.SelectIndex(index);
        }
    }
}
