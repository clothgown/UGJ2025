using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections; // 添加这个命名空间

public class MapGrid : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Vector2Int gridPos;
    private Vector3 originalScale;
    public Vector3 originalLocation;
    public bool isVisited;
    public bool canSelect;
    public bool isReached;
    public GameObject HiddenPrefab;
    public MapGridType gridType;
    public int normalType;
    private Image hiddenImage; // 若 HiddenPrefab 是 UI 对象

    public bool isNextBoss;
    public MapGrid bossGrid;
    
    [Header("场景过渡设置")]
    public GameObject transitionObject; // 在加载场景前要显示的对象
    public float transitionDelay = 2f; // 等待时间（秒）


    private void Awake()
    {
        originalScale = transform.localScale;
        originalLocation = transform.localPosition;
    }

    private void Start()
    {
        if (HiddenPrefab != null)
        {
            hiddenImage = HiddenPrefab.GetComponent<Image>();
            if (hiddenImage != null)
            {
                var color = hiddenImage.color;
                color.a = 0f;
                hiddenImage.color = color;
                HiddenPrefab.SetActive(true);
            }
            else
            {
                HiddenPrefab.SetActive(false);
            }
        }
        
        // 初始化过渡对象为隐藏状态
        if (transitionObject != null)
        {
            transitionObject.SetActive(false);
        }
    }

    // 鼠标悬停
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 可选：悬停动画
        // transform.DOScale(originalScale * 1.1f, 0.2f).SetEase(Ease.OutBack);
    }

    // 鼠标离开
    public void OnPointerExit(PointerEventData eventData)
    {
        // transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutBack);
    }

    // 点击格子
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isReached) return;
        PlayerInMap player = MapGridManager.instance.player;
        if (player == null || canSelect == false) return;

        // 先脱离父物件
        player.transform.SetParent(player.transform.parent.parent);

        // 移动动画
        player.transform.DOMove(transform.position, 0.5f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                player.transform.SetParent(transform);
                player.transform.localPosition = Vector3.zero;
                player.transform.localRotation = Quaternion.identity;
                player.currentPos = gridPos;
                MapGridManager.instance.currentGrid = this;
                
                string sceneToLoad = GetSceneToLoad();
                if (!string.IsNullOrEmpty(sceneToLoad))
                {
                    // 使用过渡效果加载场景
                    StartCoroutine(LoadSceneWithTransition(sceneToLoad));
                }
                else
                {
                    ShowNextGrids();
                }
            });
    }
    
    // 使用过渡效果加载场景
    private IEnumerator LoadSceneWithTransition(string sceneName)
    {
        // 显示过渡对象
        if (transitionObject != null)
        {
            transitionObject.SetActive(true);
            
            // 可选：添加淡入动画
            CanvasGroup canvasGroup = transitionObject.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.5f).SetEase(Ease.OutQuad);
            }
            
            Debug.Log($"显示过渡对象，等待 {transitionDelay} 秒");
        }
        
        // 等待指定时间
        yield return new WaitForSeconds(transitionDelay);
        
        // 加载场景
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneName);
    }

    private string GetSceneToLoad()
    {
        if (gridType == MapGridType.Store)
        {
            return "Store";
        }
        else if (normalType == 0 && gridType == MapGridType.Normal)
        {
            if (MapGridManager.instance.battleSceneNames != null && MapGridManager.instance.battleSceneNames.Count > 0)
            {
                int randomIndex = Random.Range(0, MapGridManager.instance.battleSceneNames.Count);
                return MapGridManager.instance.battleSceneNames[randomIndex];
            }
        }
        else if (normalType == 1 && gridType == MapGridType.Normal)
        {
            if (MapGridManager.instance.bigBattleSceneNames != null && MapGridManager.instance.bigBattleSceneNames.Count > 0)
            {
                int randomIndex = Random.Range(0, MapGridManager.instance.bigBattleSceneNames.Count);
                return MapGridManager.instance.bigBattleSceneNames[randomIndex];
            }
        }
        else if (normalType == 2)
        {
            return "choose_baoxiang";
        }
        else if (normalType == 3)
        {
            if (MapGridManager.instance.exploreSceneNames != null && MapGridManager.instance.exploreSceneNames.Count > 0)
            {
                int randomIndex = Random.Range(0, MapGridManager.instance.exploreSceneNames.Count);
                return MapGridManager.instance.exploreSceneNames[randomIndex];
            }
        }
        
        return null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 加载完成后再关闭地图
        if (MapGridManager.instance != null)
        {
            MapGridManager.instance.gameObject.SetActive(false);
        }

        // 移除事件防止重复触发
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void ShowNextGrids()
    {
        // 显示隐藏内容
        SetVisual();
        MapGridManager.instance.DimPreviousGrids(gridPos.x);
        if (isNextBoss != true)
        {
            MapGridManager.instance.HighlightNearbyGrids();
        }
        else
        {
            foreach (var grid in MapGridManager.instance.grids)
            {
                grid.transform.DOLocalMoveY(grid.originalLocation.y, MapGridManager.instance.highlightDuration).SetEase(Ease.OutQuad);
                grid.canSelect = false;
            }
            bossGrid.canSelect = true;
            bossGrid.transform.DOLocalMoveY(bossGrid.originalLocation.y + MapGridManager.instance.highlightHeight, MapGridManager.instance.highlightDuration).SetEase(Ease.OutQuad);
        }
        isReached = true;
    }

    /// <summary>
    /// 根据类型显示格子的视觉效果
    /// </summary>
    public void SetVisual()
    {
        var player = MapGridManager.instance?.player;
        if (player != null && player.currentPos == gridPos)
        {
            return;
        }

        Image selfImage = GetComponent<Image>();              // 当前格子的 UI Image
        Image hiddenImage = HiddenPrefab?.GetComponent<Image>(); // 隐藏物件的 UI Image

        if (hiddenImage != null)
        {
            // 确保初始状态
            Color hc = hiddenImage.color;
            hc.a = 0f;
            hiddenImage.color = hc;
            HiddenPrefab.SetActive(true);

            // 同时进行淡出和淡入动画
            float duration = 0.8f;
            hiddenImage.DOFade(1f, duration).SetEase(Ease.OutQuad);
            if (selfImage != null)
                selfImage.DOFade(0f, duration).SetEase(Ease.OutQuad);
        }
    }
}