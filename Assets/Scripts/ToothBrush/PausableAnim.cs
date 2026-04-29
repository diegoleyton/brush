using UnityEngine;
using System.Collections.Generic;

public abstract class PausableAnim : MonoBehaviour
{
    PauseService pauseService = new PauseService();

    protected bool IsPaused => pauseService.IsPaused;

    public void Pause(int id = 0) => pauseService.Pause(id);

    public void Play(int id = 0) => pauseService.Play(id);
}

public class PauseService
{
    private HashSet<int> counters_ = new HashSet<int>();

    public bool IsPaused => counters_.Count > 0;

    public void Pause(int id = 0)
    {
        counters_.Add(id);
    }

    public void Play(int id = 0)
    {
        counters_.Remove(id);
    }
}
