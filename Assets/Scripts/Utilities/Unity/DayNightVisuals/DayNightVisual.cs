using System;
using System.Collections;

using UnityEngine;

namespace Flowbit.Utilities.Unity.DayNightVisuals
{
    /// <summary>
    /// Toggles between day and night visuals based on the current local hour,
    /// then waits until the next transition instead of polling every frame.
    /// </summary>
    public sealed class DayNightVisual : MonoBehaviour
    {
        [SerializeField]
        private GameObject dayObject_;

        [SerializeField]
        private GameObject nightObject_;

        [SerializeField]
        [Range(0, 23)]
        private int dayStartHour_ = 7;

        [SerializeField]
        [Range(0, 59)]
        private int dayStartMinute_ = 0;

        [SerializeField]
        [Range(0, 23)]
        private int dayEndHour_ = 19;

        [SerializeField]
        [Range(0, 59)]
        private int dayEndMinute_ = 0;

        private Coroutine refreshCoroutine_;

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            ApplyCurrentVisualState();
            refreshCoroutine_ = StartCoroutine(RefreshLoop());
        }

        private void OnDisable()
        {
            if (refreshCoroutine_ == null)
            {
                return;
            }

            StopCoroutine(refreshCoroutine_);
            refreshCoroutine_ = null;
        }

        private IEnumerator RefreshLoop()
        {
            while (true)
            {
                DateTime now = DateTime.Now;
                DateTime nextTransition = GetNextTransitionTime(now);
                float waitSeconds = Mathf.Max(0.1f, (float)(nextTransition - now).TotalSeconds);
                yield return new WaitForSecondsRealtime(waitSeconds);
                ApplyCurrentVisualState();
            }
        }

        private void ApplyCurrentVisualState()
        {
            bool isDay = IsDayTime(DateTime.Now.TimeOfDay);
            dayObject_.SetActive(isDay);
            nightObject_.SetActive(!isDay);
        }

        private bool IsDayTime(TimeSpan currentTime)
        {
            TimeSpan dayStart = GetDayStartTime();
            TimeSpan dayEnd = GetDayEndTime();

            if (dayStart < dayEnd)
            {
                return currentTime >= dayStart && currentTime < dayEnd;
            }

            return currentTime >= dayStart || currentTime < dayEnd;
        }

        private DateTime GetNextTransitionTime(DateTime now)
        {
            DateTime nextDayStart = GetNextTimeOccurrence(now, GetDayStartTime());
            DateTime nextDayEnd = GetNextTimeOccurrence(now, GetDayEndTime());
            return nextDayStart <= nextDayEnd ? nextDayStart : nextDayEnd;
        }

        private TimeSpan GetDayStartTime()
        {
            return new TimeSpan(dayStartHour_, dayStartMinute_, 0);
        }

        private TimeSpan GetDayEndTime()
        {
            return new TimeSpan(dayEndHour_, dayEndMinute_, 0);
        }

        private static DateTime GetNextTimeOccurrence(DateTime now, TimeSpan targetTime)
        {
            DateTime candidate = new DateTime(
                now.Year,
                now.Month,
                now.Day,
                targetTime.Hours,
                targetTime.Minutes,
                0,
                now.Kind);

            if (candidate <= now)
            {
                candidate = candidate.AddDays(1);
            }

            return candidate;
        }

        private void ValidateReferences()
        {
            if (dayObject_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(DayNightVisual)} requires a day object reference.");
            }

            if (nightObject_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(DayNightVisual)} requires a night object reference.");
            }

            if (GetDayStartTime() == GetDayEndTime())
            {
                throw new InvalidOperationException(
                    $"{nameof(DayNightVisual)} requires a non-empty day range. {nameof(dayStartHour_)} and {nameof(dayEndHour_)} cannot be the same.");
            }
        }
    }
}
