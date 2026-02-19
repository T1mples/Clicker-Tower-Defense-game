using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
        [SerializeField] private ParticleSystem[] startScreenParticles;
        [Header("Audio")]
        [SerializeField] private AudioClip startButtonClickSound;
        [SerializeField, Range(0f, 1f)] private float startButtonClickVolume = 1f;

        private AudioSource uiAudioSource;

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
            if (root != null)
            {
                root.SetActive(true);
            }

            SetParticlesActive(true);

            if (pauseGameWhileOpen)
            {
                Time.timeScale = 0f;
            }
        }

        public void Close()
        {
            IsOpen = false;
            SetParticlesActive(false);

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
            skipOpenOnNextSceneLoad = true;
            playGameplayStartSoundOnNextSceneLoad = true;
            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
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
            }
        }
    }
}
