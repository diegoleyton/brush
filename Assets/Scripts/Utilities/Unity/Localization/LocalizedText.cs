using Flowbit.Utilities.Localization;

using UnityEngine;
using UnityEngine.UI;

namespace Flowbit.Utilities.Unity.Localization
{
    /// <summary>
    /// Applies localized text to a Unity UI Text component.
    /// </summary>
    [ExecuteAlways]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField]
        private Text targetText_;

        [SerializeField]
        private LocalizedTextReference textReference_ = new LocalizedTextReference();

        private void Reset()
        {
            if (targetText_ == null)
            {
                targetText_ = GetComponent<Text>();
            }
        }

        private void Awake()
        {
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            if (targetText_ == null)
            {
                targetText_ = GetComponent<Text>();
            }

            Apply();
        }

        [ContextMenu("Refresh Localized Text")]
        public void Apply()
        {
            if (targetText_ == null)
            {
                return;
            }

            targetText_.text = LocalizationServiceLocator.GetText(textReference_);
        }

        public void SetReference(LocalizedTextReference textReference)
        {
            textReference_ = textReference ?? new LocalizedTextReference();
            Apply();
        }
    }
}
