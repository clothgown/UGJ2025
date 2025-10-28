using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;

public enum MapGridType 
{ 
    Normal,
    Store,
    Start,
    Boss
}

[System.Serializable]
public class MapGridTypeAndPrefab
{
    public int type;
    public GameObject Prefab;
}
public class MapGridManager : MonoBehaviour
{
    
    public List<string> battleSceneNames = new List<string>();
    public List<string> bigBattleSceneNames = new List<string>();
    public List<string> exploreSceneNames = new List<string>();
    public static MapGridManager instance;
    public MapGrid[] grids;
    public PlayerInMap player;

    public float highlightHeight = 0.5f; // 升高高度
    public float highlightDuration = 0.3f; // 动画时间

    public MapGridTypeAndPrefab[] mapGridTypeAndPrefabs;
    public MapGrid currentGrid;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            grids = FindObjectsOfType<MapGrid>();
            AssignRandomNormalPrefabs();
            PlacePlayerAtStartPos();


            
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AssignRandomNormalPrefabs()
    {
        if (mapGridTypeAndPrefabs == null || mapGridTypeAndPrefabs.Length == 0)
        {
            Debug.LogWarning("MapGridTypeAndPrefabs 列表为空，请在 Inspector 中配置！");
            return;
        }

        // Normal 类型格子列表
        List<MapGrid> normalGrids = new List<MapGrid>();
        foreach (var grid in grids)
        {
            if (grid.gridType == MapGridType.Normal)
                normalGrids.Add(grid);
        }

        if (normalGrids.Count < 14)
        {
            Debug.LogWarning("Normal 格子数量不足 14 个！");
            return;
        }

        // 生成 Normal 类型顺序列表
        // 5 探索(normalType=3)，5 战斗(normalType=0/1)，2 奖励(normalType=2)，2 随机(normalType=0~3)
        List<int> normalTypes = new List<int>();
        normalTypes.AddRange(new int[] { 3, 3, 3, 3, 3 }); // 探索
        normalTypes.AddRange(new int[] { 0, 0, 1, 1, 0 }); // 战斗，可按需要调整比例
        normalTypes.AddRange(new int[] { 2, 2 });          // 奖励
        normalTypes.AddRange(new int[] { Random.Range(0, 4), Random.Range(0, 4) }); // 随机类型

        // 打乱顺序，但保证同类型不会连续两次
        for (int i = 1; i < normalTypes.Count; i++)
        {
            if (normalTypes[i] == normalTypes[i - 1])
            {
                // 找到后面第一个不同类型交换
                for (int j = i + 1; j < normalTypes.Count; j++)
                {
                    if (normalTypes[j] != normalTypes[i - 1])
                    {
                        int tmp = normalTypes[i];
                        normalTypes[i] = normalTypes[j];
                        normalTypes[j] = tmp;
                        break;
                    }
                }
            }
        }

        // 为每个 Normal 格子分配 prefab
        for (int i = 0; i < 14; i++)
        {
            MapGrid grid = normalGrids[i];
            int type = normalTypes[i];
            grid.normalType = type;

            // 从 mapGridTypeAndPrefabs 找对应 type 的 prefab
            MapGridTypeAndPrefab prefabData = System.Array.Find(mapGridTypeAndPrefabs, x => x.type == type);
            if (prefabData != null && prefabData.Prefab != null)
            {
                GameObject instance = Instantiate(prefabData.Prefab, grid.transform);
                instance.SetActive(false); // 初始隐藏
                grid.HiddenPrefab = instance;
            }
        }

        Debug.Log("已为 Normal 格子分配完成符合规则的 Prefab。");
    }

