using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;
using System.Collections;

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

        [SerializeField]
        private float initialAnimationDelay_ = 0.8f;


        private const string HappyName = "Happy";
        private const string PrepareToEatName = "PreEat";

        private const string IdleName = "Idle";

        private const string mouthEatName = "Eat";

        private const string mouthCleanName = "Clean";

        private const string mouthNoMoreFoodName = "NoMoreFood";

        private const string mouthIdleName = "Idle";

        private const string mouthPreEatName = "PreEat";

        private const string mouthHappyName = "Happy";

        bool isHappy = false;

        bool isPreparingToEat = false;

        /// <summary>
        /// Plays the pet eat presentation.
        /// </summary>
        public void Eat()
        {
        }

        /// <summary>
        /// Prepares the pet to receive food.
        /// </summary>
        public void PrepareToEat(bool withDelay = false)
        {
            isPreparingToEat = true;
            SetTrigger(PrepareToEatName, mouthPreEatName, withDelay);
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
        public void StartTouching(bool withDelay = false)
        {
            isHappy = true;
            SetTrigger(HappyName, mouthHappyName, withDelay);
        }

        /// <summary>
        /// Stops the pet continuous touching reaction.
        /// </summary>
        public void StopTouching(bool withDelay = false)
        {
            isHappy = false;
            GoToNextState(withDelay);
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

        public void ExitFoodState(bool withDelay = false)
        {
            isPreparingToEat = false;
            GoToNextState(withDelay);
        }

        private void GoToNextState(bool withDelay)
        {
            if (isPreparingToEat)
            {
                //TODO: What if they cannot eat?
                SetTrigger(PrepareToEatName, mouthPreEatName, withDelay);
                return;
            }
            if (isHappy)
            {
                SetTrigger(HappyName, mouthHappyName, withDelay);
                return;
            }

            SetTrigger(IdleName, mouthIdleName, withDelay);
        }

        private void SetTrigger(string bodyName, string mouthName, bool withDelay)
        {
            if (!withDelay)
            {
                if (bodyName != null)
                {
                    bodyAnimationController_.SetTrigger(bodyName);
                }
                if (mouthName != null)
                {
                    mouthAnimationController_.SetTrigger(mouthName);
                }
            }
            else
            {
                StartCoroutine(SetTriggerAfterDelayCoroutine(bodyName, mouthName));
            }
        }

        private IEnumerator SetTriggerAfterDelayCoroutine(string bodyName, string mouthName)
        {
            yield return new WaitForSeconds(initialAnimationDelay_);
            SetTrigger(bodyName, mouthName, false);
        }
    }
}
