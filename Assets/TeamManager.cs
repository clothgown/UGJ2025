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

    // ✅ 新增字段
    public float currentHealth;     // 当前血量
    public float maxHealth;         // 最大血量

    public CharacterInfo(int id, string characterName, string buttonName, bool isUnlocked, float maxHealth = 100f)
    {
        this.id = id;
        this.characterName = characterName;
        this.buttonName = buttonName;
        this.isUnlocked = isUnlocked;
        this.maxHealth = maxHealth;
        this.currentHealth = maxHealth; // 默认满血
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
    public void SortAndUpdateStatus()
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
                Debug.Log($"✅ 找到按钮：{matchedButton.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ 未找到按钮：{info.buttonName}");
            }

            // 匹配角色
            UnitController matchedUnit = unitControllers.Find(u => u.name == info.characterName);
            if (matchedUnit != null)
            {
                sortedUnits.Add(matchedUnit);
                matchedUnit.gameObject.SetActive(info.isUnlocked);

                // ✅ 同步角色的血量信息
                if (matchedUnit.healthSystem != null)
                {
                    info.currentHealth = matchedUnit.currentHealth;
                    info.maxHealth = matchedUnit.maxHealth;
                    Debug.Log($"🩸 记录角色 {info.characterName} 血量：{info.currentHealth}/{info.maxHealth}");
                }
                else if (matchedUnit.healthSystem == null)
                {
                    // 如果角色还没初始化血量系统，尝试用 UnitController 自身的 currentHealth
                    info.currentHealth = matchedUnit.currentHealth;
                    info.maxHealth = matchedUnit.maxHealth;
                    Debug.Log($"🩸（备用）记录角色 {info.characterName} 血量：{info.currentHealth}/{info.maxHealth}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ 未找到角色：{info.characterName}");
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

    /// <summary>
    /// 获取指定角色当前血量
    /// </summary>
    /// <param name="characterName">角色在场景中的名称（UnitController 名）</param>
    /// <returns>当前血量，如果未找到则返回 -1</returns>
    public float GetCharacterHealth(string characterName)
    {
        CharacterInfo info = characterInfos.Find(c => c.characterName == characterName);
        if (info != null)
        {
            Debug.Log($"🔍 获取 {characterName} 当前血量：{info.currentHealth}");
            return info.currentHealth;
        }
        else
        {
            Debug.LogWarning($"⚠️ 未找到角色：{characterName}");
            return -1f;
        }
    }

    /// <summary>
    /// 获取指定角色的当前血量与最大血量
    /// </summary>
    /// <param name="characterName">角色名</param>
    /// <returns>(current, max)，未找到返回(-1, -1)</returns>
    public (float current, float max) GetCharacterHealthInfo(string characterName)
    {
        CharacterInfo info = characterInfos.Find(c => c.characterName == characterName);
        if (info != null)
        {
            Debug.Log($"🔍 获取 {characterName} 血量信息：{info.currentHealth}/{info.maxHealth}");
            return (info.currentHealth, info.maxHealth);
        }
        else
        {
            Debug.LogWarning($"⚠️ 未找到角色：{characterName}");
            return (-1f, -1f);
        }
    }
    /// <summary>
    /// 从当前场景的 UnitController 同步血量到 CharacterInfos
    /// （每次回合开始调用）
    /// </summary>
    public void RefreshCharacterHealthFromScene()
    {
        foreach (var info in characterInfos)
        {
            var unit = unitControllers.Find(u => u.name == info.characterName);
            if (unit != null && unit.healthSystem != null)
            {
                info.currentHealth = unit.currentHealth;
                info.maxHealth = unit.maxHealth;
                Debug.Log($"🔁 刷新角色血量：{info.characterName}  {info.currentHealth}/{info.maxHealth}");
            }
            else
            {
                Debug.LogWarning($"⚠️ 未找到 {info.characterName} 或其 HealthSystem，无法同步血量。");
            }
        }
    }
}
