using System;
using System.Collections;

using Flowbit.Utilities.Core.Events;

using Game.Unity.Definitions.Events;

using UnityEngine;
using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Hides its GameObject while a room inventory drag is active.
    /// </summary>
    public sealed class HideOnInventoryDrag : MonoBehaviour
    {
        [SerializeField]
        private Vector3 deltaHidePosition_;

        [SerializeField]
        private bool animated_;

        [SerializeField]
        [Min(0f)]
        private float duration_ = 0.2f;

        private EventDispatcher dispatcher_;
        private Transform cachedTransform_;
        private Coroutine animationCoroutine_;
        private bool subscribed_;
        private bool wasActiveBeforeDrag_;
        private Vector3 shownLocalPosition_;
        private Vector3 hiddenLocalPosition_;
        private bool positionsInitialized_;
        private bool isHiddenByDrag_;

        [Inject]
        public void Construct(EventDispatcher dispatcher)
        {
            dispatcher_ = dispatcher;
        }

        private void Awake()
        {
            cachedTransform_ = transform;
            wasActiveBeforeDrag_ = gameObject.activeSelf;
            InitializePositions();
            SubscribeToDispatcher();
        }

        private void OnEnable()
        {
            if (animated_ && !isHiddenByDrag_)
            {
                InitializePositions();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromDispatcher();
        }

        private void OnRoomInventoryDragStarted(RoomInventoryDragStartedEvent _)
        {
            wasActiveBeforeDrag_ = gameObject.activeSelf;

            if (!animated_)
            {
                gameObject.SetActive(false);
                return;
            }

            if (!wasActiveBeforeDrag_)
            {
                return;
            }

            InitializePositions();
            isHiddenByDrag_ = true;
            StartAnimatedTransition(hiddenLocalPosition_);
        }

        private void OnRoomInventoryDragEnded(RoomInventoryDragEndedEvent _)
        {
            if (!wasActiveBeforeDrag_)
            {
                return;
            }

            if (!animated_)
            {
                gameObject.SetActive(true);
                return;
            }

            gameObject.SetActive(true);
            isHiddenByDrag_ = false;
            StartAnimatedTransition(shownLocalPosition_);
        }

        private void SubscribeToDispatcher()
        {
            if (subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Subscribe<RoomInventoryDragStartedEvent>(OnRoomInventoryDragStarted);
            dispatcher_.Subscribe<RoomInventoryDragEndedEvent>(OnRoomInventoryDragEnded);
            subscribed_ = true;
        }

        private void UnsubscribeFromDispatcher()
        {
            if (!subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<RoomInventoryDragStartedEvent>(OnRoomInventoryDragStarted);
            dispatcher_.Unsubscribe<RoomInventoryDragEndedEvent>(OnRoomInventoryDragEnded);
            subscribed_ = false;
        }

        private void InitializePositions()
        {
            if (cachedTransform_ == null)
            {
                cachedTransform_ = transform;
            }

            shownLocalPosition_ = cachedTransform_.localPosition;
            hiddenLocalPosition_ = shownLocalPosition_ + deltaHidePosition_;
            positionsInitialized_ = true;
        }

        private void StartAnimatedTransition(Vector3 targetLocalPosition)
        {
            if (!positionsInitialized_)
            {
                InitializePositions();
            }

            if (animationCoroutine_ != null)
            {
                StopCoroutine(animationCoroutine_);
            }

            if (duration_ <= 0f)
            {
                cachedTransform_.localPosition = targetLocalPosition;
                return;
            }

            animationCoroutine_ = StartCoroutine(AnimateLocalPosition(targetLocalPosition));
        }

        private IEnumerator AnimateLocalPosition(Vector3 targetLocalPosition)
        {
            Vector3 startLocalPosition = cachedTransform_.localPosition;
            float elapsed = 0f;

            while (elapsed < duration_)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration_);
                cachedTransform_.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, t);
                yield return null;
            }

            cachedTransform_.localPosition = targetLocalPosition;
            animationCoroutine_ = null;
        }
    }
}
