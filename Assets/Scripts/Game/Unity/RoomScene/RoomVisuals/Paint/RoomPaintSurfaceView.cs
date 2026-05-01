using System;
using System.Collections;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.Instantiator;
using Flowbit.Utilities.Unity.UI;

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
    /// Visual component for a fixed paintable room surface.
    /// </summary>
    public sealed class RoomPaintSurfaceView : MonoBehaviour
    {
        [SerializeField]
        private int surfaceId_;

        [SerializeField]
        private Image image_;

        private RoomSettings roomSettings_;
        private EventDispatcher dispatcher_;
        private IObjectInstantiator unityInstantiator_;
        private ScreenBlocker screenBlocker_;
        private Coroutine colorTransitionCoroutine_;
        private Coroutine paintEffectCleanupCoroutine_;
        private RectTransform activePaintEffect_;
        private IDisposable paintBlockScope_;

        public int SurfaceId => surfaceId_;

        [Inject]
        public void Construct(
            RoomSettings roomSettings,
            EventDispatcher dispatcher,
            ScreenBlocker screenBlocker,
            [Inject(Id = InstantiatorIds.Unity)] IObjectInstantiator unityInstantiator)
        {
            roomSettings_ = roomSettings;
            dispatcher_ = dispatcher;
            screenBlocker_ = screenBlocker;
            unityInstantiator_ = unityInstantiator;
        }

        private void Awake()
        {
            if (image_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomPaintSurfaceView)} requires an {nameof(Image)} reference.");
            }
        }

        private void OnDestroy()
        {
            if (colorTransitionCoroutine_ != null)
            {
                StopCoroutine(colorTransitionCoroutine_);
                colorTransitionCoroutine_ = null;
            }

            if (paintEffectCleanupCoroutine_ != null)
            {
                StopCoroutine(paintEffectCleanupCoroutine_);
                paintEffectCleanupCoroutine_ = null;
            }

            DisposePaintBlockScope();
        }

        public void ResetColor()
        {
            StopColorTransition();

            image_.color = DefaultProfileState.DefaultRoomSurfaceColor;
        }

        public void ApplyPaintColor(Color color)
        {
            ApplyPaintColor(color, animate: true, dropScreenPosition: null);
        }

        public void ApplyPaintColor(Color color, bool animate, Vector2? dropScreenPosition = null)
        {
            StopColorTransition();

            if (!animate)
            {
                DisposePaintBlockScope();
                image_.color = color;
                return;
            }

            colorTransitionCoroutine_ = StartCoroutine(AnimateColorTransition(color));
            SpawnPaintEffect(color, dropScreenPosition);
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

        private void SpawnPaintEffect(Color color, Vector2? dropScreenPosition)
        {
            if (roomSettings_ == null || roomSettings_.PaintSurfaceEffectPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomPaintSurfaceView)} requires {nameof(RoomSettings)}.{nameof(RoomSettings.PaintSurfaceEffectPrefab)} to play the paint effect.");
            }

            if (unityInstantiator_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomPaintSurfaceView)} requires a Unity {nameof(IObjectInstantiator)} binding.");
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomPaintSurfaceView)} requires a parent canvas to place the paint effect.");
            }

            if (activePaintEffect_ != null)
            {
                Destroy(activePaintEffect_.gameObject);
                activePaintEffect_ = null;
            }

            if (paintEffectCleanupCoroutine_ != null)
            {
                StopCoroutine(paintEffectCleanupCoroutine_);
                paintEffectCleanupCoroutine_ = null;
            }

            DisposePaintBlockScope();
            paintBlockScope_ = screenBlocker_?.BlockScope("RoomPaintEffect");

            RectTransform rootCanvasTransform = canvas.rootCanvas.transform as RectTransform;
            activePaintEffect_ = unityInstantiator_.InstantiatePrefab(
                roomSettings_.PaintSurfaceEffectPrefab,
                rootCanvasTransform);
            activePaintEffect_.SetAsLastSibling();
            PositionPaintEffect(activePaintEffect_, rootCanvasTransform, canvas.rootCanvas, dropScreenPosition);
            TintGraphics(activePaintEffect_, color);

            float effectLifetimeSeconds = Mathf.Max(0f, roomSettings_.PaintSurfaceEffectLifetimeSeconds);
            if (effectLifetimeSeconds <= 0f)
            {
                Destroy(activePaintEffect_.gameObject);
                activePaintEffect_ = null;
                DisposePaintBlockScope();
                dispatcher_?.Send(new RoomPaintEffectCompletedEvent(surfaceId_));
                return;
            }

            paintEffectCleanupCoroutine_ = StartCoroutine(DestroyPaintEffectAfterDelay(effectLifetimeSeconds));
        }

        private void PositionPaintEffect(
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

        private IEnumerator DestroyPaintEffectAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);

            if (activePaintEffect_ != null)
            {
                Destroy(activePaintEffect_.gameObject);
                activePaintEffect_ = null;
            }

            paintEffectCleanupCoroutine_ = null;
            DisposePaintBlockScope();
            dispatcher_?.Send(new RoomPaintEffectCompletedEvent(surfaceId_));
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

        private void DisposePaintBlockScope()
        {
            if (paintBlockScope_ == null)
            {
                return;
            }

            paintBlockScope_.Dispose();
            paintBlockScope_ = null;
        }
    }
}
