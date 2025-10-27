using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("场景设置")]
    public string mainMenuScene = "MainMenu";
    public string gameScene = "Map";

    [Header("UI设置")]
    public GameObject settingsPanel;
    public GameObject loadingPanel;
    public Slider loadingProgressBar;

    [Header("音频设置")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("图形设置")]
    public Dropdown qualityDropdown;
    public Dropdown resolutionDropdown;

    private bool isGamePaused = false;

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

        // 初始化设置
        InitializeSettings();
    }

    void Update()
    {
        // ESC键打开/关闭设置界面
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    #region 场景管理
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
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            // 更新进度条
            if (loadingProgressBar != null)
                loadingProgressBar.value = progress;

            if (operation.progress >= 0.9f)
            {
                // 等待1秒显示100%
                yield return new WaitForSeconds(0.5f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        // 隐藏加载界面
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }
    #endregion

    #region 设置界面管理
    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            bool newState = !settingsPanel.activeSelf;
            settingsPanel.SetActive(newState);

            // 暂停/恢复游戏
            if (newState)
            {
                PauseGame();
            }
            else
            {
                ResumeGame();
            }
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            PauseGame();
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            ResumeGame();
            SaveSettings();
        }
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
        isGamePaused = true;
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
        isGamePaused = false;
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
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

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
        AudioListener.volume = volume;
    }

    public void SetMusicVolume(float volume)
    {
        // 这里需要你根据音频系统调整
        // 例如：AudioManager.Instance.SetMusicVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        // 这里需要你根据音频系统调整
        // 例如：AudioManager.Instance.SetSFXVolume(volume);
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

        // 图形设置
        if (qualityDropdown != null)
            qualityDropdown.value = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());

        if (resolutionDropdown != null)
            resolutionDropdown.value = PlayerPrefs.GetInt("Resolution", 0);

        // 应用设置
        SetMasterVolume(masterVolumeSlider != null ? masterVolumeSlider.value : 1f);
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