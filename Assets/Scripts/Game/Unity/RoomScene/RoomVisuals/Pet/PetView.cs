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
        public event System.Action EatAnimationCompleted;
        public event System.Action PaintAnimationCompleted;

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

        private const string FunName = "Fun";

        private const string mouthEatName = "Eat";

        private const string mouthPostEatName = "PostEat";

        private const string mouthCleanName = "Clean";

        private const string mouthNoMoreFoodName = "NoMoreFood";

        private const string mouthIdleName = "Idle";

        private const string mouthPreEatName = "PreEat";

        private const string mouthHappyName = "Happy";

        private const string mouthFunName = "Fun";

        private const string mouthPaintName = "Sourprise";

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

        private static readonly TriggerNames funTriggerNames_ = new TriggerNames(FunName, mouthFunName);
        private static readonly TriggerNames paintTriggerNames_ = new TriggerNames(IdleName, mouthPaintName);

        [SerializeField]
        private Animator bodyAnimationController_;

        [SerializeField]
        private Animator mouthAnimationController_;

        [SerializeField]
        private float eatDuration_ = 3f;

        [SerializeField]
        private float postEatDuration_ = 2f;

        [SerializeField]
        private float paintDuration_ = 2f;

        [SerializeField]
        private float postPaintDuration_ = 1f;

        private bool isHappy_ = false;

        private bool isPreparingToEat_ = false;

        private bool isEating_ = false;

        private bool isPainting_ = false;

        private PetEatStatus petEatState_;

        /// <summary>
        /// Plays the pet eat presentation.
        /// </summary>
        public void Eat()
        {
            if (isEating_ || isPainting_)
            {
                return;
            }

            isEating_ = true;
            StartCoroutine(EatCoroutine());
            SetTrigger(eatTriggerNames_);
        }

        /// <summary>
        /// Prepares the pet to receive food.
        /// </summary>
        public void PrepareToEat(PetEatStatus peteatState)
        {
            isPreparingToEat_ = true;
            petEatState_ = peteatState;
            if (isEating_ || isPainting_)
            {
                return;
            }

            SetTrigger(preEatTriggerNames_[petEatState_]);
        }

        public void ExitFoodState()
        {
            isEating_ = false;
            isPreparingToEat_ = false;
            GoToNextState();
        }

        /// <summary>
        /// Plays the pet paint presentation.
        /// </summary>
        public void Paint()
        {
            if (isPainting_ || isEating_)
            {
                return;
            }

            isPainting_ = true;
            StartCoroutine(PaintCoroutine());
            SetTrigger(paintTriggerNames_);
        }

        /// <summary>
        /// Exits the pet paint presentation state.
        /// </summary>
        public void ExitPaintState()
        {
            isPainting_ = false;
            GoToNextState();
        }

        /// <summary>
        /// Plays the pet single-touch reaction.
        /// </summary>
        public void Touch()
        {
            if (isPreparingToEat_ || isEating_ || isPainting_)
            {
                return;
            }

            SetTrigger(funTriggerNames_);
        }

        /// <summary>
        /// Starts the pet continuous touching reaction.
        /// </summary>
        public void StartTouching()
        {
            isHappy_ = true;

            if (isPreparingToEat_ || isEating_ || isPainting_)
            {
                return;
            }

            SetTrigger(happyTriggerNames_);
        }

        /// <summary>
        /// Stops the pet continuous touching reaction.
        /// </summary>
        public void StopTouching()
        {
            isHappy_ = false;
            GoToNextState();
        }

        private void GoToNextState()
        {
            if (isEating_ || isPainting_)
            {
                return;
            }
            if (isPreparingToEat_)
            {
                SetTrigger(preEatTriggerNames_[petEatState_]);
                return;
            }
            if (isHappy_)
            {
                SetTrigger(happyTriggerNames_);
                return;
            }

            SetTrigger(iddleTriggerNames_);
        }

        private void SetTrigger(TriggerNames triggerNames)
        {
            if (triggerNames.BodyTriggerName != null)
            {
                bodyAnimationController_.SetTrigger(triggerNames.BodyTriggerName);
            }
            if (triggerNames.MouthTriggerName != null)
            {
                mouthAnimationController_.SetTrigger(triggerNames.MouthTriggerName);
            }
        }

        private IEnumerator EatCoroutine()
        {
            SetTrigger(eatTriggerNames_);
            yield return new WaitForSeconds(eatDuration_);
            SetTrigger(postEatTriggerNames_);
            yield return new WaitForSeconds(postEatDuration_);
            ExitFoodState();
            EatAnimationCompleted?.Invoke();
        }

        private IEnumerator PaintCoroutine()
        {
            SetTrigger(paintTriggerNames_);
            yield return new WaitForSeconds(paintDuration_);
            SetTrigger(funTriggerNames_);
            yield return new WaitForSeconds(postPaintDuration_);
            ExitPaintState();
            PaintAnimationCompleted?.Invoke();
        }
    }
}
