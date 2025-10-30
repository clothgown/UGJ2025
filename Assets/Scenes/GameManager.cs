using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SceneBGMConfig
{
    public string sceneName;
    public string bgmName;
    public bool stopOnExit = false;
    public float fadeOutTime = 2f; // 淡出时间
}

[System.Serializable]
public class SceneAmbientConfig
{
    public string sceneName;
    public string ambientName;
    public bool stopOnExit = false;
    public bool loop = true;
    public float fadeInTime = 2f;
    public float fadeOutTime = 2f;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("场景设置")]
    public string mainMenuScene = "MainMenu";
    public string gameScene = "Map";

    [Header("场景BGM配置")]
    [SerializeField] private SceneBGMConfig[] sceneBGMConfigs;
    private Dictionary<string, SceneBGMConfig> sceneBGMDictionary = new Dictionary<string, SceneBGMConfig>();

    [Header("场景环境音配置")]
    [SerializeField] private SceneAmbientConfig[] sceneAmbientConfigs;
    private Dictionary<string, SceneAmbientConfig> sceneAmbientDictionary = new Dictionary<string, SceneAmbientConfig>();

    [Header("UI设置")]
    public GameObject settingsPanel;
    public GameObject loadingPanel;
    public Slider loadingProgressBar;

    [Header("音频设置")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider ambientVolumeSlider;

    [Header("图形设置")]
    public Dropdown qualityDropdown;
    public Dropdown resolutionDropdown;

    private bool isGamePaused = false;
    private string previousSceneName = "";
    private Coroutine currentBGMTransitionCoroutine;
    private Coroutine currentAmbientTransitionCoroutine;

    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化BGM和环境音配置
        InitializeBGMConfig();
        InitializeAmbientConfig();

        // 初始化设置
        InitializeSettings();

        // 监听场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // 取消监听场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        
    }

    #region BGM管理 - 修复平滑过渡
    private void InitializeBGMConfig()
    {
        foreach (SceneBGMConfig config in sceneBGMConfigs)
        {
            if (!string.IsNullOrEmpty(config.sceneName) && !sceneBGMDictionary.ContainsKey(config.sceneName))
            {
                sceneBGMDictionary.Add(config.sceneName, config);
            }
        }
    }

    // 平滑切换BGM
    private void TransitionBGM(string sceneName)
    {
        if (sceneBGMDictionary.ContainsKey(sceneName))
        {
            SceneBGMConfig config = sceneBGMDictionary[sceneName];
            if (!string.IsNullOrEmpty(config.bgmName) && AudioManager.Instance != null)
            {
                // 如果有正在进行的BGM过渡协程，先停止它
                if (currentBGMTransitionCoroutine != null)
                {
                    StopCoroutine(currentBGMTransitionCoroutine);
                }

                // 开始新的BGM过渡
                currentBGMTransitionCoroutine = StartCoroutine(SmoothBGMTransition(config.bgmName, config.fadeOutTime));
                Debug.Log($"场景 {sceneName} 切换BGM: {config.bgmName}");
            }
        }
        else
        {
            Debug.LogWarning($"场景 {sceneName} 没有配置BGM");
        }
    }



    // 平滑BGM过渡协程

    private IEnumerator SmoothBGMTransition(string newBGMName, float fadeOutTime)
    {
        // 如果当前有BGM在播放，先淡出
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(AudioManager.Instance.GetCurrentBGMName()))
        {
            yield return StartCoroutine(FadeOutBGM(fadeOutTime));
        }

        // 播放新的BGM并淡入
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(newBGMName);
        }

        currentBGMTransitionCoroutine = null;
    }

    // BGM淡出协程
    private IEnumerator FadeOutBGM(float fadeTime)
    {
        // 获取当前BGM的音量
        float currentVolume = AudioManager.Instance.GetBGMVolume();
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float volume = Mathf.Lerp(currentVolume, 0f, timer / fadeTime);
            AudioManager.Instance.SetBGMVolume(volume);
            yield return null;
        }

        // 停止BGM
        AudioManager.Instance.StopBGM();
        // 恢复BGM音量设置
        AudioManager.Instance.SetBGMVolume(musicVolumeSlider != null ? musicVolumeSlider.value : 1f);
    }

    // 手动切换BGM（带淡入淡出效果）
    public void SwitchBGM(string bgmName, float fadeOutTime = 2f)
    {
        if (AudioManager.Instance != null)
        {
            if (currentBGMTransitionCoroutine != null)
            {
                StopCoroutine(currentBGMTransitionCoroutine);
            }

            currentBGMTransitionCoroutine = StartCoroutine(SmoothBGMTransition(bgmName, fadeOutTime));
        }
    }

    // 暂停BGM
    public void PauseBGM()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseBGM();
        }
    }

    // 恢复BGM
    public void ResumeBGM()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeBGM();
        }
    }

    // 停止BGM（带淡出效果）
    public void StopBGM(float fadeOutTime = 2f)
    {
        if (AudioManager.Instance != null)
        {
            if (currentBGMTransitionCoroutine != null)
            {
                StopCoroutine(currentBGMTransitionCoroutine);
            }

            currentBGMTransitionCoroutine = StartCoroutine(FadeOutBGM(fadeOutTime));
        }
    }

    // 获取当前场景的BGM配置
    public SceneBGMConfig GetCurrentSceneBGMConfig()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (sceneBGMDictionary.ContainsKey(currentSceneName))
        {
            return sceneBGMDictionary[currentSceneName];
        }
        return null;
    }
    #endregion

    #region 环境音管理
    private void InitializeAmbientConfig()
    {
        foreach (SceneAmbientConfig config in sceneAmbientConfigs)
        {
            if (!string.IsNullOrEmpty(config.sceneName) && !sceneAmbientDictionary.ContainsKey(config.sceneName))
            {
                sceneAmbientDictionary.Add(config.sceneName, config);
            }
        }
    }

    // 播放场景环境音
    private void PlaySceneAmbient(string sceneName)
    {
        if (sceneAmbientDictionary.ContainsKey(sceneName))
        {
            SceneAmbientConfig config = sceneAmbientDictionary[sceneName];
            if (!string.IsNullOrEmpty(config.ambientName) && AudioManager.Instance != null)
            {
                // 如果有正在进行的淡出协程，先停止它
                if (currentAmbientTransitionCoroutine != null)
                {
                    StopCoroutine(currentAmbientTransitionCoroutine);
                }

                // 播放环境音（带淡入效果）
                currentAmbientTransitionCoroutine = StartCoroutine(FadeInAmbient(config.ambientName, config.fadeInTime));
                Debug.Log($"场景 {sceneName} 播放环境音: {config.ambientName}");
            }
        }
    }

    // 停止场景环境音
    private void StopSceneAmbient(string sceneName, bool immediate = false)
    {
        if (sceneAmbientDictionary.ContainsKey(sceneName) && AudioManager.Instance != null)
        {
            SceneAmbientConfig config = sceneAmbientDictionary[sceneName];

            // 如果有正在进行的淡入协程，先停止它
            if (currentAmbientTransitionCoroutine != null)
            {
                StopCoroutine(currentAmbientTransitionCoroutine);
            }

            if (immediate)
            {
                AudioManager.Instance.StopAmbient();
            }
            else
            {
                // 淡出环境音
                currentAmbientTransitionCoroutine = StartCoroutine(FadeOutAmbient(config.fadeOutTime));
            }
        }
    }

    // 环境音淡入协程
    private IEnumerator FadeInAmbient(string ambientName, float fadeTime)
    {
        AudioManager.Instance.PlayAmbient(ambientName);

        float timer = 0f;
        float targetVolume = AudioManager.Instance.GetAmbientVolume();

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float volume = Mathf.Lerp(0f, targetVolume, timer / fadeTime);
            AudioManager.Instance.SetAmbientVolume(volume);
            yield return null;
        }

        AudioManager.Instance.SetAmbientVolume(targetVolume);
        currentAmbientTransitionCoroutine = null;
    }

    // 环境音淡出协程
    private IEnumerator FadeOutAmbient(float fadeTime)
    {
        float startVolume = AudioManager.Instance.GetAmbientVolume();
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float volume = Mathf.Lerp(startVolume, 0f, timer / fadeTime);
            AudioManager.Instance.SetAmbientVolume(volume);
            yield return null;
        }

        AudioManager.Instance.StopAmbient();
        // 恢复环境音音量设置
        AudioManager.Instance.SetAmbientVolume(ambientVolumeSlider != null ? ambientVolumeSlider.value : 5f);
        currentAmbientTransitionCoroutine = null;
    }

    // 手动切换环境音
    public void SwitchAmbient(string ambientName, float fadeInTime = 2f, float fadeOutTime = 2f)
    {
        if (AudioManager.Instance != null)
        {
            if (currentAmbientTransitionCoroutine != null)
            {
                StopCoroutine(currentAmbientTransitionCoroutine);
            }

            currentAmbientTransitionCoroutine = StartCoroutine(SwitchAmbientCoroutine(ambientName, fadeInTime, fadeOutTime));
        }
    }

    private IEnumerator SwitchAmbientCoroutine(string newAmbientName, float fadeInTime, float fadeOutTime)
    {
        // 淡出当前环境音
        yield return FadeOutAmbient(fadeOutTime);

        // 淡入新环境音
        yield return FadeInAmbient(newAmbientName, fadeInTime);
    }

    // 停止环境音
    public void StopAmbient(float fadeOutTime = 2f)
    {
        if (AudioManager.Instance != null)
        {
            if (currentAmbientTransitionCoroutine != null)
            {
                StopCoroutine(currentAmbientTransitionCoroutine);
            }

            currentAmbientTransitionCoroutine = StartCoroutine(FadeOutAmbient(fadeOutTime));
        }
    }

    // 获取当前场景的环境音配置
    public SceneAmbientConfig GetCurrentSceneAmbientConfig()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (sceneAmbientDictionary.ContainsKey(currentSceneName))
        {
            return sceneAmbientDictionary[currentSceneName];
        }
        return null;
    }
    #endregion

    #region 场景管理
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string currentSceneName = scene.name;

        // 处理上一个场景的BGM和环境音
        if (!string.IsNullOrEmpty(previousSceneName))
        {
            // 停止上一个场景的BGM（如果配置了）
            if (sceneBGMDictionary.ContainsKey(previousSceneName) &&
                sceneBGMDictionary[previousSceneName].stopOnExit)
            {
                float fadeOutTime = sceneBGMDictionary[previousSceneName].fadeOutTime;
                StopBGM(fadeOutTime);
            }

            // 停止上一个场景的环境音（如果配置了）
            if (sceneAmbientDictionary.ContainsKey(previousSceneName) &&
                sceneAmbientDictionary[previousSceneName].stopOnExit)
            {
                float fadeOutTime = sceneAmbientDictionary[previousSceneName].fadeOutTime;
                StopAmbient(fadeOutTime);
            }
        }

        // 播放当前场景的BGM（带平滑过渡）
        TransitionBGM(currentSceneName);

        // 播放当前场景的环境音
        PlaySceneAmbient(currentSceneName);

        previousSceneName = currentSceneName;
    }

    public void LoadMainMenu()
    {
        StartCoroutine(LoadSceneAsync(mainMenuScene));
    }

    public void LoadGameScene()
    {
        StartCoroutine(LoadSceneAsync(gameScene));
    }

    public void LoadSceneByName(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    public void RestartCurrentScene()
    {
        StartCoroutine(LoadSceneAsync(SceneManager.GetActiveScene().name));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // 显示加载界面
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingProgressBar != null)
                loadingProgressBar.value = 0;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            

            if (operation.progress >= 0.9f)
            {
                // 等待0.5秒显示100%
                yield return new WaitForSeconds(2.6f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        // 隐藏加载界面
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }
    #endregion

    

    #region 设置管理
    private void InitializeSettings()
    {
        // 加载保存的设置
        LoadSettings();

        // 设置UI事件监听
        SetupUIListeners();
    }

    private void SetupUIListeners()
    {
        // 音频设置
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetBGMVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

        if (ambientVolumeSlider != null)
            ambientVolumeSlider.onValueChanged.AddListener(SetAmbientVolume);

        // 图形设置
        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged.AddListener(SetQualityLevel);
            // 填充质量选项
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
            // 填充分辨率选项
            PopulateResolutions();
        }
    }

    // 音频设置
    public void SetMasterVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(volume);
    }

    public void SetBGMVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBGMVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(volume);
    }

    public void SetAmbientVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetAmbientVolume(volume);
    }

    // 图形设置
    public void SetQualityLevel(int level)
    {
        QualitySettings.SetQualityLevel(level);
    }

    public void SetResolution(int index)
    {
        Resolution[] resolutions = Screen.resolutions;
        if (index >= 0 && index < resolutions.Length)
        {
            Resolution resolution = resolutions[index];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        }
    }

    private void PopulateResolutions()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>();
        int currentResolutionIndex = 0;

        Resolution[] resolutions = Screen.resolutions;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height}";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void ToggleFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    // 保存和加载设置
    private void SaveSettings()
    {
        // 音频设置
        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);

        if (musicVolumeSlider != null)
            PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);

        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);

        if (ambientVolumeSlider != null)
            PlayerPrefs.SetFloat("AmbientVolume", ambientVolumeSlider.value);

        // 图形设置
        if (qualityDropdown != null)
            PlayerPrefs.SetInt("QualityLevel", qualityDropdown.value);

        if (resolutionDropdown != null)
            PlayerPrefs.SetInt("Resolution", resolutionDropdown.value);

        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        // 音频设置
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

        if (ambientVolumeSlider != null)
            ambientVolumeSlider.value = PlayerPrefs.GetFloat("AmbientVolume", 1f);

        // 图形设置
        if (qualityDropdown != null)
            qualityDropdown.value = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());

        if (resolutionDropdown != null)
            resolutionDropdown.value = PlayerPrefs.GetInt("Resolution", 0);

        // 应用设置
        SetMasterVolume(masterVolumeSlider != null ? masterVolumeSlider.value : 1f);
        SetAmbientVolume(ambientVolumeSlider != null ? ambientVolumeSlider.value : 1f);
    }
    #endregion

    #region 退出游戏
    public void QuitGame()
    {
        Debug.Log("退出游戏");

        // 保存设置
        SaveSettings();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void QuitToDesktop()
    {
        // 显示确认对话框
        // 这里可以添加确认对话框逻辑
        QuitGame();
    }
    #endregion
}