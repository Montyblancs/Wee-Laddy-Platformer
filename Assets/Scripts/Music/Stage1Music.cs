using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stage1Music : MonoBehaviour
{

    public AudioClip StageMusic;
    public AudioClip BossMusic;
    public int loopDuration;
    public int loopEnd;

    AudioSource thisSource;

    // Use this for initialization
    void Start()
    {
        thisSource = GetComponent<AudioSource>();
        //Get length of intro and loop using the debug option below
        Debug.Log(thisSource.clip.samples);
        // Intro duration = 305280
        // loopDuration - The length of just the looping section of music
        // loopDuration = 1812672
        // loopEnd - The length of the entire song + intro. This is where the music should stop and loop.
        // loopEnd = 2117952

        //Boss Music Values
        //Intro duration = 470272
        //loopDuration = 1881088
        //loopEnd = 2351360
    }

    //Set to check every 3ms (Edit -> Project Settings -> Time -> Fixed Timestep)
    //(DO NOT USE THE DEFAULT UNITY PHYSICS ENGINE WITH THIS 3MS SETTING)
    private void FixedUpdate()
    {
        if (loopDuration > 0 && loopEnd > 0)
        {
            // * StageMusic.frequency
            //Debug.Log(thisSource.timeSamples + " | " + loopEnd);
            if (thisSource.timeSamples > loopEnd)
            {
                Debug.Log("looping");
                thisSource.timeSamples -= loopDuration;
            }
        }
    }

    public void Update()
    {
        
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
        //thisSource.timeSamples to get current time
        loopDuration = 1881088;
        loopEnd = 2351360;
        thisSource.Play();
    }
}
