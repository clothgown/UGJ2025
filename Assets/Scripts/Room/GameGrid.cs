using DG.Tweening;   // 引入 DOTween 命名空间
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum GridState
{
    None,   // 普通格子
    Water,  // 水格子，不可行走
    Oil     // 油格子（可做特殊效果）
}

public class GameGrid : MonoBehaviour
{
    public Vector2Int gridPos;
    public SpriteRenderer rend;
    private Color originalColor;
    public Color hoverColor = Color.green;
    public Color moveRangeColor = new Color(1f, 0.5f, 0f, 0.5f); // 橙色
    public bool isInRange = false;
    public bool canChangeState = false;


    public SpriteRenderer selectGrid;
    public bool isAttackTarget = false;
    public bool isOccupied = false;

    public UnitController occupiedPlayer;
    public EnemyUnit currentEnemy;
    public bool isInterable = false;

    public Vector3 playerOriginalScale;
    public int sortingOrder;

    // ✅ 新增部分：格子状态管理
    [Header("Grid State Settings")]
    public GridState currentState = GridState.None;

    // 三种状态颜色（你可以在 Inspector 调整）
    public Color normalColor = Color.white;
    public Color waterColor = new Color(0f, 0.5f, 1f, 0.8f);
    public Color oilColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);

    private void Awake()
    {
        rend = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        sortingOrder = gridPos.x + gridPos.y;
        if (selectGrid != null)
            selectGrid.sortingOrder = -sortingOrder + 1;
        originalColor = rend.color;
        selectGrid.enabled = false;

        // 初始化时刷新一次外观
        UpdateGridAppearance();
    }

    void OnMouseEnter()
    {
        selectGrid.enabled = true;
        IsoGrid2D.instance.currentSelectedGrid = this;

        if (occupiedPlayer != null)
        {
            Transform playerTransform = occupiedPlayer.transform;
            if (playerOriginalScale == Vector3.zero)
                playerOriginalScale = playerTransform.localScale;

            playerTransform.DOKill();
            playerTransform.localScale = playerOriginalScale;
            playerTransform.DOScale(playerOriginalScale * 1.1f, 0.1f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad);
        }
    }

    void OnMouseExit()
    {
        selectGrid.enabled = false;
        IsoGrid2D.instance.currentSelectedGrid = null;
    }
    public void SetColor(Color color) => rend.color = color;
    public void ResetColor() => rend.color = originalColor;
    void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // 点击水格子无效
        if (currentState == GridState.Water)
        {
            Debug.Log("这是水格子，无法移动或交互。");
            return;
        }

        if (occupiedPlayer != null)
        {
            TurnManager.instance.ChangePlayer(occupiedPlayer);
        }

        NormalGridClick();
    }

    void NormalGridClick()
    {
        UnitController playerController = IsoGrid2D.instance.controller.GetComponent<UnitController>();

        if (isInRange)
        {
            playerController.MoveToGrid(this);
            IsoGrid2D.instance.ResetWaiting();
            return;
        }

        if (isAttackTarget)
        {
            if (playerController.isNextAttackDizziness)
            {
                currentEnemy.Dizziness();
                playerController.Attack(this);
                playerController.isNextAttackDizziness = false;
            }
            else if (playerController.isNextAttackMultiple)
            {
                StartCoroutine(AttackMultiple());
                playerController.isNextAttackMultiple = false;
            }
            else if (playerController.isNextAttackPull)
            {
                playerController.Attack(this);
                currentEnemy.BePulled(playerController.currentGridPos, playerController.PullDistance);
                playerController.PullDistance = 0;
                playerController.isNextAttackPull = false;
            }
            else
            {
                playerController.Attack(this);
            }

            FindAnyObjectByType<HorizontalCardHolder>().DrawCardAndUpdate();
            IsoGrid2D.instance.ResetWaiting();
        }
    }

    private IEnumerator AttackMultiple()
    {
        var unitController = IsoGrid2D.instance.controller.GetComponent<UnitController>();
        for (int i = 0; i < unitController.SegmentCount; i++)
        {
            unitController.Attack(this);
            yield return new WaitForSeconds(0.2f);
        }
    }

    // 外部调用设置格子状态
    public void SetState(GridState newState)
    {
        currentState = newState;
        UpdateGridAppearance();
    }

    // 根据状态刷新外观
    public void UpdateGridAppearance()
    {
        switch (currentState)
        {
            case GridState.None:
                rend.color = normalColor;
                break;
            case GridState.Water:
                rend.color = waterColor;
                break;
            case GridState.Oil:
                rend.color = oilColor;
                break;
        }
    }
}
