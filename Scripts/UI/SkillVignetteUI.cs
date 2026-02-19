using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ClickerTowerDefense
{
    public class SkillVignetteUI : MonoBehaviour
    {
        [SerializeField] private Image overlayImage;
        [SerializeField] private Color megaStrikeColor = new Color(1f, 0.9f, 0.2f, 0.3f);
        [SerializeField] private Color freezeColor = new Color(1f, 1f, 1f, 0.22f);
        [SerializeField] private float megaStrikeFadeIn = 0.05f;
        [SerializeField] private float megaStrikeFadeOut = 0.35f;
        [SerializeField] private float freezeFadeOut = 0.2f;

        private SkillSystem skillSystem;
        private Coroutine effectRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureOnSceneCanvas(SceneManager.GetActiveScene());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureOnSceneCanvas(scene);
        }

        private static void EnsureOnSceneCanvas(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            Canvas canvas = null;
            for (int i = 0; i < roots.Length; i++)
            {
                canvas = roots[i].GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                {
                    break;
                }
            }

            if (canvas != null && canvas.GetComponent<SkillVignetteUI>() == null)
            {
                canvas.gameObject.AddComponent<SkillVignetteUI>();
            }
        }

        private void Awake()
        {
            EnsureOverlay();
            SetOverlayAlpha(0f);
            ResolveSkillSystem();
        }

        private void OnEnable()
        {
            ResolveSkillSystem();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveSkillSystem()
        {
            if (skillSystem == null)
            {
                skillSystem = SkillSystem.Instance;
            }

            if (skillSystem == null)
            {
                skillSystem = FindFirstObjectByType<SkillSystem>();
            }
        }

        private void Subscribe()
        {
            if (skillSystem != null)
            {
                skillSystem.SkillUsed -= OnSkillUsed;
                skillSystem.SkillUsed += OnSkillUsed;
            }
        }

        private void Unsubscribe()
        {
            if (skillSystem != null)
            {
                skillSystem.SkillUsed -= OnSkillUsed;
            }
        }

        private void OnSkillUsed(SkillType skillType)
        {
            if (overlayImage == null)
            {
                EnsureOverlay();
            }

            if (overlayImage == null)
            {
                return;
            }

            if (effectRoutine != null)
            {
                StopCoroutine(effectRoutine);
                effectRoutine = null;
            }

            if (skillType == SkillType.MegaStrike)
            {
                effectRoutine = StartCoroutine(MegaStrikeRoutine());
            }
            else if (skillType == SkillType.Freeze)
            {
                effectRoutine = StartCoroutine(FreezeRoutine());
            }
        }

        private IEnumerator MegaStrikeRoutine()
        {
            if (overlayImage != null)
            {
                overlayImage.transform.SetAsLastSibling();
            }

            overlayImage.color = new Color(megaStrikeColor.r, megaStrikeColor.g, megaStrikeColor.b, 0f);

            float t = 0f;
            while (t < megaStrikeFadeIn)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(0f, megaStrikeColor.a, t / Mathf.Max(0.001f, megaStrikeFadeIn));
                overlayImage.color = new Color(megaStrikeColor.r, megaStrikeColor.g, megaStrikeColor.b, a);
                yield return null;
            }

            t = 0f;
            while (t < megaStrikeFadeOut)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(megaStrikeColor.a, 0f, t / Mathf.Max(0.001f, megaStrikeFadeOut));
                overlayImage.color = new Color(megaStrikeColor.r, megaStrikeColor.g, megaStrikeColor.b, a);
                yield return null;
            }

            SetOverlayAlpha(0f);
            effectRoutine = null;
        }

        private IEnumerator FreezeRoutine()
        {
            if (overlayImage != null)
            {
                overlayImage.transform.SetAsFirstSibling();
            }

            overlayImage.color = freezeColor;
            float remaining = SkillSystem.FreezeDurationSeconds;
            while (remaining > 0f)
            {
                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            float t = 0f;
            while (t < freezeFadeOut)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(freezeColor.a, 0f, t / Mathf.Max(0.001f, freezeFadeOut));
                overlayImage.color = new Color(freezeColor.r, freezeColor.g, freezeColor.b, a);
                yield return null;
            }

            SetOverlayAlpha(0f);
            effectRoutine = null;
        }

        private void EnsureOverlay()
        {
            if (overlayImage != null)
            {
                return;
            }

            RectTransform canvasRect = transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            Transform existing = canvasRect.Find("SkillVignetteOverlay");
            RectTransform overlayRect;
            if (existing != null)
            {
                overlayRect = existing as RectTransform;
            }
            else
            {
                GameObject go = new GameObject("SkillVignetteOverlay", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(canvasRect, false);
                overlayRect = go.transform as RectTransform;
            }

            if (overlayRect == null)
            {
                return;
            }

            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.SetAsLastSibling();

            overlayImage = overlayRect.GetComponent<Image>();
            if (overlayImage == null)
            {
                overlayImage = overlayRect.gameObject.AddComponent<Image>();
            }

            overlayImage.raycastTarget = false;
        }

        private void SetOverlayAlpha(float alpha)
        {
            if (overlayImage == null)
            {
                return;
            }

            Color c = overlayImage.color;
            c.a = alpha;
            overlayImage.color = c;
        }
    }
}
