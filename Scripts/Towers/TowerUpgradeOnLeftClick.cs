using UnityEngine;
using UnityEngine.EventSystems;

namespace ClickerTowerDefense
{
    public class TowerUpgradeOnLeftClick : MonoBehaviour
    {
        private IUpgradeable upgradeable;
        private Collider2D towerCollider;
        [SerializeField] private float clickPadding = 0.08f;
        private bool waitForMouseRelease;
        private int spawnedFrame;

        private void Awake()
        {
            upgradeable = GetComponent<IUpgradeable>();
            towerCollider = GetComponent<Collider2D>();
            spawnedFrame = Time.frameCount;
            waitForMouseRelease = Input.GetMouseButton(0);
        }

        private void Update()
        {
            if (GameMenuUI.IsMenuOpen || StartScreenUI.IsOpen)
            {
                return;
            }

            if (waitForMouseRelease)
            {
                if (!Input.GetMouseButton(0))
                {
                    waitForMouseRelease = false;
                }
                return;
            }

            if (Time.frameCount <= spawnedFrame + 1)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryUpgradeUnderMouse();
            }
        }

        private void TryUpgradeUnderMouse()
        {
            if (upgradeable == null || towerCollider == null || Camera.main == null)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = towerCollider.bounds.center.z;

            bool hit = towerCollider.OverlapPoint(mouseWorld);
            if (!hit)
            {
                float sqrDist = (towerCollider.bounds.ClosestPoint(mouseWorld) - mouseWorld).sqrMagnitude;
                hit = sqrDist <= (clickPadding * clickPadding);
            }

            if (hit)
            {
                upgradeable.TryUpgrade();
            }
        }
    }
}
