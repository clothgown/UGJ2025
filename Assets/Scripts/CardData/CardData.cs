using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Card/CardData", order = 1)]
public class CardData : ScriptableObject
{
    public string cardName;
    public string description;
    public Sprite cardSprite;        // 卡牌图片
    public GameObject visualPrefab;  // 可选自定义预制体
    public int manaCost;             // 消耗魔法值
    
    public CardEffectType effectType;

    // 对应数值，根据 effectType 作用不同
    [Tooltip("攻击或治疗数值，根据 EffectType 作用")]
    public float amount = 0f;

    // 远程攻击专用参数
    [Header("Ranged Attack Settings")]
    public int attackRange = 0;   // 远程攻击范围（格子数）

    public AttackAttribute attackAttribute = AttackAttribute.None;
    public int SegmentCount = 0;
    public int PullDistance = 0;

    [Header("Grid State Change")]
    public GridState targetGridState = GridState.None;
    // Enum for simple effects
    public enum CardEffectType
    {
        MoveUnit,
        Attack,         // 近战攻击，使用 amount
        Heal,           // 治疗，使用 amount
        RemoteAttack,    // 远程攻击，使用 amount + attackRange
        Shield,
        Switch,
        Bloodsucking,
        StraightAttack,
        ChangeGridState,
        Double
    }

    // 攻击属性（可扩展）
    public enum AttackAttribute
    {
        None,
        Fire,
        Ice,
        Poison,
        Lightning,
        Dizziness,
        MultipleDamage,
        Pull,
    }



    // Optional: Delegate for custom effects
    public System.Action ExecuteCustomEffect;
}
