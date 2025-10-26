using System.Collections.Generic;
using UnityEngine;

public class AllPlayerState : MonoBehaviour
{
    public static AllPlayerState Instance;  // 单例，全局访问

    [Header("记录的角色名字和血量")]
    public List<string> unitNames = new List<string>();   // 保存名字
    public List<float> unitHealths = new List<float>();   // 保存血量

    private List<UnitController> allUnits = new List<UnitController>();

    private void Awake()
    {
        // 确保只保留一个实例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景不销毁
        }
        else
        {
            Destroy(gameObject);
            return;
        }

    }

    private void Start()
    {
        // 查找场景中的所有 UnitController
        allUnits.AddRange(FindObjectsOfType<UnitController>());


    }

    private void Update()
    {

    }

    public void UpdateUnitStates()
    {
        unitNames.Clear();
        unitHealths.Clear();

        foreach (var unit in allUnits)
        {
            if (unit == null) continue;

            string name = string.IsNullOrEmpty(unit.characterName) ? unit.name : unit.characterName;
            float health = unit.currentHealth;

            unitNames.Add(name);
            unitHealths.Add(health);
        }

        Debug.Log("=== 所有Unit状态已更新 ===");
        for (int i = 0; i < unitNames.Count; i++)
        {
            Debug.Log($"{unitNames[i]} | 血量：{unitHealths[i]}");
        }
    }

    /// <summary>
    /// 根据名字获取对应单位的血量
    /// </summary>
    public float GetHealthByName(string unitName)
    {
        int index = unitNames.IndexOf(unitName);
        if (index >= 0 && index < unitHealths.Count)
            return unitHealths[index];
        else
            return -1f; // 没找到
    }
}
