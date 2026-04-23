using System.Collections;
using UnityEngine;

namespace Flowbit.Utilities.Coroutines
{
    /// <summary>
    /// Provides an abstraction to run and control Unity coroutines from non-MonoBehaviour classes.
    /// </summary>
    public interface ICoroutineService
    {
        /// <summary>
        /// Starts a coroutine.
        /// </summary>
        /// <param name="routine">The coroutine to execute.</param>
        /// <returns>A handle to the started coroutine.</returns>
        Coroutine StartCoroutine(IEnumerator routine);

        /// <summary>
        /// Stops a running coroutine.
        /// </summary>
        /// <param name="coroutine">The coroutine to stop.</param>
        void StopCoroutine(Coroutine coroutine);

        /// <summary>
        /// Stops all running coroutines.
        /// </summary>
        void StopAllCoroutines();
    }
}