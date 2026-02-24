using UnityEngine;

namespace ClickerTowerDefense
{
    public class CameraBounds2D : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector2 worldMin = new Vector2(-10f, -5f);
        [SerializeField] private Vector2 worldMax = new Vector2(10f, 5f);
        [SerializeField] private bool lockZ = true;
        [SerializeField] private float fixedZ = -10f;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null || !targetCamera.orthographic)
            {
                return;
            }

            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * targetCamera.aspect;

            Vector3 position = targetCamera.transform.position;

            float minX = worldMin.x + halfWidth;
            float maxX = worldMax.x - halfWidth;
            float minY = worldMin.y + halfHeight;
            float maxY = worldMax.y - halfHeight;

            // Extremely wide/tall windows can make bounds invalid; keep camera centered in that axis.
            position.x = minX > maxX
                ? (worldMin.x + worldMax.x) * 0.5f
                : Mathf.Clamp(position.x, minX, maxX);

            position.y = minY > maxY
                ? (worldMin.y + worldMax.y) * 0.5f
                : Mathf.Clamp(position.y, minY, maxY);

            if (lockZ)
            {
                position.z = fixedZ;
            }

            targetCamera.transform.position = position;
        }
    }
}