    public void AssignRandomHiddenPrefabs()
    {
        if (mapGridTypeAndPrefabs == null || mapGridTypeAndPrefabs.Length == 0)
        {
            Debug.LogWarning("MapGridTypeAndPrefabs 列表为空，请在 Inspector 中配置！");
            return;
        }

        foreach (var grid in grids)
        {
            if (grid.gridType != (int)MapGridType.Normal)
                continue;
            // 为每个格子随机分配一个Prefab
            int randomIndex = Random.Range(0, mapGridTypeAndPrefabs.Length);
            GameObject prefabToAssign = mapGridTypeAndPrefabs[randomIndex].Prefab;

            if (prefabToAssign == null)
            {
                Debug.LogWarning($"MapGridTypeAndPrefabs[{randomIndex}] 的 Prefab 为空，跳过该格子。");
                continue;
            }
            grid.normalType = randomIndex;
            // 生成隐藏预制体（不激活）
            GameObject instance = Instantiate(prefabToAssign, grid.transform);
            instance.SetActive(false); // 初始隐藏
            grid.HiddenPrefab = instance;
        }

        Debug.Log($"已为 {grids.Length} 个格子随机分配 HiddenPrefab。");
    }


    private void Start()
    {
        HighlightNearbyGrids(); // 游戏开始时高亮
    }

    void PlacePlayerAtStartPos()
    {
        foreach (var grid in grids)
        {
            if (grid.gridPos == player.startPos)
            {
                player.transform.SetParent(grid.transform);
                player.transform.localPosition = Vector3.zero;
                player.transform.localRotation = Quaternion.identity;
                return;
            }
        }
        Debug.LogWarning("没有找到与 Player startPos 对应的格子!");
    }

    public List<MapGrid> lastHighlightedGrids = new List<MapGrid>();

    public void HighlightNearbyGrids()
    {

        // 先让上一轮高亮的格子回到原位
        foreach (var grid in lastHighlightedGrids)
        {
            grid.transform.DOLocalMoveY(grid.originalLocation.y, highlightDuration).SetEase(Ease.OutQuad);
            grid.canSelect = false;
        }
        lastHighlightedGrids.Clear();

        Vector2Int pos = player.currentPos;
        Vector2Int[] directions = new Vector2Int[]
        {
        new Vector2Int(0, 1),  // 上
        new Vector2Int(0, -1), // 下
        new Vector2Int(1, 0),  // 右
        new Vector2Int(-1,0)   // 左
        };

        foreach (var dir in directions)
        {
            Vector2Int targetPos = pos + dir;
            MapGrid grid = GetGridAtPosition(targetPos);
            if (grid != null)
            {
                if(grid.gridPos.x < player.currentPos.x) continue;
                if (grid.isReached) continue;
                //if (grid.isVisited) continue;
                if (grid.gridType != MapGridType.Start)
                {
                    grid.transform.DOLocalMoveY(grid.originalLocation.y + highlightHeight, highlightDuration).SetEase(Ease.OutQuad);
                }
                if(grid.gridType == MapGridType.Normal)
                {
                    grid.SetVisual();
                }
                // 高亮格子
                
                // 保存到当前高亮列表
                lastHighlightedGrids.Add(grid);
                grid.canSelect = true;
                grid.isVisited = true;

            }
        }
    }

    public void ClearHighlight()
    {
        foreach (MapGrid grid in grids)
        {
            grid.canSelect = false;
            
        }
    }

    // 根据 gridPos 获取格子
    MapGrid GetGridAtPosition(Vector2Int pos)
    {
        foreach (var grid in grids)
        {
            if (grid.gridPos == pos)
                return grid;
        }
        return null;
    }

    public void DimPreviousGrids(int currentX)
    {
        foreach (var grid in grids)
        {
            if (grid.gridPos.x < currentX)
            {
                // 获取该格子及其所有子物件的 Image
                Image[] images = grid.GetComponentsInChildren<Image>(true);

                foreach (var img in images)
                {
                    if (img == null) continue;

                    Color c = img.color;
                    // 目标灰色：稍暗一点，而非完全灰
                    Color target = new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, c.a);

                    // 使用 DOTween 平滑变灰（0.5 秒）
                    img.DOColor(target, 0.5f).SetEase(Ease.OutSine);
                }
            }
        }
    }


}
