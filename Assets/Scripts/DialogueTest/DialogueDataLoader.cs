using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DialogueDataLoader : MonoBehaviour
{
    [System.Serializable]
    public class TableFileMapping
    {
        public string fileName;
        public DialogueType dialogueType;
    }

    [Header("数据文件配置")]
    public List<TableFileMapping> tableMappings = new List<TableFileMapping>
    {
        new TableFileMapping { fileName = "30000-dialogue-关卡内-战斗后.csv", dialogueType = DialogueType.Battle },
        new TableFileMapping { fileName = "20000-dialogue-关卡内-探索.csv", dialogueType = DialogueType.Explore },
        new TableFileMapping { fileName = "0-dialogue-关卡内S&E.csv", dialogueType = DialogueType.Normal },
        new TableFileMapping { fileName = "10000-dialogue-关卡外.csv", dialogueType = DialogueType.Outside },
        new TableFileMapping { fileName = "50000-dialogue-dead.csv", dialogueType = DialogueType.Dead }
    };

    [Header("数据存储路径")]
    public string dataFolderPath = "DialogueData";

    [Header("调试选项")]
    public bool debugMode = true;
    public bool autoLoadOnStart = true;

    // 存储所有对话节点的字典
    private Dictionary<int, DialogueNode> _allNodes = new Dictionary<int, DialogueNode>();
    public Dictionary<int, DialogueNode> AllNodes => _allNodes;

    // 事件：数据加载完成
    public event Action OnDataLoaded;

    void Start()
    {
        if (autoLoadOnStart)
        {
            LoadAllDialogueData();
        }
    }

    /// <summary>
    /// 加载所有对话数据
    /// </summary>
    public void LoadAllDialogueData()
    {
        _allNodes.Clear();

        int totalLoaded = 0;
        foreach (var mapping in tableMappings)
        {
            int count = LoadTableData(mapping.fileName, mapping.dialogueType);
            totalLoaded += count;

            if (debugMode)
            {
                Debug.Log($"已加载 {mapping.fileName}: {count} 个节点");
            }
        }

        if (debugMode)
        {
            Debug.Log($"对话数据加载完成！总计: {totalLoaded} 个节点");
        }

        OnDataLoaded?.Invoke();
    }

    /// <summary>
    /// 加载单个表格数据
    /// </summary>
    private int LoadTableData(string fileName, DialogueType tableType)
    {
        int loadedCount = 0;

        try
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, dataFolderPath, fileName);

            if (!File.Exists(filePath))
            {
                // 如果在StreamingAssets中找不到，尝试Resources文件夹
                string resourcePath = Path.Combine(dataFolderPath, Path.GetFileNameWithoutExtension(fileName));
                TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);

                if (textAsset != null)
                {
                    ProcessCSVData(textAsset.text, tableType, ref loadedCount);
                }
                else
                {
                    Debug.LogWarning($"找不到对话文件: {fileName}");
                    return 0;
                }
            }
            else
            {
                string csvData = File.ReadAllText(filePath);
                ProcessCSVData(csvData, tableType, ref loadedCount);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"加载表格数据失败: {fileName}, 错误: {e.Message}");
        }

        return loadedCount;
    }

    /// <summary>
    /// 处理CSV数据
    /// </summary>
    private void ProcessCSVData(string csvData, DialogueType tableType, ref int loadedCount)
    {
        string[] lines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            Debug.LogWarning($"CSV文件数据不足: {tableType}");
            return;
        }

        // 解析表头
        string[] headers = ParseCSVLine(lines[0]);

        // 处理数据行
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = ParseCSVLine(lines[i]);

            // 创建数据字典
            Dictionary<string, string> dataDict = new Dictionary<string, string>();
            for (int j = 0; j < Mathf.Min(headers.Length, values.Length); j++)
            {
                string header = headers[j].Trim();
                string value = values[j].Trim();

                if (!string.IsNullOrEmpty(header) && !dataDict.ContainsKey(header))
                {
                    dataDict.Add(header, value);
                }
            }

            // 创建对话节点
            DialogueNode node = DialogueNode.CreateFromDictionary(dataDict, tableType);

            if (node != null && node.ID > 0)
            {
                if (_allNodes.ContainsKey(node.ID))
                {
                    Debug.LogWarning($"重复的对话ID: {node.ID}，在表格 {tableType}，将被覆盖");
                    _allNodes[node.ID] = node;
                }
                else
                {
                    _allNodes.Add(node.ID, node);
                }
                loadedCount++;
            }
        }
    }

    /// <summary>
    /// 解析CSV行（处理逗号在引号内的情况）
    /// </summary>
    private string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        // 添加最后一个字段
        result.Add(currentField);

        return result.ToArray();
    }

    /// <summary>
    /// 根据ID获取对话节点
    /// </summary>
    public DialogueNode GetNode(int id)
    {
        if (_allNodes.ContainsKey(id))
        {
            return _allNodes[id];
        }

        if (debugMode)
        {
            Debug.LogWarning($"找不到对话节点: {id}");
        }
        return null;
    }

    /// <summary>
    /// 根据关卡获取所有相关节点
    /// </summary>
    public List<DialogueNode> GetNodesByLevel(string level)
    {
        List<DialogueNode> result = new List<DialogueNode>();

        foreach (var node in _allNodes.Values)
        {
            if (node.Level == level)
            {
                result.Add(node);
            }
        }

        return result;
    }

    /// <summary>
    /// 根据类型获取所有节点
    /// </summary>
    public List<DialogueNode> GetNodesByType(DialogueType type)
    {
        List<DialogueNode> result = new List<DialogueNode>();

        foreach (var node in _allNodes.Values)
        {
            if (node.TableType == type)
            {
                result.Add(node);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取起始节点（ID为0的节点）
    /// </summary>
    public DialogueNode GetStartNode()
    {
        return GetNode(0);
    }

    /// <summary>
    /// 获取下一个可用的节点ID（用于编辑器扩展）
    /// </summary>
    public int GetNextAvailableID()
    {
        int maxID = 0;
        foreach (int id in _allNodes.Keys)
        {
            if (id > maxID) maxID = id;
        }
        return maxID + 1;
    }

    /// <summary>
    /// 检查节点是否存在
    /// </summary>
    public bool NodeExists(int id)
    {
        return _allNodes.ContainsKey(id);
    }

    /// <summary>
    /// 手动添加节点（用于编辑器扩展）
    /// </summary>
    public void AddNode(DialogueNode node)
    {
        if (node == null) return;

        if (_allNodes.ContainsKey(node.ID))
        {
            Debug.LogWarning($"节点ID {node.ID} 已存在，将被覆盖");
            _allNodes[node.ID] = node;
        }
        else
        {
            _allNodes.Add(node.ID, node);
        }
    }

    /// <summary>
    /// 导出数据到JSON（用于调试或数据备份）
    /// </summary>
    public string ExportToJSON()
    {
        List<DialogueNode> nodesList = new List<DialogueNode>(_allNodes.Values);

        // 由于Unity的JsonUtility不能直接序列化字典，我们使用列表
        Wrapper wrapper = new Wrapper { nodes = nodesList };
        return JsonUtility.ToJson(wrapper, true);
    }

    [System.Serializable]
    private class Wrapper
    {
        public List<DialogueNode> nodes;
    }

    /// <summary>
    /// 从JSON导入数据（用于调试或数据恢复）
    /// </summary>
    public void ImportFromJSON(string jsonData)
    {
        try
        {
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(jsonData);
            _allNodes.Clear();

            foreach (var node in wrapper.nodes)
            {
                _allNodes.Add(node.ID, node);
            }

            if (debugMode)
            {
                Debug.Log($"从JSON导入 {_allNodes.Count} 个节点");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON导入失败: {e.Message}");
        }
    }

    /// <summary>
    /// 获取统计数据
    /// </summary>
    public void GetStatistics(out int totalNodes, out Dictionary<DialogueType, int> typeCounts)
    {
        totalNodes = _allNodes.Count;
        typeCounts = new Dictionary<DialogueType, int>();

        foreach (var node in _allNodes.Values)
        {
            if (typeCounts.ContainsKey(node.TableType))
            {
                typeCounts[node.TableType]++;
            }
            else
            {
                typeCounts.Add(node.TableType, 1);
            }
        }
    }
}