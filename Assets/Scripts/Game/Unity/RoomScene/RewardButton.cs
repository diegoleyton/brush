using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Encapsulates the reward entry point shown from the room when rewards are pending.
    /// </summary>
    public sealed class RewardButton : MonoBehaviour
    {
        [SerializeField]
        private GameObject root_;

        [SerializeField]
        private Button button_;

        [SerializeField]
        private Text countText_;

        private Action clickAction_;

        private void Awake()
        {
            ValidateSerializedReferences();
        }

        private void OnDestroy()
        {
            if (button_ != null && clickAction_ != null)
            {
                button_.onClick.RemoveListener(OnClicked);
            }
        }

        public void Initialize(Action onClick)
        {
            ValidateSerializedReferences();

            if (button_ != null && clickAction_ != null)
            {
                button_.onClick.RemoveListener(OnClicked);
            }

            clickAction_ = onClick;
            button_.onClick.AddListener(OnClicked);
        }

        public void SetPendingRewardCount(int pendingRewardCount)
        {
            int sanitizedCount = Math.Max(0, pendingRewardCount);
            root_.SetActive(sanitizedCount > 0);
            countText_.text = sanitizedCount.ToString();
        }

        private void OnClicked()
        {
            clickAction_?.Invoke();
        }

        private void ValidateSerializedReferences()
        {
            if (root_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RewardButton)} requires a root GameObject reference.");
            }

            if (button_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RewardButton)} requires a Button reference.");
            }

            if (countText_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RewardButton)} requires a count Text reference.");
            }
        }
    }
}
