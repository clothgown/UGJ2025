using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("背景音乐")]
    [SerializeField] private AudioClip[] bgmClips; // 所有背景音乐
    [SerializeField] private float bgmCrossfadeTime = 2f; // 背景音乐淡入淡出时间

    [Header("UI音效")]
    [SerializeField] private AudioClip begindrag, enddrag, click, off, changeturn,next;

    [Header("攻击音效")]
    [SerializeField] private AudioClip changjianattack, gongattack, bishouattack, attacked, healed, move, xdouble, xmass;

    private AudioSource bgmSource1, bgmSource2, uiSource, vfxSource;
    private int currentBgmSource = 1; // 当前正在播放的BGM音源
    private string currentBgmName = ""; // 当前播放的BGM名称
    private Dictionary<string, AudioClip> bgmDictionary = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 确保在场景切换时不销毁
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 初始化音源
        bgmSource1 = gameObject.AddComponent<AudioSource>();
        bgmSource2 = gameObject.AddComponent<AudioSource>();
        uiSource = gameObject.AddComponent<AudioSource>();
        vfxSource = gameObject.AddComponent<AudioSource>();

        // 设置BGM音源为循环
        bgmSource1.loop = true;
        bgmSource2.loop = true;

        // 构建BGM字典，便于通过名称查找
        BuildBgmDictionary();

        // 播放默认背景音乐
        if (bgmClips.Length > 0)
        {
            PlayBGM(0); // 播放第一个背景音乐
        }
    }

    // 构建BGM字典
    private void BuildBgmDictionary()
    {
        foreach (AudioClip clip in bgmClips)
        {
            if (clip != null && !bgmDictionary.ContainsKey(clip.name))
            {
                bgmDictionary.Add(clip.name, clip);
            }
        }
    }

    // 通过索引播放背景音乐
    public void PlayBGM(int index)
    {
        if (index >= 0 && index < bgmClips.Length && bgmClips[index] != null)
        {
            StartCoroutine(CrossfadeBGM(bgmClips[index]));
            currentBgmName = bgmClips[index].name;
        }
        else
        {
            Debug.LogWarning($"BGM索引 {index} 无效");
        }
    }

    // 通过名称播放背景音乐
    public void PlayBGM(string bgmName)
    {
        if (bgmDictionary.ContainsKey(bgmName))
        {
            StartCoroutine(CrossfadeBGM(bgmDictionary[bgmName]));
            currentBgmName = bgmName;
        }
        else
        {
            Debug.LogWarning($"找不到名为 {bgmName} 的背景音乐");
        }
    }

    // 淡入淡出切换背景音乐
    private IEnumerator CrossfadeBGM(AudioClip newBGM)
    {
        AudioSource currentSource = (currentBgmSource == 1) ? bgmSource1 : bgmSource2;
        AudioSource nextSource = (currentBgmSource == 1) ? bgmSource2 : bgmSource1;

        // 设置新音源
        nextSource.clip = newBGM;
        nextSource.volume = 0f;
        nextSource.Play();

        // 淡入淡出
        float timer = 0f;
        while (timer < bgmCrossfadeTime)
        {
            timer += Time.deltaTime;
            float ratio = timer / bgmCrossfadeTime;

            currentSource.volume = 1f - ratio;
            nextSource.volume = ratio;

            yield return null;
        }

        // 完成切换
        currentSource.Stop();
        currentBgmSource = (currentBgmSource == 1) ? 2 : 1;
    }

    // 暂停背景音乐
    public void PauseBGM()
    {
        AudioSource currentSource = (currentBgmSource == 1) ? bgmSource1 : bgmSource2;
        currentSource.Pause();
    }

    // 恢复背景音乐
    public void ResumeBGM()
    {
        AudioSource currentSource = (currentBgmSource == 1) ? bgmSource1 : bgmSource2;
        currentSource.Play();
    }

    // 停止背景音乐
    public void StopBGM()
    {
        bgmSource1.Stop();
        bgmSource2.Stop();
        currentBgmName = "";
    }

    // 设置背景音乐音量
    public void SetBGMVolume(float volume)
    {
        bgmSource1.volume = volume;
        bgmSource2.volume = volume;
    }

    // 获取当前播放的BGM名称
    public string GetCurrentBGMName()
    {
        return currentBgmName;
    }

    // 通用播放方法
    public void PlaySound(AudioClip clip, AudioSource source = null)
    {
        if (clip == null) return;

        source = source ?? vfxSource; // 默认使用vfxSource
        source.clip = clip;
        source.Play();
    }

    // UI音效
    public void PlaybegindragAudio() => PlaySound(begindrag, uiSource);
    public void PlayenddragAudio() => PlaySound(enddrag, uiSource);
    public void PlayclickAudio() => PlaySound(click, uiSource);
    public void PlaychangeturnAudio() => PlaySound(changeturn, uiSource);
    public void PlayoffAudio() => PlaySound(off, uiSource);
    public void PlaynextturnAudio() => PlaySound(next, uiSource);

    // 攻击音效
    public void PlaychangjianAudio() => PlaySound(changjianattack);
    public void PlaygongAudio() => PlaySound(gongattack);
    public void PlaybishouAudio() => PlaySound(bishouattack);
    public void Playattacked() => PlaySound(attacked);
    public void PlayhealAudio() => PlaySound(healed);
    public void PlaymoveAudio() => PlaySound(move);
    public void PlaydoubleAudio() => PlaySound(xdouble);
    public void PlaymassAudio() => PlaySound(xmass);

}