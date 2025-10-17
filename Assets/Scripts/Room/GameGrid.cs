using DG.Tweening;   // 引入 DOTween 命名空间
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class GameGrid : MonoBehaviour
{
    public Vector2Int gridPos;
    public SpriteRenderer rend;
    public Color originalColor;
    public Color hoverColor = Color.green;
    public Color moveRangeColor = new Color(1f, 0.5f, 0f, 0.5f); // 橙色
    public bool isInRange = false;
    public bool canChangeState = false;


    public SpriteRenderer selectGrid;
    public bool isAttackTarget = false;
    public bool isOccupied = false;
    public bool canHeal = false;
    public UnitController occupiedPlayer;
    public EnemyUnit currentEnemy;
    public bool isInterable = false;

    public Vector3 playerOriginalScale;
    public int sortingOrder;

    // ✅ 新增部分：格子状态管理
    [Header("Grid State Settings")]
    public GridState currentState = GridState.None;

    // 三种状态颜色（你可以在 Inspector 调整）
    public SpriteRenderer stateGrid;
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
            selectGrid.sortingOrder = -sortingOrder +1;
        if (stateGrid != null)
            stateGrid.sortingOrder = -501;
        originalColor = rend.color;
        selectGrid.enabled = false;
        stateGrid.enabled = false;
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

        if (canChangeState)
        {
            var newState = IsoGrid2D.instance.gridStateToChange;

            // 如果目标状态是None，不做任何事
            if (newState == GridState.None)
            {
                Debug.Log("当前未选择任何格子状态。");
                return;
            }

            // 改变格子状态
            SetState(newState);



            // 播放动画反馈
            transform.DOPunchScale(Vector3.one * 0.1f, 0.25f, 6, 0.6f);

            Debug.Log($"格子 {gridPos} 状态已改为：{newState}");

            // 重置全局状态
            IsoGrid2D.instance.gridStateToChange = GridState.None;



            FindAnyObjectByType<HorizontalCardHolder>().DrawCardAndUpdate();
            IsoGrid2D.instance.ResetWaiting();
            return; // 提前结束
        }

        if (canHeal)
        {
            UnitController playerController = IsoGrid2D.instance.controller.GetComponent<UnitController>();
            if (this.occupiedPlayer != null)
            {
                
                if (playerController.isNextAttackMass)
                {
                    playerController.RecoverState();
                    FindAnyObjectByType<HorizontalCardHolder>().ChangeAllCardToNormal();

                    IsoGrid2D.instance.DealMassHeal(playerController.healPoint);
                    FindAnyObjectByType<HorizontalCardHolder>().DrawCardAndUpdate();
                    IsoGrid2D.instance.ResetWaiting();
                    return;
                }
                else
                {
                    UnitController playerToHeal = this.occupiedPlayer;
                    playerToHeal.Heal(playerController.healPoint);
                    FindAnyObjectByType<HorizontalCardHolder>().DrawCardAndUpdate();
                    IsoGrid2D.instance.ResetWaiting();
                }
                    

            }
        }
        else if (occupiedPlayer != null)
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
            if (playerController.isNextAttackDouble)
            {
                playerController.attackDamage *= 2;
                playerController.RecoverState();
                FindAnyObjectByType<HorizontalCardHolder>().ChangeAllCardToNormal();
            }

            if(playerController.isNextAttackMass)
            {
                playerController.RecoverState();
                FindAnyObjectByType<HorizontalCardHolder>().ChangeAllCardToNormal();
                IsoGrid2D.instance.DealMassAttackDamage(playerController.attackDamage);
                FindAnyObjectByType<HorizontalCardHolder>().DrawCardAndUpdate();
                IsoGrid2D.instance.ResetWaiting();
                return;
            }

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
            else if (playerController.isNextAttackFire)
            {
                if (currentState == GridState.Oil)
                {
                    playerController.attackDamage *= 2;
                    playerController.RecoverState();
                }
                playerController.Attack(this);

                playerController.isNextAttackFire = false;
            }
            else if (playerController.isNextAttackIce)
            {
                if (currentState == GridState.Water)
                {
                    playerController.attackDamage *= 2;
                    currentEnemy.Dizziness();
                }

                playerController.Attack(this);

                playerController.isNextAttackIce = false;
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
                stateGrid.enabled = false;
                stateGrid.color = normalColor;
                break;
            case GridState.Water:
                stateGrid.enabled = true;
                stateGrid.color = waterColor;
                break;
            case GridState.Oil:
                stateGrid.enabled = true;
                stateGrid.color = oilColor;
                break;
        }
    }


}
