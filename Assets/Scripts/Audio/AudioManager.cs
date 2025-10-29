using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public bool loop = false;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音频设置")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float ambientVolume = 5f;

    [Header("背景音乐")]
    [SerializeField] private Sound[] bgmSounds;
    [SerializeField] private float bgmCrossfadeTime = 2f;

    [Header("音效")]
    [SerializeField] private Sound[] sfxSounds;

    [Header("环境音")]
    [SerializeField] private Sound[] ambientSounds;

    // 音频源
    [SerializeField] private AudioSource bgmSource1, bgmSource2;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private Dictionary<string, AudioSource> loopedSfxSources = new Dictionary<string, AudioSource>();

    // 当前状态
    private int currentBgmSource = 1;
    private string currentBgmName = "";
    private Dictionary<string, Sound> soundDictionary = new Dictionary<string, Sound>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            BuildSoundDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        // 初始化音量
        SetMasterVolume(masterVolume);
        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);
        SetAmbientVolume(ambientVolume);

        Debug.Log($"AudioManager初始化完成 - SFX音量: {sfxSource.volume}");
    }
    private void InitializeAudioSources()
    {
        // 创建BGM音源（用于交叉淡入淡出）
        bgmSource1 = CreateAudioSource("BGM Source 1", true);
        bgmSource2 = CreateAudioSource("BGM Source 2", true);

        // 创建音效音源
        sfxSource = CreateAudioSource("SFX Source", false);

        // 创建环境音源
        ambientSource = CreateAudioSource("Ambient Source", true);
    }

    private AudioSource CreateAudioSource(string name, bool loop)
    {
        GameObject sourceObject = new GameObject(name);
        sourceObject.transform.SetParent(transform);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        return source;
    }

    private void BuildSoundDictionary()
    {
        // 合并所有音频到字典
        AddSoundsToDictionary(bgmSounds);
        AddSoundsToDictionary(sfxSounds);
        AddSoundsToDictionary(ambientSounds);
    }

    private void AddSoundsToDictionary(Sound[] sounds)
    {
        foreach (Sound sound in sounds)
        {
            if (sound != null && !string.IsNullOrEmpty(sound.name) && !soundDictionary.ContainsKey(sound.name))
            {
                soundDictionary.Add(sound.name, sound);
            }
        }
    }

    private void Update()
    {
        // 实时更新音量
        UpdateAllVolumes();
    }

    private void UpdateAllVolumes()
    {
        // 更新BGM音量
        if (bgmSource1 != null) bgmSource1.volume = bgmVolume * masterVolume;
        if (bgmSource2 != null) bgmSource2.volume = bgmVolume * masterVolume;
        // 更新音效音量
        if (sfxSource != null) sfxSource.volume = sfxVolume * masterVolume;
        // 更新环境音音量
        if (ambientSource != null) ambientSource.volume = ambientVolume * masterVolume;
        // 更新循环音效音量
        foreach (var source in loopedSfxSources.Values)
        {
            if (source != null) source.volume = sfxVolume * masterVolume;
        }
    }

    #region 背景音乐管理
    public void PlayBGM(string bgmName)
    {
        if (soundDictionary.ContainsKey(bgmName) && soundDictionary[bgmName].clip != null)
        {
            StartCoroutine(CrossfadeBGM(soundDictionary[bgmName]));
            currentBgmName = bgmName;
        }
        else
        {
            Debug.LogWarning($"找不到名为 {bgmName} 的背景音乐");
        }
    }

    public void PlayBGM(int index)
    {
        if (index >= 0 && index < bgmSounds.Length && bgmSounds[index].clip != null)
        {
            StartCoroutine(CrossfadeBGM(bgmSounds[index]));
            currentBgmName = bgmSounds[index].name;
        }
    }

    private IEnumerator CrossfadeBGM(Sound newBGM)
    {
        AudioSource currentSource = (currentBgmSource == 1) ? bgmSource1 : bgmSource2;
        AudioSource nextSource = (currentBgmSource == 1) ? bgmSource2 : bgmSource1;

        // 设置新音源
        nextSource.clip = newBGM.clip;
        nextSource.volume = 0f;
        nextSource.Play();

        // 淡入淡出
        float timer = 0f;
        while (timer < bgmCrossfadeTime)
        {
            timer += Time.deltaTime;
            float ratio = timer / bgmCrossfadeTime;

            currentSource.volume = (1f - ratio) * bgmVolume * masterVolume;
            nextSource.volume = ratio * bgmVolume * masterVolume;

            yield return null;
        }

        // 完成切换
        currentSource.Stop();
        currentBgmSource = (currentBgmSource == 1) ? 2 : 1;
    }

    public void PauseBGM()
    {
        AudioSource currentSource = (currentBgmSource == 1) ? bgmSource1 : bgmSource2;
        currentSource.Pause();
    }

    public void ResumeBGM()
    {
        AudioSource currentSource = (currentBgmSource == 1) ? bgmSource1 : bgmSource2;
        currentSource.Play();
    }

    public void StopBGM()
    {
        bgmSource1.Stop();
        bgmSource2.Stop();
        currentBgmName = "";
    }
    #endregion

    #region 音效管理
    public void PlaySFX(string name)
    {
        if (sfxSource == null)
        {
            Debug.LogError("SFX AudioSource is null! Cannot play SFX.");
            return;
        }

        Sound s = Array.Find(sfxSounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning($"SFX未找到: {name}");
            return;
        }
        if (s.clip == null)
        {
            Debug.LogWarning($"SFX '{name}' 找到了但未绑定 AudioClip！");
            return;
        }
        sfxSource.PlayOneShot(s.clip, s.volume);
        Debug.Log($"播放SFX: {name}, 音量: {s.volume}");
    }


    public void PlaySFXLoop(string sfxName)
    {
        if (soundDictionary.ContainsKey(sfxName) && !loopedSfxSources.ContainsKey(sfxName))
        {
            Sound sound = soundDictionary[sfxName];
            AudioSource loopSource = CreateAudioSource($"Loop SFX: {sfxName}", true);
            loopSource.clip = sound.clip;
            loopSource.volume = sound.volume * sfxVolume * masterVolume;
            loopSource.pitch = sound.pitch;
            loopSource.Play();

            loopedSfxSources.Add(sfxName, loopSource);
        }
    }

    public void StopSFXLoop(string sfxName)
    {
        if (loopedSfxSources.ContainsKey(sfxName))
        {
            AudioSource source = loopedSfxSources[sfxName];
            source.Stop();
            Destroy(source.gameObject);
            loopedSfxSources.Remove(sfxName);
        }
    }
    // 在AudioManager中添加这些方法
    public float GetBGMVolume()
    {
        // 返回当前BGM音量
        AudioSource currentSource = (currentBgmSource == 1) ? bgmSource1 : bgmSource2;
        return currentSource.volume;
    }

    public float GetAmbientVolume()
    {
        // 返回当前环境音音量
        return ambientSource.volume;
    }

    public string GetCurrentBGMName()
    {
        // 返回当前播放的BGM名称
        return currentBgmName;
    }
    public void StopAllLoopSFX()
    {
        foreach (var source in loopedSfxSources.Values)
        {
            source.Stop();
            Destroy(source.gameObject);
        }
        loopedSfxSources.Clear();
    }
    #endregion

    #region 环境音管理
    public void PlayAmbient(string ambientName)
    {
        if (soundDictionary.ContainsKey(ambientName))
        {
            Sound sound = soundDictionary[ambientName];
            ambientSource.clip = sound.clip;
            ambientSource.volume = sound.volume * ambientVolume * masterVolume;
            ambientSource.pitch = sound.pitch;
            ambientSource.Play();
        }
    }

    public void StopAmbient()
    {
        ambientSource.Stop();
    }
    #endregion

    #region 音量控制
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    public void SetAmbientVolume(float volume)
    {
        ambientVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }
    #endregion

    #region 便捷方法 - 保留原SoundManager的方法
    // UI音效
    public void PlayBeginDrag() => PlaySFX("begindrag");
    public void PlayEndDrag() => PlaySFX("enddrag");
    public void PlayClick() => PlaySFX("click");
    public void PlayChangeTurn() => PlaySFX("changeturn");
    public void PlayOff() => PlaySFX("close");
    public void PlayNextTurn() => PlaySFX("nextturn");

    // 攻击音效
    public void PlayChangJian() => PlaySFX("changjianattack");
    public void PlayGong() => PlaySFX("gongattack");
    public void PlayBiShou() => PlaySFX("bishouattack");
    public void PlayAttacked() => PlaySFX("attacked");
    public void PlayHeal() => PlaySFX("healed");
    public void PlayMove() => PlaySFX("move");
    public void PlayDouble() => PlaySFX("xdouble");
    public void PlayMass() => PlaySFX("xmass");
    #endregion
}