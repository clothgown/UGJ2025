using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

[System.Serializable]
public class CharacterInfo
{
    public int id;                  // 角色ID
    public string characterName;    // 场景中角色名（UnitController 名）
    public string buttonName;       // UI 按钮名（HeadUI 下的按钮）
    public bool isUnlocked;         // 是否已解锁

    public CharacterInfo(int id, string characterName, string buttonName, bool isUnlocked)
    {
        this.id = id;
        this.characterName = characterName;
        this.buttonName = buttonName;
        this.isUnlocked = isUnlocked;
    }
}

public class TeamManager : MonoBehaviour
{
    public static TeamManager instance;

    [Header("HeadUI 下的所有按钮")]
    public List<Button> headUIButtons = new List<Button>();

    [Header("场景中所有 UnitController")]
    public List<UnitController> unitControllers = new List<UnitController>();
    public List<GameObject> windowObjects = new List<GameObject>();
    [Header("角色信息表（手动设定）")]
    public List<CharacterInfo> characterInfos = new List<CharacterInfo>();

    private void Awake()
    {
        // ✅ 单例模式 + 跨场景保持
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded; // 注册场景加载回调
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 首次启动时初始化
        RefreshSceneReferences();
    }

    /// <summary>
    /// 每次场景加载后重新查找 UI 和角色
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSceneReferences();
        RefreshSceneReferences();

        // 判断场景里是否有 TurnManager
        if (FindAnyObjectByType<TurnManager>()==null)
        {
            Debug.Log("⚠️ 场景中没有 TurnManager，尝试寻找同名 Window 组件");

            windowObjects.Clear();
            foreach (var info in characterInfos)
            {
                GameObject window = GameObject.Find(info.buttonName); // 或者 info.windowName，如果你单独加了字段
                if (window != null)
                {
                    window.SetActive(info.isUnlocked);
                    windowObjects.Add(window);
                    Debug.Log($"🌟 记录并设置 Window {window.name} Active={info.isUnlocked}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ 未找到 Window: {info.buttonName}");
                }
            }
        }
    }

    /// <summary>
    /// 重新搜寻 HeadUI 按钮与场景角色
    /// </summary>
    private void RefreshSceneReferences()
    {
        headUIButtons.Clear();
        unitControllers.Clear();

        // 🔹 获取 HeadUI 下所有按钮
        GameObject headUI = GameObject.Find("HeadUI");
        if (headUI != null)
        {
            headUIButtons.AddRange(headUI.GetComponentsInChildren<Button>(true));
        }
        else
        {
            Debug.LogWarning("未找到名为 'HeadUI' 的物体！");
        }

        // 🔹 获取所有 UnitController
        unitControllers.AddRange(FindObjectsOfType<UnitController>(true));

        // 🔹 排序并更新显示状态
        SortAndUpdateStatus();
    }

    /// <summary>
    /// 按角色表排序 + 根据是否解锁更新显示状态
    /// </summary>
    private void SortAndUpdateStatus()
    {
        // 按 ID 排序
        characterInfos.Sort((a, b) => a.id.CompareTo(b.id));

        List<Button> sortedButtons = new List<Button>();
        List<UnitController> sortedUnits = new List<UnitController>();

        foreach (var info in characterInfos)
        {
            // 匹配按钮
            Button matchedButton = headUIButtons.Find(b => b.name == info.buttonName);
            if (matchedButton != null)
            {
                sortedButtons.Add(matchedButton);
                matchedButton.gameObject.SetActive(info.isUnlocked);
                Debug.Log(matchedButton);
            }
            else
            {
                Debug.LogWarning($"未找到按钮：{info.buttonName}");
            }

            // 匹配角色
            UnitController matchedUnit = unitControllers.Find(u => u.name == info.characterName);
            if (matchedUnit != null)
            {
                sortedUnits.Add(matchedUnit);
                matchedUnit.gameObject.SetActive(info.isUnlocked);
            }
            else
            {
                Debug.LogWarning($"未找到角色：{info.characterName}");
            }
        }

        headUIButtons = sortedButtons;
        unitControllers = sortedUnits;

    }

    /// <summary>
    /// 动态解锁角色
    /// </summary>
    public void UnlockCharacter(int id)
    {
        CharacterInfo target = characterInfos.Find(c => c.id == id);
        if (target != null)
        {
            target.isUnlocked = true;
            Debug.Log($"🔓 已解锁角色：{target.characterName}");
            SortAndUpdateStatus();
        }
        else
        {
            Debug.LogWarning($"❌ 未找到ID={id}的角色！");
        }
    }

    private int ExtractNumber(string name)
    {
        Match match = Regex.Match(name, @"\\d+");
        return match.Success ? int.Parse(match.Value) : -1;
    }
}
