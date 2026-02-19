using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerTowerDefense
{
    public class TowerSelectionUI : MonoBehaviour
    {
        [SerializeField] private TowerPlacementManager placementManager;
        [SerializeField] private ToggleGroup toggleGroup;
        [SerializeField] private Toggle[] toggles;
        [SerializeField] private Color selectedColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private bool useRoundedButtons = true;
        [SerializeField] private bool overrideButtonSpriteInCode = true;
        [SerializeField] private bool overrideLabelColorInCode = true;
        [SerializeField] private int roundedSpriteSize = 64;
        [SerializeField] private int roundedRadius = 10;
        [SerializeField] private Color labelColor = Color.black;
        [Header("Audio")]
        [SerializeField] private AudioClip towerSelectClickSound;
        [SerializeField, Range(0f, 1f)] private float towerSelectClickVolume = 1f;

        private readonly Dictionary<Toggle, string> baseLabels = new Dictionary<Toggle, string>();
        private static Sprite roundedButtonSprite;
        private AudioSource uiAudioSource;

        private void Awake()
        {
            if (placementManager == null)
            {
                placementManager = FindFirstObjectByType<TowerPlacementManager>();
            }

            if (toggleGroup == null)
            {
                toggleGroup = GetComponent<ToggleGroup>();
            }

            if (toggleGroup == null)
            {
                toggleGroup = gameObject.AddComponent<ToggleGroup>();
            }

            InitializeToggles();
        }

        private void OnEnable()
        {
            InitializeToggles();
            SyncFromSelection();
        }

        private void Update()
        {
            if (!Application.isPlaying || StartScreenUI.IsOpen || GameMenuUI.IsMenuOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SelectIndex(0); // Solo-Tower
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SelectIndex(1); // Multi-Tower
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SelectIndex(2); // Slow-Tower
            }
        }

        private void InitializeToggles()
        {
            if (toggles == null || toggles.Length == 0)
            {
                toggles = CollectTogglesInHierarchyOrder();
            }

            baseLabels.Clear();
            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle toggle = toggles[i];
                if (toggle == null)
                {
                    continue;
                }

                toggle.group = toggleGroup;
                int index = ResolveIndex(toggle.name, i);
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(isOn => OnToggleChanged(index, isOn));

                Text label = toggle.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    string source = GetDisplayName(index, label.text);
                    baseLabels[toggle] = source;
                }
            }
        }

        private Toggle[] CollectTogglesInHierarchyOrder()
        {
            List<Toggle> ordered = new List<Toggle>();
            Transform parent = transform;
            for (int i = 0; i < parent.childCount; i++)
            {
                Toggle toggle = parent.GetChild(i).GetComponent<Toggle>();
                if (toggle != null)
                {
                    ordered.Add(toggle);
                }
            }

            if (ordered.Count > 0)
            {
                return ordered.ToArray();
            }

            // Fallback for nested layouts.
            return GetComponentsInChildren<Toggle>(true)
                .OrderBy(t => t.transform.GetSiblingIndex())
                .ToArray();
        }

        private void OnToggleChanged(int index, bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            if (placementManager != null)
            {
                placementManager.SelectTower(index);
            }

            PlayTowerSelectSound();
            RefreshVisual(index);
        }

        public void SelectIndex(int index)
        {
            if (toggles == null || toggles.Length == 0)
            {
                InitializeToggles();
            }

            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle toggle = toggles[i];
                if (toggle == null)
                {
                    continue;
                }

                bool shouldBeOn = ResolveIndex(toggle.name, i) == index;
                toggle.SetIsOnWithoutNotify(shouldBeOn);
            }

            if (placementManager != null)
            {
                placementManager.SelectTower(index);
            }

            PlayTowerSelectSound();
            RefreshVisual(index);
        }

        private void SyncFromSelection()
        {
            int selected = placementManager != null ? placementManager.SelectedIndex : 0;
            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle toggle = toggles[i];
                if (toggle == null)
                {
                    continue;
                }

                bool isSelected = ResolveIndex(toggle.name, i) == selected;
                toggle.SetIsOnWithoutNotify(isSelected);
            }

            RefreshVisual(selected);
        }

        private void RefreshVisual(int selectedIndex)
        {
            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle toggle = toggles[i];
                if (toggle == null)
                {
                    continue;
                }

                bool isSelected = ResolveIndex(toggle.name, i) == selectedIndex;
                Image bg = toggle.GetComponent<Image>();
                if (bg != null)
                {
                    ApplyRounded(bg);
                    bg.color = isSelected ? selectedColor : normalColor;
                }

                Text label = toggle.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    if (!baseLabels.TryGetValue(toggle, out string baseLabel))
                    {
                        baseLabel = GetDisplayName(ResolveIndex(toggle.name, i), label.text);
                        baseLabels[toggle] = baseLabel;
                    }

                    label.text = baseLabel;
                    if (overrideLabelColorInCode)
                    {
                        label.color = labelColor;
                    }
                }
            }
        }

        private int ResolveIndex(string uiName, int fallback)
        {
            if (string.IsNullOrWhiteSpace(uiName))
            {
                return fallback;
            }

            string lower = uiName.ToLowerInvariant();
            if (lower.Contains("square"))
            {
                return 0;
            }

            if (lower.Contains("circle"))
            {
                return 1;
            }

            if (lower.Contains("triangle"))
            {
                return 2;
            }

            return fallback;
        }

        private string GetDisplayName(int index, string fallback)
        {
            if (placementManager != null)
            {
                TowerOption[] options = placementManager.TowerOptions;
                if (options != null && index >= 0 && index < options.Length)
                {
                    TowerOption option = options[index];
                    if (option != null && !string.IsNullOrWhiteSpace(option.displayName))
                    {
                        return option.displayName;
                    }
                }
            }

            return string.IsNullOrWhiteSpace(fallback) ? "Tower" : fallback;
        }

        private void ApplyRounded(Image image)
        {
            if (!useRoundedButtons || !overrideButtonSpriteInCode || image == null)
            {
                return;
            }

            Sprite sprite = GetRoundedButtonSprite();
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
        }

        private Sprite GetRoundedButtonSprite()
        {
            int size = Mathf.Clamp(roundedSpriteSize, 32, 256);
            int radius = Mathf.Clamp(roundedRadius, 2, size / 2);
            if (roundedButtonSprite == null || roundedButtonSprite.texture == null || roundedButtonSprite.texture.width != size)
            {
                roundedButtonSprite = CreateRoundedRectSprite(size, radius);
            }

            return roundedButtonSprite;
        }

        private static Sprite CreateRoundedRectSprite(int size, int radius)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            int r = Mathf.Clamp(radius, 1, size / 2);
            float rr = r - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = 1f;
                    int dx = x < r ? r - x : (x >= size - r ? x - (size - r - 1) : 0);
                    int dy = y < r ? r - y : (y >= size - r ? y - (size - r - 1) : 0);
                    if (dx > 0 && dy > 0)
                    {
                        float dist = Mathf.Sqrt((dx * dx) + (dy * dy));
                        alpha = Mathf.Clamp01(rr - dist + 1f);
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                new Vector4(r, r, r, r));
        }

        private void PlayTowerSelectSound()
        {
            if (towerSelectClickSound == null)
            {
                return;
            }

            EnsureAudioSource();
            if (uiAudioSource != null)
            {
                uiAudioSource.PlayOneShot(towerSelectClickSound, towerSelectClickVolume * AudioSettings.SfxVolume);
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
    }
}
