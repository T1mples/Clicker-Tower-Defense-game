using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using YG;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ClickerTowerDefense
{
    public class StartScreenUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private static bool skipOpenOnNextSceneLoad;
        private static bool playGameplayStartSoundOnNextSceneLoad;

        public static void SkipOpenOnNextLoad()
        {
            skipOpenOnNextSceneLoad = true;
            playGameplayStartSoundOnNextSceneLoad = true;
        }

        [SerializeField] private GameObject root;
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private bool pauseGameWhileOpen = true;
        [SerializeField] private bool hideTilemapsWhileOpen = true;
        [Header("Forced Hidden While Start Screen Open")]
        [SerializeField] private bool forceHideGameplayObjectsWhileOpen = true;
        [SerializeField] private GameObject[] additionalObjectsToHide;
        [SerializeField] private string[] autoHideObjectNames = { "Table", "CentralTower", "SkillUseButton", "MenuToggleButton" };
        [Header("Best Score")]
        [SerializeField] private Text scoreText;
        [SerializeField] private ParticleSystem[] startScreenParticles;
        [Header("Audio")]
        [SerializeField] private AudioClip startButtonClickSound;
        [SerializeField, Range(0f, 1f)] private float startButtonClickVolume = 1f;

        private AudioSource uiAudioSource;
        private TilemapRenderer[] cachedTilemaps;
        private bool[] cachedTilemapStates;
        private readonly List<GameObject> hiddenSceneRoots = new List<GameObject>();
        private readonly List<GameObject> hiddenCanvasSiblings = new List<GameObject>();
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private readonly List<bool> hiddenRendererStates = new List<bool>();
        private readonly List<GameObject> forcedHiddenObjects = new List<GameObject>();
        private readonly List<bool> forcedHiddenOriginalStates = new List<bool>();

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            if (startButton == null)
            {
                startButton = GetComponentInChildren<Button>(true);
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
                startButton.onClick.AddListener(OnStartClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitClicked);
                quitButton.onClick.AddListener(OnQuitClicked);
            }

            if (skipOpenOnNextSceneLoad)
            {
                skipOpenOnNextSceneLoad = false;
                Close();
                PlayDeferredGameplayStartSound();
                return;
            }

            Open();
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitClicked);
            }

            if (pauseGameWhileOpen && IsOpen)
            {
                Time.timeScale = 1f;
            }

            IsOpen = false;
        }

        public void Open()
        {
            IsOpen = true;
            HideEverythingExceptStartScreen();
            if (root != null)
            {
                root.SetActive(true);
            }

            SetParticlesActive(true);
            SetTilemapsVisible(false);
            ApplyForcedHiddenObjects();
            UpdateBestScoreText();
            if (YG2.saves != null)
            {
                Debug.Log(YG2.saves.score);
            }

            if (pauseGameWhileOpen)
            {
                Time.timeScale = 0f;
            }
        }

        public void Close()
        {
            IsOpen = false;
            SetParticlesActive(false);
            SetTilemapsVisible(true);
            RestoreHiddenObjects();
            RestoreForcedHiddenObjects();

            if (root != null)
            {
                root.SetActive(false);
            }

            if (pauseGameWhileOpen)
            {
                Time.timeScale = 1f;
            }
        }

        private void OnStartClicked()
        {
            PlayStartButtonSound();
            Time.timeScale = 1f;
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.StopMusicAndCountdown();
            }

            skipOpenOnNextSceneLoad = true;
            playGameplayStartSoundOnNextSceneLoad = true;
            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }

        private void Update()
        {
            if (!Application.isPlaying || !IsOpen)
            {
                return;
            }

            // Runtime UI can appear after Open() (e.g. auto-built menu), keep hiding it while start screen is active.
            ApplyForcedHiddenObjects();
            UpdateBestScoreText();
        }

        private void SetParticlesActive(bool active)
        {
            if (startScreenParticles == null || startScreenParticles.Length == 0)
            {
                return;
            }

            for (int i = 0; i < startScreenParticles.Length; i++)
            {
                ParticleSystem ps = startScreenParticles[i];
                if (ps == null)
                {
                    continue;
                }

                if (active)
                {
                    if (!ps.isPlaying)
                    {
                        ps.Play(true);
                    }
                }
                else
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private void SetTilemapsVisible(bool visible)
        {
            if (!hideTilemapsWhileOpen)
            {
                return;
            }

            if (!visible)
            {
                CacheTilemaps();
                if (cachedTilemaps == null)
                {
                    return;
                }

                for (int i = 0; i < cachedTilemaps.Length; i++)
                {
                    TilemapRenderer renderer = cachedTilemaps[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.enabled = false;
                }

                return;
            }

            if (cachedTilemaps == null || cachedTilemapStates == null)
            {
                return;
            }

            int count = Mathf.Min(cachedTilemaps.Length, cachedTilemapStates.Length);
            for (int i = 0; i < count; i++)
            {
                TilemapRenderer renderer = cachedTilemaps[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = cachedTilemapStates[i];
            }
        }

        private void CacheTilemaps()
        {
            cachedTilemaps = FindObjectsByType<TilemapRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (cachedTilemaps == null)
            {
                cachedTilemapStates = null;
                return;
            }

            cachedTilemapStates = new bool[cachedTilemaps.Length];
            for (int i = 0; i < cachedTilemaps.Length; i++)
            {
                cachedTilemapStates[i] = cachedTilemaps[i] != null && cachedTilemaps[i].enabled;
            }
        }

        private void HideEverythingExceptStartScreen()
        {
            hiddenSceneRoots.Clear();
            hiddenCanvasSiblings.Clear();
            hiddenRenderers.Clear();
            hiddenRendererStates.Clear();

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject startCanvasRoot = transform.root != null ? transform.root.gameObject : null;
            Transform startTransform = root != null ? root.transform : transform;
            Canvas startCanvas = startTransform.GetComponentInParent<Canvas>(true);

            if (startCanvas != null)
            {
                Transform canvasTransform = startCanvas.transform;
                for (int i = 0; i < canvasTransform.childCount; i++)
                {
                    Transform child = canvasTransform.GetChild(i);
                    bool keepChild = child == startTransform || startTransform.IsChildOf(child);
                    if (keepChild)
                    {
                        continue;
                    }

                    GameObject sibling = child.gameObject;
                    if (!sibling.activeSelf)
                    {
                        continue;
                    }

                    sibling.SetActive(false);
                    hiddenCanvasSiblings.Add(sibling);
                }
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject sceneRoot = roots[i];
                if (sceneRoot == null || !sceneRoot.activeSelf)
                {
                    continue;
                }

                if (startCanvasRoot != null && sceneRoot == startCanvasRoot)
                {
                    continue;
                }

                if (sceneRoot.GetComponentInChildren<Camera>(true) != null)
                {
                    continue;
                }

                if (sceneRoot.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) != null)
                {
                    continue;
                }

                sceneRoot.SetActive(false);
                hiddenSceneRoots.Add(sceneRoot);
            }

            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Transform rendererTransform = renderer.transform;
                if (startTransform != null && (rendererTransform == startTransform || rendererTransform.IsChildOf(startTransform)))
                {
                    continue;
                }

                // Keep the camera pipeline alive.
                if (renderer.GetComponentInParent<Camera>() != null)
                {
                    continue;
                }

                hiddenRenderers.Add(renderer);
                hiddenRendererStates.Add(renderer.enabled);
                renderer.enabled = false;
            }
        }

        private void RestoreHiddenObjects()
        {
            int rendererCount = Mathf.Min(hiddenRenderers.Count, hiddenRendererStates.Count);
            for (int i = 0; i < rendererCount; i++)
            {
                Renderer renderer = hiddenRenderers[i];
                if (renderer != null)
                {
                    renderer.enabled = hiddenRendererStates[i];
                }
            }

            hiddenRenderers.Clear();
            hiddenRendererStates.Clear();

            for (int i = 0; i < hiddenCanvasSiblings.Count; i++)
            {
                GameObject sibling = hiddenCanvasSiblings[i];
                if (sibling != null)
                {
                    sibling.SetActive(true);
                }
            }

            hiddenCanvasSiblings.Clear();

            for (int i = 0; i < hiddenSceneRoots.Count; i++)
            {
                GameObject sceneRoot = hiddenSceneRoots[i];
                if (sceneRoot != null)
                {
                    sceneRoot.SetActive(true);
                }
            }

            hiddenSceneRoots.Clear();
        }

        private void ApplyForcedHiddenObjects()
        {
            if (!forceHideGameplayObjectsWhileOpen)
            {
                return;
            }

            if (additionalObjectsToHide != null)
            {
                for (int i = 0; i < additionalObjectsToHide.Length; i++)
                {
                    TrackAndDisable(additionalObjectsToHide[i]);
                }
            }

            if (autoHideObjectNames == null)
            {
                return;
            }

            for (int i = 0; i < autoHideObjectNames.Length; i++)
            {
                string objectName = autoHideObjectNames[i];
                if (string.IsNullOrWhiteSpace(objectName))
                {
                    continue;
                }

                GameObject target = GameObject.Find(objectName);
                TrackAndDisable(target);
            }
        }

        private void TrackAndDisable(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            Transform startTransform = root != null ? root.transform : transform;
            if (target.transform == startTransform || target.transform.IsChildOf(startTransform))
            {
                return;
            }

            int existingIndex = forcedHiddenObjects.IndexOf(target);
            if (existingIndex >= 0)
            {
                if (target.activeSelf)
                {
                    target.SetActive(false);
                }

                return;
            }

            forcedHiddenObjects.Add(target);
            forcedHiddenOriginalStates.Add(target.activeSelf);
            if (target.activeSelf)
            {
                target.SetActive(false);
            }
        }

        private void RestoreForcedHiddenObjects()
        {
            int count = Mathf.Min(forcedHiddenObjects.Count, forcedHiddenOriginalStates.Count);
            for (int i = 0; i < count; i++)
            {
                GameObject target = forcedHiddenObjects[i];
                if (target == null)
                {
                    continue;
                }

                target.SetActive(forcedHiddenOriginalStates[i]);
            }

            forcedHiddenObjects.Clear();
            forcedHiddenOriginalStates.Clear();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void PlayStartButtonSound()
        {
            if (startButtonClickSound == null)
            {
                return;
            }

            EnsureAudioSource();
            if (uiAudioSource != null)
            {
                uiAudioSource.PlayOneShot(startButtonClickSound, startButtonClickVolume * AudioSettings.SfxVolume);
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

        private void PlayDeferredGameplayStartSound()
        {
            if (!playGameplayStartSoundOnNextSceneLoad)
            {
                return;
            }

            playGameplayStartSoundOnNextSceneLoad = false;
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.PlayGameplayStartSound();
                gameManager.BeginGameplayMusicCountdown();
            }
        }

        private void UpdateBestScoreText()
        {
            if (scoreText == null)
            {
                scoreText = FindScoreText();
            }

            if (scoreText == null)
            {
                return;
            }

            int bestScore = YG2.saves != null ? YG2.saves.score : 0;
            scoreText.text = "Your best score: " + bestScore;
        }

        private Text FindScoreText()
        {
            Transform rootTransform = root != null ? root.transform : transform;
            if (rootTransform == null)
            {
                return null;
            }

            Transform scoreTransform = rootTransform.Find("Score");
            if (scoreTransform != null)
            {
                return scoreTransform.GetComponent<Text>();
            }

            Text[] allTexts = rootTransform.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < allTexts.Length; i++)
            {
                Text t = allTexts[i];
                if (t != null && t.gameObject.name == "Score")
                {
                    return t;
                }
            }

            return null;
        }
    }
}
