using UnityEngine;

namespace ClickerTowerDefense
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class TowerRangeView : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour rangeSource;
        [SerializeField] private float circleScale = 1f;
        [SerializeField] private bool showOnlyOnHover = true;
        [SerializeField] private bool hideOnStart = true;
        [SerializeField] private bool useGeneratedSmoothCircle = true;
        [SerializeField] private int generatedCircleSize = 256;

        private SpriteRenderer circleRenderer;
        private int hoverCount;
        private IRangeProvider rangeProvider;
        private static Sprite generatedCircleSprite;

        private void Awake()
        {
            if (rangeSource == null)
            {
                rangeProvider = GetComponentInParent<IRangeProvider>();
                rangeSource = rangeProvider as MonoBehaviour;
            }
            else
            {
                rangeProvider = rangeSource as IRangeProvider;
            }

            circleRenderer = GetComponent<SpriteRenderer>();
            EnsureSmoothCircleSprite();

            if (hideOnStart && circleRenderer != null)
            {
                circleRenderer.enabled = false;
            }
        }

        private void Update()
        {
            if (rangeProvider == null || circleRenderer == null)
            {
                return;
            }

            float radius = rangeProvider.Range;
            float diameter = radius * 2f;
            float spriteSize = 1f;
            if (circleRenderer.sprite != null)
            {
                spriteSize = circleRenderer.sprite.bounds.size.x;
                if (spriteSize <= 0f)
                {
                    spriteSize = 1f;
                }
            }

            float scale = (diameter * circleScale) / spriteSize;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        public void SetHovered(bool hovered)
        {
            if (!showOnlyOnHover || circleRenderer == null)
            {
                return;
            }

            if (hovered)
            {
                hoverCount++;
            }
            else
            {
                hoverCount = Mathf.Max(0, hoverCount - 1);
            }

            circleRenderer.enabled = hoverCount > 0;
        }

        private void EnsureSmoothCircleSprite()
        {
            if (!useGeneratedSmoothCircle || circleRenderer == null)
            {
                return;
            }

            int size = Mathf.Clamp(generatedCircleSize, 64, 1024);
            if (generatedCircleSprite == null || Mathf.RoundToInt(generatedCircleSprite.texture.width) != size)
            {
                generatedCircleSprite = CreateCircleSprite(size);
            }

            circleRenderer.sprite = generatedCircleSprite;
        }

        private static Sprite CreateCircleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 4;

            float radius = (size - 2) * 0.5f;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float edge = radius - 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center.x;
                    float dy = y - center.y;
                    float dist = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float alpha = dist <= edge ? 1f : Mathf.Clamp01(1f - (dist - edge));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            // 1 pixel per unit is fine; scale is computed from sprite bounds.
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
