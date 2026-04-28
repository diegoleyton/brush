using System;
using System.Collections;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.Instantiator;

using Game.Core.Configuration;
using Game.Unity.Definitions;
using Game.Unity.Definitions.Events;
using Game.Unity.Settings;

using UnityEngine;
using UnityEngine.UI;

using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual component for the pet skin surface.
    /// </summary>
    public sealed class PetSkinSurfaceView : MonoBehaviour
    {
        [SerializeField]
        private Image image_;

        private RoomSettings roomSettings_;
        private EventDispatcher dispatcher_;
        private IObjectInstantiator unityInstantiator_;
        private Coroutine colorTransitionCoroutine_;
        private Coroutine effectCleanupCoroutine_;
        private RectTransform activeEffect_;

        [Inject]
        public void Construct(
            RoomSettings roomSettings,
            EventDispatcher dispatcher,
            [Inject(Id = InstantiatorIds.Unity)] IObjectInstantiator unityInstantiator)
        {
            roomSettings_ = roomSettings;
            dispatcher_ = dispatcher;
            unityInstantiator_ = unityInstantiator;
        }

        private void Awake()
        {
            if (image_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PetSkinSurfaceView)} requires an {nameof(Image)} reference.");
            }
        }

        private void OnDestroy()
        {
            if (colorTransitionCoroutine_ != null)
            {
                StopCoroutine(colorTransitionCoroutine_);
                colorTransitionCoroutine_ = null;
            }

            if (effectCleanupCoroutine_ != null)
            {
                StopCoroutine(effectCleanupCoroutine_);
                effectCleanupCoroutine_ = null;
            }
        }

        public void ResetColor()
        {
            StopColorTransition();
            image_.color = DefaultProfileState.DefaultRoomSurfaceColor;
        }

        public void ApplySkinColor(Color color)
        {
            ApplySkinColor(color, animate: true, dropScreenPosition: null);
        }

        public void ApplySkinColor(Color color, bool animate, Vector2? dropScreenPosition = null)
        {
            StopColorTransition();

            if (!animate)
            {
                image_.color = color;
                return;
            }

            colorTransitionCoroutine_ = StartCoroutine(AnimateColorTransition(color));
            SpawnSkinEffect(color, dropScreenPosition);
        }

        private IEnumerator AnimateColorTransition(Color targetColor)
        {
            Color startColor = image_.color;
            float duration = roomSettings_ != null
                ? Mathf.Max(0f, roomSettings_.PaintSurfaceColorTransitionDuration)
                : 0f;

            if (duration <= 0f)
            {
                image_.color = targetColor;
                colorTransitionCoroutine_ = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                image_.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            image_.color = targetColor;
            colorTransitionCoroutine_ = null;
        }

        private void SpawnSkinEffect(Color color, Vector2? dropScreenPosition)
        {
            if (roomSettings_ == null || roomSettings_.PaintSurfaceEffectPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PetSkinSurfaceView)} requires {nameof(RoomSettings)}.{nameof(RoomSettings.PaintSurfaceEffectPrefab)} to play the skin effect.");
            }

            if (unityInstantiator_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PetSkinSurfaceView)} requires a Unity {nameof(IObjectInstantiator)} binding.");
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PetSkinSurfaceView)} requires a parent canvas to place the skin effect.");
            }

            if (activeEffect_ != null)
            {
                Destroy(activeEffect_.gameObject);
                activeEffect_ = null;
            }

            if (effectCleanupCoroutine_ != null)
            {
                StopCoroutine(effectCleanupCoroutine_);
                effectCleanupCoroutine_ = null;
            }

            RectTransform rootCanvasTransform = canvas.rootCanvas.transform as RectTransform;
            activeEffect_ = unityInstantiator_.InstantiatePrefab(
                roomSettings_.PaintSurfaceEffectPrefab,
                rootCanvasTransform);
            activeEffect_.SetAsLastSibling();
            PositionEffect(activeEffect_, rootCanvasTransform, canvas.rootCanvas, dropScreenPosition);
            TintGraphics(activeEffect_, color);

            float effectLifetimeSeconds = Mathf.Max(0f, roomSettings_.PaintSurfaceEffectLifetimeSeconds);
            if (effectLifetimeSeconds <= 0f)
            {
                Destroy(activeEffect_.gameObject);
                activeEffect_ = null;
                dispatcher_?.Send(new PetSkinEffectCompletedEvent());
                return;
            }

            effectCleanupCoroutine_ = StartCoroutine(DestroyEffectAfterDelay(effectLifetimeSeconds));
        }

        private void PositionEffect(
            RectTransform effectTransform,
            RectTransform canvasTransform,
            Canvas rootCanvas,
            Vector2? dropScreenPosition)
        {
            if (effectTransform == null || canvasTransform == null || rootCanvas == null)
            {
                return;
            }

            effectTransform.anchorMin = effectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            effectTransform.pivot = new Vector2(0.5f, 0.5f);
            Camera eventCamera = rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            if (!dropScreenPosition.HasValue ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasTransform,
                    dropScreenPosition.Value,
                    eventCamera,
                    out Vector2 localPoint))
            {
                effectTransform.anchoredPosition = Vector2.zero;
                return;
            }

            effectTransform.anchoredPosition = localPoint;
        }

        private IEnumerator DestroyEffectAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);

            if (activeEffect_ != null)
            {
                Destroy(activeEffect_.gameObject);
                activeEffect_ = null;
            }

            effectCleanupCoroutine_ = null;
            dispatcher_?.Send(new PetSkinEffectCompletedEvent());
        }

        private void StopColorTransition()
        {
            if (colorTransitionCoroutine_ == null)
            {
                return;
            }

            StopCoroutine(colorTransitionCoroutine_);
            colorTransitionCoroutine_ = null;
        }

        private static void TintGraphics(Component root, Color color)
        {
            if (root == null)
            {
                return;
            }

            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int index = 0; index < graphics.Length; index++)
            {
                Graphic graphic = graphics[index];
                if (graphic == null)
                {
                    continue;
                }

                graphic.color = color;
            }
        }
    }
}
