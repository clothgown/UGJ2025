using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public Sound[] musicSounds, sfxSounds;
    public AudioSource musicSource, sfxSource, loopSfxSource; // Add a separate AudioSource for looped SFX

    private Dictionary<string, AudioSource> loopedSfxSources = new Dictionary<string, AudioSource>();
    public bool isBoss = false;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Initialize the loop SFX AudioSource
        
        //loopSfxSource.loop = true; // Set the loopSfxSource to loop
    }

    private void Start()
    {
        
            PlayMusic("BG");


    }

    public void PlayMusic(string name)
    {
        Sound s = Array.Find(musicSounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Music: " + name + " not found!");
            return;
        }
        musicSource.clip = s.clip;
        musicSource.Play();
    }

    public void PlaySFX(string name)
    {
        Sound s = Array.Find(sfxSounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("SFX: " + name + " not found!");
            return;
        }
        sfxSource.PlayOneShot(s.clip);
    }

    // New method to play an SFX in loop
    public void PlaySFXLoop(string name, float pitch = 0.8f)
    {

        var sound = Array.Find(sfxSounds, s => s.name == name);
        if (sound != null)
        {
            loopSfxSource.clip = sound.clip;
            loopSfxSource.pitch = pitch;
            if (!loopSfxSource.isPlaying)
            {
                loopSfxSource.Play();
            }
        }
        else
        {
            Debug.LogWarning("Sound not found: " + name);
        }
    }

    public void StopSFX(string name)
    {
        if (loopSfxSource.isPlaying)
        {
            loopSfxSource.Stop();
    
        }
    }

    public bool IsPlayingSFX(string name)
    {
        if (loopedSfxSources.TryGetValue(name, out var source))
        {
            return source.isPlaying;
        }
        return false;
    }

    public void ToggleMusic()
    {
        musicSource.mute = !musicSource.mute;
    }

    public void ToggleSFX()
    {
        sfxSource.mute = !sfxSource.mute;
        loopSfxSource.mute = !loopSfxSource.mute; // Also toggle mute for the loopSfxSource
    }

    public void MusicVolume(float volume)
    {
        musicSource.volume = volume;
    }

    public void SFXVolume(float volume)
    {
        sfxSource.volume = volume;
        loopSfxSource.volume = volume; // Also set volume for the loopSfxSource
    }
}
