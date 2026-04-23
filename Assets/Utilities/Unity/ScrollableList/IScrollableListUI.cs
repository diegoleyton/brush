using System.Collections.Generic;

namespace Flowbit.Utilities.Unity.ScrollableList
{
    /// <summary>
    /// Common API for scrollable lists backed by recyclable UI elements.
    /// </summary>
    public interface IScrollableListUI<T>
    {
        /// <summary>
        /// Gets the number of items currently bound to the list.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Replaces the current list data.
        /// </summary>
        void SetData(IReadOnlyList<T> data);

        /// <summary>
        /// Refreshes the currently visible elements.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Scrolls the list so the given index becomes visible.
        /// </summary>
        void ScrollToIndex(int index);

        /// <summary>
        /// Hides the list root object.
        /// </summary>
        void Hide();

        /// <summary>
        /// Shows the list root object.
        /// </summary>
        void Show();
    }
}
