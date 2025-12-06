using UnityEngine;
using UnityEngine.Playables;

public class TimelineSwapper : MonoBehaviour
{
    public PlayableDirector director;
    public PlayableAsset walkInTimeline;
    public PlayableAsset walkOutTimeline;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            PlayWalkOut();
        }
    }

    public void PlayWalkIn()
    {
        director.playableAsset = walkInTimeline;
        director.time = 0;
        director.Play();
    }

    public void PlayWalkOut()
    {
        director.playableAsset = walkOutTimeline;
        director.time = 0;
        director.Play();
    }
}
