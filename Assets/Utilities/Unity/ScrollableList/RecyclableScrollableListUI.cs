using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Flowbit.Utilities.Unity.ScrollableList
{
    /// <summary>
    /// Supported scroll directions for recyclable lists.
    /// </summary>
    public enum ScrollableListDirection
    {
        Vertical,
        Horizontal
    }

    /// <summary>
    /// Recyclable scroll list for fixed-size UI elements.
    /// Inherit with concrete generic arguments to use it as a Unity component.
    /// </summary>
    public abstract class RecyclableScrollableListUI<T, TElement> : MonoBehaviour, IScrollableListUI<T>
        where TElement : BaseScrollableListElement<T>
    {
        [Header("References")]
        [SerializeField]
        private ScrollRect scrollRect_;

        [SerializeField]
        private RectTransform elementParent_;

        [SerializeField]
        private TElement elementPrefab_;

        [Header("Layout")]
        [SerializeField]
        private ScrollableListDirection direction_ = ScrollableListDirection.Vertical;

        [SerializeField]
        [Min(0f)]
        private float spacing_ = 0f;

        [SerializeField]
        [Min(0f)]
        private float startPadding_ = 0f;

        [SerializeField]
        [Min(0f)]
        private float endPadding_ = 0f;

        [SerializeField]
        [Min(0.01f)]
        private float elementSize_ = 100f;

        [SerializeField]
        [Min(0)]
        private int extraVisibleElements_ = 2;

        [SerializeField]
        private bool forceAnchorsForDirection_ = true;

        private readonly List<T> dataList_ = new();
        private readonly List<TElement> pooledElements_ = new();
        private readonly List<int> pooledIndexes_ = new();

        private RectTransform viewport_;
        private RectTransform content_;
        private bool initialized_;
        private bool scrollListenerBound_;

        /// <summary>
        /// Gets the number of items currently bound to the list.
        /// </summary>
        public int Count => dataList_.Count;

        protected virtual void Awake()
        {
            InitializeIfNeeded();
            BindScrollListener();
        }

        protected virtual void OnEnable()
        {
            InitializeIfNeeded();
            BindScrollListener();
            Refresh();
        }

        protected virtual void OnDestroy()
        {
            UnbindScrollListener();
        }

        /// <summary>
        /// Replaces the current list data and refreshes the visible elements.
        /// </summary>
        public void SetData(IReadOnlyList<T> data)
        {
            InitializeIfNeeded();

            dataList_.Clear();

            if (data != null)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    dataList_.Add(data[i]);
                }
            }

            UpdateContentHeight();
            EnsurePoolSize();
            RefreshVisible(forceRebind: true);
        }

        /// <summary>
        /// Refreshes the currently visible range.
        /// </summary>
        public void Refresh()
        {
            InitializeIfNeeded();
            UpdateContentHeight();
            EnsurePoolSize();
            RefreshVisible(forceRebind: true);
        }

        /// <summary>
        /// Scrolls the list so the given index becomes visible near the top.
        /// </summary>
        public void ScrollToIndex(int index)
        {
            InitializeIfNeeded();

            if (content_ == null || viewport_ == null || dataList_.Count == 0)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, dataList_.Count - 1);

            Vector2 anchoredPosition = content_.anchoredPosition;

            float target = GetElementStart(index);
            float maxScroll = GetMaxScroll();

            if (direction_ == ScrollableListDirection.Vertical)
            {
                anchoredPosition.y = Mathf.Clamp(target, 0f, maxScroll);
            }
            else
            {
                anchoredPosition.x = -Mathf.Clamp(target, 0f, maxScroll);
            }

            content_.anchoredPosition = anchoredPosition;

            RefreshVisible(forceRebind: true);
        }

        /// <summary>
        /// Hides the list root object.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the list root object.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Configures the list references and layout at runtime before first use.
        /// </summary>
        public void Configure(
            ScrollRect scrollRect,
            RectTransform elementParent,
            TElement elementPrefab,
            ScrollableListDirection direction,
            float elementSize,
            float spacing = 0f,
            float startPadding = 0f,
            float endPadding = 0f,
            int extraVisibleElements = 2,
            bool forceAnchorsForDirection = true)
        {
            if (scrollRect_ != scrollRect)
            {
                UnbindScrollListener();
            }

            scrollRect_ = scrollRect;
            elementParent_ = elementParent;
            elementPrefab_ = elementPrefab;
            direction_ = direction;
            elementSize_ = Mathf.Max(0.01f, elementSize);
            spacing_ = Mathf.Max(0f, spacing);
            startPadding_ = Mathf.Max(0f, startPadding);
            endPadding_ = Mathf.Max(0f, endPadding);
            extraVisibleElements_ = Mathf.Max(0, extraVisibleElements);
            forceAnchorsForDirection_ = forceAnchorsForDirection;
            initialized_ = false;

            if (isActiveAndEnabled)
            {
                BindScrollListener();
            }
        }

        private void InitializeIfNeeded()
        {
            if (initialized_)
            {
                return;
            }

            if (scrollRect_ == null)
            {
                scrollRect_ = GetComponentInChildren<ScrollRect>(true);
            }

            if (scrollRect_ == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} requires a {nameof(ScrollRect)} reference.");
            }

            content_ = scrollRect_.content;
            viewport_ = scrollRect_.viewport != null
                ? scrollRect_.viewport
                : scrollRect_.GetComponent<RectTransform>();

            if (content_ == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} requires the ScrollRect to have a content RectTransform.");
            }

            if (elementParent_ == null)
            {
                elementParent_ = content_;
            }

            if (elementPrefab_ == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} requires an element prefab.");
            }

            ConfigureContentForDirection();
            initialized_ = true;
        }

        private void BindScrollListener()
        {
            if (scrollListenerBound_ || scrollRect_ == null)
            {
                return;
            }

            scrollRect_.onValueChanged.AddListener(OnScrollValueChanged);
            scrollListenerBound_ = true;
        }

        private void UnbindScrollListener()
        {
            if (!scrollListenerBound_ || scrollRect_ == null)
            {
                return;
            }

            scrollRect_.onValueChanged.RemoveListener(OnScrollValueChanged);
            scrollListenerBound_ = false;
        }

        private void EnsurePoolSize()
        {
            int requiredPoolSize = Mathf.Max(1, GetVisibleSlotCount() + extraVisibleElements_);

            while (pooledElements_.Count < requiredPoolSize)
            {
                TElement element = InstantiateElement(elementPrefab_, elementParent_);
                element.gameObject.SetActive(false);
                pooledElements_.Add(element);
                pooledIndexes_.Add(-1);
            }
        }

        /// <summary>
        /// Instantiates a pooled element.
        /// </summary>
        protected virtual TElement InstantiateElement(TElement elementPrefab, RectTransform elementParent)
        {
            return Instantiate(elementPrefab, elementParent);
        }

        private void RefreshVisible(bool forceRebind)
        {
            if (!initialized_)
            {
                return;
            }

            int firstVisibleIndex = GetFirstVisibleIndex();

            for (int i = 0; i < pooledElements_.Count; i++)
            {
                TElement element = pooledElements_[i];
                int dataIndex = firstVisibleIndex + i;

                if (dataIndex < 0 || dataIndex >= dataList_.Count)
                {
                    pooledIndexes_[i] = -1;
                    element.Hide();
                    element.gameObject.SetActive(false);
                    continue;
                }

                RectTransform elementRect = element.transform as RectTransform;
                if (elementRect != null)
                {
                    PositionElement(elementRect, dataIndex);
                }

                if (!element.gameObject.activeSelf)
                {
                    element.gameObject.SetActive(true);
                }

                if (forceRebind || pooledIndexes_[i] != dataIndex)
                {
                    pooledIndexes_[i] = dataIndex;
                    element.Show(dataList_[dataIndex]);
                }
            }
        }

        private void PositionElement(RectTransform elementRect, int dataIndex)
        {
            if (forceAnchorsForDirection_)
            {
                if (direction_ == ScrollableListDirection.Vertical)
                {
                    elementRect.anchorMin = new Vector2(0f, 1f);
                    elementRect.anchorMax = new Vector2(1f, 1f);
                    elementRect.pivot = new Vector2(0.5f, 1f);
                }
                else
                {
                    elementRect.anchorMin = new Vector2(0f, 1f);
                    elementRect.anchorMax = new Vector2(0f, 1f);
                    elementRect.pivot = new Vector2(0f, 1f);
                }
            }

            Vector2 sizeDelta = elementRect.sizeDelta;
            if (direction_ == ScrollableListDirection.Vertical)
            {
                sizeDelta.y = elementSize_;
            }
            else
            {
                sizeDelta.x = elementSize_;
            }

            elementRect.sizeDelta = sizeDelta;

            if (direction_ == ScrollableListDirection.Vertical)
            {
                elementRect.anchoredPosition = new Vector2(
                    elementRect.anchoredPosition.x,
                    -GetElementStart(dataIndex));
            }
            else
            {
                elementRect.anchoredPosition = new Vector2(
                    GetElementStart(dataIndex),
                    elementRect.anchoredPosition.y);
            }
        }

        private void UpdateContentHeight()
        {
            if (!initialized_ || content_ == null)
            {
                return;
            }

            Vector2 sizeDelta = content_.sizeDelta;

            if (direction_ == ScrollableListDirection.Vertical)
            {
                sizeDelta.y = GetContentLength();
            }
            else
            {
                sizeDelta.x = GetContentLength();
            }

            content_.sizeDelta = sizeDelta;
        }

        private void ConfigureContentForDirection()
        {
            if (content_ == null || !forceAnchorsForDirection_)
            {
                return;
            }

            if (direction_ == ScrollableListDirection.Vertical)
            {
                content_.anchorMin = new Vector2(0f, 1f);
                content_.anchorMax = new Vector2(1f, 1f);
                content_.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                content_.anchorMin = new Vector2(0f, 1f);
                content_.anchorMax = new Vector2(0f, 1f);
                content_.pivot = new Vector2(0f, 1f);
            }
        }

        private int GetVisibleSlotCount()
        {
            float viewportLength = GetViewportLength();
            if (viewportLength <= 0f)
            {
                return 1;
            }

            return Mathf.CeilToInt(viewportLength / GetStride());
        }

        private int GetFirstVisibleIndex()
        {
            if (content_ == null || dataList_.Count == 0)
            {
                return 0;
            }

            float scroll = GetCurrentScroll();
            float offset = Mathf.Max(0f, scroll - startPadding_);
            int index = Mathf.FloorToInt(offset / GetStride());
            int maxIndex = Mathf.Max(0, dataList_.Count - 1);
            return Mathf.Clamp(index, 0, maxIndex);
        }

        private float GetContentLength()
        {
            if (dataList_.Count == 0)
            {
                return startPadding_ + endPadding_;
            }

            return startPadding_
                + endPadding_
                + (dataList_.Count * elementSize_)
                + ((dataList_.Count - 1) * spacing_);
        }

        private float GetElementStart(int dataIndex)
        {
            return startPadding_ + (dataIndex * GetStride());
        }

        private float GetStride()
        {
            return elementSize_ + spacing_;
        }

        private float GetViewportLength()
        {
            if (viewport_ == null)
            {
                return 0f;
            }

            return direction_ == ScrollableListDirection.Vertical
                ? viewport_.rect.height
                : viewport_.rect.width;
        }

        private float GetCurrentScroll()
        {
            if (content_ == null)
            {
                return 0f;
            }

            return direction_ == ScrollableListDirection.Vertical
                ? Mathf.Max(0f, content_.anchoredPosition.y)
                : Mathf.Max(0f, -content_.anchoredPosition.x);
        }

        private float GetMaxScroll()
        {
            return Mathf.Max(0f, GetContentLength() - GetViewportLength());
        }

        private void OnScrollValueChanged(Vector2 _)
        {
            RefreshVisible(forceRebind: false);
        }
    }
}
