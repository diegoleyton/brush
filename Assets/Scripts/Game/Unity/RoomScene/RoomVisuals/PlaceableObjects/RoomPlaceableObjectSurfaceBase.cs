using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Base visual surface that can host a placed room item instance.
    /// </summary>
    public abstract class RoomPlaceableObjectSurfaceBase : MonoBehaviour
    {
        [SerializeField]
        private RectTransform itemContainer_;

        public void SetPlacedVisual(RectTransform visual)
        {
            ClearPlacedVisual();

            if (visual == null || itemContainer_ == null)
            {
                return;
            }

            visual.SetParent(itemContainer_, false);
            visual.anchorMin = Vector2.zero;
            visual.anchorMax = Vector2.one;
            visual.pivot = new Vector2(0.5f, 0.5f);
            visual.offsetMin = Vector2.zero;
            visual.offsetMax = Vector2.zero;
            visual.localScale = Vector3.one;
        }

        public void ClearPlacedVisual()
        {
            if (itemContainer_ == null)
            {
                return;
            }

            for (int index = itemContainer_.childCount - 1; index >= 0; index--)
            {
                Transform child = itemContainer_.GetChild(index);
                if (child != null)
                {
                    Object.Destroy(child.gameObject);
                }
            }
        }

        public T GetPlacedVisualComponent<T>() where T : Component
        {
            if (itemContainer_ == null || itemContainer_.childCount == 0)
            {
                return null;
            }

            for (int index = itemContainer_.childCount - 1; index >= 0; index--)
            {
                Transform child = itemContainer_.GetChild(index);
                if (child == null)
                {
                    continue;
                }

                T component = child.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
