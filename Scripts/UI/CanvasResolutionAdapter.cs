using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ClickerTowerDefense
{
    public static class CanvasResolutionAdapter
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            ApplyToScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyToScene(scene);
        }

        private static void ApplyToScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Canvas[] canvases = roots[i].GetComponentsInChildren<Canvas>(true);
                for (int j = 0; j < canvases.Length; j++)
                {
                    ConfigureCanvas(canvases[j]);
                }
            }
        }

        private static void ConfigureCanvas(Canvas canvas)
        {
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
            {
                return;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<CanvasTextRefreshOnResize>() == null)
            {
                canvas.gameObject.AddComponent<CanvasTextRefreshOnResize>();
            }
        }
    }
}
