using DG.Tweening;   // 引入 DOTween 命名空间
using System.Collections;
using Unity.VisualScripting;
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
    public Color explorecolor = new Color(0.9960785f, 0.9803922f, 0.9294118f, 0.5f);
    public bool isInRange = false;
    public bool canChangeState = false;


    public SpriteRenderer selectGrid;
    public bool isAttackTarget = false;
    public bool isOccupied = false;
    public bool canHeal = false;
    public UnitController occupiedPlayer;
    public ItemInGrid ocuupiedItem;
    public bool canDialogue;
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

    public bool isLeaveGrid = false;
    public Color LeaveColor = new Color(00f, 0.5f, 0f, 0.8f);

    

    private void Awake()
    {
        rend = GetComponent<SpriteRenderer>();
        originalColor = rend.color;
    }

    void Start()
    {
        sortingOrder = gridPos.x + gridPos.y;
        if (selectGrid != null)
            selectGrid.sortingOrder = -sortingOrder +1;
        if (stateGrid != null)
            stateGrid.sortingOrder = -501;
        
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
    public void SetColor(Color color)
    {
        if (isLeaveGrid) return;
        rend.color = color;
    }
    public void ResetColor() => rend.color = originalColor;
    void OnMouseDown()
    {
        if (FindAnyObjectByType<DialogueSystem>().isDialoguing == true) return;
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if(IsoGrid2D.instance.isLocate)
        {
            if(isAttackTarget)
            {
                if (IsoGrid2D.instance.column != null)
                {
                    // 生成 column 的预制体
                    GameObject newObj = Instantiate(IsoGrid2D.instance.column);

                    // 设置父物体为 column
                    newObj.transform.SetParent(this.transform);
                    newObj.GetComponent<ItemInGrid>().cornerA = gridPos;
                    // 重置本地位置、旋转和缩放
                    newObj.transform.localPosition = new Vector3(0,1.4f,0);
                    newObj.transform.localRotation = Quaternion.identity;
                    newObj.transform.localScale = Vector3.one;


                    IsoGrid2D.instance.isWaitingForGridClick = false;
                    IsoGrid2D.instance.isLocate = false;

                    ItemCardManager.instance.DecreaseCard(2);
                    if (ItemCardManager.instance.GetCardCount(2) <= 0)
                    {
                        CanvasGroup canvasGroup = IsoGrid2D.instance.locateCard.GetComponent<CanvasGroup>();
                        if (canvasGroup != null)
                        {
                            canvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
                            {
                                IsoGrid2D.instance.locateCard.gameObject.SetActive(false);
                                IsoGrid2D.instance.controller.GetComponent<UnitController>().Move();
                            });
                        }
                    }

                    
                    

                }
            }
        }
        Debug.Log($"点击格子: {gridPos}, 探索模式: {ExplorationManager.IsInExploration()}, canDialogue: {canDialogue}, ocuupiedItem: {ocuupiedItem != null}");

        // 优先处理探索模式下的物品交互
        if (ExplorationManager.IsInExploration() && canDialogue && ocuupiedItem != null)
        {
            Debug.Log($"探索模式下与物品交互: {ocuupiedItem.gameObject.name}, 类型: {ocuupiedItem.GetType()}");
            ocuupiedItem.Interact();
            IsoGrid2D.instance.ResetWaiting();
            return; // 交互后直接返回，不执行其他逻辑
        }

        // 战斗模式下的物品交互
        if (canDialogue && isInterable && ocuupiedItem != null)
        {
            Debug.Log($"战斗模式下与物品交互: {ocuupiedItem.gameObject.name}");
            ocuupiedItem.Interact();
            IsoGrid2D.instance.ResetWaiting();
            return; // 交互后直接返回，不执行其他逻辑
        }
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
                else if(playerController.isNextAttackDouble)
                {
                    UnitController playerToHeal = this.occupiedPlayer;
                    playerToHeal.Heal(playerController.healPoint*2);
                    playerController.RecoverState();
                    FindAnyObjectByType<HorizontalCardHolder>().ChangeAllCardToNormal();
                    FindAnyObjectByType<HorizontalCardHolder>().DrawCardAndUpdate();
                    IsoGrid2D.instance.ResetWaiting();
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
        

        NormalGridClick();
    }

    void NormalGridClick()
    {
        Debug.Log(2);
        
        if (occupiedPlayer != null)
        {
            Debug.Log(TurnManager.instance);
            TurnManager.instance.ChangePlayer(occupiedPlayer);
        }

        UnitController playerController = IsoGrid2D.instance.controller.GetComponent<UnitController>();

        if (isInRange && IsoGrid2D.instance.isLocate!=true)
        {
            playerController.MoveToGrid(this);
            IsoGrid2D.instance.ResetWaiting();
            return;
        }
        if(canDialogue && isInterable)
        {
            Debug.Log("对话");
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
            if(playerController.isNextAttackChange)
            {
                if(occupiedPlayer != null || currentEnemy!=null)
                {
                    if(occupiedPlayer != null)
                    {
                        IsoGrid2D.instance.SwapUnitPositions(playerController, occupiedPlayer);
                    }
                    else if(currentEnemy != null)
                    {
                        IsoGrid2D.instance.SwapUnitPositions(playerController, currentEnemy);
                    }
                    playerController.isNextAttackChange = false;
                    IsoGrid2D.instance.ClearHighlight();
                    FindAnyObjectByType<HorizontalCardHolder>().DrawCardAndUpdate();
                    IsoGrid2D.instance.ResetWaiting();
                }
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

                FindAnyObjectByType<HorizontalCardHolder>().DrawCardAndUpdate();
                IsoGrid2D.instance.ResetWaiting();
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
                OnFireAttackHit();
                playerController.Attack(this);

                FindAnyObjectByType<HorizontalCardHolder>().DrawCardAndUpdate();
                IsoGrid2D.instance.ResetWaiting();
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

                FindAnyObjectByType<HorizontalCardHolder>().DrawCardAndUpdate();
                IsoGrid2D.instance.ResetWaiting();
                playerController.isNextAttackIce = false;
            }
            else
            {
                if(currentEnemy!=null)
                {
                    playerController.Attack(this);

                    FindAnyObjectByType<HorizontalCardHolder>().DrawCardAndUpdate();
                    IsoGrid2D.instance.ResetWaiting();
                }


            }

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
    public void OnGridClicked()
    {
        // 如果在探索模式且这个格子可以对话
        if (ExplorationManager.IsInExploration() && canDialogue && ocuupiedItem != null)
        {
            ocuupiedItem.Interact();
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
        if (isLeaveGrid == true)
        {
            originalColor = normalColor;
            stateGrid.color = normalColor;
            return;
        }
        switch (currentState)
        {
            case GridState.None:
                stateGrid.enabled = false;
                stateGrid.color = normalColor;
                break;
            case GridState.Water:
                stateGrid.enabled = true;
                stateGrid.color = waterColor;
                AudioManager.Instance.PlaySFX("water");
                break;
            case GridState.Oil:
                stateGrid.enabled = true;
                stateGrid.color = oilColor;
                AudioManager.Instance.PlaySFX("oil");

                break;
        }
    }

    public void OnFireAttackHit()
    {
        GameObject vfxPrefab = null;
        float destroyTime = 3f;
        // 根据格子状态播放不同的VFX
        switch (currentState)
        {
            case GridState.Water:
                ClearStateAfterVFX();
                break;
            default:
                vfxPrefab = Resources.Load<GameObject>("fireVFX");
                StartCoroutine(ClearStateAfterVFX());
                break;
        }
        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, transform.position, Quaternion.identity);
            vfx.transform.SetParent(transform);

            Renderer vfxRenderer = vfx.GetComponent<Renderer>();
            if (vfxRenderer != null)
            {
                vfxRenderer.sortingOrder = -sortingOrder + 10;
            }

            Destroy(vfx, destroyTime);
        }
    }
        public void OnIceAttackHit()
    {
        GameObject vfxPrefab = null;
        float destroyTime = 3f;
        // 根据格子状态播放不同的VFX
        switch (currentState)
        {
            case GridState.Oil:
                ClearStateAfterVFX();
                break;
            default:
                vfxPrefab = Resources.Load<GameObject>("iceVFX");
                StartCoroutine(ClearStateAfterVFX());
                break;
        }
        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, transform.position, Quaternion.identity);
            vfx.transform.SetParent(transform);

            Renderer vfxRenderer = vfx.GetComponent<Renderer>();
            if (vfxRenderer != null)
            {
                vfxRenderer.sortingOrder = -sortingOrder + 10;
            }

            Destroy(vfx, destroyTime);
        }
    }

    

    private IEnumerator ClearStateAfterVFX()
    {
        // 等待VFX播放一段时间后再清除油状态
        yield return new WaitForSeconds(1.5f);

        // 清除油状态
        SetState(GridState.None);
        Debug.Log("油格状态已清除");
    }
}

