using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using YG;

namespace ClickerTowerDefense
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button restartButton;
        [SerializeField] private GameObject titleObject;
        [SerializeField] private Text summaryText;
        [SerializeField] private Font summaryFont;
        [SerializeField] private BaseHealth baseHealth;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private bool pauseOnGameOver = true;
        [SerializeField] private bool hideOtherUiOnGameOver = true;
        [SerializeField] private bool hideWorldOnGameOver = true;
        [Header("Audio")]
        [SerializeField] private AudioClip gameOverSound;
        [SerializeField, Range(0f, 1f)] private float gameOverSoundVolume = 1f;

        private AudioSource uiAudioSource;

        private void Awake()
        {
            EnsureEventSystemExists();

            if (baseHealth == null)
            {
                baseHealth = FindFirstObjectByType<BaseHealth>();
            }

            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (restartButton == null)
            {
                restartButton = GetComponentInChildren<Button>(true);
            }

            EnsureSummaryTextExists();
            ApplySummaryFont();

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (baseHealth != null)
            {
                baseHealth.GameOverEvent += OnGameOver;
            }

            if (panel == null)
            {
                panel = gameObject;
            }

            SetVisible(false);
        }

        private void EnsureEventSystemExists()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                return;
            }

            GameObject go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private void OnDestroy()
        {
            if (baseHealth != null)
            {
                baseHealth.GameOverEvent -= OnGameOver;
            }
        }

        private void OnGameOver()
        {
            TrySaveBestScore();

            if (pauseOnGameOver)
            {
                Time.timeScale = 0f;
            }

            if (hideOtherUiOnGameOver)
            {
                HideOtherUi();
            }

            if (hideWorldOnGameOver)
            {
                HideWorldObjects();
            }

            UpdateSummaryText();
            SetVisible(true);
            PlayGameOverSound();
        }

        private void TrySaveBestScore()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (gameManager == null || YG2.saves == null)
            {
                return;
            }

            if (gameManager.Score <= YG2.saves.score)
            {
                return;
            }

            YG2.saves.score = gameManager.Score;
            YG2.SaveProgress();
            Debug.Log("saved on cloud");
            YG2.SetLeaderboard("LB", gameManager.Score);
            Debug.Log("send to leaderboard: " + gameManager.Score);
        }

        private void OnRestartClicked()
        {
            Time.timeScale = 1f;
            StartScreenUI.SkipOpenOnNextLoad();
            if (baseHealth != null)
            {
                baseHealth.RestartScene();
                return;
            }

            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.StopMusicAndCountdown();
            }

            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(activeScene.buildIndex);
        }

        private void SetVisible(bool value)
        {
            if (panel != null)
            {
                panel.SetActive(value);
                if (summaryText != null)
                {
                    summaryText.gameObject.SetActive(value);
                }
                return;
            }

            if (titleObject != null)
            {
                titleObject.SetActive(value);
            }

            if (summaryText != null)
            {
                summaryText.gameObject.SetActive(value);
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(value);
            }
        }

        private void UpdateSummaryText()
        {
            if (summaryText == null)
            {
                return;
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
            }

            int score = gameManager != null ? gameManager.Score : 0;
            int wave = 1;
            if (waveManager != null)
            {
                wave = Mathf.Max(1, waveManager.CurrentWaveIndex + 1);
            }

            summaryText.text = "Your total score: " + score + ", Wave: " + wave;
        }

        private void EnsureSummaryTextExists()
        {
            if (summaryText != null)
            {
                return;
            }

            Transform existing = transform.Find("GameOverSummaryText");
            if (existing != null)
            {
                summaryText = existing.GetComponent<Text>();
                if (summaryText != null)
                {
                    return;
                }
            }

            GameObject go = new GameObject("GameOverSummaryText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -6f);
            rect.sizeDelta = new Vector2(640f, 40f);

            summaryText = go.GetComponent<Text>();
            summaryText.font = GetSummaryFont();

            summaryText.fontSize = 28;
            summaryText.alignment = TextAnchor.MiddleCenter;
            summaryText.color = Color.white;
            summaryText.text = "Your total score: 0, Wave: 1";
            summaryText.raycastTarget = false;
        }

        private void ApplySummaryFont()
        {
            if (summaryText == null)
            {
                return;
            }

            Font font = GetSummaryFont();
            if (font != null)
            {
                summaryText.font = font;
            }
        }

        private Font GetSummaryFont()
        {
            if (summaryFont != null)
            {
                return summaryFont;
            }

#if UNITY_EDITOR
            summaryFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/FPSFont/FPS Gaming Font/Square-Black.ttf");
            if (summaryFont != null)
            {
                return summaryFont;
            }
#endif

            Font fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fallback == null)
            {
                fallback = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return fallback;
        }

        private void PlayGameOverSound()
        {
            if (gameOverSound == null)
            {
                return;
            }

            EnsureAudioSource();
            if (uiAudioSource != null)
            {
                uiAudioSource.PlayOneShot(gameOverSound, gameOverSoundVolume * AudioSettings.SfxVolume);
            }
        }

        private void EnsureAudioSource()
        {
            if (uiAudioSource != null)
            {
                return;
            }

            uiAudioSource = GetComponent<AudioSource>();
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
            }

            uiAudioSource.playOnAwake = false;
            uiAudioSource.loop = false;
            uiAudioSource.spatialBlend = 0f;
        }

        private void HideOtherUi()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            Transform keepTransform = panel != null ? panel.transform : transform;
            Transform keepCanvasRoot = keepTransform.root;

            if (keepTransform.parent != null)
            {
                Transform parent = keepTransform.parent;
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    if (child == keepTransform)
                    {
                        continue;
                    }

                    child.gameObject.SetActive(false);
                }
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null || !root.activeSelf)
                {
                    continue;
                }

                if (keepCanvasRoot != null && root == keepCanvasRoot.gameObject)
                {
                    continue;
                }

                if (root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) != null)
                {
                    continue;
                }

                if (root.GetComponentInChildren<Canvas>(true) != null)
                {
                    root.SetActive(false);
                }
            }
        }

        private void HideWorldObjects()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject canvasRoot = transform.root != null ? transform.root.gameObject : null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null || root == canvasRoot)
                {
                    continue;
                }

                if (root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) != null)
                {
                    continue;
                }

                // Keep camera active so Unity does not show "Display 1 No cameras rendering".
                if (root.GetComponentInChildren<Camera>(true) != null)
                {
                    continue;
                }

                root.SetActive(false);
            }
        }
    }
}
