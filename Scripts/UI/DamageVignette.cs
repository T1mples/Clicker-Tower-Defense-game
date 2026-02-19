using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerTowerDefense
{
    public class DamageVignette : MonoBehaviour
    {
        [SerializeField] private Image vignetteImage;
        [SerializeField] private BaseHealth baseHealth;
        [SerializeField] private float fadeInTime = 0.08f;
        [SerializeField] private float fadeOutTime = 0.35f;
        [SerializeField] private float maxAlpha = 0.35f;
        [Header("Quality")]
        [SerializeField] private bool generateHighQualityVignette = true;
        [SerializeField] private int vignetteTextureSize = 1024;
        [SerializeField, Range(0.3f, 0.95f)] private float clearCenterRadius = 0.62f;

        private Coroutine fadeRoutine;
        private static Sprite generatedVignetteSprite;

        private void Awake()
        {
            if (vignetteImage == null)
            {
                vignetteImage = GetComponent<Image>();
            }

            if (baseHealth == null)
            {
                baseHealth = FindFirstObjectByType<BaseHealth>();
            }

            if (generateHighQualityVignette)
            {
                ApplyHighQualityVignetteSprite();
            }

            SetAlpha(0f);
        }

        private void OnEnable()
        {
            if (baseHealth != null)
            {
                baseHealth.HealthChanged += OnHealthChanged;
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
            if (current < max)
            {
                Trigger();
            }
        }

        private void Trigger()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            fadeRoutine = StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            float t = 0f;
            while (t < fadeInTime)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(0f, maxAlpha, t / fadeInTime));
                yield return null;
            }

            t = 0f;
            while (t < fadeOutTime)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(maxAlpha, 0f, t / fadeOutTime));
                yield return null;
            }

            SetAlpha(0f);
            fadeRoutine = null;
        }

        private void SetAlpha(float alpha)
        {
            if (vignetteImage == null)
            {
                return;
            }

            Color c = vignetteImage.color;
            c.a = alpha;
            vignetteImage.color = c;
        }

        private void ApplyHighQualityVignetteSprite()
        {
            if (vignetteImage == null)
            {
                return;
            }

            if (generatedVignetteSprite == null)
            {
                generatedVignetteSprite = CreateVignetteSprite(
                    Mathf.Clamp(vignetteTextureSize, 256, 2048),
                    Mathf.Clamp(clearCenterRadius, 0.3f, 0.95f));
            }

            vignetteImage.sprite = generatedVignetteSprite;
            vignetteImage.type = Image.Type.Simple;
            vignetteImage.preserveAspect = false;
        }

        private static Sprite CreateVignetteSprite(int size, float clearCenter)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 8;

            float center = (size - 1) * 0.5f;
            float maxRadius = center;
            float startRadius = maxRadius * clearCenter;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float t = Mathf.InverseLerp(startRadius, maxRadius, distance);
                    float alpha = Mathf.SmoothStep(0f, 1f, t);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
        }
    }
}
