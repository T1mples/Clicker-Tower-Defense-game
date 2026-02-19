using UnityEngine;
using UnityEngine.EventSystems;

namespace ClickerTowerDefense
{
    public class MenuButtonHoverSfx : MonoBehaviour, IPointerEnterHandler
    {
        private GameMenuUI menuUi;

        public void Initialize(GameMenuUI owner)
        {
            menuUi = owner;
        }

        private void Awake()
        {
            if (menuUi == null)
            {
                menuUi = GetComponentInParent<GameMenuUI>();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (menuUi != null)
            {
                menuUi.PlayMenuButtonHoverSound();
            }
        }
    }
}
