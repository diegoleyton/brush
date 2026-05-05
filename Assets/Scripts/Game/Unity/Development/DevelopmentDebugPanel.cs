using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.ScreenBlocker;
using Flowbit.Utilities.Storage;
using Flowbit.Utilities.Unity.RemoteCommunication;
using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Game.Unity.Development
{
    /// <summary>
    /// Editor/development-only debug overlay for mutating the active profile while iterating quickly in any scene.
    /// The UI is generated in code so it can be dropped in automatically without scene wiring.
    /// </summary>
    public sealed class DevelopmentDebugPanel : MonoBehaviour
    {
        private sealed class ItemGrantState
        {
            public string ItemId = "1";
            public string Quantity = "1";
        }

        private readonly Dictionary<InteractionPointType, ItemGrantState> itemStates_ =
            new Dictionary<InteractionPointType, ItemGrantState>();

        private DataRepository repository_;
        private IRoomGameplayService roomGameplayService_;
        private EventDispatcher dispatcher_;
        private IChildrenApiClient childrenApiClient_;
        private IChildGameStateSyncService childGameStateSyncService_;
        private ScreenBlocker screenBlocker_;
        private DevelopmentRemoteSimulationSettings remoteSimulationSettings_;
        private IAuthService authService_;
        private IDataStorage dataStorage_;

        private bool isOpen_;
        private Rect windowRect_ = new Rect(24f, 96f, 560f, 820f);
        private string coinsInput_ = "100";
        private string feedback_ = string.Empty;
        private Vector2 scrollPosition_;
        private bool hasLoggedCreation_;
        private bool hasLoggedRenderBlocked_;
        private bool hasLoggedRenderReady_;
        private GUIStyle windowStyle_;
        private GUIStyle buttonStyle_;
        private GUIStyle labelStyle_;
        private GUIStyle textFieldStyle_;

        [Inject]
        public void Construct(
            DataRepository repository,
            IRoomGameplayService roomGameplayService,
            EventDispatcher dispatcher,
            IChildrenApiClient childrenApiClient,
            IChildGameStateSyncService childGameStateSyncService,
            ScreenBlocker screenBlocker,
            DevelopmentRemoteSimulationSettings remoteSimulationSettings,
            IAuthService authService,
            IDataStorage dataStorage)
        {
            repository_ = repository;
            roomGameplayService_ = roomGameplayService;
            dispatcher_ = dispatcher;
            childrenApiClient_ = childrenApiClient;
            childGameStateSyncService_ = childGameStateSyncService;
            screenBlocker_ = screenBlocker;
            remoteSimulationSettings_ = remoteSimulationSettings;
            authService_ = authService;
            dataStorage_ = dataStorage;
        }

        private void Awake()
        {
            name = nameof(DevelopmentDebugPanel);
            hideFlags = HideFlags.DontSave;

            itemStates_[InteractionPointType.PLACEABLE_OBJECT] = new ItemGrantState();
            itemStates_[InteractionPointType.FOOD] = new ItemGrantState();
            itemStates_[InteractionPointType.PAINT] = new ItemGrantState();
            itemStates_[InteractionPointType.SKIN] = new ItemGrantState();
            itemStates_[InteractionPointType.HAT] = new ItemGrantState();
            itemStates_[InteractionPointType.DRESS] = new ItemGrantState { ItemId = "0" };
            itemStates_[InteractionPointType.EYES] = new ItemGrantState();

            if (!hasLoggedCreation_)
            {
                hasLoggedCreation_ = true;
                Debug.Log(
                    $"[DebugPanel] Created. Scene={SceneManager.GetActiveScene().name}, " +
                    $"Editor={Application.isEditor}, DebugBuild={Debug.isDebugBuild}, Screen={Screen.width}x{Screen.height}");
            }
        }

        private void OnEnable()
        {
            dispatcher_?.Subscribe<CurrencyUpdatedEvent>(OnDebugStateChanged);
            dispatcher_?.Subscribe<InventoryUpdatedEvent>(OnDebugStateChanged);
            dispatcher_?.Subscribe<PendingRewardEvent>(OnDebugStateChanged);
            dispatcher_?.Subscribe<ProfileSwitchedEvent>(OnDebugStateChanged);
            dispatcher_?.Subscribe<ProfileUpdatedEvent>(OnDebugStateChanged);
        }

        private void OnDisable()
        {
            dispatcher_?.Unsubscribe<CurrencyUpdatedEvent>(OnDebugStateChanged);
            dispatcher_?.Unsubscribe<InventoryUpdatedEvent>(OnDebugStateChanged);
            dispatcher_?.Unsubscribe<PendingRewardEvent>(OnDebugStateChanged);
            dispatcher_?.Unsubscribe<ProfileSwitchedEvent>(OnDebugStateChanged);
            dispatcher_?.Unsubscribe<ProfileUpdatedEvent>(OnDebugStateChanged);
        }

        private void OnGUI()
        {
            if (!ShouldRender())
            {
                return;
            }

            EnsureGuiStyles();

            if (!hasLoggedRenderReady_)
            {
                hasLoggedRenderReady_ = true;
                Debug.Log(
                    $"[DebugPanel] Rendering toggle. Scene={SceneManager.GetActiveScene().name}, Screen={Screen.width}x{Screen.height}");
            }

            DrawFloatingToggle();

            if (!isOpen_)
            {
                return;
            }

            windowRect_ = GUI.Window(
                GetInstanceID(),
                windowRect_,
                DrawWindow,
                "Debug Panel",
                windowStyle_);
        }

        private void DrawFloatingToggle()
        {
            float buttonWidth = 190f;
            Rect buttonRect = new Rect(
                Mathf.Max(20f, Screen.width - buttonWidth - 20f),
                20f,
                buttonWidth,
                58f);
            if (GUI.Button(buttonRect, isOpen_ ? "Hide Debug" : "Debug", buttonStyle_))
            {
                isOpen_ = !isOpen_;
                feedback_ = string.Empty;
            }
        }

        private void DrawWindow(int _)
        {
            GUI.skin.label = labelStyle_;
            GUI.skin.button = buttonStyle_;
            GUI.skin.textField = textFieldStyle_;

            GUILayout.BeginVertical();

            DrawCurrentProfileSummary();

            scrollPosition_ = GUILayout.BeginScrollView(scrollPosition_, GUILayout.Height(660f));

            DrawCoinsSection();
            DrawRemoteSimulationSection();
            DrawAuthSimulationSection();
            DrawPendingRewardSection();
            DrawInventorySection(InteractionPointType.PLACEABLE_OBJECT, "Placeable Objects", roomGameplayService_.AddPlaceableObject);
            DrawInventorySection(InteractionPointType.FOOD, "Food", roomGameplayService_.AddFood);
            DrawInventorySection(InteractionPointType.PAINT, "Paint", roomGameplayService_.AddPaint);
            DrawInventorySection(InteractionPointType.SKIN, "Skin", roomGameplayService_.AddSkin);
            DrawInventorySection(InteractionPointType.HAT, "Hat", roomGameplayService_.AddHat);
            DrawInventorySection(InteractionPointType.DRESS, "Dress", roomGameplayService_.AddDress);
            DrawInventorySection(InteractionPointType.EYES, "Eyes", roomGameplayService_.AddEyes);
            DrawResetSection();

            GUILayout.EndScrollView();

            if (!string.IsNullOrWhiteSpace(feedback_))
            {
                GUILayout.Label(feedback_);
            }

            if (GUILayout.Button("Close"))
            {
                isOpen_ = false;
                feedback_ = string.Empty;
            }

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void DrawCurrentProfileSummary()
        {
            Profile currentProfile = repository_?.CurrentProfile;
            if (currentProfile == null)
            {
                GUILayout.Label("No current profile");
                return;
            }

            GUILayout.Label($"Profile: {currentProfile.Name}");
            GUILayout.Label($"Pet: {currentProfile.PetData?.Name ?? string.Empty}");
            GUILayout.Label($"Coins: {currentProfile.Coins}");
            GUILayout.Label($"Pending rewards: {currentProfile.PendingRewardCount}");
            GUILayout.Space(6f);
        }

        private void DrawCoinsSection()
        {
            GUILayout.Label("Coins");
            GUILayout.BeginHorizontal();
            coinsInput_ = GUILayout.TextField(coinsInput_, GUILayout.Width(130f), GUILayout.Height(40f));
            if (GUILayout.Button("Add Coins"))
            {
                if (TryReadPositiveInt(coinsInput_, "coins", out int amount))
                {
                    _ = AddCoinsAsync(amount);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);
        }

        private void DrawRemoteSimulationSection()
        {
            GUILayout.Label("Network Simulation");

            bool simulateSlowNetwork = remoteSimulationSettings_ != null && remoteSimulationSettings_.SimulateSlowNetwork;
            bool newSimulateSlowNetwork = GUILayout.Toggle(simulateSlowNetwork, "Slow Network");
            if (remoteSimulationSettings_ != null && newSimulateSlowNetwork != simulateSlowNetwork)
            {
                remoteSimulationSettings_.SimulateSlowNetwork = newSimulateSlowNetwork;
                feedback_ = newSimulateSlowNetwork ? "Slow network enabled." : "Slow network disabled.";
            }

            bool simulateNetworkFailure =
                remoteSimulationSettings_ != null && remoteSimulationSettings_.SimulateNetworkFailure;
            bool newSimulateNetworkFailure = GUILayout.Toggle(simulateNetworkFailure, "Network Failure");
            if (remoteSimulationSettings_ != null &&
                newSimulateNetworkFailure != simulateNetworkFailure)
            {
                remoteSimulationSettings_.SimulateNetworkFailure = newSimulateNetworkFailure;
                feedback_ = newSimulateNetworkFailure
                    ? "Network failure enabled."
                    : "Network failure disabled.";
            }

            bool simulateUnresponsiveNetwork =
                remoteSimulationSettings_ != null && remoteSimulationSettings_.SimulateUnresponsiveNetwork;
            bool newSimulateUnresponsiveNetwork = GUILayout.Toggle(simulateUnresponsiveNetwork, "Unresponsive Network");
            if (remoteSimulationSettings_ != null &&
                newSimulateUnresponsiveNetwork != simulateUnresponsiveNetwork)
            {
                remoteSimulationSettings_.SimulateUnresponsiveNetwork = newSimulateUnresponsiveNetwork;
                feedback_ = newSimulateUnresponsiveNetwork
                    ? "Unresponsive network enabled."
                    : "Unresponsive network disabled.";
            }

            bool simulateUnauthorizedSession =
                remoteSimulationSettings_ != null && remoteSimulationSettings_.SimulateUnauthorizedSession;
            bool newSimulateUnauthorizedSession = GUILayout.Toggle(simulateUnauthorizedSession, "Unauthorized Session");
            if (remoteSimulationSettings_ != null &&
                newSimulateUnauthorizedSession != simulateUnauthorizedSession)
            {
                remoteSimulationSettings_.SimulateUnauthorizedSession = newSimulateUnauthorizedSession;
                feedback_ = newSimulateUnauthorizedSession
                    ? "Unauthorized session simulation enabled."
                    : "Unauthorized session simulation disabled.";
            }

            GUILayout.Space(8f);
        }

        private void DrawAuthSimulationSection()
        {
            GUILayout.Label("Auth Simulation");

            if (GUILayout.Button("Expire Access Token"))
            {
                _ = ExpireAccessTokenAsync();
            }

            GUILayout.Space(8f);
        }

        private async Task AddCoinsAsync(int amount)
        {
            Profile currentProfile = repository_?.CurrentProfile;
            if (currentProfile == null || string.IsNullOrWhiteSpace(currentProfile.RemoteProfileId))
            {
                feedback_ = "No current profile.";
                return;
            }

            if (childrenApiClient_ == null)
            {
                feedback_ = "Children API unavailable.";
                return;
            }

            IDisposable blockScope = screenBlocker_?.BlockScope(
                "DebugGrantCoins",
                showLoadingWithTime: true,
                loadingMessage: "Granting coins...");
            try
            {
                bool granted = await childrenApiClient_.GrantCoinsAsync(
                    currentProfile.RemoteProfileId,
                    amount,
                    "Debug panel grant");

                if (!granted)
                {
                    feedback_ = "Failed to grant coins.";
                    return;
                }
            }
            catch (AuthSessionInvalidatedException)
            {
                return;
            }
            finally
            {
                blockScope?.Dispose();
            }

            if (childGameStateSyncService_ != null)
            {
                await childGameStateSyncService_.ReloadCurrentProfileAsync();
            }

            feedback_ = $"+{amount} coins";
        }

        private async Task ExpireAccessTokenAsync()
        {
            AuthSession currentSession = authService_?.CurrentSession;
            if (currentSession == null || !currentSession.HasRefreshToken)
            {
                feedback_ = "No refreshable auth session.";
                return;
            }

            currentSession.ExpiresAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 60;

            if (dataStorage_ != null)
            {
                await dataStorage_.SaveAsync(AuthService.SessionStorageKey, currentSession);
            }

            feedback_ = "Access token expired locally. The next protected request should refresh the session automatically.";
        }

        private void DrawPendingRewardSection()
        {
            GUILayout.Label("Pending Reward");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1"))
            {
                int nextCount = Math.Max(0, (repository_?.CurrentProfile?.PendingRewardCount ?? 0) + 1);
                repository_?.SetPendingRewardCount(nextCount);
                feedback_ = $"Pending rewards: {nextCount}.";
            }

            if (GUILayout.Button("Clear"))
            {
                repository_?.SetPendingRewardCount(0);
                feedback_ = "Pending rewards cleared.";
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);
        }

        private void DrawInventorySection(
            InteractionPointType interactionPointType,
            string label,
            Action<int, int> addAction)
        {
            if (!itemStates_.TryGetValue(interactionPointType, out ItemGrantState state))
            {
                return;
            }

            GUILayout.Label(label);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Item", GUILayout.Width(48f));
            state.ItemId = GUILayout.TextField(state.ItemId, GUILayout.Width(84f), GUILayout.Height(40f));
            GUILayout.Label("Qty", GUILayout.Width(40f));
            state.Quantity = GUILayout.TextField(state.Quantity, GUILayout.Width(84f), GUILayout.Height(40f));

            if (GUILayout.Button("Add"))
            {
                TryAddInventoryItem(interactionPointType, label, state, addAction);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        private void DrawResetSection()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Misc");
            if (GUILayout.Button("Reset Pet Times"))
            {
                roomGameplayService_?.ResetPetTimes();
                feedback_ = "Pet times reset.";
            }
        }

        private void TryAddInventoryItem(
            InteractionPointType interactionPointType,
            string label,
            ItemGrantState state,
            Action<int, int> addAction)
        {
            if (repository_?.CurrentProfile == null)
            {
                feedback_ = "No current profile.";
                return;
            }

            if (addAction == null)
            {
                feedback_ = $"{label}: action unavailable.";
                return;
            }

            if (!TryReadNonNegativeInt(state.ItemId, $"{label} item id", out int itemId))
            {
                return;
            }

            if (!TryReadPositiveInt(state.Quantity, $"{label} quantity", out int quantity))
            {
                return;
            }

            if (!ItemCatalog.IsValidItemId(interactionPointType, itemId))
            {
                feedback_ = $"{label}: invalid item id {itemId}.";
                return;
            }

            addAction(itemId, quantity);
            feedback_ = $"+{quantity} {label} item {itemId}";
        }

        private bool TryReadPositiveInt(string rawValue, string label, out int value)
        {
            value = 0;
            if (!int.TryParse(rawValue?.Trim(), out value) || value <= 0)
            {
                feedback_ = $"{label} must be a positive integer.";
                return false;
            }

            return true;
        }

        private bool TryReadNonNegativeInt(string rawValue, string label, out int value)
        {
            value = 0;
            if (!int.TryParse(rawValue?.Trim(), out value) || value < 0)
            {
                feedback_ = $"{label} must be a non-negative integer.";
                return false;
            }

            return true;
        }

        private void OnDebugStateChanged(CurrencyUpdatedEvent _) => RepaintIfOpen();
        private void OnDebugStateChanged(InventoryUpdatedEvent _) => RepaintIfOpen();
        private void OnDebugStateChanged(PendingRewardEvent _) => RepaintIfOpen();
        private void OnDebugStateChanged(ProfileSwitchedEvent _) => RepaintIfOpen();
        private void OnDebugStateChanged(ProfileUpdatedEvent _) => RepaintIfOpen();

        private void RepaintIfOpen()
        {
            if (isOpen_)
            {
                feedback_ = string.Empty;
            }
        }

        private static bool ShouldEnableForRuntime()
        {
            if (Application.isEditor)
            {
                return true;
            }

            return Debug.isDebugBuild;
        }

        private bool ShouldRender()
        {
            if (!ShouldEnableForRuntime())
            {
                LogRenderBlocked("Runtime gate blocked rendering.");
                return false;
            }

            return true;
        }

        private void LogRenderBlocked(string reason)
        {
            if (hasLoggedRenderBlocked_)
            {
                return;
            }

            hasLoggedRenderBlocked_ = true;
            Debug.Log($"[DebugPanel] {reason}");
        }

        private void EnsureGuiStyles()
        {
            if (windowStyle_ != null &&
                buttonStyle_ != null &&
                labelStyle_ != null &&
                textFieldStyle_ != null)
            {
                return;
            }

            windowStyle_ = new GUIStyle(GUI.skin.window)
            {
                fontSize = 24,
                padding = new RectOffset(18, 18, 30, 18)
            };

            buttonStyle_ = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fixedHeight = 46f
            };

            labelStyle_ = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                wordWrap = true
            };

            textFieldStyle_ = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 19,
                fixedHeight = 40f
            };
        }
    }
}
