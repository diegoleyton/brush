using System;
using Flowbit.Utilities.Localization;
using Flowbit.Utilities.ScreenBlocker;
using Game.Core.Services;
using Flowbit.Utilities.Unity.UI;
using Game.Unity.UI;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.Unity.Auth
{
    /// <summary>
    /// Reusable auth view for account creation and login.
    /// Navigation stays outside this component so it can be reused in any auth scene or prefab.
    /// </summary>
    public sealed class AuthPanel : MonoBehaviour
    {
        public event Action<AuthResult> AuthSucceeded;

        [Header("Inputs")]
        [SerializeField]
        private InputField emailInput_;

        [SerializeField]
        private InputField passwordInput_;

        [Header("Status")]
        [SerializeField]
        private Text sessionStatusText_;

        [SerializeField]
        private FeedbackMessage feedbackMessage_;

        [Header("Buttons")]
        [SerializeField]
        private Button createAccountButton_;

        [SerializeField]
        private Button loginButton_;

        [Header("Defaults")]
        [SerializeField]
        private string defaultFamilyName_ = "Mi familia";

        private IAuthService authService_;
        private ScreenBlocker screenBlocker_;

        private bool isBusy_;

        [Inject]
        public void Construct(IAuthService authService, ScreenBlocker screenBlocker)
        {
            authService_ = authService;
            screenBlocker_ = screenBlocker;
        }

        private void Awake()
        {
            ValidateSerializedReferences();
            WireButtons();
            RefreshView();
        }

        private void OnEnable()
        {
            RefreshView();
        }

        private void OnDestroy()
        {
            UnwireButtons();
        }

        public async void CreateAccount()
        {
            if (authService_ == null || isBusy_)
            {
                return;
            }

            isBusy_ = true;
            SetFeedback(string.Empty);
            RefreshButtonState();

            IDisposable blockScope = screenBlocker_?.BlockScope(
                "CreateAccount",
                showLoadingWithTime: true,
                loadingMessage: LocalizationServiceLocator.GetText(
                    "loading.auth.create_account",
                    "Creating account..."));
            try
            {
                AuthResult result = await authService_.CreateAccountAsync(
                    ReadText(emailInput_),
                    ReadText(passwordInput_),
                    ResolveFamilyName());

                HandleCreateAccountResult(result);
            }
            catch (Exception exception)
            {
                SetFeedback(RemoteOperationMessageFormatter.Format(
                    exception,
                    LocalizationServiceLocator.GetText("auth.create_account.error", "Could not create account.")));
            }
            finally
            {
                blockScope?.Dispose();
                isBusy_ = false;
                RefreshView();
            }
        }

        public async void Login()
        {
            if (authService_ == null || isBusy_)
            {
                return;
            }

            isBusy_ = true;
            SetFeedback(string.Empty);
            RefreshButtonState();

            IDisposable blockScope = screenBlocker_?.BlockScope(
                "Login",
                showLoadingWithTime: true,
                loadingMessage: LocalizationServiceLocator.GetText(
                    "loading.auth.login",
                    "Signing in..."));
            try
            {
                AuthResult result = await authService_.LoginAsync(
                    ReadText(emailInput_),
                    ReadText(passwordInput_));

                HandleLoginResult(result);
            }
            catch (Exception exception)
            {
                SetFeedback(RemoteOperationMessageFormatter.Format(
                    exception,
                    LocalizationServiceLocator.GetText("auth.login.error", "Could not sign in.")));
            }
            finally
            {
                blockScope?.Dispose();
                isBusy_ = false;
                RefreshView();
            }
        }

        private void HandleCreateAccountResult(AuthResult result)
        {
            if (result == null)
            {
                SetFeedback(LocalizationServiceLocator.GetText("auth.result.none", "No auth result was returned."));
                return;
            }

            if (!result.IsSuccess)
            {
                SetFeedback(result.ErrorMessage ?? LocalizationServiceLocator.GetText("auth.error.generic", "Authentication failed."));
                return;
            }

            ShowSuccess(LocalizationServiceLocator.GetText("auth.create_account.success", "Account created."));
            AuthSucceeded?.Invoke(result);
        }

        private void HandleLoginResult(AuthResult result)
        {
            if (result == null)
            {
                SetFeedback(LocalizationServiceLocator.GetText("auth.result.none", "No auth result was returned."));
                return;
            }

            if (!result.IsSuccess)
            {
                SetFeedback(result.ErrorMessage ?? LocalizationServiceLocator.GetText("auth.error.generic", "Authentication failed."));
                return;
            }

            ShowSuccess(LocalizationServiceLocator.GetText("auth.login.success", "Login successful."));
            AuthSucceeded?.Invoke(result);
        }

        private void RefreshView()
        {
            if (sessionStatusText_ != null)
            {
                sessionStatusText_.text = authService_ != null && authService_.HasSession
                    ? string.Format(
                        LocalizationServiceLocator.GetText("auth.status.authenticated_as", "Authenticated as {0}"),
                        authService_.CurrentSession?.Email)
                    : LocalizationServiceLocator.GetText("auth.status.no_session", "No active session");
            }

            RefreshButtonState();
        }

        private void RefreshButtonState()
        {
            bool enableAuthActions = !isBusy_;

            SetButtonState(createAccountButton_, enableAuthActions);
            SetButtonState(loginButton_, enableAuthActions);
        }

        private void SetFeedback(string message)
        {
            feedbackMessage_?.ShowError(message);
        }

        private void ShowSuccess(string message)
        {
            feedbackMessage_?.ShowSuccess(message, durationSeconds: 2f);
        }

        private void WireButtons()
        {
            BindButton(createAccountButton_, CreateAccount);
            BindButton(loginButton_, Login);
        }

        private void UnwireButtons()
        {
            UnbindButton(createAccountButton_, CreateAccount);
            UnbindButton(loginButton_, Login);
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void UnbindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        private static void SetButtonState(Button button, bool enabled)
        {
            if (button != null)
            {
                button.interactable = enabled;
            }
        }

        private static string ReadText(InputField inputField) =>
            inputField != null ? inputField.text.Trim() : string.Empty;

        private void ValidateSerializedReferences()
        {
            if (emailInput_ == null ||
                passwordInput_ == null ||
                sessionStatusText_ == null ||
                feedbackMessage_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(AuthPanel)} is missing one or more required serialized references.");
            }
        }

        private string ResolveFamilyName()
        {
            if (!string.IsNullOrWhiteSpace(defaultFamilyName_))
            {
                return defaultFamilyName_.Trim();
            }

            return "Mi familia";
        }
    }
}
