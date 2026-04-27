using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game.Unity.Definitions;
using Game.Core.Data;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual component for a pet.
    /// </summary>
    public sealed class PetView : MonoBehaviour
    {
        private class TriggerNames
        {
            public string BodyTriggerName { get; private set; }
            public string MouthTriggerName { get; private set; }

            public TriggerNames(string bodyTriggerName, string mouthTriggerName)
            {
                MouthTriggerName = mouthTriggerName;
                BodyTriggerName = bodyTriggerName;
            }
        }

        private const string HappyName = "Happy";
        private const string PrepareToEatName = "PreEat";

        private const string IdleName = "Idle";

        private const string mouthEatName = "Eat";

        private const string mouthPostEatName = "PostEat";

        private const string mouthCleanName = "Clean";

        private const string mouthNoMoreFoodName = "NoMoreFood";

        private const string mouthIdleName = "Idle";

        private const string mouthPreEatName = "PreEat";

        private const string mouthHappyName = "Happy";

        private readonly static Dictionary<PetEatStatus, TriggerNames> preEatTriggerNames_ = new Dictionary<PetEatStatus, TriggerNames>
        {
            {PetEatStatus.OK, new TriggerNames(PrepareToEatName, mouthPreEatName)},
            {PetEatStatus.NO_AFTER_BRUSHING, new TriggerNames(IdleName, mouthCleanName)},
            {PetEatStatus.NO_MORE, new TriggerNames(IdleName, mouthNoMoreFoodName)}
        };

        private static readonly TriggerNames happyTriggerNames_ = new TriggerNames(HappyName, mouthHappyName);
        private static readonly TriggerNames iddleTriggerNames_ = new TriggerNames(IdleName, mouthIdleName);

        private static readonly TriggerNames eatTriggerNames_ = new TriggerNames(IdleName, mouthEatName);
        private static readonly TriggerNames postEatTriggerNames_ = new TriggerNames(IdleName, mouthPostEatName);

        [SerializeField]
        private Animator bodyAnimationController_;

        [SerializeField]
        private Animator mouthAnimationController_;

        [SerializeField]
        private float initialAnimationDelay_ = 0.8f;

        [SerializeField]
        private float eatDuration_ = 3f;

        [SerializeField]
        private float postEatDuration_ = 2f;

        private bool isHappy_ = false;

        private bool isPreparingToEat_ = false;

        private bool isEating_ = false;

        private PetEatStatus petEatState_;

        /// <summary>
        /// Plays the pet eat presentation.
        /// </summary>
        public void Eat()
        {
            Debug.Log("exit food");
            isEating_ = true;
            StartCoroutine(EatCoroutine());
            SetTrigger(eatTriggerNames_, false);
        }

        /// <summary>
        /// Prepares the pet to receive food.
        /// </summary>
        public void PrepareToEat(PetEatStatus peteatState, bool withDelay = false)
        {
            isPreparingToEat_ = true;
            petEatState_ = peteatState;
            if (isEating_)
            {
                return;
            }

            SetTrigger(preEatTriggerNames_[petEatState_], withDelay);
        }

        public void ExitFoodState(bool withDelay = false)
        {
            Debug.Log("exit food");
            isEating_ = false;
            isPreparingToEat_ = false;
            GoToNextState(withDelay);
        }

        /// <summary>
        /// Plays the pet single-touch reaction.
        /// </summary>
        public void Touch()
        {
            if (isPreparingToEat_ || isEating_)
            {
                return;
            }
        }

        /// <summary>
        /// Starts the pet continuous touching reaction.
        /// </summary>
        public void StartTouching(bool withDelay = false)
        {
            isHappy_ = true;

            if (isPreparingToEat_ || isEating_)
            {
                return;
            }

            SetTrigger(happyTriggerNames_, withDelay);
        }

        /// <summary>
        /// Stops the pet continuous touching reaction.
        /// </summary>
        public void StopTouching(bool withDelay = false)
        {
            isHappy_ = false;
            GoToNextState(withDelay);
        }

        private void GoToNextState(bool withDelay)
        {
            if (isPreparingToEat_)
            {
                SetTrigger(preEatTriggerNames_[petEatState_], withDelay);
                return;
            }
            if (isHappy_)
            {
                SetTrigger(happyTriggerNames_, withDelay);
                return;
            }

            SetTrigger(iddleTriggerNames_, withDelay);
        }

        private void SetTrigger(TriggerNames triggerNames, bool withDelay)
        {
            if (!withDelay)
            {
                if (triggerNames.BodyTriggerName != null)
                {
                    Debug.Log("Bdy: " + triggerNames.BodyTriggerName);
                    bodyAnimationController_.SetTrigger(triggerNames.BodyTriggerName);
                }
                if (triggerNames.MouthTriggerName != null)
                {
                    Debug.Log("Mouth: " + triggerNames.MouthTriggerName);
                    mouthAnimationController_.SetTrigger(triggerNames.MouthTriggerName);
                }
            }
            else
            {
                StartCoroutine(SetTriggerAfterDelayCoroutine(triggerNames));
            }
        }

        private IEnumerator SetTriggerAfterDelayCoroutine(TriggerNames triggerNames)
        {
            yield return new WaitForSeconds(initialAnimationDelay_);
            SetTrigger(triggerNames, false);
        }

        private IEnumerator EatCoroutine()
        {
            SetTrigger(eatTriggerNames_, false);
            yield return new WaitForSeconds(eatDuration_);
            SetTrigger(postEatTriggerNames_, false);
            yield return new WaitForSeconds(postEatDuration_);
            ExitFoodState();
        }
    }
}
