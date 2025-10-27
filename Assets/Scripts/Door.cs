using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour
{
    [Tooltip("门所在的格子坐标（可以有多个）")]
    public Vector2Int[] gameGridsPos;

    [Header("场景设置")]
    public string targetSceneName = "Map";
    public int targetSceneIndex = -1;

    [Header("UI设置")]
    public GameObject YNBackPanel;

    private UnitController[] allUnits;
    private bool hasTriggered = false;

    private void Start()
    {
        allUnits = FindObjectsOfType<UnitController>();

        // 初始化格子
        GameGrid[] allGrids = FindObjectsOfType<GameGrid>();
        foreach (var grid in allGrids)
        {
            foreach (var pos in gameGridsPos)
            {
                if (grid.gridPos == pos)
                {
                    grid.isLeaveGrid = true;
                    grid.normalColor = grid.LeaveColor;
                    grid.UpdateGridAppearance();
                    break;
                }
            }
        }

        // 初始化UI
        if (YNBackPanel != null)
        {
            YNBackPanel.SetActive(false);
        }

        // 打印场景信息用于调试
        Debug.Log("当前Build Settings中的场景:");
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneManager.GetSceneByBuildIndex(i).path;
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            Debug.Log($" - [{i}] {sceneName}");
        }
    }

    void Update()
    {
        if (hasTriggered) return;

        foreach (var unit in allUnits)
        {
            if (unit == null) continue;

            foreach (var gridPos in gameGridsPos)
            {
                if (unit.currentGridPos == gridPos)
                {
                    OnAnyUnitReachDoor(unit);
                    return;
                }
            }
        }
    }

    private void OnAnyUnitReachDoor(UnitController unit)
    {
        hasTriggered = true;
        Debug.Log($"单位 {unit.name} 到达门格子");
        ShowYNBackPanel();
        Time.timeScale = 0f;
    }

    private void ShowYNBackPanel()
    {
        if (YNBackPanel != null)
        {
            YNBackPanel.SetActive(true);
            SetupPanelButtons();
        }
        else
        {
            Debug.LogError("YNBackPanel 未设置！");
            // 如果没有UI，直接跳转
            LoadTargetScene();
        }
    }

    private void SetupPanelButtons()
    {
        // 根据你的实际按钮名称修改这里
        var confirmButton = YNBackPanel.transform.Find("ConfirmButton")?.GetComponent<UnityEngine.UI.Button>();
        var cancelButton = YNBackPanel.transform.Find("CancelButton")?.GetComponent<UnityEngine.UI.Button>();

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmLeave);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelLeave);
        }
    }

    public void OnConfirmLeave()
    {
        Debug.Log("确认离开");
        Time.timeScale = 1f;
        LoadTargetScene();
    }

    public void OnCancelLeave()
    {
        Debug.Log("取消离开");
        if (YNBackPanel != null) YNBackPanel.SetActive(false);
        Time.timeScale = 1f;
        ResetTrigger();
    }

    private void LoadTargetScene()
    {
        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        Debug.Log("开始异步加载场景");

        // 确保时间尺度重置
        Time.timeScale = 1f;

        // 显示加载界面（如果有）
        // ShowLoadingScreen();

        AsyncOperation asyncLoad;

        if (targetSceneIndex >= 0)
        {
            asyncLoad = SceneManager.LoadSceneAsync(targetSceneIndex);
        }
        else
        {
            asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        }

        asyncLoad.allowSceneActivation = false;

        // 等待加载完成
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            Debug.Log($"场景加载进度: {progress * 100}%");

            // 更新加载界面进度条
            // UpdateLoadingProgress(progress);

            if (asyncLoad.progress >= 0.9f)
            {
                // 等待一帧确保所有资源加载完成
                yield return new WaitForSeconds(0.5f);

                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        Debug.Log("场景加载完成");

        // 隐藏加载界面
        // HideLoadingScreen();

        // 强制刷新场景
        yield return new WaitForEndOfFrame();

        // 手动调用一些初始化方法
        InitializeLoadedScene();
    }

    private void InitializeLoadedScene()
    {
        Debug.Log("手动初始化加载的场景");

        // 查找并激活所有可能被禁用的重要对象
        ReactivateCriticalObjects();


        // 特别确保主相机存在并激活
        EnsureMainCamera();

        // 强制刷新渲染
        RefreshRendering();
    }

    private void EnsureMainCamera()
    {
        // 查找场景中的主相机
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("未找到主相机，尝试查找所有相机");

            // 查找所有相机
            Camera[] allCameras = FindObjectsOfType<Camera>();

            if (allCameras.Length > 0)
            {
                // 激活第一个找到的相机并设置为主相机
                mainCamera = allCameras[0];
                mainCamera.tag = "MainCamera";
                mainCamera.gameObject.SetActive(true);
                Debug.Log($"已激活相机: {mainCamera.name}");
            }
            else
            {
                Debug.LogError("场景中未找到任何相机！");
                // 可以在这里创建一个新的相机
                CreateNewMainCamera();
            }
        }
        else
        {
            Debug.Log($"找到主相机: {mainCamera.name}");
            mainCamera.gameObject.SetActive(true);
        }

        // 确保音频监听器存在
        EnsureAudioListener();
    }

    private void CreateNewMainCamera()
    {
        Debug.Log("创建新的主相机");
        GameObject cameraObject = new GameObject("MainCamera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();

        // 设置相机的基本属性
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.blue;
    }

    private void EnsureAudioListener()
    {
        // 确保有且只有一个音频监听器
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length == 0)
        {
            Camera.main.gameObject.AddComponent<AudioListener>();
        }
        else if (listeners.Length > 1)
        {
            // 只保留第一个，禁用其他的
            for (int i = 1; i < listeners.Length; i++)
            {
                listeners[i].enabled = false;
            }
        }
    }

    private void RefreshRendering()
    {
        // 强制刷新渲染
        foreach (var cam in FindObjectsOfType<Camera>())
        {
            cam.Render();
        }
    }

    private void ReactivateCriticalObjects()
    {
        // 激活所有被标记为重要的对象
        GameObject[] criticalObjects = GameObject.FindGameObjectsWithTag("Essential");
        foreach (GameObject obj in criticalObjects)
        {
            obj.SetActive(true);
        }

        // 确保事件系统存在
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem").AddComponent<UnityEngine.EventSystems.EventSystem>()
                                        .AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    

    

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}