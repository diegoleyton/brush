using UnityEngine;

namespace Flowbit.Utilities.Unity.UI
{
    /// <summary>
    /// Moves a UI element (RectTransform) from a start target to an end target in a loop.
    /// When the element reaches the end target, it instantly teleports back to the start
    /// and repeats the movement. Uses anchoredPosition for proper UI behavior.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UILoopMove : MonoBehaviour
    {
        [SerializeField] private RectTransform startTarget;
        [SerializeField] private RectTransform endTarget;
        [SerializeField] private float speed = 200f; // UI suele usar valores más altos

        private RectTransform rectTransform;
        private Vector2 currentTarget;

        /// <summary>
        /// Initializes references and sets the starting position.
        /// </summary>
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Sets initial position and first movement target.
        /// </summary>
        private void Start()
        {
            if (startTarget == null || endTarget == null)
            {
                Debug.LogError("StartTarget and EndTarget must be assigned.");
                enabled = false;
                return;
            }

            rectTransform.anchoredPosition = startTarget.anchoredPosition;
            currentTarget = endTarget.anchoredPosition;
        }

        /// <summary>
        /// Moves the UI element toward the target every frame.
        /// When reaching the end, it teleports back to the start and loops again.
        /// </summary>
        private void Update()
        {
            rectTransform.anchoredPosition = Vector2.MoveTowards(
                rectTransform.anchoredPosition,
                currentTarget,
                speed * Time.deltaTime
            );

            if (Vector2.Distance(rectTransform.anchoredPosition, currentTarget) < 0.1f)
            {
                rectTransform.anchoredPosition = startTarget.anchoredPosition;
                currentTarget = endTarget.anchoredPosition;
            }
        }
    }
}