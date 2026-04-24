using UnityEngine;

namespace Flowbit.Utilities.Unity.ScrollableList
{
    /// <summary>
    /// Base class for recycled list elements bound to a concrete data type.
    /// </summary>
    public abstract class BaseScrollableListElement<T> : MonoBehaviour
    {
        /// <summary>
        /// Shows and binds the element to the given data.
        /// </summary>
        public abstract void Show(T data);

        /// <summary>
        /// Hides the element when it is no longer bound to visible data.
        /// </summary>
        public abstract void Hide();
    }
}
