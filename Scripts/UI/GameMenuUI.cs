using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ClickerTowerDefense
{
    [ExecuteAlways]
    public class GameMenuUI : MonoBehaviour
    {
        public static bool IsMenuOpen { get; private set; }

        private enum ShopSection
        {
            Skills,
            Upgrade
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureOnSceneCanvas(SceneManager.GetActiveScene());
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

            // Avoid creating a second runtime instance with default (empty) serialized values.
            GameMenuUI existing = FindFirstObjectByType<GameMenuUI>();
            if (existing != null)
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

            if (canvas != null)
            {
                if (canvas.GetComponent<GameMenuUI>() == null)
                {
                    canvas.gameObject.AddComponent<GameMenuUI>();
                }
            }
        }

        [Header("Visual")]
        [SerializeField] private Font uiFont;
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.82f);
        [SerializeField] private Color tabSelectedColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color tabNormalColor = Color.white;
        [SerializeField] private Color actionButtonColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        [SerializeField] private Color menuOpenButtonColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color skillReadyButtonColor = new Color(1f, 0.9f, 0.25f, 1f);

        [Header("Optional")]
        [SerializeField] private bool pauseGameplayWhenOpen;
        [SerializeField] private bool previewInEditor = true;
        [SerializeField] private bool useSceneLayoutInPlayMode = true;
        [Header("Audio")]
        [SerializeField] private AudioClip menuToggleClickSound;
        [SerializeField, Range(0f, 1f)] private float menuToggleClickVolume = 1f;
        [SerializeField] private AudioClip menuButtonHoverSound;
        [SerializeField, Range(0f, 1f)] private float menuButtonHoverVolume = 1f;
        [SerializeField] private AudioClip menuButtonClickSound;
        [SerializeField, Range(0f, 1f)] private float menuButtonClickVolume = 1f;

        private GameObject menuRoot;
        private GameObject shopTabContent;
        private GameObject settingsTabContent;
        private GameObject controlsTabContent;

        private Button menuToggleButton;
        private Button shopTabButton;
        private Button settingsTabButton;
        private Button controlsTabButton;
        private Button shopSkillsSectionButton;
        private Button shopUpgradeSectionButton;
        private Button freezePurchaseButton;
        private Button megaStrikePurchaseButton;
        private Button upgradeAutoClickerButton;
        private Button upgradeBaseHpButton;
        private Button upgradeTowerLimitButton;
        private Button upgradeClickButton;
        private Button restartButton;
        private Button giveUpButton;
        private Button skipWaveButton;
        private Button backToStartButton;
        private Button skillUseButton;
        private Slider sfxVolumeSlider;
        private Slider musicVolumeSlider;

        private Text shopStatusText;
        private Text skillUseButtonText;
        private Text shopTitleText;
        private Text upgradeBaseHpButtonText;
        private Text upgradeTowerLimitButtonText;
        private Text upgradeClickButtonText;
        private Text upgradeAutoClickerButtonText;
        private Text sfxVolumeValueText;
        private Text musicVolumeValueText;

        private bool isOpen;
        private float nextSkillUiUpdateTime;
        private ShopSection currentShopSection = ShopSection.Skills;
        private static Sprite roundedButtonSprite;
        private static AudioClip cachedMenuToggleClickSound;
        private static float cachedMenuToggleClickVolume = 1f;
        private static AudioClip cachedMenuButtonHoverSound;
        private static float cachedMenuButtonHoverVolume = 1f;
        private static AudioClip cachedMenuButtonClickSound;
        private static float cachedMenuButtonClickVolume = 1f;

        private SkillSystem skillSystem;
        private GameManager gameManager;
        private AutoClickerSystem autoClickerSystem;
        private BaseHealth baseHealth;
        private TowerPlacementManager towerPlacementManager;
        private CentralTower centralTower;
        private WaveManager waveManager;
        private AudioSource uiAudioSource;
        private bool backToStartRuntimeBound;

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                RectTransform canvasRect = transform as RectTransform;
                bool hasExistingLayout = canvasRect != null && TryBindExistingUi(canvasRect);
                if (!hasExistingLayout && previewInEditor)
                {
                    BuildUiIfNeeded();
                }

                if (previewInEditor && menuRoot != null)
                {
                    menuRoot.SetActive(true);
                }
                else if (menuRoot != null)
                {
                    menuRoot.SetActive(false);
                }

                return;
            }

            RestoreCachedMenuAudio();
            BuildUiIfNeeded();
            TryResolveSystems();
            SetMenuOpen(false);
            SelectTab(showShop: true);
            RefreshShopStatus("Buy a skill. You can own only one.");
            RefreshSkillButton();
            CacheMenuAudio();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!backToStartRuntimeBound)
            {
                TryBindBackToStartRuntime();
            }

            if (!StartScreenUI.IsOpen && Input.GetKeyDown(KeyCode.Space))
            {
                OnUseSkillClicked();
            }

            if (!StartScreenUI.IsOpen && !IsGameOverActive() && Input.GetKeyDown(KeyCode.Escape))
            {
                PlayMenuToggleClickSound();
                SetMenuOpen(!isOpen);
            }

            if (Time.unscaledTime >= nextSkillUiUpdateTime)
            {
                nextSkillUiUpdateTime = Time.unscaledTime + 0.2f;
                RefreshSkillButton();
            }
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            IsMenuOpen = false;
            CacheMenuAudio();

            if (skillSystem != null)
            {
                skillSystem.StateChanged -= OnSkillStateChanged;
            }

            if (pauseGameplayWhenOpen)
            {
                Time.timeScale = 1f;
            }
        }

        private void TryResolveSystems()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
                if (gameManager == null)
                {
                    gameManager = FindFirstObjectByType<GameManager>();
                }
            }

            if (skillSystem == null)
            {
                skillSystem = FindFirstObjectByType<SkillSystem>();
            }

            if (baseHealth == null)
            {
                baseHealth = FindFirstObjectByType<BaseHealth>();
            }

            if (autoClickerSystem == null)
            {
                autoClickerSystem = FindFirstObjectByType<AutoClickerSystem>();
            }

            if (towerPlacementManager == null)
            {
                towerPlacementManager = FindFirstObjectByType<TowerPlacementManager>();
            }

            if (centralTower == null)
            {
                centralTower = FindFirstObjectByType<CentralTower>();
            }

            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
            }

            if (skillSystem != null)
            {
                skillSystem.StateChanged -= OnSkillStateChanged;
                skillSystem.StateChanged += OnSkillStateChanged;
            }
        }

        private void BuildUiIfNeeded()
        {
            RectTransform canvasRect = transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            if (Application.isPlaying && useSceneLayoutInPlayMode && TryBindExistingUi(canvasRect))
            {
                BindUiEvents();
                return;
            }

            menuToggleButton = BuildMenuToggleButton(canvasRect);
            skillUseButton = BuildSkillUseButton(canvasRect, out skillUseButtonText);

            menuRoot = BuildMenuRoot(canvasRect);
            RectTransform tabsBar = BuildTabsBar(menuRoot.transform as RectTransform);
            RectTransform contentRoot = BuildContentRoot(menuRoot.transform as RectTransform);

            shopTabButton = BuildTabButton(tabsBar, "TabShopButton", "Shop", 56f);
            settingsTabButton = BuildTabButton(tabsBar, "TabSettingsButton", "Settings", 156f);
            controlsTabButton = BuildTabButton(tabsBar, "TabControlsButton", "Controls", 256f);

            shopTabContent = BuildShopContent(
                contentRoot,
                out shopSkillsSectionButton,
                out shopUpgradeSectionButton,
                out freezePurchaseButton,
                out megaStrikePurchaseButton,
                out upgradeAutoClickerButton,
                out upgradeBaseHpButton,
                out upgradeTowerLimitButton,
                out upgradeClickButton,
                out shopStatusText);
            settingsTabContent = BuildSettingsContent(
                contentRoot,
                out restartButton,
                out giveUpButton,
                out skipWaveButton,
                out backToStartButton);
            controlsTabContent = BuildControlsContent(contentRoot);

            BindUiEvents();
        }

        private void BindUiEvents()
        {
            if (menuToggleButton == null || skillUseButton == null)
            {
                return;
            }

            menuToggleButton.onClick.RemoveListener(OnMenuToggleClicked);
            menuToggleButton.onClick.AddListener(OnMenuToggleClicked);

            shopTabButton.onClick.RemoveListener(OnShopTabClicked);
            shopTabButton.onClick.AddListener(OnShopTabClicked);

            settingsTabButton.onClick.RemoveListener(OnSettingsTabClicked);
            settingsTabButton.onClick.AddListener(OnSettingsTabClicked);
            controlsTabButton.onClick.RemoveListener(OnControlsTabClicked);
            controlsTabButton.onClick.AddListener(OnControlsTabClicked);

            shopSkillsSectionButton.onClick.RemoveListener(OnShopSkillsSectionClicked);
            shopSkillsSectionButton.onClick.AddListener(OnShopSkillsSectionClicked);

            shopUpgradeSectionButton.onClick.RemoveListener(OnShopUpgradeSectionClicked);
            shopUpgradeSectionButton.onClick.AddListener(OnShopUpgradeSectionClicked);

            freezePurchaseButton.onClick.RemoveListener(OnBuyFreezeClicked);
            freezePurchaseButton.onClick.AddListener(OnBuyFreezeClicked);

            megaStrikePurchaseButton.onClick.RemoveListener(OnBuyMegaStrikeClicked);
            megaStrikePurchaseButton.onClick.AddListener(OnBuyMegaStrikeClicked);

            upgradeAutoClickerButton.onClick.RemoveListener(OnUpgradeAutoClickerClicked);
            upgradeAutoClickerButton.onClick.AddListener(OnUpgradeAutoClickerClicked);

            upgradeBaseHpButton.onClick.RemoveListener(OnUpgradeBaseHpClicked);
            upgradeBaseHpButton.onClick.AddListener(OnUpgradeBaseHpClicked);

            upgradeTowerLimitButton.onClick.RemoveListener(OnUpgradeTowerLimitClicked);
            upgradeTowerLimitButton.onClick.AddListener(OnUpgradeTowerLimitClicked);

            upgradeClickButton.onClick.RemoveListener(OnUpgradeClickClicked);
            upgradeClickButton.onClick.AddListener(OnUpgradeClickClicked);

            restartButton.onClick.RemoveListener(OnRestartClicked);
            restartButton.onClick.AddListener(OnRestartClicked);

            giveUpButton.onClick.RemoveListener(OnGiveUpClicked);
            giveUpButton.onClick.AddListener(OnGiveUpClicked);

            skipWaveButton.onClick.RemoveListener(OnSkipWaveClicked);
            skipWaveButton.onClick.AddListener(OnSkipWaveClicked);

            if (backToStartButton != null)
            {
                backToStartButton.onClick.RemoveListener(OnBackToStartClicked);
                backToStartButton.onClick.AddListener(OnBackToStartClicked);
            }

            skillUseButton.onClick.RemoveListener(OnUseSkillClicked);
            skillUseButton.onClick.AddListener(OnUseSkillClicked);

            BindAudioVolumeSliders();
            WireMenuButtonAudio();
        }

        private bool TryBindExistingUi(RectTransform canvasRect)
        {
            Transform menuRootTransform = FindDeepChild(canvasRect, "GameMenuRoot");
            if (menuRootTransform == null)
            {
                return false;
            }

            menuRoot = menuRootTransform.gameObject;
            menuToggleButton = FindDeepChild(canvasRect, "MenuToggleButton")?.GetComponent<Button>();
            skillUseButton = FindDeepChild(canvasRect, "SkillUseButton")?.GetComponent<Button>();
            skillUseButtonText = skillUseButton != null ? skillUseButton.GetComponentInChildren<Text>(true) : null;

            shopTabButton = FindDeepChild(menuRootTransform, "TabShopButton")?.GetComponent<Button>();
            settingsTabButton = FindDeepChild(menuRootTransform, "TabSettingsButton")?.GetComponent<Button>();
            controlsTabButton = FindDeepChild(menuRootTransform, "TabControlsButton")?.GetComponent<Button>();

            shopTabContent = FindDeepChild(menuRootTransform, "ShopTabContent")?.gameObject;
            settingsTabContent = FindDeepChild(menuRootTransform, "SettingsTabContent")?.gameObject;
            controlsTabContent = FindDeepChild(menuRootTransform, "ControlsTabContent")?.gameObject;

            shopSkillsSectionButton = FindDeepChild(menuRootTransform, "ShopSectionSkillsButton")?.GetComponent<Button>();
            shopUpgradeSectionButton = FindDeepChild(menuRootTransform, "ShopSectionUpgradeButton")?.GetComponent<Button>();
            freezePurchaseButton = FindDeepChild(menuRootTransform, "BuyFreezeButton")?.GetComponent<Button>();
            megaStrikePurchaseButton = FindDeepChild(menuRootTransform, "BuyMegaStrikeButton")?.GetComponent<Button>();
            upgradeAutoClickerButton = FindDeepChild(menuRootTransform, "UpgradeAutoClickerButton")?.GetComponent<Button>();
            upgradeBaseHpButton = FindDeepChild(menuRootTransform, "UpgradeBaseHpButton")?.GetComponent<Button>();
            upgradeTowerLimitButton = FindDeepChild(menuRootTransform, "UpgradeTowerLimitButton")?.GetComponent<Button>();
            upgradeClickButton = FindDeepChild(menuRootTransform, "UpgradeClickButton")?.GetComponent<Button>();

            restartButton = FindDeepChild(menuRootTransform, "RestartFromSettingsButton")?.GetComponent<Button>();
            giveUpButton = FindDeepChild(menuRootTransform, "GiveUpFromSettingsButton")?.GetComponent<Button>();
            skipWaveButton = FindDeepChild(menuRootTransform, "SkipWaveFromSettingsButton")?.GetComponent<Button>();
            backToStartButton = FindDeepChild(menuRootTransform, "BackToStartFromSettingsButton")?.GetComponent<Button>();
            if (backToStartButton == null && settingsTabContent != null)
            {
                backToStartButton = FindButtonByText(settingsTabContent.transform, "back to start");
            }

            shopStatusText = FindDeepChild(menuRootTransform, "StatusText")?.GetComponent<Text>();
            shopTitleText = FindDeepChild(menuRootTransform, "ShopTabContent")?.Find("Title")?.GetComponent<Text>();
            upgradeBaseHpButtonText = upgradeBaseHpButton != null ? upgradeBaseHpButton.GetComponentInChildren<Text>(true) : null;
            upgradeTowerLimitButtonText = upgradeTowerLimitButton != null ? upgradeTowerLimitButton.GetComponentInChildren<Text>(true) : null;
            upgradeClickButtonText = upgradeClickButton != null ? upgradeClickButton.GetComponentInChildren<Text>(true) : null;
            upgradeAutoClickerButtonText = upgradeAutoClickerButton != null ? upgradeAutoClickerButton.GetComponentInChildren<Text>(true) : null;

            Transform sfxRow = FindDeepChild(menuRootTransform, "SfxVolumeSliderRow");
            Transform musicRow = FindDeepChild(menuRootTransform, "MusicVolumeSliderRow");
            sfxVolumeSlider = sfxRow != null ? sfxRow.GetComponentInChildren<Slider>(true) : null;
            musicVolumeSlider = musicRow != null ? musicRow.GetComponentInChildren<Slider>(true) : null;
            sfxVolumeValueText = sfxRow != null ? FindDeepChild(sfxRow, "Value")?.GetComponent<Text>() : null;
            musicVolumeValueText = musicRow != null ? FindDeepChild(musicRow, "Value")?.GetComponent<Text>() : null;

            return menuToggleButton != null
                && skillUseButton != null
                && shopTabButton != null
                && settingsTabButton != null
                && controlsTabButton != null
                && shopTabContent != null
                && settingsTabContent != null
                && controlsTabContent != null;
        }

        private static Transform FindDeepChild(Transform root, string targetName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == targetName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindDeepChild(root.GetChild(i), targetName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static Button FindButtonByText(Transform root, string textToFindLower)
        {
            if (root == null || string.IsNullOrWhiteSpace(textToFindLower))
            {
                return null;
            }

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                Text label = button.GetComponentInChildren<Text>(true);
                if (label == null || string.IsNullOrWhiteSpace(label.text))
                {
                    continue;
                }

                if (label.text.Trim().ToLowerInvariant().Contains(textToFindLower))
                {
                    return button;
                }
            }

            return null;
        }

        private void OnMenuToggleClicked()
        {
            PlayMenuToggleClickSound();
            SetMenuOpen(!isOpen);
        }

#if UNITY_EDITOR
        [ContextMenu("Build Menu In Scene")]
        private void BuildMenuInScene()
        {
            BuildUiIfNeeded();
            if (menuRoot != null)
            {
                menuRoot.SetActive(true);
            }

            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif

        private void RestoreCachedMenuAudio()
        {
            if (menuToggleClickSound == null && cachedMenuToggleClickSound != null)
            {
                menuToggleClickSound = cachedMenuToggleClickSound;
            }

            if (menuButtonHoverSound == null && cachedMenuButtonHoverSound != null)
            {
                menuButtonHoverSound = cachedMenuButtonHoverSound;
            }

            if (menuButtonClickSound == null && cachedMenuButtonClickSound != null)
            {
                menuButtonClickSound = cachedMenuButtonClickSound;
            }

            menuToggleClickVolume = Mathf.Clamp01(menuToggleClickVolume);
            menuButtonHoverVolume = Mathf.Clamp01(menuButtonHoverVolume);
            menuButtonClickVolume = Mathf.Clamp01(menuButtonClickVolume);
        }

        private void CacheMenuAudio()
        {
            if (menuToggleClickSound != null)
            {
                cachedMenuToggleClickSound = menuToggleClickSound;
            }

            if (menuButtonHoverSound != null)
            {
                cachedMenuButtonHoverSound = menuButtonHoverSound;
            }

            if (menuButtonClickSound != null)
            {
                cachedMenuButtonClickSound = menuButtonClickSound;
            }

            cachedMenuToggleClickVolume = menuToggleClickVolume;
            cachedMenuButtonHoverVolume = menuButtonHoverVolume;
            cachedMenuButtonClickVolume = menuButtonClickVolume;
        }

        private void OnShopTabClicked()
        {
            SelectTab(showShop: true);
        }

        private void OnSettingsTabClicked()
        {
            SelectTab(showShop: false);
        }

        private void OnControlsTabClicked()
        {
            SelectTab(showShop: false, showControls: true);
        }

        private void OnShopSkillsSectionClicked()
        {
            SelectShopSection(ShopSection.Skills);
        }

        private void OnShopUpgradeSectionClicked()
        {
            SelectShopSection(ShopSection.Upgrade);
        }

        private void OnBuyFreezeClicked()
        {
            TryResolveSystems();
            if (skillSystem == null)
            {
                RefreshShopStatus("Skill system not found.");
                return;
            }

            bool success = skillSystem.TryPurchase(SkillType.Freeze);
            if (success)
            {
                PlaySkillPurchaseSound();
            }

            RefreshShopStatus(success
                ? "Purchased: Freeze. Previous skill replaced."
                : "Not enough coins for Freeze.");
            RefreshSkillButton();
        }

        private void OnBuyMegaStrikeClicked()
        {
            TryResolveSystems();
            if (skillSystem == null)
            {
                RefreshShopStatus("Skill system not found.");
                return;
            }

            bool success = skillSystem.TryPurchase(SkillType.MegaStrike);
            if (success)
            {
                PlaySkillPurchaseSound();
            }

            RefreshShopStatus(success
                ? "Purchased: Mega Strike. Previous skill replaced."
                : "Not enough coins for Mega Strike.");
            RefreshSkillButton();
        }

        private void OnUseSkillClicked()
        {
            TryResolveSystems();
            if (skillSystem == null)
            {
                return;
            }

            bool used = skillSystem.TryUseOwnedSkill();
            if (!used)
            {
                float cooldown = skillSystem.GetRemainingCooldown();
                if (cooldown > 0f)
                {
                    RefreshShopStatus("Skill cooldown: " + FormatTime(cooldown));
                }
                else if (skillSystem.OwnedSkill == SkillType.None)
                {
                    RefreshShopStatus("Buy a skill in Shop first.");
                }
            }

            RefreshSkillButton();
        }

        private void OnRestartClicked()
        {
            Time.timeScale = 1f;
            StartScreenUI.SkipOpenOnNextLoad();

            if (baseHealth == null)
            {
                baseHealth = FindFirstObjectByType<BaseHealth>();
            }

            if (baseHealth != null)
            {
                baseHealth.RestartScene();
                return;
            }

            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }

        private void OnGiveUpClicked()
        {
            if (baseHealth == null)
            {
                baseHealth = FindFirstObjectByType<BaseHealth>();
            }

            if (baseHealth == null || baseHealth.IsGameOver)
            {
                return;
            }

            baseHealth.TakeDamage(baseHealth.CurrentHealth);
        }

        private void OnSkipWaveClicked()
        {
            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
            }

            if (waveManager == null)
            {
                RefreshShopStatus("Wave manager not found.");
                return;
            }

            bool skipped = waveManager.SkipCurrentWave();
            if (skipped)
            {
                RefreshShopStatus("Wave skipped.");
            }
            else
            {
                RefreshShopStatus("No active wave to skip.");
            }
        }

        private void OnBackToStartClicked()
        {
            StartScreenUI startScreen = ResolveStartScreen();
            if (startScreen == null)
            {
                RefreshShopStatus("Start screen not found.");
                return;
            }

            SetMenuOpen(false);
            startScreen.Open();
        }

        private void TryBindBackToStartRuntime()
        {
            if (backToStartButton == null)
            {
                Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
                for (int i = 0; i < buttons.Length; i++)
                {
                    Button candidate = buttons[i];
                    if (candidate == null || candidate.gameObject == null)
                    {
                        continue;
                    }

                    if (!candidate.gameObject.scene.IsValid() || !candidate.gameObject.scene.isLoaded)
                    {
                        continue;
                    }

                    string nameLower = candidate.gameObject.name.ToLowerInvariant();
                    if (nameLower.Contains("backtostart") || nameLower.Contains("startfromsettings"))
                    {
                        backToStartButton = candidate;
                        break;
                    }
                }
            }

            if (backToStartButton == null)
            {
                return;
            }

            backToStartButton.onClick.RemoveListener(OnBackToStartClicked);
            backToStartButton.onClick.AddListener(OnBackToStartClicked);
            backToStartRuntimeBound = true;
        }

        private StartScreenUI ResolveStartScreen()
        {
            StartScreenUI startScreen = FindFirstObjectByType<StartScreenUI>();
            if (startScreen != null)
            {
                return startScreen;
            }

            StartScreenUI[] all = Resources.FindObjectsOfTypeAll<StartScreenUI>();
            for (int i = 0; i < all.Length; i++)
            {
                StartScreenUI candidate = all[i];
                if (candidate == null)
                {
                    continue;
                }

                GameObject go = candidate.gameObject;
                if (go == null)
                {
                    continue;
                }

                if (!go.scene.IsValid() || !go.scene.isLoaded)
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private bool IsGameOverActive()
        {
            if (baseHealth == null)
            {
                baseHealth = FindFirstObjectByType<BaseHealth>();
            }

            return baseHealth != null && baseHealth.IsGameOver;
        }

        private void OnUpgradeAutoClickerClicked()
        {
            if (autoClickerSystem == null)
            {
                autoClickerSystem = FindFirstObjectByType<AutoClickerSystem>();
            }

            if (autoClickerSystem == null)
            {
                RefreshShopStatus("AutoClicker system not found.");
                UpdateAutoClickerUpgradeButton();
                return;
            }

            if (autoClickerSystem.IsUpgradeMaxed)
            {
                RefreshShopStatus("Maximum auto-clicker upgrade reached.");
                UpdateAutoClickerUpgradeButton();
                return;
            }

            bool success = autoClickerSystem.TryUpgrade();
            if (success)
            {
                PlayUpgradePurchaseSound();
                RefreshShopStatus("Auto-clicker upgraded: " + autoClickerSystem.IncomePerSecond + " coins/sec.");
            }
            else
            {
                RefreshShopStatus("Not enough coins for auto-clicker upgrade.");
            }

            UpdateAutoClickerUpgradeButton();
        }

        private void OnUpgradeBaseHpClicked()
        {
            if (baseHealth == null)
            {
                baseHealth = FindFirstObjectByType<BaseHealth>();
            }

            if (baseHealth == null)
            {
                RefreshShopStatus("Base health component not found.");
                UpdateBaseUpgradeButton();
                return;
            }

            if (baseHealth.IsBaseUpgradeMaxed)
            {
                RefreshShopStatus("Maximum base HP upgrade reached.");
                UpdateBaseUpgradeButton();
                return;
            }

            bool success = baseHealth.TryUpgradeBaseHealth();
            if (success)
            {
                PlayUpgradePurchaseSound();
                RefreshShopStatus("Base upgraded: +" + 5 + " max HP.");
            }
            else
            {
                RefreshShopStatus("Not enough coins for base HP upgrade.");
            }

            UpdateBaseUpgradeButton();
            UpdateTowerLimitUpgradeButton();
            UpdateAutoClickerUpgradeButton();
        }

        private void OnUpgradeTowerLimitClicked()
        {
            if (towerPlacementManager == null)
            {
                towerPlacementManager = FindFirstObjectByType<TowerPlacementManager>();
            }

            if (towerPlacementManager == null)
            {
                RefreshShopStatus("Tower placement manager not found.");
                UpdateTowerLimitUpgradeButton();
                return;
            }

            if (towerPlacementManager.IsTowerLimitUpgradeMaxed)
            {
                RefreshShopStatus("Maximum tower limit upgrade reached.");
                UpdateTowerLimitUpgradeButton();
                return;
            }

            bool success = towerPlacementManager.TryUpgradeTowerLimit();
            if (success)
            {
                PlayUpgradePurchaseSound();
                RefreshShopStatus("Tower limit upgraded: +5 slots.");
            }
            else
            {
                RefreshShopStatus("Not enough coins for tower limit upgrade.");
            }

            UpdateTowerLimitUpgradeButton();
        }

        private void OnUpgradeClickClicked()
        {
            if (centralTower == null)
            {
                centralTower = FindFirstObjectByType<CentralTower>();
            }

            if (centralTower == null)
            {
                RefreshShopStatus("Central tower not found.");
                UpdateClickUpgradeButton();
                return;
            }

            if (centralTower.IsClickUpgradeMaxed)
            {
                RefreshShopStatus("Maximum click upgrade reached.");
                UpdateClickUpgradeButton();
                return;
            }

            bool success = centralTower.TryUpgradeClick();
            if (success)
            {
                PlayUpgradePurchaseSound();
                RefreshShopStatus("Click upgraded. Coins per click: " + centralTower.CoinsPerClick);
            }
            else
            {
                RefreshShopStatus("Not enough coins for click upgrade.");
            }

            UpdateClickUpgradeButton();
        }

        private void OnSkillStateChanged()
        {
            RefreshSkillButton();
        }

        public void PlayMenuButtonHoverSound()
        {
            PlayUiOneShot(menuButtonHoverSound, menuButtonHoverVolume);
        }

        public void PlayMenuButtonClickSound()
        {
            PlayUiOneShot(menuButtonClickSound, menuButtonClickVolume);
        }

        private void PlayMenuToggleClickSound()
        {
            PlayUiOneShot(menuToggleClickSound, menuToggleClickVolume);
        }

        private void PlayUiOneShot(AudioClip clip, float volume)
        {
            if (clip == null)
            {
                return;
            }

            EnsureUiAudioSource();
            if (uiAudioSource != null)
            {
                uiAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume) * AudioSettings.SfxVolume);
            }
        }

        private void EnsureUiAudioSource()
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

        private void WireMenuButtonAudio()
        {
            AttachMenuButtonAudio(shopTabButton);
            AttachMenuButtonAudio(settingsTabButton);
            AttachMenuButtonAudio(controlsTabButton);
            AttachMenuButtonAudio(shopSkillsSectionButton);
            AttachMenuButtonAudio(shopUpgradeSectionButton);
            AttachMenuButtonAudio(freezePurchaseButton, false);
            AttachMenuButtonAudio(megaStrikePurchaseButton, false);
            AttachMenuButtonAudio(upgradeAutoClickerButton, false);
            AttachMenuButtonAudio(upgradeBaseHpButton, false);
            AttachMenuButtonAudio(upgradeTowerLimitButton, false);
            AttachMenuButtonAudio(upgradeClickButton, false);
            AttachMenuButtonAudio(restartButton);
            AttachMenuButtonAudio(giveUpButton);
            AttachMenuButtonAudio(skipWaveButton);
            AttachMenuButtonAudio(backToStartButton);
        }

        private void AttachMenuButtonAudio(Button button, bool withClickSound = true)
        {
            if (button == null)
            {
                return;
            }

            MenuButtonHoverSfx hoverSfx = button.GetComponent<MenuButtonHoverSfx>();
            if (hoverSfx == null)
            {
                hoverSfx = button.gameObject.AddComponent<MenuButtonHoverSfx>();
            }

            hoverSfx.Initialize(this);
            button.onClick.RemoveListener(PlayMenuButtonClickSound);
            if (withClickSound)
            {
                button.onClick.AddListener(PlayMenuButtonClickSound);
            }
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (gameManager != null)
            {
                gameManager.SetSfxVolume(value);
            }

            if (sfxVolumeValueText != null)
            {
                sfxVolumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
            }
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (gameManager != null)
            {
                gameManager.SetMusicVolume(value);
            }

            if (musicVolumeValueText != null)
            {
                musicVolumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
            }
        }

        private void BindAudioVolumeSliders()
        {
            TryResolveSystems();
            if (gameManager == null)
            {
                return;
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
                sfxVolumeSlider.value = gameManager.GetSfxVolume();
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
                if (sfxVolumeValueText != null)
                {
                    sfxVolumeValueText.text = Mathf.RoundToInt(gameManager.GetSfxVolume() * 100f) + "%";
                }
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
                musicVolumeSlider.value = gameManager.GetMusicVolume();
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
                if (musicVolumeValueText != null)
                {
                    musicVolumeValueText.text = Mathf.RoundToInt(gameManager.GetMusicVolume() * 100f) + "%";
                }
            }
        }

        private void PlayUpgradePurchaseSound()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (gameManager != null)
            {
                gameManager.PlayShopUpgradePurchaseSound();
            }
        }

        private void PlaySkillPurchaseSound()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (gameManager != null)
            {
                gameManager.PlaySkillPurchaseSound();
            }
        }

        private void SetMenuOpen(bool value)
        {
            isOpen = value;
            IsMenuOpen = value;
            if (menuRoot != null)
            {
                menuRoot.SetActive(value);
            }

            SetButtonColor(menuToggleButton, value ? menuOpenButtonColor : actionButtonColor);

            if (pauseGameplayWhenOpen)
            {
                Time.timeScale = value ? 0f : 1f;
            }
        }

        private void SelectTab(bool showShop)
        {
            SelectTab(showShop, false);
        }

        private void SelectTab(bool showShop, bool showControls)
        {
            if (shopTabContent != null)
            {
                shopTabContent.SetActive(showShop);
            }

            if (settingsTabContent != null)
            {
                settingsTabContent.SetActive(!showShop && !showControls);
            }

            if (controlsTabContent != null)
            {
                controlsTabContent.SetActive(showControls);
            }

            SetButtonColor(shopTabButton, showShop ? tabSelectedColor : tabNormalColor);
            SetButtonColor(settingsTabButton, (!showShop && !showControls) ? tabSelectedColor : tabNormalColor);
            SetButtonColor(controlsTabButton, showControls ? tabSelectedColor : tabNormalColor);

            if (showShop)
            {
                SelectShopSection(currentShopSection);
            }
        }

        private void SelectShopSection(ShopSection section)
        {
            currentShopSection = section;
            bool showSkills = section == ShopSection.Skills;
            bool showUpgrade = section == ShopSection.Upgrade;

            if (freezePurchaseButton != null)
            {
                freezePurchaseButton.gameObject.SetActive(showSkills);
            }

            if (megaStrikePurchaseButton != null)
            {
                megaStrikePurchaseButton.gameObject.SetActive(showSkills);
            }

            if (upgradeAutoClickerButton != null)
            {
                upgradeAutoClickerButton.gameObject.SetActive(showUpgrade);
            }

            if (upgradeBaseHpButton != null)
            {
                upgradeBaseHpButton.gameObject.SetActive(showUpgrade);
            }

            if (upgradeTowerLimitButton != null)
            {
                upgradeTowerLimitButton.gameObject.SetActive(showUpgrade);
            }

            if (upgradeClickButton != null)
            {
                upgradeClickButton.gameObject.SetActive(showUpgrade);
            }

            if (shopTitleText != null)
            {
                if (showSkills)
                {
                    shopTitleText.text = "Shop: Skills";
                }
                else
                {
                    shopTitleText.text = "Shop: Upgrade";
                }
            }

            SetButtonColor(shopSkillsSectionButton, showSkills ? tabSelectedColor : tabNormalColor);
            SetButtonColor(shopUpgradeSectionButton, showUpgrade ? tabSelectedColor : tabNormalColor);

            if (showUpgrade)
            {
                UpdateBaseUpgradeButton();
                UpdateTowerLimitUpgradeButton();
                UpdateClickUpgradeButton();
                UpdateAutoClickerUpgradeButton();
            }
        }

        private void RefreshShopStatus(string message)
        {
            if (shopStatusText != null)
            {
                shopStatusText.text = message;
            }
        }

        private void RefreshSkillButton()
        {
            TryResolveSystems();

            if (skillUseButton == null || skillUseButtonText == null)
            {
                return;
            }

            if (skillSystem == null || skillSystem.OwnedSkill == SkillType.None)
            {
                skillUseButton.interactable = false;
                skillUseButtonText.text = "Skill: none";
                SetButtonColor(skillUseButton, actionButtonColor);
                return;
            }

            string skillName = SkillSystem.GetDisplayName(skillSystem.OwnedSkill);
            float cooldown = skillSystem.GetRemainingCooldown();
            if (cooldown > 0f)
            {
                skillUseButton.interactable = false;
                skillUseButtonText.text = skillName + " " + FormatTime(cooldown);
                SetButtonColor(skillUseButton, actionButtonColor);
            }
            else
            {
                skillUseButton.interactable = true;
                skillUseButtonText.text = "Use " + skillName;
                SetButtonColor(skillUseButton, skillReadyButtonColor);
            }
        }

        private void UpdateBaseUpgradeButton()
        {
            if (upgradeBaseHpButton == null)
            {
                return;
            }

            if (upgradeBaseHpButtonText == null)
            {
                upgradeBaseHpButtonText = upgradeBaseHpButton.GetComponentInChildren<Text>(true);
            }

            if (baseHealth == null)
            {
                baseHealth = FindFirstObjectByType<BaseHealth>();
            }

            if (baseHealth == null)
            {
                upgradeBaseHpButton.interactable = false;
                if (upgradeBaseHpButtonText != null)
                {
                    upgradeBaseHpButtonText.text = "Base HP Upgrade\nBase not found";
                }
                return;
            }

            if (baseHealth.IsBaseUpgradeMaxed)
            {
                upgradeBaseHpButton.interactable = false;
                if (upgradeBaseHpButtonText != null)
                {
                    upgradeBaseHpButtonText.text = "Maximum upgrade reached";
                }
                return;
            }

            int level = baseHealth.BaseUpgradeLevel;
            int max = baseHealth.MaxBaseUpgradeLevel;
            int cost = baseHealth.GetNextBaseUpgradeCost();
            upgradeBaseHpButton.interactable = true;
            if (upgradeBaseHpButtonText != null)
            {
                upgradeBaseHpButtonText.text =
                    "Upgrade Base HP (+5)\nCost: " + cost + " (" + level + "/" + max + ")";
            }
        }

        private void UpdateTowerLimitUpgradeButton()
        {
            if (upgradeTowerLimitButton == null)
            {
                return;
            }

            if (upgradeTowerLimitButtonText == null)
            {
                upgradeTowerLimitButtonText = upgradeTowerLimitButton.GetComponentInChildren<Text>(true);
            }

            if (towerPlacementManager == null)
            {
                towerPlacementManager = FindFirstObjectByType<TowerPlacementManager>();
            }

            if (towerPlacementManager == null)
            {
                upgradeTowerLimitButton.interactable = false;
                if (upgradeTowerLimitButtonText != null)
                {
                    upgradeTowerLimitButtonText.text = "Tower Limit Upgrade\nSystem not found";
                }
                return;
            }

            if (towerPlacementManager.IsTowerLimitUpgradeMaxed)
            {
                upgradeTowerLimitButton.interactable = false;
                if (upgradeTowerLimitButtonText != null)
                {
                    upgradeTowerLimitButtonText.text = "Maximum upgrade reached";
                }
                return;
            }

            int level = towerPlacementManager.TowerLimitUpgradeLevel;
            int max = towerPlacementManager.MaxTowerLimitUpgradeLevel;
            int cost = towerPlacementManager.GetNextTowerLimitUpgradeCost();
            upgradeTowerLimitButton.interactable = true;
            if (upgradeTowerLimitButtonText != null)
            {
                upgradeTowerLimitButtonText.text =
                    "Upgrade Tower Limit (+5)\nCost: " + cost + " (" + level + "/" + max + ")";
            }
        }

        private void UpdateClickUpgradeButton()
        {
            if (upgradeClickButton == null)
            {
                return;
            }

            if (upgradeClickButtonText == null)
            {
                upgradeClickButtonText = upgradeClickButton.GetComponentInChildren<Text>(true);
            }

            if (centralTower == null)
            {
                centralTower = FindFirstObjectByType<CentralTower>();
            }

            if (centralTower == null)
            {
                upgradeClickButton.interactable = false;
                if (upgradeClickButtonText != null)
                {
                    upgradeClickButtonText.text = "Click Upgrade\nCentral tower not found";
                }
                return;
            }

            if (centralTower.IsClickUpgradeMaxed)
            {
                upgradeClickButton.interactable = false;
                if (upgradeClickButtonText != null)
                {
                    upgradeClickButtonText.text =
                        "Maximum upgrade reached\nCoins/Click: " + centralTower.CoinsPerClick;
                }
                return;
            }

            int level = centralTower.ClickUpgradeLevel;
            int max = centralTower.MaxClickUpgradeLevel;
            int cost = centralTower.GetNextClickUpgradeCost();
            upgradeClickButton.interactable = true;
            if (upgradeClickButtonText != null)
            {
                upgradeClickButtonText.text =
                    "Upgrade Click (x2)\nCost: " + cost + " (" + level + "/" + max + ")";
            }
        }

        private void UpdateAutoClickerUpgradeButton()
        {
            if (upgradeAutoClickerButton == null)
            {
                return;
            }

            if (upgradeAutoClickerButtonText == null)
            {
                upgradeAutoClickerButtonText = upgradeAutoClickerButton.GetComponentInChildren<Text>(true);
            }

            if (autoClickerSystem == null)
            {
                autoClickerSystem = FindFirstObjectByType<AutoClickerSystem>();
            }

            if (autoClickerSystem == null)
            {
                upgradeAutoClickerButton.interactable = false;
                if (upgradeAutoClickerButtonText != null)
                {
                    upgradeAutoClickerButtonText.text = "AutoClicker Upgrade\nSystem not found";
                }
                return;
            }

            if (autoClickerSystem.IsUpgradeMaxed)
            {
                upgradeAutoClickerButton.interactable = false;
                if (upgradeAutoClickerButtonText != null)
                {
                    upgradeAutoClickerButtonText.text =
                        "Maximum upgrade reached\nIncome: " + autoClickerSystem.IncomePerSecond + "/sec";
                }
                return;
            }

            int level = autoClickerSystem.UpgradeLevel;
            int max = autoClickerSystem.MaxUpgradeLevel;
            int cost = autoClickerSystem.GetNextUpgradeCost();
            upgradeAutoClickerButton.interactable = true;
            if (upgradeAutoClickerButtonText != null)
            {
                upgradeAutoClickerButtonText.text =
                    "Upgrade AutoClicker\nCost: " + cost + " (" + level + "/" + max + ")";
            }
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            int minutes = total / 60;
            int remain = total % 60;
            return minutes.ToString("00") + ":" + remain.ToString("00");
        }

        private Button BuildMenuToggleButton(RectTransform canvasRect)
        {
            RectTransform buttonRect = GetOrCreateRectTransform(canvasRect, "MenuToggleButton");
            SetAnchoredRect(buttonRect, 1f, 1f, 1f, 1f, -60f, -22f, 90f, 34f);

            Image bg = GetOrAdd<Image>(buttonRect.gameObject);
            SetButtonBackground(bg, actionButtonColor, true);

            Button button = GetOrAdd<Button>(buttonRect.gameObject);
            EnsureButtonText(buttonRect, "Menu", TextAnchor.MiddleCenter, 18, Color.black);
            return button;
        }

        private Button BuildSkillUseButton(RectTransform canvasRect, out Text labelText)
        {
            RectTransform buttonRect = GetOrCreateRectTransform(canvasRect, "SkillUseButton");
            SetAnchoredRect(buttonRect, 1f, 0f, 1f, 0f, -110f, 26f, 210f, 44f);

            Image bg = GetOrAdd<Image>(buttonRect.gameObject);
            SetButtonBackground(bg, actionButtonColor, true);

            Button button = GetOrAdd<Button>(buttonRect.gameObject);
            labelText = EnsureButtonText(buttonRect, "Skill: none", TextAnchor.MiddleCenter, 20, Color.black);
            return button;
        }

        private GameObject BuildMenuRoot(RectTransform canvasRect)
        {
            RectTransform rootRect = GetOrCreateRectTransform(canvasRect, "GameMenuRoot");
            SetAnchoredRect(rootRect, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 560f, 500f);

            Image bg = GetOrAdd<Image>(rootRect.gameObject);
            bg.color = panelColor;

            return rootRect.gameObject;
        }

        private RectTransform BuildTabsBar(RectTransform menuRect)
        {
            RectTransform tabsRect = GetOrCreateRectTransform(menuRect, "TabsBar");
            SetTopStretchRect(tabsRect, 12f, 12f, 12f, 40f);

            Image bg = GetOrAdd<Image>(tabsRect.gameObject);
            bg.color = new Color(1f, 1f, 1f, 0.08f);

            return tabsRect;
        }

        private RectTransform BuildContentRoot(RectTransform menuRect)
        {
            RectTransform contentRect = GetOrCreateRectTransform(menuRect, "ContentRoot");
            SetStretchRect(contentRect, 12f, 12f, 58f, 12f);
            return contentRect;
        }

        private Button BuildTabButton(RectTransform tabsBar, string name, string label, float leftOffset)
        {
            RectTransform buttonRect = GetOrCreateRectTransform(tabsBar, name);
            SetTopLeftRect(buttonRect, leftOffset, 4f, 96f, 32f);

            Image bg = GetOrAdd<Image>(buttonRect.gameObject);
            bg.color = tabNormalColor;

            Button button = GetOrAdd<Button>(buttonRect.gameObject);
            EnsureButtonText(buttonRect, label, TextAnchor.MiddleCenter, 18, Color.black);
            return button;
        }

        private GameObject BuildShopContent(
            RectTransform contentRoot,
            out Button skillsSectionButton,
            out Button upgradeSectionButton,
            out Button freezeButton,
            out Button megaStrikeButton,
            out Button autoClickerUpgradeButton,
            out Button upgradeButton,
            out Button towerLimitUpgradeButton,
            out Button clickUpgradeButton,
            out Text statusText)
        {
            RectTransform panelRect = GetOrCreateRectTransform(contentRoot, "ShopTabContent");
            SetStretchRect(panelRect, 0f, 0f, 0f, 0f);

            Image bg = GetOrAdd<Image>(panelRect.gameObject);
            bg.color = new Color(1f, 1f, 1f, 0.06f);

            RectTransform titleRect = GetOrCreateRectTransform(panelRect, "Title");
            SetTopStretchRect(titleRect, 12f, 12f, 10f, 30f);
            shopTitleText = GetOrAdd<Text>(titleRect.gameObject);
            shopTitleText.text = "Shop: Skills";
            shopTitleText.alignment = TextAnchor.MiddleLeft;
            shopTitleText.color = Color.white;
            shopTitleText.font = GetBuiltinFont();
            shopTitleText.fontSize = 24;
            shopTitleText.raycastTarget = false;

            skillsSectionButton = BuildSectionSwitchButton(panelRect, "ShopSectionSkillsButton", "Skills", 12f);
            upgradeSectionButton = BuildSectionSwitchButton(panelRect, "ShopSectionUpgradeButton", "Upgrade", 132f);

            freezeButton = BuildShopActionButton(
                panelRect,
                "BuyFreezeButton",
                "Buy Freeze (2000)\nFreeze enemies + delay spawn 5s",
                88f);

            megaStrikeButton = BuildShopActionButton(
                panelRect,
                "BuyMegaStrikeButton",
                "Buy Mega Strike (10000)\nDamage = 50% current HP to all enemies",
                158f);

            autoClickerUpgradeButton = BuildShopActionButton(
                panelRect,
                "UpgradeAutoClickerButton",
                "Upgrade AutoClicker\nCost: 1000 (0/8)",
                320f);
            upgradeAutoClickerButtonText = autoClickerUpgradeButton.GetComponentInChildren<Text>(true);

            upgradeButton = BuildShopActionButton(
                panelRect,
                "UpgradeBaseHpButton",
                "Upgrade Base HP (+5)\nCost: 100 (0/4)",
                110f);
            upgradeBaseHpButtonText = upgradeButton.GetComponentInChildren<Text>(true);

            towerLimitUpgradeButton = BuildShopActionButton(
                panelRect,
                "UpgradeTowerLimitButton",
                "Upgrade Tower Limit (+5)\nCost: 100 (0/6)",
                180f);
            upgradeTowerLimitButtonText = towerLimitUpgradeButton.GetComponentInChildren<Text>(true);

            clickUpgradeButton = BuildShopActionButton(
                panelRect,
                "UpgradeClickButton",
                "Upgrade Click (x2)\nCost: 1000 (0/4)",
                250f);
            upgradeClickButtonText = clickUpgradeButton.GetComponentInChildren<Text>(true);

            RectTransform statusRect = GetOrCreateRectTransform(panelRect, "StatusText");
            SetBottomStretchRect(statusRect, 12f, 12f, 4f, 42f);
            statusText = GetOrAdd<Text>(statusRect.gameObject);
            statusText.alignment = TextAnchor.UpperLeft;
            statusText.color = Color.white;
            statusText.font = GetBuiltinFont();
            statusText.fontSize = 15;
            statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statusText.verticalOverflow = VerticalWrapMode.Truncate;
            statusText.raycastTarget = false;

            SelectShopSection(ShopSection.Skills);
            return panelRect.gameObject;
        }

        private Button BuildSectionSwitchButton(RectTransform parent, string name, string label, float left)
        {
            RectTransform buttonRect = GetOrCreateRectTransform(parent, name);
            SetTopLeftRect(buttonRect, left, 46f, 110f, 34f);

            Image bg = GetOrAdd<Image>(buttonRect.gameObject);
            bg.color = tabNormalColor;

            Button button = GetOrAdd<Button>(buttonRect.gameObject);
            EnsureButtonText(buttonRect, label, TextAnchor.MiddleCenter, 15, Color.black);
            return button;
        }

        private GameObject BuildSettingsContent(
            RectTransform contentRoot,
            out Button settingsRestartButton,
            out Button settingsGiveUpButton,
            out Button settingsSkipWaveButton,
            out Button settingsBackToStartButton)
        {
            RectTransform panelRect = GetOrCreateRectTransform(contentRoot, "SettingsTabContent");
            SetStretchRect(panelRect, 0f, 0f, 0f, 0f);

            Image bg = GetOrAdd<Image>(panelRect.gameObject);
            bg.color = new Color(1f, 1f, 1f, 0.06f);

            RectTransform titleRect = GetOrCreateRectTransform(panelRect, "Title");
            SetTopStretchRect(titleRect, 12f, 12f, 10f, 30f);
            Text title = GetOrAdd<Text>(titleRect.gameObject);
            title.text = "Settings";
            title.alignment = TextAnchor.MiddleLeft;
            title.color = Color.white;
            title.font = GetBuiltinFont();
            title.fontSize = 24;
            title.raycastTarget = false;

            settingsRestartButton = BuildSettingsButton(panelRect, "RestartFromSettingsButton", "Restart", 70f);
            settingsGiveUpButton = BuildSettingsButton(panelRect, "GiveUpFromSettingsButton", "Give Up", 122f);
            settingsSkipWaveButton = BuildSettingsButton(panelRect, "SkipWaveFromSettingsButton", "Skip Wave", 174f);
            settingsBackToStartButton = BuildSettingsButton(panelRect, "BackToStartFromSettingsButton", "Back To Start", 226f);

            sfxVolumeSlider = BuildSettingsSlider(
                panelRect,
                "SfxVolumeSlider",
                "Effects volume",
                292f,
                out sfxVolumeValueText);
            musicVolumeSlider = BuildSettingsSlider(
                panelRect,
                "MusicVolumeSlider",
                "Music volume",
                358f,
                out musicVolumeValueText);
            return panelRect.gameObject;
        }

        private GameObject BuildControlsContent(RectTransform contentRoot)
        {
            RectTransform panelRect = GetOrCreateRectTransform(contentRoot, "ControlsTabContent");
            SetStretchRect(panelRect, 0f, 0f, 0f, 0f);

            Image bg = GetOrAdd<Image>(panelRect.gameObject);
            bg.color = new Color(1f, 1f, 1f, 0.06f);

            RectTransform titleRect = GetOrCreateRectTransform(panelRect, "Title");
            SetTopStretchRect(titleRect, 12f, 12f, 10f, 30f);
            Text title = GetOrAdd<Text>(titleRect.gameObject);
            title.text = "Controls";
            title.alignment = TextAnchor.MiddleLeft;
            title.color = Color.white;
            title.font = GetBuiltinFont();
            title.fontSize = 24;
            title.raycastTarget = false;

            RectTransform bodyRect = GetOrCreateRectTransform(panelRect, "Body");
            SetTopStretchRect(bodyRect, 12f, 12f, 60f, 90f);
            Text body = GetOrAdd<Text>(bodyRect.gameObject);
            body.text = "LMB: place/upgrade tower\nRMB: remove tower";
            body.alignment = TextAnchor.UpperLeft;
            body.color = Color.white;
            body.font = GetBuiltinFont();
            body.fontSize = 20;
            body.raycastTarget = false;

            return panelRect.gameObject;
        }

        private Button BuildShopActionButton(RectTransform parent, string name, string label, float y)
        {
            RectTransform buttonRect = GetOrCreateRectTransform(parent, name);
            SetTopStretchRect(buttonRect, 12f, 12f, y, 56f);

            Image bg = GetOrAdd<Image>(buttonRect.gameObject);
            SetButtonBackground(bg, actionButtonColor, false);

            Button button = GetOrAdd<Button>(buttonRect.gameObject);
            EnsureButtonText(buttonRect, label, TextAnchor.MiddleLeft, 16, Color.black);
            return button;
        }

        private Button BuildSettingsButton(RectTransform parent, string name, string label, float top)
        {
            RectTransform buttonRect = GetOrCreateRectTransform(parent, name);
            SetTopLeftRect(buttonRect, 12f, top, 180f, 44f);

            Image bg = GetOrAdd<Image>(buttonRect.gameObject);
            SetButtonBackground(bg, actionButtonColor, false);

            Button button = GetOrAdd<Button>(buttonRect.gameObject);
            EnsureButtonText(buttonRect, label, TextAnchor.MiddleCenter, 18, Color.black);
            return button;
        }

        private Slider BuildSettingsSlider(
            RectTransform parent,
            string name,
            string label,
            float top,
            out Text valueText)
        {
            RectTransform rowRect = GetOrCreateRectTransform(parent, name + "Row");
            SetTopStretchRect(rowRect, 12f, 12f, top, 58f);

            RectTransform labelRect = GetOrCreateRectTransform(rowRect, "Label");
            SetTopStretchRect(labelRect, 0f, 70f, 0f, 20f);
            Text labelText = GetOrAdd<Text>(labelRect.gameObject);
            labelText.text = label;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            labelText.font = GetBuiltinFont();
            labelText.fontSize = 15;
            labelText.raycastTarget = false;

            RectTransform valueRect = GetOrCreateRectTransform(rowRect, "Value");
            SetTopRightRect(valueRect, 0f, 0f, 64f, 20f);
            valueText = GetOrAdd<Text>(valueRect.gameObject);
            valueText.text = "100%";
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.color = Color.white;
            valueText.font = GetBuiltinFont();
            valueText.fontSize = 15;
            valueText.raycastTarget = false;

            RectTransform sliderRect = GetOrCreateRectTransform(rowRect, "Slider");
            SetTopStretchRect(sliderRect, 0f, 0f, 26f, 22f);
            Image sliderBackground = GetOrAdd<Image>(sliderRect.gameObject);
            sliderBackground.color = new Color(1f, 1f, 1f, 0.2f);

            RectTransform fillArea = GetOrCreateRectTransform(sliderRect, "Fill Area");
            fillArea.anchorMin = new Vector2(0f, 0f);
            fillArea.anchorMax = new Vector2(1f, 1f);
            fillArea.offsetMin = new Vector2(10f, 5f);
            fillArea.offsetMax = new Vector2(-10f, -5f);

            RectTransform fillRect = GetOrCreateRectTransform(fillArea, "Fill");
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = GetOrAdd<Image>(fillRect.gameObject);
            fillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

            RectTransform handleArea = GetOrCreateRectTransform(sliderRect, "Handle Slide Area");
            handleArea.anchorMin = new Vector2(0f, 0f);
            handleArea.anchorMax = new Vector2(1f, 1f);
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);

            RectTransform handleRect = GetOrCreateRectTransform(handleArea, "Handle");
            handleRect.sizeDelta = new Vector2(16f, 22f);
            Image handleImage = GetOrAdd<Image>(handleRect.gameObject);
            handleImage.color = Color.white;

            Slider slider = GetOrAdd<Slider>(sliderRect.gameObject);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private Text EnsureButtonText(RectTransform parentButton, string textValue, TextAnchor anchor, int fontSize, Color color)
        {
            RectTransform textRect = GetOrCreateRectTransform(parentButton, "Text");
            SetAnchoredRect(textRect, 0f, 0f, 1f, 1f, 0f, 0f, 0f, 0f);

            Text text = GetOrAdd<Text>(textRect.gameObject);
            text.text = textValue;
            text.alignment = anchor;
            text.color = color;
            text.font = GetBuiltinFont();
            text.fontSize = fontSize;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void SetButtonColor(Button button, Color color)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private static void SetButtonBackground(Image image, Color color, bool rounded)
        {
            if (image == null)
            {
                return;
            }

            image.color = color;
            if (!rounded)
            {
                image.type = Image.Type.Simple;
                return;
            }

            Sprite sprite = GetRoundedButtonSprite();
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }
        }

        private static Sprite GetRoundedButtonSprite()
        {
            if (roundedButtonSprite != null)
            {
                return roundedButtonSprite;
            }

            roundedButtonSprite = CreateRoundedRectSprite(64, 10);
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

        private static RectTransform GetOrCreateRectTransform(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child as RectTransform;
            }

            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.transform as RectTransform;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }

            return component;
        }

        private Font GetBuiltinFont()
        {
            if (uiFont != null)
            {
                return uiFont;
            }

#if UNITY_EDITOR
            uiFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/FPSFont/FPS Gaming Font/Square-Black.ttf");
            if (uiFont != null)
            {
                return uiFont;
            }
#endif

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }

        private static void SetAnchoredRect(
            RectTransform rect,
            float anchorMinX,
            float anchorMinY,
            float anchorMaxX,
            float anchorMaxY,
            float anchoredX,
            float anchoredY,
            float sizeDeltaX,
            float sizeDeltaY)
        {
            rect.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rect.anchoredPosition = new Vector2(anchoredX, anchoredY);
            rect.sizeDelta = new Vector2(sizeDeltaX, sizeDeltaY);
            rect.localScale = Vector3.one;
        }

        private static void SetStretchRect(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.localScale = Vector3.one;
        }

        private static void SetTopStretchRect(RectTransform rect, float left, float right, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(-(left + right), height);
            rect.anchoredPosition = new Vector2(0f, -(top + (height * 0.5f)));
            rect.localScale = Vector3.one;
        }

        private static void SetBottomStretchRect(RectTransform rect, float left, float right, float bottom, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(-(left + right), height);
            rect.anchoredPosition = new Vector2(0f, bottom + (height * 0.5f));
            rect.localScale = Vector3.one;
        }

        private static void SetTopLeftRect(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(left + (width * 0.5f), -(top + (height * 0.5f)));
            rect.localScale = Vector3.one;
        }

        private static void SetTopRightRect(RectTransform rect, float right, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(-(right + (width * 0.5f)), -(top + (height * 0.5f)));
            rect.localScale = Vector3.one;
        }
    }
}
