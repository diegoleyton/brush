using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Flowbit.Utilities.Unity.General
{
    /// <summary>
    /// Destroys the gameObject after a certain amount of time.
    /// </summary>
    public class AutoDestroy : MonoBehaviour
    {
        [SerializeField]
        private float timeToDestroy_ = 1f;

        private void Start()
        {
            StartCoroutine(SelfDestroy());
        }

        private IEnumerator SelfDestroy()
        {
            yield return new WaitForSeconds(timeToDestroy_);
            Destroy(gameObject);
        }
    }
}