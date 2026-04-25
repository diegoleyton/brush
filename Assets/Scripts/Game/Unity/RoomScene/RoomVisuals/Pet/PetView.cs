using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual component for a pet.
    /// </summary>
    public sealed class PetView : MonoBehaviour
    {
        [SerializeField]
        private Animator bodyAnimationController_;

        [SerializeField]
        private Animator mouthAnimationController_;

        /// <summary>
        /// Plays the pet eat presentation.
        /// </summary>
        public void Eat()
        {
        }

        /// <summary>
        /// Prepares the pet to receive food.
        /// </summary>
        public void PrepareToEat()
        {
        }

        /// <summary>
        /// Plays the pet single-touch reaction.
        /// </summary>
        public void Touch()
        {
        }

        /// <summary>
        /// Starts the pet continuous touching reaction.
        /// </summary>
        public void StartTouching()
        {
        }

        /// <summary>
        /// Returns the pet to its idle state immediately.
        /// </summary>
        public void Idle()
        {

        }

        /// <summary>
        /// Returns the pet to idle after a short delay.
        /// </summary>
        public void IdleAfterDelay()
        {

        }

        /// <summary>
        /// Plays the pet reaction for being full and unable to eat.
        /// </summary>
        public void FoodFull()
        {

        }

        /// <summary>
        /// Plays the pet reaction for recently brushing and being unable to eat.
        /// </summary>
        public void FoodClean()
        {

        }
    }
}
