using UnityEngine;

namespace Flowbit.Utilities.Unity.UI
{
    /// <summary>
    /// Controls a UI component animator and provides a method to transition it to its final state.
    /// </summary>
    public class UIComponentAnimatorController : MonoBehaviour
    {
        private const string FinishTrigger = "finish";

        [SerializeField]
        [Tooltip("Animator used to control the UI component state transitions.")]
        private Animator _animator;

        /// <summary>
        /// Triggers the animator transition to the final state.
        /// </summary>
        public void GoToFinalState()
        {
            if (_animator == null)
            {
                Debug.LogWarning($"{nameof(UIComponentAnimatorController)} on {gameObject.name} is missing an Animator reference.");
                return;
            }

            _animator.SetTrigger(FinishTrigger);
        }
    }
}