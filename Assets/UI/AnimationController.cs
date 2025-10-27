using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class AnimationController : MonoBehaviour
{
    [System.Serializable]
    public class AnimationItem
    {
        public string animationId; // 动画的唯一ID
        public DOTweenAnimation animation;
        public string description; // 动画描述（可选）
    }

    [System.Serializable]
    public class AnimationGroup
    {
        public string groupName;
        public AnimationItem[] animationItems; // 改为使用AnimationItem数组
        public bool playInSequence = false;
        public float sequenceDelay = 0.1f;
    }

    [Header("动画组设置")]
    public AnimationGroup personAnimations;
    public AnimationGroup backAnimations;

    [Header("所有动画项（按ID管理）")]
    public AnimationItem[] allAnimationItems; // 所有动画的全局列表

    [Header("按钮设置")]
    public Button personButton;
    public Button backButton;

    [Header("调试ID播放")]
    public string testAnimationId = ""; // 用于测试的动画ID

    private Dictionary<string, AnimationItem> animationDict = new Dictionary<string, AnimationItem>();
    private Dictionary<DOTweenAnimation, Tween> activeTweens = new Dictionary<DOTweenAnimation, Tween>();
    private bool isPlayingPersonAnim = false;
    private bool isPlayingBackAnim = false;

    void Start()
    {
        // 设置按钮点击事件
        if (personButton != null)
        {
            personButton.onClick.AddListener(OnPersonButtonClick);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClick);
        }

        // 初始化动画字典
        InitializeAnimationDictionary();

        // 初始化所有动画
        InitializeAllAnimations();
    }

    void InitializeAnimationDictionary()
    {
        animationDict.Clear();

        // 添加所有全局动画项
        if (allAnimationItems != null)
        {
            foreach (AnimationItem item in allAnimationItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.animationId) && item.animation != null)
                {
                    if (!animationDict.ContainsKey(item.animationId))
                    {
                        animationDict[item.animationId] = item;
                    }
                    else
                    {
                        Debug.LogWarning($"重复的动画ID: {item.animationId}");
                    }
                }
            }
        }

        // 添加人员动画组中的动画项
        if (personAnimations.animationItems != null)
        {
            foreach (AnimationItem item in personAnimations.animationItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.animationId) && item.animation != null)
                {
                    if (!animationDict.ContainsKey(item.animationId))
                    {
                        animationDict[item.animationId] = item;
                    }
                }
            }
        }

        // 添加返回动画组中的动画项
        if (backAnimations.animationItems != null)
        {
            foreach (AnimationItem item in backAnimations.animationItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.animationId) && item.animation != null)
                {
                    if (!animationDict.ContainsKey(item.animationId))
                    {
                        animationDict[item.animationId] = item;
                    }
                }
            }
        }

        Debug.Log($"动画字典初始化完成，共 {animationDict.Count} 个动画");
    }

    void InitializeAllAnimations()
    {
        // 初始化所有在字典中的动画
        foreach (var kvp in animationDict)
        {
            AnimationItem item = kvp.Value;
            if (item.animation != null)
            {
                item.animation.autoPlay = false;
                item.animation.autoKill = false;
                item.animation.CreateTween();
                item.animation.tween.Pause();
            }
        }
    }

    // 人员按钮点击事件
    public void OnPersonButtonClick()
    {
        if (isPlayingPersonAnim) return;

        StopAllAnimations();
        PlayAnimationGroup(personAnimations, true);
        Debug.Log("播放人员动画组");
    }

    // 返回按钮点击事件
    public void OnBackButtonClick()
    {
        if (isPlayingBackAnim) return;

        StopAllAnimations();
        PlayAnimationGroup(backAnimations, false);
        Debug.Log("播放返回动画组");
    }

    // 根据ID播放单个动画
    public void PlayAnimationById(string animationId)
    {
        if (string.IsNullOrEmpty(animationId))
        {
            Debug.LogWarning("动画ID为空！");
            return;
        }

        if (animationDict.ContainsKey(animationId))
        {
            AnimationItem item = animationDict[animationId];
            if (item.animation != null)
            {
                PlaySingleAnimation(item.animation);
                Debug.Log($"播放动画: {animationId} - {item.description}");
            }
            else
            {
                Debug.LogError($"动画ID '{animationId}' 对应的动画组件为空！");
            }
        }
        else
        {
            Debug.LogError($"未找到动画ID: {animationId}");
            Debug.Log($"可用的动画ID: {string.Join(", ", animationDict.Keys.ToArray())}");
        }
    }

    // 根据ID播放多个动画
    public void PlayAnimationsByIds(string[] animationIds, bool playInSequence = false, float sequenceDelay = 0.1f)
    {
        if (animationIds == null || animationIds.Length == 0)
        {
            Debug.LogWarning("动画ID数组为空！");
            return;
        }

        if (playInSequence)
        {
            StartCoroutine(PlayAnimationsByIdsInSequence(animationIds, sequenceDelay));
        }
        else
        {
            foreach (string id in animationIds)
            {
                PlayAnimationById(id);
            }
        }
    }

    System.Collections.IEnumerator PlayAnimationsByIdsInSequence(string[] animationIds, float delay)
    {
        foreach (string id in animationIds)
        {
            PlayAnimationById(id);
            yield return new WaitForSeconds(delay);
        }
    }

    // 播放动画组
    void PlayAnimationGroup(AnimationGroup animationGroup, bool isPersonGroup)
    {
        if (animationGroup.animationItems == null || animationGroup.animationItems.Length == 0)
        {
            Debug.LogWarning($"动画组 '{animationGroup.groupName}' 为空！");
            return;
        }

        // 设置播放状态
        if (isPersonGroup)
        {
            isPlayingPersonAnim = true;
            isPlayingBackAnim = false;
        }
        else
        {
            isPlayingPersonAnim = false;
            isPlayingBackAnim = true;
        }

        if (animationGroup.playInSequence)
        {
            StartCoroutine(PlayAnimationItemsInSequence(animationGroup));
        }
        else
        {
            foreach (AnimationItem item in animationGroup.animationItems)
            {
                if (item != null && item.animation != null)
                {
                    PlaySingleAnimation(item.animation);
                }
            }
        }
    }

    System.Collections.IEnumerator PlayAnimationItemsInSequence(AnimationGroup animationGroup)
    {
        foreach (AnimationItem item in animationGroup.animationItems)
        {
            if (item != null && item.animation != null)
            {
                PlaySingleAnimation(item.animation);
                yield return new WaitForSeconds(animationGroup.sequenceDelay);
            }
        }
    }

    void PlaySingleAnimation(DOTweenAnimation anim)
    {
        if (anim == null) return;

        // 停止这个动画之前可能正在运行的实例
        if (activeTweens.ContainsKey(anim) && activeTweens[anim] != null && activeTweens[anim].IsActive())
        {
            activeTweens[anim].Kill();
        }

        // 重新创建并播放动画
        anim.CreateTween();
        anim.tween.OnComplete(() => OnAnimationComplete(anim));
        anim.tween.OnKill(() => OnAnimationKilled(anim));
        anim.tween.Restart();

        activeTweens[anim] = anim.tween;
    }

    void OnAnimationComplete(DOTweenAnimation anim)
    {
        Debug.Log($"动画 {anim.gameObject.name} 播放完成");
        CheckAllAnimationsComplete();
    }

    void OnAnimationKilled(DOTweenAnimation anim)
    {
        if (activeTweens.ContainsKey(anim))
        {
            activeTweens.Remove(anim);
        }
    }

    void CheckAllAnimationsComplete()
    {
        bool allComplete = true;
        foreach (var kvp in activeTweens)
        {
            if (kvp.Value != null && kvp.Value.IsActive() && !kvp.Value.IsComplete())
            {
                allComplete = false;
                break;
            }
        }

        if (allComplete)
        {
            isPlayingPersonAnim = false;
            isPlayingBackAnim = false;
            Debug.Log("所有动画播放完成");
        }
    }

    // 停止所有正在播放的动画
    public void StopAllAnimations()
    {
        foreach (var kvp in activeTweens)
        {
            if (kvp.Value != null && kvp.Value.IsActive())
            {
                kvp.Value.Kill();
            }
        }
        activeTweens.Clear();

        isPlayingPersonAnim = false;
        isPlayingBackAnim = false;
    }

    // 停止指定ID的动画
    public void StopAnimationById(string animationId)
    {
        if (animationDict.ContainsKey(animationId))
        {
            AnimationItem item = animationDict[animationId];
            if (item.animation != null && activeTweens.ContainsKey(item.animation))
            {
                activeTweens[item.animation].Kill();
                activeTweens.Remove(item.animation);
            }
        }
    }

    // 强制重置所有动画到初始状态
    public void ResetAllAnimations()
    {
        StopAllAnimations();

        ResetAnimationGroup(personAnimations);
        ResetAnimationGroup(backAnimations);

        // 重置全局动画列表
        if (allAnimationItems != null)
        {
            foreach (AnimationItem item in allAnimationItems)
            {
                if (item.animation != null)
                {
                    item.animation.DORewind();
                }
            }
        }
    }

    void ResetAnimationGroup(AnimationGroup animationGroup)
    {
        if (animationGroup.animationItems == null) return;

        foreach (AnimationItem item in animationGroup.animationItems)
        {
            if (item.animation != null)
            {
                item.animation.DORewind();
            }
        }
    }

    void OnDestroy()
    {
        StopAllAnimations();
    }

    // 编辑器调试方法
    [ContextMenu("测试人员动画")]
    public void TestPersonAnimations()
    {
        OnPersonButtonClick();
    }

    [ContextMenu("测试返回动画")]
    public void TestBackAnimations()
    {
        OnBackButtonClick();
    }

    [ContextMenu("测试当前ID动画")]
    public void TestCurrentIdAnimation()
    {
        if (!string.IsNullOrEmpty(testAnimationId))
        {
            PlayAnimationById(testAnimationId);
        }
        else
        {
            Debug.LogWarning("测试动画ID为空！");
        }
    }

    [ContextMenu("停止所有动画")]
    public void DebugStopAllAnimations()
    {
        StopAllAnimations();
    }

    [ContextMenu("打印所有动画ID")]
    public void PrintAllAnimationIds()
    {
        Debug.Log("所有动画ID:");
        foreach (string id in animationDict.Keys)
        {
            Debug.Log($" - {id}");
        }
    }
}