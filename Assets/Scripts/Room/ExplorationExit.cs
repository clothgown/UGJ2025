using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Exit : ItemInGrid
{
    [Header("探索出口")]
    public string exitName = "出口";
    public bool showConfirmationDialogue = true;

    void Start()
    {
        // 确保在战斗模式下也是可交互的
        isInterable = true;
    }

    public override void Interact()
    {
        if (!isInterable) return;

        // 检查当前模式
        if (ExplorationManager.IsInExploration())
        {
            // 探索模式下的退出逻辑
            HandleExplorationExit();
        }
        else
        {
            // 战斗模式下的退出逻辑
            HandleBattleExit();
        }
    }

    // 处理探索模式退出
    private void HandleExplorationExit()
    {
        if (showConfirmationDialogue && dialogueFile != null)
        {
            // 显示确认对话
            base.Interact();

            // 监听对话结束事件
            StartCoroutine(WaitForDialogueEnd());
        }
        else
        {
            // 直接结束探索
            EndExploration();
        }
    }

    // 处理战斗模式退出
    private void HandleBattleExit()
    {
        Debug.Log($"在战斗中发现 {exitName}，直接退出战斗");

        // 标记战斗失败
        if (TurnManager.instance != null)
        {
            TurnManager.instance.isWin = false;
        }

       
        if (PanelManager.instance != null)
        {
            PanelManager.instance.ShowEndPanel();
        }

        Debug.Log("战斗结束，返回地图");
        SceneManager.LoadScene("Map");
    
     }

    private IEnumerator WaitForDialogueEnd()
    {
        // 等待对话系统完成
        DialogueSystem dialogueSystem = FindObjectOfType<DialogueSystem>();
        if (dialogueSystem != null)
        {
            // 等待对话结束
            yield return new WaitUntil(() => !dialogueSystem.isDialoguing);

            // 对话结束后结束探索
            EndExploration();
        }
        else
        {
            // 如果没有对话系统，直接结束
            EndExploration();
        }
    }

    private void EndExploration()
    {
        Debug.Log($"通过 {exitName} 离开探索区域");

        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.EndExplorationAndReturn();
        }
        SceneManager.LoadScene("Map");
    }
}