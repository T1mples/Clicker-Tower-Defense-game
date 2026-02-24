using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerTowerDefense
{
    [DisallowMultipleComponent]
    public class CanvasTextRefreshOnResize : MonoBehaviour
    {
        [SerializeField] private Canvas targetCanvas;
        [SerializeField, Range(1, 5)] private int waitFramesBeforeRefresh = 2;

        private int lastWidth = -1;
        private int lastHeight = -1;
        private Coroutine refreshCoroutine;

        private void Awake()
        {
            if (targetCanvas == null)
            {
                targetCanvas = GetComponent<Canvas>();
            }
        }

        private void OnEnable()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            ForceRefreshNow();
        }

        private void Update()
        {
            if (Screen.width == lastWidth && Screen.height == lastHeight)
            {
                return;
            }

            lastWidth = Screen.width;
            lastHeight = Screen.height;

            if (refreshCoroutine != null)
            {
                StopCoroutine(refreshCoroutine);
            }

            refreshCoroutine = StartCoroutine(RefreshAfterResize());
        }

        private IEnumerator RefreshAfterResize()
        {
            int frames = Mathf.Max(1, waitFramesBeforeRefresh);
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }

            ForceRefreshNow();
            refreshCoroutine = null;
        }

        private void ForceRefreshNow()
        {
            if (targetCanvas == null)
            {
                return;
            }

            Text[] texts = targetCanvas.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                {
                    texts[i].SetAllDirty();
                }
            }

            RectTransform root = targetCanvas.transform as RectTransform;
            if (root != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            }

            Canvas.ForceUpdateCanvases();
        }
    }
}
