using UnityEngine;

namespace ClickerTowerDefense
{
    public class TowerRangeHover : MonoBehaviour
    {
        [SerializeField] private TowerRangeView rangeView;
        [SerializeField] private Collider2D hoverCollider;

        private bool isHovered;

        private void Awake()
        {
            if (rangeView == null)
            {
                rangeView = GetComponentInChildren<TowerRangeView>();
            }

            if (hoverCollider == null)
            {
                hoverCollider = GetComponent<Collider2D>();
            }
        }

        private void Update()
        {
            if (rangeView == null || hoverCollider == null || Camera.main == null)
            {
                return;
            }

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            bool hoveredNow = hoverCollider.OverlapPoint(mouseWorld);
            if (hoveredNow != isHovered)
            {
                isHovered = hoveredNow;
                rangeView.SetHovered(isHovered);
            }
        }
    }
}
