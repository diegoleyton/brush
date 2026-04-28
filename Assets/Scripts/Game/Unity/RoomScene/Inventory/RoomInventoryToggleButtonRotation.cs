using System.Collections;

using Flowbit.Utilities.Core.Events;

using Game.Unity.Definitions.Events;

using UnityEngine;
using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Animates the inventory toggle button rotation to reflect whether the inventory is open.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class RoomInventoryToggleButtonRotation : MonoBehaviour
    {
        private const float ClosedZRotation = 0f;
        private const float OpenZRotation = 180f;

        private EventDispatcher dispatcher_;
        private RectTransform rectTransform_;
        private Coroutine rotationCoroutine_;
        private bool subscribed_;

        [SerializeField]
        [Min(0f)]
        private float rotationDurationSeconds_ = 0.2f;

        [Inject]
        public void Construct(EventDispatcher dispatcher)
        {
            dispatcher_ = dispatcher;
        }

        private void Awake()
        {
            rectTransform_ = GetComponent<RectTransform>();
            SetRotationImmediate(ClosedZRotation);
            SubscribeToDispatcher();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDispatcher();
        }

        private void OnRoomInventoryOpened(RoomInventoryOpenedEvent _)
        {
            AnimateTo(OpenZRotation);
        }

        private void OnRoomInventoryClosed(RoomInventoryClosedEvent _)
        {
            AnimateTo(ClosedZRotation);
        }

        private void AnimateTo(float targetZRotation)
        {
            if (rectTransform_ == null)
            {
                return;
            }

            if (rotationCoroutine_ != null)
            {
                StopCoroutine(rotationCoroutine_);
            }

            if (rotationDurationSeconds_ <= 0f)
            {
                SetRotationImmediate(targetZRotation);
                rotationCoroutine_ = null;
                return;
            }

            rotationCoroutine_ = StartCoroutine(AnimateRotation(targetZRotation));
        }

        private IEnumerator AnimateRotation(float targetZRotation)
        {
            float elapsed = 0f;
            float startZRotation = NormalizeZRotation(rectTransform_.localEulerAngles.z);
            float shortestDelta = Mathf.DeltaAngle(startZRotation, targetZRotation);

            while (elapsed < rotationDurationSeconds_)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / rotationDurationSeconds_);
                float currentZRotation = startZRotation + shortestDelta * t;
                SetRotationImmediate(currentZRotation);
                yield return null;
            }

            SetRotationImmediate(targetZRotation);
            rotationCoroutine_ = null;
        }

        private void SetRotationImmediate(float zRotation)
        {
            if (rectTransform_ == null)
            {
                return;
            }

            rectTransform_.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        }

        private void SubscribeToDispatcher()
        {
            if (subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Subscribe<RoomInventoryOpenedEvent>(OnRoomInventoryOpened);
            dispatcher_.Subscribe<RoomInventoryClosedEvent>(OnRoomInventoryClosed);
            subscribed_ = true;
        }

        private void UnsubscribeFromDispatcher()
        {
            if (!subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<RoomInventoryOpenedEvent>(OnRoomInventoryOpened);
            dispatcher_.Unsubscribe<RoomInventoryClosedEvent>(OnRoomInventoryClosed);
            subscribed_ = false;
        }

        private static float NormalizeZRotation(float zRotation)
        {
            if (zRotation > 180f)
            {
                return zRotation - 360f;
            }

            return zRotation;
        }
    }
}
