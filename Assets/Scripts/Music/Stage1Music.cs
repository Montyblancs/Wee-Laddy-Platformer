using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stage1Music : MonoBehaviour
{

    public AudioClip StageMusic;
    public AudioClip BossMusic;

    AudioSource thisSource;

    // Use this for initialization
    void Start()
    {
        //This needs two audiosources in order to transition properly. Loading a clip adds overhead, and thus an audable delay in the track.
        thisSource = GetComponent<AudioSource>();
    }

    public void PlayBossTrack()
    {
        StopAllCoroutines();
        StartCoroutine(SwapTrack(1f, BossMusic));
    }

    //Coroutine Timers
    public IEnumerator SwapTrack(float FadeTime, AudioClip nextClip)
    {
        float startVolume = thisSource.volume;

        while (thisSource.volume > 0)
        {
            thisSource.volume -= startVolume * Time.deltaTime / FadeTime;

            yield return null;
        }
        thisSource.Stop();
        thisSource.volume = startVolume;
        thisSource.clip = nextClip;
        thisSource.Play();
    }
}
